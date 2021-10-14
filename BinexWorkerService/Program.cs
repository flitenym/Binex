using Binex.FileInfo;
using Microsoft.Extensions.Hosting;
using SharedLibrary.Commands;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace BinexWorkerService
{
    public class Program
    {
        public static SettingsFileInfo Settings { get; set; }

        public static void Main(string[] args)
        {
            GetSettingsCommand.Execute(null);
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
                        c.CronExpression = Settings.CronExpression;
                    });
                });

        private static AsyncCommand getSettingsCommand;

        public static AsyncCommand GetSettingsCommand => getSettingsCommand ?? (getSettingsCommand = new AsyncCommand(x => GetSettings()));

        public static async Task GetSettings()
        {
            Settings = await FileOperations.GetFileInfo();
        }
    }
}