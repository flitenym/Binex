using Binance.Net;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.SpotData;
using Binance.Net.Objects.Spot.WalletData;
using CryptoExchange.Net.Authentication;
using SharedLibrary.Helper;
using SharedLibrary.Helper.StaticInfo;
using SharedLibrary.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Binex.Api
{
    public static class BinanceApi
    {
        public const HttpStatusCode SuccessCode = HttpStatusCode.OK;
        public static async Task<(bool IsSuccess, string ApiKey, string ApiSecret)> GetApiDataAsync()
        {
            if (SharedProvider.GetFromDictionaryByKey(InfoKeys.ApiKeyBinanceKey) is string apiKeyValue &&
                SharedProvider.GetFromDictionaryByKey(InfoKeys.ApiSecretBinanceKey) is string apiSecretValue)
            {
                return (true, apiKeyValue, apiSecretValue);
            }

            await HelperMethods.Message("Не удалось получить данные Api, проверьте настройки");
            return (false, null, null);
        }

        public static async Task<(bool IsSuccess, string ApiAddress)> GetApiAddressAsync()
        {
            if (SharedProvider.GetFromDictionaryByKey(InfoKeys.ApiAddressBinanceKey) is string apiAddressValue)
            {
                return (true, apiAddressValue);
            }

            await HelperMethods.Message("Не удалось получить данные адрес, проверьте настройки");
            return (false, null);
        }

        public static async Task<(bool IsSuccess, List<BinanceBalance> Currencies)> GetAllCurrenciesAsync()
        {
            var apiData = await GetApiDataAsync();

            if (!apiData.IsSuccess)
            {
                return (false, null);
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret),
                AutoTimestamp = false
            };

            var client = new BinanceClient(options);

            var result = await client.General.GetAccountInfoAsync();

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}");
                return (false, null);
            }

            return (true, result.Data.Balances.Where(x => x.Free != 0 && x.Asset != "USDT").ToList());
        }

        public static async Task<bool> WithdrawalPlacedAsync(string fromAsset, string toAsset, decimal amount, string network = "BSC")
        {
            var apiData = await GetApiDataAsync();

            if (!apiData.IsSuccess)
            {
                return false;
            }

            var apiAddress = await GetApiAddressAsync();

            if (!apiAddress.IsSuccess)
            {
                return false;
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret),
                AutoTimestamp = false
            };

            var client = new BinanceClient(options);

            var result = client.WithdrawDeposit.Withdraw(fromAsset, apiAddress.ApiAddress, amount, network: network, addressTag: toAsset);

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}");
                return false;
            }

            await HelperMethods.Message("Оплата выставлена");

            return true;
        }

        public static async Task<(bool IsSuccess, decimal? Average)> GetAverageBetweenCurreniesAsync(
            string fromAsset, 
            string toAsset, 
            Binance.Net.Enums.KlineInterval interval, 
            DateTime startDate,
            DateTime endDate)
        {
            var apiData = await GetApiDataAsync();

            if (!apiData.IsSuccess)
            {
                return (false, null);
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret),
                AutoTimestamp = false
            };

            var client = new BinanceClient(options);

            var result = await client.Spot.Market.GetKlinesAsync($"{fromAsset}{toAsset}", interval, startDate, endDate);

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}");
                return (false, null);
            }

            return (true, result.Data.Average(x => x.Open));
        }
    }
}
