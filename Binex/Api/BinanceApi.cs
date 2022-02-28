using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Objects;
using Binance.Net.Objects.Models.Spot;
using Binex.Helper.StaticInfo;
using CryptoExchange.Net.Authentication;
using NLog;
using SharedLibrary.FileInfo;
using SharedLibrary.Helper;
using SharedLibrary.Helper.StaticInfo;
using SharedLibrary.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Binex.Api
{
    public static class BinanceApi
    {
        public const HttpStatusCode SuccessCode = HttpStatusCode.OK;
        public const int DustTransferSixHours = 32110;

        /// <summary>
        /// Получение API данных для работы с Binance
        /// </summary>
        public static async Task<(bool IsSuccess, string ApiKey, string ApiSecret)> GetApiDataAsync(SettingsFileInfo settings = null, Logger logger = null)
        {
            try
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
                    if (settings == null)
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
                    else
                    {
                        if (!string.IsNullOrEmpty(settings.ApiKey) && !string.IsNullOrEmpty(settings.ApiSecret))
                        {
                            return (true, settings.ApiKey, settings.ApiSecret);
                        }
                        else
                        {
                            return (false, null, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await HelperMethods.Message($"Не удалось получить данные Api {ex.ToString()}", logger: logger);
                return (false, null, null);
            }
        }

        /// <summary>
        /// Получение всех криптовалют у пользователя, с балансом > 0 без USDT и BNB
        /// </summary>
        public static async Task<(bool IsSuccess, List<BinanceBalance> Currencies)> GetAllCurrenciesWithout_USDT_BNB_Async(BinanceExchangeInfo exchangeInfo, SettingsFileInfo settings = null, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(settings: settings, logger: logger);

            if (!apiData.IsSuccess)
            {
                return (false, null);
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret)
            };

            var client = new BinanceClient(options);

            var result = await client.SpotApi.Account.GetAccountInfoAsync();

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return (false, null);
            }

            var currencies = result.Data.Balances.Where(x => x.Available != 0 && x.Asset != StaticClass.USDT && x.Asset != StaticClass.BNB).ToList();

            return (true, currencies);
        }

        public static async Task<(bool IsSuccess, List<(string Asset, decimal Quantity, bool IsDust)> Currencies)> GetAllCurrenciesWithoutUSDTWithQuantityAsync(BinanceExchangeInfo exchangeInfo, SettingsFileInfo settings = null, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(settings: settings, logger: logger);

            if (!apiData.IsSuccess)
            {
                return (false, null);
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret)
            };

            var client = new BinanceClient(options);

            var result = await client.SpotApi.Account.GetAccountInfoAsync();

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return (false, null);
            }

            //требуется поставить на первое место BTC, т.к. при продаже возможны переводы в BTC и нам продажа снова и снова будет не очень
            var currencies = result.Data.Balances.Where(x => x.Available != 0 && x.Asset == StaticClass.BTC).Concat(
                result.Data.Balances.Where(x => x.Available != 0 && x.Asset != StaticClass.USDT && x.Asset != StaticClass.BTC && x.Asset != StaticClass.BNB)).ToList();

            List<(string, decimal, bool)> currenciesData = new List<(string, decimal, bool)>();

            foreach (var currency in currencies)
            {
                (bool isSuccessGetQuantity, decimal resultQuantity, bool isDust, string toAsset) = await GetQuantity(exchangeInfo, currency.Asset, currency.Available * 0.98m, settings: settings, logger: logger);

                if (isSuccessGetQuantity)
                {
                    await HelperMethods.Message($"Для {currency.Asset} количество определилось как {resultQuantity} из {currency.Available}, {(isDust ? "ПЫЛЬ" : "НЕ ПЫЛЬ")}.", logger: logger);
                    currenciesData.Add((currency.Asset, resultQuantity, isDust));
                }
            }

            return (true, currenciesData);
        }

        /// <summary>
        /// Получение информации по криптовалюте
        /// </summary>
        public static async Task<(bool IsSuccess, (string FromAsset, string ToAsset, decimal Quantity, bool IsDust) Currency)> GetСurrencyAsync(BinanceExchangeInfo exchangeInfo, string asset, SettingsFileInfo settings = null, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(settings: settings, logger: logger);

            if (!apiData.IsSuccess)
            {
                return (false, default);
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret)
            };

            var client = new BinanceClient(options);

            var result = await client.SpotApi.Account.GetAccountInfoAsync();

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return (false, default);
            }

            var currency = result.Data.Balances.FirstOrDefault(x => x.Available != 0 && x.Asset == asset);

            if (currency != null)
            {
                (bool isSuccessQuantity, decimal resultQuantity, bool isDust, string toAsset) = await GetQuantity(exchangeInfo, currency.Asset, currency.Available * 0.98m, settings: settings, logger: logger);

                if (!isSuccessQuantity)
                {
                    return (false, default);
                }

                await HelperMethods.Message($"Для {currency.Asset} ({toAsset}) количество определилось как {resultQuantity} из {currency.Available}({currency.Available}), {(isDust ? "ПЫЛЬ" : "НЕ ПЫЛЬ")}.", logger: logger);

                return (true, (currency.Asset, toAsset, resultQuantity, isDust));
            }
            else
            {
                await HelperMethods.Message($"Не найдена монета {asset}", logger: logger);
                return (true, default);
            }
        }

        /// <summary>
        /// Получение всех криптовалют у пользователя, с балансом > 0, и без BNB
        /// </summary>
        public static async Task<(bool IsSuccess, List<BinanceBalance> Currencies)> GetGeneralAllCurrenciesWithoutBNBAsync(SettingsFileInfo settings = null, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(settings: settings, logger: logger);

            if (!apiData.IsSuccess)
            {
                return (false, null);
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret)
            };

            var client = new BinanceClient(options);

            var result = await client.SpotApi.Account.GetAccountInfoAsync();

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return (false, null);
            }

            return (true, result.Data.Balances.Where(x => x.Available != 0 && x.Asset != StaticClass.BNB).ToList());
        }

        /// <summary>
        /// Получение всех криптовалют у пользователя, с балансом > 0, и равной asset
        /// </summary>
        /// <param name="asset">Получение информации по указанной валюте</param>
        public static async Task<(bool IsSuccess, List<BinanceBalance> Currencies)> GetGeneralAllCurrenciesByAssetAsync(string asset, SettingsFileInfo settings = null, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(settings: settings, logger: logger);

            if (!apiData.IsSuccess)
            {
                return (false, null);
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret)
            };

            var client = new BinanceClient(options);

            var result = await client.SpotApi.Account.GetAccountInfoAsync();

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return (false, null);
            }

            //требуется поставить на первое место BTC, т.к. при продаже возможны переводы в BTC и нам продажа снова и снова будет не очень
            return (true, result.Data.Balances.Where(x => x.Available != 0 && x.Asset == asset).ToList());
        }

        /// <summary>
        /// Получение информации по валюте у пользователя
        /// </summary>
        /// <param name="asset">Криптовалюта</param>
        public static async Task<(bool IsSuccess, BinanceUserAsset Currency)> GetCoinAsync(string asset, SettingsFileInfo settings = null, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(settings: settings, logger: logger);

            if (!apiData.IsSuccess)
            {
                return (false, null);
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret)
            };

            var client = new BinanceClient(options);

            var result = await client.SpotApi.Account.GetUserAssetsAsync();

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return (false, null);
            }

            return (true, result.Data.FirstOrDefault(x => x.Asset == asset));
        }

        /// <summary>
        /// Перевод по адресу с указанной сетью
        /// </summary>
        /// <param name="fromAsset">Перевод из какой монеты</param>
        /// <param name="toAsset">Перевод в какую монету</param>
        /// <param name="amount">Сумма перевода</param>
        /// <param name="address">Адрес, кому переводится</param>
        /// <param name="network">Сеть, по которой переводится</param>
        public static async Task<bool> WithdrawalPlacedAsync(string fromAsset, decimal amount, string address, string network, SettingsFileInfo settings = null, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(settings: settings, logger: logger);

            if (!apiData.IsSuccess)
            {
                return false;
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret)
            };

            var client = new BinanceClient(options);

            var result = await client.SpotApi.Account.WithdrawAsync(asset: fromAsset, address: address, quantity: amount, network: network);

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
        public static async Task<(bool IsSuccess, decimal? Average)> GetAverageBetweenCurreniesAsync(string fromAsset, string toAsset, Binance.Net.Enums.KlineInterval interval, DateTime startDate, DateTime endDate, SettingsFileInfo settings = null, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(settings: settings, logger: logger);

            if (!apiData.IsSuccess)
            {
                return (false, null);
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret)
            };

            var client = new BinanceClient(options);

            var result = await client.SpotApi.ExchangeData.GetKlinesAsync($"{fromAsset}{toAsset}", interval, startDate, endDate);

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);
                return (false, null);
            }

            return (true, result.Data.Average(x => x.OpenPrice));
        }

        /// <summary>
        /// Получение цены между валютами
        /// </summary>
        /// <param name="fromAsset">Первая валюта</param>
        /// <param name="toAsset">Вторая валюта</param>
        public static async Task<(bool IsSuccess, BinancePrice Price)> GetPrice(string fromAsset, string toAsset, SettingsFileInfo settings = null, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(settings: settings, logger: logger);

            if (!apiData.IsSuccess)
            {
                return (false, null);
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret)
            };

            var client = new BinanceClient(options);

            var result = await client.SpotApi.ExchangeData.GetPriceAsync($"{fromAsset}{toAsset}");

            if (result.ResponseStatusCode != SuccessCode)
            {
                return (false, null);
            }

            return (true, result.Data);
        }

        /// <summary>
        /// Продажа монеты
        /// </summary>
        /// <param name="quantity">Количество перевода</param>
        /// <param name="fromAsset">Из какой валюты</param>
        /// <param name="toAsset">В какую валюту</param>
        public static async Task<bool> SellCoinAsync(decimal quantity, string fromAsset, string toAsset = "USDT", SettingsFileInfo settings = null, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(settings: settings, logger: logger);

            if (!apiData.IsSuccess)
            {
                return false;
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret)
            };

            var client = new BinanceClient(options);

            var result = await client.SpotApi.Trading.PlaceOrderAsync($"{fromAsset}{toAsset}", Binance.Net.Enums.OrderSide.Sell, Binance.Net.Enums.SpotOrderType.Market, quoteQuantity: quantity);

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
        public static async Task<(bool IsSuccess, decimal? Balance)> GetFuturesUsdtAccountUsdtBalanceAsync(SettingsFileInfo settings = null, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(settings: settings, logger: logger);

            if (!apiData.IsSuccess)
            {
                return (false, null);
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret)
            };

            var client = new BinanceClient(options);

            var accountInfoResult = await client.UsdFuturesApi.Account.GetAccountInfoAsync();

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
        public static async Task<bool> TransferSpotToUsdtAsync(SettingsFileInfo settings = null, Logger logger = null)
        {
            (bool isAccountUsdtSuccess, decimal? balance) = await GetFuturesUsdtAccountUsdtBalanceAsync(settings: settings, logger: logger);

            if (!isAccountUsdtSuccess)
            {
                return false;
            }

            var apiData = await GetApiDataAsync(settings: settings, logger: logger);

            if (!apiData.IsSuccess)
            {
                return false;
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret)
            };

            var client = new BinanceClient(options);

            var result = await client.GeneralApi.Futures.TransferFuturesAccountAsync(StaticClass.USDT, balance.Value, Binance.Net.Enums.FuturesTransferType.FromUsdtFuturesToSpot);

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
        public static async Task<(bool IsSuccess, Dictionary<string, BinanceAssetDetails> AssetDetails)> GetBinanceAssetDetails(SettingsFileInfo settings = null, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(settings: settings, logger: logger);

            if (!apiData.IsSuccess)
            {
                return (false, null);
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret)
            };

            var client = new BinanceClient(options);

            var result = await client.SpotApi.ExchangeData.GetAssetDetailsAsync();

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
        public static async Task<(bool IsSuccess, BinanceExchangeInfo ExchangeInfo)> GetExchangeInfo(SettingsFileInfo settings = null, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(settings: settings, logger: logger);

            if (!apiData.IsSuccess)
            {
                return (false, null);
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret)
            };

            var client = new BinanceClient(options);

            var result = await client.SpotApi.ExchangeData.GetExchangeInfoAsync();

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
        public static async Task<(bool IsSuccess, decimal Average)> GetAverageInfo(string fromAsset, string toAsset, SettingsFileInfo settings = null, Logger logger = null)
        {
            var apiData = await GetApiDataAsync(settings: settings, logger: logger);

            if (!apiData.IsSuccess)
            {
                return default;
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret)
            };

            var client = new BinanceClient(options);

            var result = await client.SpotApi.ExchangeData.GetCurrentAvgPriceAsync($"{fromAsset}{toAsset}");

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
        public static async Task<(bool IsSuccess, string Message)> TransferDustAsync(List<(string assetCurrency, decimal quantityCurrency, bool isDustCurrency)> currencies, SettingsFileInfo settings = null, Logger logger = null)
        {
            List<string> assets = new List<string>();

            // получаем валюты, которые были определены как Пыль
            assets.AddRange(currencies.Where(x => x.isDustCurrency).Select(y => y.assetCurrency));

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

            var apiData = await GetApiDataAsync(settings: settings, logger: logger);

            if (!apiData.IsSuccess)
            {
                return (false, null);
            }

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiData.ApiKey, apiData.ApiSecret)
            };

            var client = new BinanceClient(options);

            var result = await client.SpotApi.Account.DustTransferAsync(assets);

            if (result.ResponseStatusCode != SuccessCode)
            {
                await HelperMethods.Message($"Ошибка. {result.Error}", logger: logger);

                if (result.Error.Code == 32110)
                {
                    return (true, "Перевод монет с маленьким балансом не выполнен, т.к. можно производить раз в 6 часов");
                }

                return (false, null);
            }

            return (true, "Перевод монет с маленьким балансом выполнен.");
        }

        public static async Task<(bool IsSuccess, decimal Quantity, bool IsDust, string ToAsset)> GetQuantity(BinanceExchangeInfo exchangeInfo, string fromAsset, decimal quantity, SettingsFileInfo settings = null, Logger logger = null)
        {
            (bool isSuccessGetQuantityUSDT, decimal resultQuantityUsdt, bool isDustUsdt, string toAssetUsdt) = await GetQuantity(exchangeInfo, fromAsset, StaticClass.USDT, quantity, settings: settings, logger: logger);

            if (isSuccessGetQuantityUSDT && !string.IsNullOrEmpty(toAssetUsdt))
            {
                return (true, resultQuantityUsdt, isDustUsdt, toAssetUsdt);
            }
            else if (string.IsNullOrEmpty(toAssetUsdt))
            {
                (bool isSuccessGetQuantityAnother, decimal resultQuantityAnother, bool isDustAnother, string toAssetAnother) = await GetQuantity(exchangeInfo, fromAsset, null, quantity, settings: settings, logger: logger);

                if (isSuccessGetQuantityAnother && !string.IsNullOrEmpty(toAssetAnother))
                {
                    return (true, resultQuantityAnother, isDustAnother, toAssetAnother);
                }
            }

            return default;
        }

        /// <summary>
        /// Получение количества монет, которое удовлетворяет правилам выставления ордера на маркет
        /// </summary>
        /// <param name="exchangeInfo">Системная информация по валютам</param>
        /// <param name="asset">Валюта</param>
        /// <param name="quantity">Количество для продажи</param>
        /// <returns></returns>
        public static async Task<(bool IsSuccess, decimal Quantity, bool IsDust, string ToAsset)> GetQuantity(BinanceExchangeInfo exchangeInfo, string fromAsset, string toAsset, decimal quantity, SettingsFileInfo settings = null, Logger logger = null)
        {
            BinanceSymbol symbolInfo;
            if (string.IsNullOrEmpty(toAsset))
            {
                symbolInfo = exchangeInfo?.Symbols?.FirstOrDefault(x => x?.BaseAsset == fromAsset);
            }
            else
            {
                symbolInfo = exchangeInfo?.Symbols?.FirstOrDefault(x => x?.BaseAsset == fromAsset && x?.QuoteAsset == toAsset);
            }

            if (symbolInfo == null)
            {
                logger.Trace($"Не найдены фильтры для {fromAsset}");
                return (false, default(decimal), false, null);
            }

            var symbolFilterLotSize = symbolInfo.LotSizeFilter;
            var symbolFilterMinNotional = symbolInfo.MinNotionalFilter;

            (bool isSuccessGetPriceInfo, BinancePrice price) = await GetPrice(fromAsset, symbolInfo.QuoteAsset, settings: settings, logger: logger);

            if (!isSuccessGetPriceInfo)
            {
                logger.Trace($"Не удалось получить цену для {fromAsset}{symbolInfo.QuoteAsset}");
                return (false, default(decimal), false, symbolInfo.QuoteAsset);
            }

            // преобразует число, например 304.012334 если stepsize = 0.001 в 304.012
            decimal resultQuantity = Math.Round(quantity * price.Price, BitConverter.GetBytes(decimal.GetBits(symbolFilterLotSize.StepSize / 1.0000000000m)[3])[2], MidpointRounding.ToNegativeInfinity)
                + new decimal(0, 0, 0, false, (byte)symbolInfo.BaseAssetPrecision);

            logger?.Trace($"ResultQuantity: {resultQuantity}, StepSize: {symbolFilterLotSize.StepSize}, MinQuantity: {symbolFilterLotSize.MinQuantity}, MaxQuantity: {symbolFilterLotSize.MaxQuantity}");

            if (resultQuantity == 0)
            {
                logger.Trace($"Полученное значение для {fromAsset} = 0");
                return (true, resultQuantity, true, symbolInfo.QuoteAsset);
            }

            if (resultQuantity >= symbolFilterLotSize.MinQuantity && resultQuantity <= symbolFilterLotSize.MaxQuantity)
            {
                logger?.Trace("Проверку на LOT_SIZE прошло");

                if (!isSuccessGetPriceInfo)
                {
                    return default;
                }

                logger?.Trace($"ResultQuantity: {resultQuantity}, Price: {price.Price}, MinNotional: {symbolFilterMinNotional.MinNotional}");

                if (resultQuantity * 1.05m < symbolFilterMinNotional.MinNotional)
                {
                    logger?.Trace("Проверку на MIN_NOTIONAL не прошло");
                    return (true, resultQuantity, true, symbolInfo.QuoteAsset);
                }
                else
                {
                    logger?.Trace("Проверку на MIN_NOTIONAL прошло");
                    return (true, resultQuantity, false, symbolInfo.QuoteAsset);
                }
            }

            return default;
        }
    }
}