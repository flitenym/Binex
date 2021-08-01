using Microsoft.Extensions.Hosting;
using System;
using System.Configuration;

namespace BinexWorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddCronJob<BinanceSell>(c =>
                    {
                        c.TimeZoneInfo = TimeZoneInfo.Utc;
                        c.CronExpression = ConfigurationManager.AppSettings["Cron"];
                    });
                });
    }
}