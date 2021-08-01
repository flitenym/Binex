using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedLibrary.Helper;
using SharedLibrary.Helper.StaticInfo;
using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace BinexWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var x = await HelperMethods.GetByKeyInDBAsync(InfoKeys.ApiSecretBinanceKey);
            }
        }
    }
}
