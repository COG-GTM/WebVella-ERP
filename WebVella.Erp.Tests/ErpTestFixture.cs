using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WebVella.Erp;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models.AutoMapper;
using WebVella.Erp.Database;
using WebVella.Erp.Hooks;
using WebVella.Erp.Web.Models.AutoMapper;
using Xunit;

namespace WebVella.Erp.Tests
{
	/// <summary>
	/// Boots a complete ERP engine against a disposable PostgreSQL database and
	/// runs the metadata patches for every module plugin (both the new Wave 1
	/// modules and the existing Project/Crm/Mail/TravelERP plugins). Shared by all
	/// tests via the "erp" collection so the expensive bootstrap happens once.
	///
	/// Connection string resolution (first non-empty wins):
	///   1. env TEST_ERP_CONNECTION_STRING  (full Npgsql connection string)
	///   2. built from env PGHOST/PGPORT/PGUSER/PGPASSWORD (defaults localhost:5432/postgres/postgres)
	/// The target database (env TEST_ERP_DATABASE, default "erp_test") is dropped
	/// and recreated on construction.
	/// </summary>
	public class ErpTestFixture : IDisposable
	{
		public string ConnectionString { get; }
		public string DatabaseName { get; }
		private readonly string _adminConnectionString;

		public ErpTestFixture()
		{
			// Every Site host sets this Npgsql switch in Startup; the ERP data layer
			// writes UTC DateTimes to 'timestamp without time zone' columns.
			AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

			var customCulture = new CultureInfo("en-US");
			customCulture.NumberFormat.NumberDecimalSeparator = ".";
			CultureInfo.DefaultThreadCurrentCulture = customCulture;
			CultureInfo.DefaultThreadCurrentUICulture = customCulture;

			var host = Env("PGHOST", "localhost");
			var port = Env("PGPORT", "5432");
			var user = Env("PGUSER", "postgres");
			var password = Env("PGPASSWORD", "postgres");
			DatabaseName = Env("TEST_ERP_DATABASE", "erp_test");

			var explicitConn = Environment.GetEnvironmentVariable("TEST_ERP_CONNECTION_STRING");
			if (!string.IsNullOrWhiteSpace(explicitConn))
			{
				ConnectionString = explicitConn;
				var csb = new NpgsqlConnectionStringBuilder(explicitConn);
				DatabaseName = csb.Database;
				csb.Database = "postgres";
				_adminConnectionString = csb.ConnectionString;
			}
			else
			{
				_adminConnectionString = $"Server={host};Port={port};User Id={user};Password={password};Database=postgres;Pooling=false;";
				ConnectionString = $"Server={host};Port={port};User Id={user};Password={password};Database={DatabaseName};Pooling=true;MinPoolSize=1;MaxPoolSize=20;CommandTimeout=120;";
			}

			RecreateDatabase();
			BootErp();
		}

		private static string Env(string key, string fallback)
		{
			var v = Environment.GetEnvironmentVariable(key);
			return string.IsNullOrWhiteSpace(v) ? fallback : v;
		}

		private void RecreateDatabase()
		{
			using (var conn = new NpgsqlConnection(_adminConnectionString))
			{
				conn.Open();
				Exec(conn, $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{DatabaseName}' AND pid <> pg_backend_pid();");
				Exec(conn, $"DROP DATABASE IF EXISTS \"{DatabaseName}\";");
				Exec(conn, $"CREATE DATABASE \"{DatabaseName}\";");
			}
		}

		private static void Exec(NpgsqlConnection conn, string sql)
		{
			using (var cmd = new NpgsqlCommand(sql, conn))
				cmd.ExecuteNonQuery();
		}

		private void BootErp()
		{
			var settings = new Dictionary<string, string>
			{
				["Settings:ConnectionString"] = ConnectionString,
				["Settings:EncryptionKey"] = "BC93B776A42877CFEE808823BA8B37C83B6B0AD23198AC3AF2B5A54DCB647658",
				["Settings:Lang"] = "en",
				["Settings:Locale"] = "en-US",
				["Settings:TimeZoneName"] = "UTC",
				["Settings:DevelopmentMode"] = "true",
				["Settings:EnableBackgroungJobs"] = "false",
				["Settings:EnableFileSystemStorage"] = "false",
				["Settings:EmailEnabled"] = "false",
				["Settings:Jwt:Key"] = "ThisIsMySecretKeyThisIsMySecretKeyThisIsMySecretKey",
				["Settings:Jwt:Issuer"] = "webvella-erp",
				["Settings:Jwt:Audience"] = "webvella-erp"
			};
			var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
			ErpSettings.Initialize(configuration);

			using (SecurityContext.OpenSystemScope())
			{
				DbContext.CreateContext(ErpSettings.ConnectionString);
				try
				{
					var service = new ErpService();

					// plugins are initialized in registration order; base plugins first
					// (mirrors every Site host: NextPlugin -> SdkPlugin -> module plugins)
					service.Plugins.Add(new WebVella.Erp.Plugins.Next.NextPlugin());
					service.Plugins.Add(new WebVella.Erp.Plugins.SDK.SdkPlugin());
					service.Plugins.Add(new WebVella.Erp.Plugins.Project.ProjectPlugin());
					service.Plugins.Add(new WebVella.Erp.Plugins.Crm.CrmPlugin());
					service.Plugins.Add(new WebVella.Erp.Plugins.Mail.MailPlugin());
					service.Plugins.Add(new WebVella.Erp.Plugins.TravelERP.TravelErpPlugin());
					service.Plugins.Add(new WebVella.Erp.Plugins.Finance.FinancePlugin());
					service.Plugins.Add(new WebVella.Erp.Plugins.Workflow.WorkflowPlugin());
					service.Plugins.Add(new WebVella.Erp.Plugins.Procurement.ProcurementPlugin());

					var cfg = ErpAutoMapperConfiguration.MappingExpressions;
					ErpAutoMapperConfiguration.Configure(cfg);
					ErpWebAutoMapperConfiguration.Configure(cfg);
					service.SetAutoMapperConfiguration();
					ErpAutoMapper.Initialize(cfg);

					service.InitializeSystemEntities();

					// initializes JobManager.Current / ScheduleManager.Current, which some
					// plugins (SDK, Mail) dereference in SetSchedulePlans during Initialize.
					// Background job processing stays disabled via EnableBackgroungJobs=false.
					service.InitializeBackgroundJobs(null);

					// ErpAppContext.Current must be set before plugin patches run because
					// AppService.CreateArea/ClearAppCache dereference it. Init is internal to
					// WebVella.Erp.Web, so invoke it reflectively with a minimal provider
					// (mirrors ErpMvcExtensions.UseErp -> ErpAppContext.Init(app.ApplicationServices)).
					var provider = new ServiceCollection().BuildServiceProvider();
					var appContextType = typeof(WebVella.Erp.Web.ErpAppContext);
					var initMethod = appContextType.GetMethod("Init", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
					initMethod.Invoke(null, new object[] { provider });

					// runs each plugin's Initialize -> ProcessPatches, then registers jobs + hooks
					service.InitializePlugins(provider);
				}
				finally
				{
					DbContext.CloseContext();
				}
			}
		}

		/// <summary>
		/// Runs <paramref name="action"/> inside an open DbContext + system security
		/// scope, mirroring how the web host executes record operations.
		/// </summary>
		public void Run(Action action)
		{
			using (SecurityContext.OpenSystemScope())
			using (var ctx = DbContext.CreateContext(ErpSettings.ConnectionString))
			{
				action();
			}
		}

		public T Run<T>(Func<T> func)
		{
			using (SecurityContext.OpenSystemScope())
			using (var ctx = DbContext.CreateContext(ErpSettings.ConnectionString))
			{
				return func();
			}
		}

		public void Dispose()
		{
			try { DbContext.CloseContext(); } catch { /* ignore */ }
		}
	}

	[CollectionDefinition("erp")]
	public class ErpCollection : ICollectionFixture<ErpTestFixture>
	{
	}
}
