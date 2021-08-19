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

        /// <summary>
        /// Получение API данных для работы с Binance
        /// </summary>
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

        /// <summary>
        /// Получение всех криптовалют у пользователя, с балансом > 0, и в первую очередь идет BTC, и без USDT
        /// </summary>
        public static async Task<(bool IsSuccess, List<(string Asset, decimal quantity, bool isDust)> Currencies)> GetAllCurrenciesWithoutUSDTAsync(BinanceExchangeInfo exchangeInfo, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(logger: logger);

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
            var currencies = result.Data.Balances.Where(x => x.Free != 0 && x.Asset == StaticClass.BTC).Concat(
                result.Data.Balances.Where(x => x.Free != 0 && x.Asset != StaticClass.USDT && x.Asset != StaticClass.BTC)).ToList();

            List<(string, decimal, bool)> currenciesData = new List<(string, decimal, bool)>();

            foreach (var currency in currencies)
            {
                (decimal resultQuantity, bool isDust) = await GetQuantity(exchangeInfo, currency.Asset, currency.Free, logger);

                if (resultQuantity == default(decimal))
                {
                    continue;
                }

                await HelperMethods.Message($"Для {currency.Asset} количество определилось как {resultQuantity} из {currency.Free}, {(isDust ? "ПЫЛЬ" : "НЕ ПЫЛЬ")}.", logger: logger);

                currenciesData.Add((currency.Asset, resultQuantity, isDust));
            }

            return (true, currenciesData);
        }

        /// <summary>
        /// Получение всех криптовалют у пользователя, с балансом > 0, и без BNB
        /// </summary>
        public static async Task<(bool IsSuccess, List<BinanceBalance> Currencies)> GetGeneralAllCurrenciesWithoutBNBAsync(Logger logger = null)
        {
            var apiData = await GetApiDataAsync(logger: logger);

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

            return (true, result.Data.Balances.Where(x => x.Free != 0 && x.Asset != StaticClass.BNB).ToList());
        }

        /// <summary>
        /// Получение всех криптовалют у пользователя, с балансом > 0, и равной asset
        /// </summary>
        /// <param name="asset">Получение информации по указанной валюте</param>
        public static async Task<(bool IsSuccess, List<BinanceBalance> Currencies)> GetGeneralAllCurrenciesByAssetAsync(string asset, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(logger: logger);

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

        /// <summary>
        /// Получение информации по валюте у пользователя
        /// </summary>
        /// <param name="asset">Криптовалюта</param>
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

            return (true, result.Data.FirstOrDefault(x => x.Coin == asset));
        }

        /// <summary>
        /// Перевод по адресу с указанной сетью
        /// </summary>
        /// <param name="fromAsset">Перевод из какой монеты</param>
        /// <param name="toAsset">Перевод в какую монету</param>
        /// <param name="amount">Сумма перевода</param>
        /// <param name="address">Адрес, кому переводится</param>
        /// <param name="network">Сеть, по которой переводится</param>
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

        /// <summary>
        /// Средняя сумма за указанный период между двумя валютами
        /// </summary>
        /// <param name="fromAsset">Первая монета</param>
        /// <param name="toAsset">Вторая монета</param>
        /// <param name="interval">Интервал данных</param>
        /// <param name="startDate">Начало периода</param>
        /// <param name="endDate">Конец периода</param>
        public static async Task<(bool IsSuccess, decimal? Average)> GetAverageBetweenCurreniesAsync(string fromAsset, string toAsset, Binance.Net.Enums.KlineInterval interval, DateTime startDate, DateTime endDate, Logger logger = null)
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

        /// <summary>
        /// Получение цены между валютами
        /// </summary>
        /// <param name="fromAsset">Первая валюта</param>
        /// <param name="toAsset">Вторая валюта</param>
        public static async Task<(bool IsSuccess, BinancePrice Price)> GetPrice(string fromAsset, string toAsset, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(logger: logger);

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
                return (false, null);
            }

            return (true, result.Data);
        }

        /// <summary>
        /// Продажа монеты
        /// </summary>
        /// <param name="exchangeInfo">Общая информация по валютам</param>
        /// <param name="quantity">Количество перевода</param>
        /// <param name="fromAsset">Из какой валюты</param>
        /// <param name="toAsset">В какую валюту</param>
        public static async Task<bool> SellCoinAsync(BinanceExchangeInfo exchangeInfo, decimal quantity, string fromAsset, string toAsset = "USDT", Logger logger = null)
        {
            var apiData = await GetApiDataAsync(logger: logger);

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

            var result = await client.Spot.Order.PlaceOrderAsync($"{fromAsset}{toAsset}", Binance.Net.Enums.OrderSide.Sell, Binance.Net.Enums.OrderType.Market, quoteOrderQuantity: quantity);

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка продажи валюты {fromAsset}{toAsset}. {result.Error}", logger: logger);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Получение баланса по Фьючерсу
        /// </summary>
        public static async Task<(bool IsSuccess, decimal? Balance)> GetFuturesUsdtAccountUsdtBalanceAsync(Logger logger = null)
        {
            var apiData = await GetApiDataAsync(logger);

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

            var accountInfoResult = await client.FuturesUsdt.Account.GetAccountInfoAsync();

            if (accountInfoResult.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {accountInfoResult.Error}", logger: logger);
                return (false, null);
            }

            var futuresUsdtInfo = accountInfoResult.Data.Assets.FirstOrDefault(x => x.Asset == StaticClass.USDT);

            if (futuresUsdtInfo == null)
            {
                await HelperMethods.Message($"Ошибка. Не найдена монета {StaticClass.USDT} во Фьючерсном кошельке", logger: logger);
                return (false, null);
            }

            return (true, futuresUsdtInfo.WalletBalance);
        }

        /// <summary>
        /// Перевод из Фьючерс USDT в SPOT USDT
        /// </summary>
        public static async Task<bool> TransferSpotToUsdtAsync(Logger logger = null)
        {
            (bool isAccountUsdtSuccess, decimal? balance) = await GetFuturesUsdtAccountUsdtBalanceAsync(logger: logger);

            if (!isAccountUsdtSuccess)
            {
                return false;
            }

            var apiData = await GetApiDataAsync(logger: logger);

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

            var result = await client.Spot.Futures.TransferFuturesAccountAsync(StaticClass.USDT, balance.Value, Binance.Net.Enums.FuturesTransferType.FromUsdtFuturesToSpot);

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Получение информации по валютам
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static async Task<(bool IsSuccess, Dictionary<string, BinanceAssetDetails> AssetDetails)> GetBinanceAssetDetails(Logger logger = null)
        {
            var apiData = await GetApiDataAsync(logger: logger);

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

            var result = await client.WithdrawDeposit.GetAssetDetailsAsync();

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return (false, null);
            }

            return (true, result.Data);
        }

        /// <summary>
        /// Получение системной информации бинанса, включая минимальные значения по валютам
        /// </summary>
        public static async Task<(bool IsSuccess, BinanceExchangeInfo ExchangeInfo)> GetExchangeInfo(Logger logger = null)
        {
            var apiData = await GetApiDataAsync(logger: logger);

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

        /// <summary>
        /// Получение средней цены между монетами
        /// </summary>
        public static async Task<(bool IsSuccess, decimal Average)> GetAverageInfo(string fromAsset, string toAsset, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(logger: logger);

            if (!apiData.IsSuccess)
            {
                return default;
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret),
                AutoTimestamp = false
            };

            var client = new BinanceClient(options);

            var result = await client.Spot.Market.GetCurrentAvgPriceAsync($"{fromAsset}{toAsset}");

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка получения средней цены {fromAsset}{toAsset}. {result.Error}", logger: logger);
                return default;
            }

            return (true, result.Data.Price);
        }

        /// <summary>
        /// Продажа маленького баланса
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static async Task<(bool IsSuccess, string Message)> TransferDustAsync(List<(string assetCurrency, decimal quantityCurrency, bool isDustCurrency)> currencies, Logger logger = null)
        {
            List<string> assets = new List<string>();

            assets.AddRange(currencies.Where(x => x.isDustCurrency && x.assetCurrency != StaticClass.BNB).Select(y => y.assetCurrency));

            if (!assets.Any())
            {
                string message = $"Нет монет с маленьким балансом.";
                await HelperMethods.Message(message, logger: logger);
                return (true, message);
            }
            else
            {
                logger?.Trace($"Монеты с маленьким балансом: {string.Join(", ", assets)}");
            }

            var apiData = await GetApiDataAsync(logger: logger);

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

            var result = await client.General.DustTransferAsync(assets);

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return (false, null);
            }

            return (true, "Перевод монет с маленьким балансом выполнен.");
        }

        /// <summary>
        /// Получение количества монет, которое удовлетворяет правилам выставления ордера на маркет
        /// </summary>
        /// <param name="exchangeInfo">Системная информация по валютам</param>
        /// <param name="asset">Валюта</param>
        /// <param name="quantity">Количество для продажи</param>
        /// <returns></returns>
        public static async Task<(decimal Quantity, bool IsDust)> GetQuantity(BinanceExchangeInfo exchangeInfo, string fromAsset, decimal quantity, Logger logger = null)
        {
            var symbolFilterLotSize = exchangeInfo.Symbols.FirstOrDefault(x => x.BaseAsset == fromAsset)?.LotSizeFilter;
            var symbolFilterMinNotional = exchangeInfo.Symbols.FirstOrDefault(x => x.BaseAsset == fromAsset)?.MinNotionalFilter;

            if (symbolFilterLotSize == null)
            {
                return default;
            }

            decimal resultQuantity = Math.Round(quantity, BitConverter.GetBytes(decimal.GetBits(symbolFilterLotSize.StepSize / 1.0000000000m)[3])[2], MidpointRounding.ToNegativeInfinity);

            logger?.Trace($"ResultQuantity: {resultQuantity}, StepSize: {symbolFilterLotSize.StepSize}, MinQuantity: {symbolFilterLotSize.MinQuantity}, MaxQuantity: {symbolFilterLotSize.MaxQuantity}");

            if (resultQuantity >= symbolFilterLotSize.MinQuantity && resultQuantity <= symbolFilterLotSize.MaxQuantity)
            {
                logger?.Trace("Проверку на LOT_SIZE прошло");

                (bool isSuccessGetAverageInfo, decimal averagePrice) = await GetAverageInfo(fromAsset, StaticClass.BTC, logger);

                if (!isSuccessGetAverageInfo)
                {
                    return default;
                }

                logger?.Trace($"ResultQuantity: {resultQuantity}, AveragePrice: {averagePrice}, MinNotional: {symbolFilterMinNotional.MinNotional}");
                if (resultQuantity * averagePrice >= symbolFilterMinNotional.MinNotional)
                {
                    logger?.Trace("Проверку на MIN_NOTIONAL прошло");
                    return (resultQuantity, false);
                }
                else
                {
                    logger?.Trace("Проверку на MIN_NOTIONAL не прошло");
                    return (resultQuantity, true);
                }
            }

            return default;
        }
    }
}