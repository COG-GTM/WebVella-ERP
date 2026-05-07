using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace WebVella.Erp.Site.TIMS
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseUrls("http://127.0.0.1:5001");
					webBuilder.UseStartup<Startup>();
				});
	}
}
