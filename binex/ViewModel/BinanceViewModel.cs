using Binance.Net;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.WalletData;
using Binance.Net.SubClients;
using CryptoExchange.Net.Authentication;
using SharedLibrary.Commands;
using SharedLibrary.Helper;
using SharedLibrary.Helper.StaticInfo;
using SharedLibrary.Provider;
using System.ComponentModel;
using System.Reflection;
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

            EmailCommand.Execute(null);
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

        #endregion

        #region Команда для получение Email данных

        private AsyncCommand emailCommand;

        public AsyncCommand EmailCommand => emailCommand ?? (emailCommand = new AsyncCommand(x => SetEmailsInfoFromDB()));

        private async Task SetEmailsInfoFromDB()
        {
            Emails = (await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailsKey))?.Value;
            EmailLogin = (await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailLoginKey))?.Value;
            EmailPassword = (await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailPasswordKey))?.Value;
        }

        #endregion

        #region Команда для сохранения данных

        private AsyncCommand saveCommand;

        public AsyncCommand SaveCommand => saveCommand ?? (saveCommand = new AsyncCommand(x => SaveAsync()));

        private async Task SaveAsync()
        {
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.ApiKeyBinanceKey, apiKey);
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.ApiSecretBinanceKey, apiSecret);
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.BinancePercentKey, binancePercent);
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.BinanceFuturesPercentKey, binanceFuturesPercent);
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.EmailsKey, emails);
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.EmailLoginKey, emailLogin);
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.EmailPasswordKey, emailPassword);

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

    }
}