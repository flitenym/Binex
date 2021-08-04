using Binance.Net;
using Binance.Net.Objects;
using Binance.Net.Objects.Futures.MarketData;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.MarketData;
using Binance.Net.Objects.Spot.SpotData;
using Binance.Net.Objects.Spot.WalletData;
using Binex.Helper.StaticInfo;
using CryptoExchange.Net.Authentication;
using NLog;
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
        public static async Task<(bool IsSuccess, string ApiKey, string ApiSecret)> GetApiDataAsync(Logger logger = null)
        {
            if (logger == null)
            {
                if (SharedProvider.GetFromDictionaryByKey(InfoKeys.ApiKeyBinanceKey) is string apiKeyValue &&
                SharedProvider.GetFromDictionaryByKey(InfoKeys.ApiSecretBinanceKey) is string apiSecretValue)
                {
                    return (true, apiKeyValue, apiSecretValue);
                }

                await HelperMethods.Message("Не удалось получить данные Api, проверьте настройки", logger: logger);
                return (false, null, null);
            }
            else
            {
                var apiKey = await HelperMethods.GetByKeyInDBAsync(InfoKeys.ApiKeyBinanceKey);
                var apiSecret = await HelperMethods.GetByKeyInDBAsync(InfoKeys.ApiSecretBinanceKey);

                if (!string.IsNullOrEmpty(apiKey?.Value) && !string.IsNullOrEmpty(apiSecret?.Value))
                {
                    return (true, apiKey.Value, apiSecret.Value);
                }
                else
                {
                    return (false, null, null);
                }
            }
        }

        public static async Task<(bool IsSuccess, List<BinanceBalance> Currencies)> GetAllCurrenciesAsync(Logger logger = null)
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
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return (false, null);
            }

            //требуется поставить на первое место BTC, т.к. при продаже возможны переводы в BTC и нам продажа снова и снова будет не очень
            return (true, result.Data.Balances.Where(x => x.Free != 0 && x.Asset == StaticClass.BTC).Concat(
                result.Data.Balances.Where(x => x.Free != 0 && x.Asset != StaticClass.USDT && x.Asset != StaticClass.BTC)).ToList());
        }

        public static async Task<(bool IsSuccess, List<BinanceBalance> Currencies)> GetAllCurrenciesAsync(string asset, Logger logger = null)
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
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return (false, null);
            }

            //требуется поставить на первое место BTC, т.к. при продаже возможны переводы в BTC и нам продажа снова и снова будет не очень
            return (true, result.Data.Balances.Where(x => x.Free != 0 && x.Asset == asset).ToList());
        }

        public static async Task<(bool IsSuccess, List<BinanceUserCoin> Currencies)> GetAllCoinsAsync(Logger logger = null)
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

            var result = await client.General.GetUserCoinsAsync();

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return (false, null);
            }

            return (true, result.Data.ToList());
        }

        public static async Task<(bool IsSuccess, BinanceUserCoin Currency)> GetCoinAsync(string asset, Logger logger = null)
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

            var result = await client.General.GetUserCoinsAsync();

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return (false, null);
            }

            return (true, result.Data.FirstOrDefault(x=>x.Coin == asset));
        }

        public static async Task<bool> WithdrawalPlacedAsync(string fromAsset, string toAsset, decimal amount, string address, string network, Logger logger = null)
        {
            var apiData = await GetApiDataAsync();

            if (!apiData.IsSuccess)
            {
                return false;
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret),
                AutoTimestamp = false
            };

            var client = new BinanceClient(options);

            var result = await client.WithdrawDeposit.WithdrawAsync(asset: fromAsset, address: address, amount: amount, network: network);

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return false;
            }

            return true;
        }

        public static async Task<(bool IsSuccess, decimal? Average)> GetAverageBetweenCurreniesAsync(
            string fromAsset,
            string toAsset,
            Binance.Net.Enums.KlineInterval interval,
            DateTime startDate,
            DateTime endDate,
            Logger logger = null)
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
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return (false, null);
            }

            return (true, result.Data.Average(x => x.Open));
        }

        public static async Task<(bool IsSuccess, BinanceExchangeInfo Average)> GetExchangeRules(Logger logger = null)
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

            var result = await client.Spot.System.GetExchangeInfoAsync();

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return (false, null);
            }

            return (true, result.Data);
        }

        public static async Task<(bool IsSuccess, BinancePrice Price)> GetPrice(string fromAsset, string toAsset, Logger logger = null)
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

            var result = await client.Spot.Market.GetPriceAsync($"{fromAsset}{toAsset}");

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return (false, null);
            }

            return (true, result.Data);
        }

        public static async Task<bool> SellCoinAsync(decimal quantity, string fromAsset, string toAsset = "USDT", Logger logger = null)
        {
            var apiData = await GetApiDataAsync();

            if (!apiData.IsSuccess)
            {
                return false;
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret),
                AutoTimestamp = false
            };

            var client = new BinanceClient(options);

            var result = await client.Spot.Order.PlaceOrderAsync($"{fromAsset}{toAsset}", Binance.Net.Enums.OrderSide.Sell, Binance.Net.Enums.OrderType.Market, quantity: quantity);

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return false;
            }

            return true;
        }
    }
}