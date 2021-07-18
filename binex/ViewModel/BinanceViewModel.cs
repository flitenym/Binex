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

namespace binex.ViewModel
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

        #endregion

        #region Команда для сохранения данных

        private AsyncCommand loadCommand;

        public AsyncCommand SaveCommand => loadCommand ?? (loadCommand = new AsyncCommand(x => Save()));

        private async Task Save()
        {
            await HelperMethods.UpdateByKeyInDB(InfoKeys.ApiKeyBinanceKey, apiKey);
            await HelperMethods.UpdateByKeyInDB(InfoKeys.ApiSecretBinanceKey, apiSecret);

            SharedProvider.SetToSingleton(
                    InfoKeys.ApiKeyBinanceKey,
                    apiKey);

            SharedProvider.SetToSingleton(
                    InfoKeys.ApiSecretBinanceKey,
                    apiSecret);
        }

        #endregion

    }
}
