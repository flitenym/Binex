using Cronos;
using SharedLibrary.Commands;
using SharedLibrary.Helper;
using SharedLibrary.Helper.Classes;
using SharedLibrary.Helper.StaticInfo;
using SharedLibrary.Provider;
using SharedLibrary.View;
using SharedLibrary.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Binex.ViewModel
{
    public class BinanceViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public BinanceViewModel()
        {
            if (SharedProvider.GetFromDictionaryByKey(InfoKeys.ApiKeyBinanceKey) is string apiKeyValue)
            {
                ApiKey = apiKeyValue;
            }

            if (SharedProvider.GetFromDictionaryByKey(InfoKeys.ApiSecretBinanceKey) is string apiSecretValue)
            {
                ApiSecret = apiSecretValue;
            }

            if (SharedProvider.GetFromDictionaryByKey(InfoKeys.BinancePercentKey) is string binancePercentValue)
            {
                BinancePercent = binancePercentValue;
            }

            if (SharedProvider.GetFromDictionaryByKey(InfoKeys.BinanceFuturesPercentKey) is string binanceFuturesPercentValue)
            {
                BinanceFuturesPercent = binanceFuturesPercentValue;
            }

            SetInfoCommand.Execute(null);
        }

        #region Fields

        #region ApiKey

        private string apiKey = string.Empty;
        public string ApiKey
        {
            get { return apiKey; }
            set
            {
                apiKey = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ApiKey)));
            }
        }

        #endregion

        #region ApiSecret

        private string apiSecret = string.Empty;
        public string ApiSecret
        {
            get { return apiSecret; }
            set
            {
                apiSecret = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ApiSecret)));
            }
        }

        #endregion

        #region BinancePercent

        private string binancePercent = string.Empty;
        public string BinancePercent
        {
            get { return binancePercent; }
            set
            {
                binancePercent = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(BinancePercent)));
            }
        }

        #endregion

        #region BinanceFuturesPercent

        private string binanceFuturesPercent = string.Empty;
        public string BinanceFuturesPercent
        {
            get { return binanceFuturesPercent; }
            set
            {
                binanceFuturesPercent = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(BinanceFuturesPercent)));
            }
        }

        #endregion

        #region Emails

        private string emails = string.Empty;
        public string Emails
        {
            get { return emails; }
            set
            {
                emails = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Emails)));
            }
        }

        #endregion

        #region EmailLogin

        private string emailLogin = string.Empty;
        public string EmailLogin
        {
            get { return emailLogin; }
            set
            {
                emailLogin = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(EmailLogin)));
            }
        }

        #endregion

        #region EmailPassword

        private string emailPassword = string.Empty;
        public string EmailPassword
        {
            get { return emailPassword; }
            set
            {
                emailPassword = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(EmailPassword)));
            }
        }

        #endregion

        #region Cron

        private string cron = string.Empty;
        public string Cron
        {
            get { return cron; }
            set
            {
                cron = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Cron)));
            }
        }

        #endregion

        #region BinexServiceName

        private string binexServiceName = string.Empty;
        public string BinexServiceName
        {
            get { return binexServiceName; }
            set
            {
                binexServiceName = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(BinexServiceName)));
            }
        }

        #endregion

        #region IsCurrenciesSell

        private bool isCurrenciesSell = true;
        public bool IsCurrenciesSell
        {
            get { return isCurrenciesSell; }
            set
            {
                isCurrenciesSell = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsCurrenciesSell)));
            }
        }

        #endregion

        #region IsTransferFromFuturesToSpot

        private bool isTransferFromFuturesToSpot = true;
        public bool IsTransferFromFuturesToSpot
        {
            get { return isTransferFromFuturesToSpot; }
            set
            {
                isTransferFromFuturesToSpot = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsTransferFromFuturesToSpot)));
            }
        }

        #endregion

        #region IsDustSell

        private bool isDustSell = true;
        public bool IsDustSell
        {
            get { return isDustSell; }
            set
            {
                isDustSell = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsDustSell)));
            }
        }

        #endregion

        #endregion

        #region Команда для получение Email данных

        private AsyncCommand setInfoCommand;

        public AsyncCommand SetInfoCommand => setInfoCommand ?? (setInfoCommand = new AsyncCommand(x => SetInfoFromDB()));

        private async Task SetInfoFromDB()
        {
            Emails = (await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailsKey))?.Value;
            EmailLogin = (await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailLoginKey))?.Value;
            EmailPassword = (await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailPasswordKey))?.Value;
            Cron = (await HelperMethods.GetByKeyInDBAsync(InfoKeys.CronKey))?.Value;
            BinexServiceName = (await HelperMethods.GetByKeyInDBAsync(InfoKeys.BinexServiceNameKey))?.Value;
            IsCurrenciesSell = (await HelperMethods.GetByKeyInDBAsync(InfoKeys.IsCurrenciesSellKey))?.Value == bool.TrueString;
            IsDustSell = (await HelperMethods.GetByKeyInDBAsync(InfoKeys.IsDustSellKey))?.Value == bool.TrueString;
            IsTransferFromFuturesToSpot = (await HelperMethods.GetByKeyInDBAsync(InfoKeys.IsTransferFromFuturesToSpotKey))?.Value == bool.TrueString;
        }

        #endregion

        #region Команда для сохранения данных

        private AsyncCommand saveCommand;

        public AsyncCommand SaveCommand => saveCommand ?? (saveCommand = new AsyncCommand(x => SaveAsync()));

        public async Task<bool> RestartService(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            if (service?.Status == null)
            {
                await HelperMethods.Message($"Сервис не найден {serviceName}");
                return false;
            }

            try
            {
                int millisec1 = Environment.TickCount;
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
                if (service.Status != ServiceControllerStatus.Stopped)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                }

                // count the rest of the timeout
                int millisec2 = Environment.TickCount;
                timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds - (millisec2 - millisec1));

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);

                await HelperMethods.Message($"Сервис перезапущен {serviceName}");

                return true;
            }
            catch (Exception ex)
            {
                await HelperMethods.Message($"Не удалось перезапустить сервис: {ex.Message}");
                return false;
            }
        }

        private async Task SaveAsync()
        {
            CronExpression cronExpression = await GetCronExpression();

            if (cronExpression == null)
            {
                await HelperMethods.Message("Данные не сохранены");
                return;
            }

            var cronDbValue = await HelperMethods.GetByKeyInDBAsync(InfoKeys.CronKey);
            var binexServiceNameDbValue = await HelperMethods.GetByKeyInDBAsync(InfoKeys.BinexServiceNameKey);

            if (cronDbValue?.Value != cron || binexServiceNameDbValue?.Value != binexServiceName)
            {
                await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.CronKey, cron);
                await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.BinexServiceNameKey, binexServiceName);
                await RestartService(BinexServiceName, 2000);
            }

            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.ApiKeyBinanceKey, apiKey);
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.ApiSecretBinanceKey, apiSecret);
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.BinancePercentKey, binancePercent);
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.BinanceFuturesPercentKey, binanceFuturesPercent);
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.EmailsKey, emails);
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.EmailLoginKey, emailLogin);
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.EmailPasswordKey, emailPassword);
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.IsCurrenciesSellKey, isCurrenciesSell.ToString());
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.IsDustSellKey, isDustSell.ToString());
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.IsTransferFromFuturesToSpotKey, IsTransferFromFuturesToSpot.ToString());

            SharedProvider.SetToSingleton(
                    InfoKeys.ApiKeyBinanceKey,
                    apiKey);

            SharedProvider.SetToSingleton(
                    InfoKeys.ApiSecretBinanceKey,
                    apiSecret);

            SharedProvider.SetToSingleton(
                    InfoKeys.BinancePercentKey,
                    binancePercent);

            SharedProvider.SetToSingleton(
                    InfoKeys.BinanceFuturesPercentKey,
                    binanceFuturesPercent);

            await HelperMethods.Message("Данные сохранены");
        }

        #endregion

        #region Команда для сохранения данных

        private AsyncCommand checkNextDateCommand;

        public AsyncCommand CheckNextDateCommand => checkNextDateCommand ?? (checkNextDateCommand = new AsyncCommand(x => CheckNextDateAsync()));

        private async Task<CronExpression> GetCronExpression()
        {
            CronExpression cronExpression;

            if (string.IsNullOrEmpty(Cron))
            {
                await HelperMethods.Message("Данные Cron не заданы");
                return null;
            }
            try
            {
                cronExpression = CronExpression.Parse(Cron);
                return cronExpression;
            }
            catch (Exception ex)
            {
                await HelperMethods.Message($"Неверный формат Cron: {ex.Message}");
                return null;
            }
        }

        private async Task CheckNextDateAsync()
        {
            CronExpression cronExpression = await GetCronExpression();

            if (cronExpression == null) { return; }

            var occurrences = cronExpression.GetOccurrences(DateTime.UtcNow, DateTime.UtcNow.AddMonths(2)).Take(20).ToList();

            List<TableItemView> tableItemsView = new List<TableItemView>();

            for (int i = 0; i < occurrences.Count(); i++)
            {
                tableItemsView.Add(new TableItemView() { ID = i + 1, Value = occurrences[i].ToString() });
            }

            var dataTable = HelperMethods.ToDataTable(tableItemsView);

            var showDataTable = new ShowDataTableView();
            var vm = new ShowDataTableViewModel(dataTable);
            showDataTable.DataContext = vm;
            showDataTable.ShowDialog();
        }

        #endregion
    }
}