using SharedLibrary.FileInfo;
using Microsoft.Extensions.Hosting;
using System;
using System.Configuration;
using SharedLibrary.Helper;
using NLog;

namespace BinexWorkerService
{
    public class Program
    {
        private static Logger _logger;
        public static string CronExpression { get; set; }

        public static void Main(string[] args)
        {
            _logger = LogManager.GetLogger(nameof(BinanceSell));
            GetSettings();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    try
                    {
                        services.AddCronJob<BinanceSell>(c =>
                        {
                            c.TimeZoneInfo = TimeZoneInfo.Utc;
                            c.CronExpression = CronExpression ?? ConfigurationManager.AppSettings["Cron"];
                        }, _logger);
                    }
                    catch (Exception ex)
                    {
                        _logger.Trace($"Не установить время хроноса сервис: {ex.Message}");
                    }
                });

        public static void GetSettings()
        {
            (SettingsFileInfo settings, string message) = FileOperations.GetFileInfo();
            
            if (!string.IsNullOrEmpty(message))
            {
                _logger.Trace(message);
            }

            CronExpression = settings?.CronExpression;
        }
    }
}