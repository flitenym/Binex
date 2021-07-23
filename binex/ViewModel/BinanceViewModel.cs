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

            if (SharedProvider.GetFromDictionaryByKey(InfoKeys.ApiAddressBinanceKey) is string apiAddressValue)
            {
                ApiAddress = apiAddressValue;
            }

            if (SharedProvider.GetFromDictionaryByKey(InfoKeys.BinancePercentKey) is string binancePercentValue)
            {
                BinancePercent = binancePercentValue;
            }
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

        #region ApiAddress

        private string apiAddress = string.Empty;
        public string ApiAddress
        {
            get { return apiAddress; }
            set
            {
                apiAddress = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ApiAddress)));
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

        #endregion

        #region Команда для сохранения данных

        private AsyncCommand loadCommand;

        public AsyncCommand SaveCommand => loadCommand ?? (loadCommand = new AsyncCommand(x => SaveAsync()));

        private async Task SaveAsync()
        {
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.ApiKeyBinanceKey, apiKey);
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.ApiSecretBinanceKey, apiSecret);
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.ApiAddressBinanceKey, apiAddress);
            await HelperMethods.UpdateByKeyInDBAsync(InfoKeys.BinancePercentKey, binancePercent);

            SharedProvider.SetToSingleton(
                    InfoKeys.ApiKeyBinanceKey,
                    apiKey);

            SharedProvider.SetToSingleton(
                    InfoKeys.ApiSecretBinanceKey,
                    apiSecret);

            SharedProvider.SetToSingleton(
                    InfoKeys.ApiAddressBinanceKey,
                    apiAddress);

            SharedProvider.SetToSingleton(
                    InfoKeys.BinancePercentKey,
                    binancePercent);

            await HelperMethods.Message("Данные сохранены");
        }

        #endregion

    }
}
