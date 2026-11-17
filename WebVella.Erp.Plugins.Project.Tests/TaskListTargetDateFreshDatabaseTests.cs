using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Npgsql;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Tests
{
	//needs PostgreSQL: WEBVELLA_TEST_PG, or postgres/postgres on localhost:5433
	public class TaskListTargetDateFreshDatabaseTests : IDisposable
	{
		private static readonly string ServerConnectionString =
			Environment.GetEnvironmentVariable("WEBVELLA_TEST_PG") ?? "Host=localhost;Port=5433;Username=postgres;Password=postgres;Database=postgres";

		private static readonly Guid NoOwnerCell = new Guid("8d244aa4-ad6b-464f-87e9-37a55fd18d19");

		private readonly string databaseName = "wv_target_date_" + Guid.NewGuid().ToString("N");
		private readonly string databaseConnectionString;

		public TaskListTargetDateFreshDatabaseTests()
		{
			Execute(ServerConnectionString, $"CREATE DATABASE {databaseName}");
			databaseConnectionString = new NpgsqlConnectionStringBuilder(ServerConnectionString) { Database = databaseName, Pooling = false }.ToString();
			Execute(databaseConnectionString, @"CREATE TABLE public.app_page_body_node (
				id uuid NOT NULL PRIMARY KEY, parent_id uuid, node_id uuid, page_id uuid NOT NULL,
				weight integer DEFAULT -1 NOT NULL, component_name text, options text, container_id text)");
		}

		[Fact]
		public void Update_FindsCellsCreatedEarlierInTheSameStartupTransaction()
		{
			using var connection = new NpgsqlConnection(databaseConnectionString);
			connection.Open();
			using var transaction = connection.BeginTransaction();

			//on a fresh install the cells are created by earlier patches in the same, still uncommitted, transaction
			foreach (var id in ProjectPlugin.TaskListTargetDateNodeIds)
				Execute(transaction, $"INSERT INTO public.app_page_body_node (id, page_id, component_name, options) VALUES ('{id}', '{Guid.NewGuid()}', 'WebVella.Erp.Web.Components.PcFieldDate', '{{\"value\":\"{{\\\"string\\\":\\\"RowRecord.created_on\\\"}}\",\"name\":\"created_on\"}}')");

			Assert.Equal(4, ProjectPlugin.UpdateTaskListTargetDateColumns(transaction));

			foreach (var id in ProjectPlugin.TaskListTargetDateNodeIds)
			{
				using var command = new NpgsqlCommand($"SELECT options FROM public.app_page_body_node WHERE id = '{id}'", connection, transaction);
				var options = JObject.Parse((string)command.ExecuteScalar());
				Assert.Equal("RowRecord.end_time", (string)JObject.Parse((string)options["value"])["string"]);
			}
		}

		[Fact]
		public void Update_SkipsMissingCellsWithoutFailing()
		{
			using var connection = new NpgsqlConnection(databaseConnectionString);
			connection.Open();
			using var transaction = connection.BeginTransaction();

			//e.g. an admin deleted the no-owner cell
			foreach (var id in ProjectPlugin.TaskListTargetDateNodeIds.Where(x => x != NoOwnerCell))
				Execute(transaction, $"INSERT INTO public.app_page_body_node (id, page_id) VALUES ('{id}', '{Guid.NewGuid()}')");

			Assert.Equal(3, ProjectPlugin.UpdateTaskListTargetDateColumns(transaction));
		}

		public void Dispose()
		{
			Execute(ServerConnectionString, $"DROP DATABASE IF EXISTS {databaseName} WITH (FORCE)");
		}

		private static void Execute(string connectionString, string sql)
		{
			using var connection = new NpgsqlConnection(connectionString);
			connection.Open();
			using var command = new NpgsqlCommand(sql, connection);
			command.ExecuteNonQuery();
		}

		private static void Execute(NpgsqlTransaction transaction, string sql)
		{
			using var command = new NpgsqlCommand(sql, transaction.Connection, transaction);
			command.ExecuteNonQuery();
		}
	}
}
