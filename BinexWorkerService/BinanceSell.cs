using Binance.Net.Objects.Spot.MarketData;
using Binance.Net.Objects.Spot.SpotData;
using Binex.Api;
using SharedLibrary.FileInfo;
using Binex.Helper.StaticInfo;
using BinexWorkerService.Models;
using NLog;
using SharedLibrary.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BinexWorkerService
{
    public class BinanceSell : CronJobService
    {
        private readonly Logger _logger;
        private readonly bool IsActivated;

        public BinanceSell(IScheduleConfig<BinanceSell> config)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = LogManager.GetLogger(nameof(BinanceSell));
            IsActivated = HelperMethods.LicenseVerify(_logger);
        }

        #region Overrides

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (IsActivated)
            {
                _logger.Trace($"Запуск службы {nameof(BinanceSell)}");
                await base.StartAsync(cancellationToken);
            }
            else
            {
                _logger.Trace($"Нет лицензии");
                await base.StopAsync(cancellationToken);
            }
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            _logger.Trace($"Запуск продажи");
            try
            {
                //в случае, если настройки изменились
                Settings = await FileOperations.GetFileInfo(_logger);

                bool isSuccess = await Sell(_logger);
                if (isSuccess)
                {
                    _logger?.Trace($"Продажа прошла успешно");
                }
                else
                {
                    _logger?.Info($"Продажа прошла неудачно");
                }
            }
            catch (Exception ex)
            {
                _logger?.Info($"Продажа прошла с ошибками {ex.Message}");
            }

            return;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger?.Trace($"Остановка службы {nameof(BinanceSell)}");
            return base.StopAsync(cancellationToken);
        }

        #endregion

        private async Task<bool> Sell(Logger logger)
        {
            #region Проверка Api данных

            var apiData = await BinanceApi.GetApiDataAsync(settings: Settings, logger: logger);
            if (!apiData.IsSuccess)
            {
                logger?.Error($"Нет указанных данных ApiKey в настройках.");
                return false;
            }
            else
            {
                logger?.Trace($"ApiKey указаны.");
            }

            #endregion

            #region Инициализация данных для отправки уведомлений

            bool isNeedSendEmail = true;

            if (string.IsNullOrEmpty(Settings.Emails))
            {
                logger?.Error($"Нет указанных почтовых адресов в настройках.");
                isNeedSendEmail = false;
            }
            else
            {
                logger?.Trace($"Почта указана. {Settings.Emails}");
            }

            if (string.IsNullOrEmpty(Settings.EmailLogin))
            {
                logger?.Error($"Не указана почта отправителя в настройках.");
                isNeedSendEmail = false;
            }
            else
            {
                logger?.Trace($"Почта отправителя указана. {Settings.EmailLogin}");
            }

            if (string.IsNullOrEmpty(Settings.EmailPassword))
            {
                logger?.Error($"Не указан пароль от почты отправителя в настройках.");
                isNeedSendEmail = false;
            }
            else
            {
                logger?.Trace($"Пароль от почты отправителя указан. {Settings.EmailPassword}");
            }

            #endregion

            #region Ключи из настроек для работы алгоритма

            var isTransferFromFuturesToSpot = Settings.IsTransferFromFuturesToSpot == bool.TrueString;
            if (!isTransferFromFuturesToSpot)
            {
                logger?.Error($"\"Перевод USDT из Фьючерс в Спот\" выключена в настройках.");
            }
            else
            {
                logger?.Trace($"\"Перевод USDT из Фьючерс в Спот\" указан");
            }

            var isDustSell = Settings.IsDustSell == bool.TrueString;
            if (!isDustSell)
            {
                logger?.Error($"\"Перевод монет с маленьким балансом в BNB\" выключена в настройках.");
            }
            else
            {
                logger?.Trace($"\"Перевод монет с маленьким балансом в BNB\" указан");
            }

            var isCurrenciesSell = Settings.IsCurrenciesSell == bool.TrueString;
            if (!isCurrenciesSell)
            {
                logger?.Error($"\"Продажа криптовалют\" выключена в настройках.");
            }
            else
            {
                logger?.Trace($"\"Продажа криптовалют\" указана");
            }

            #endregion

            List<CurrencyInfo> currenciesInfo = new List<CurrencyInfo>();
            string isSuccessTransferSpotEmail = string.Empty;
            string isDustSellEmail = string.Empty;

            #region Перевод USDT из Фьючерс в Спот

            if (isTransferFromFuturesToSpot)
            {
                logger.Trace("Начинаем перевод USDT из фьючерс в спот");

                // перевод USDT из фьючерс в спот
                bool isSuccessTransferSpot = await BinanceApi.TransferSpotToUsdtAsync(settings: Settings, logger: logger);

                if (isSuccessTransferSpot)
                {
                    isSuccessTransferSpotEmail = "<p>Перевод USDT из фьючерс в спот был осуществлен.</p>";

                    logger?.Trace($"Перевод USDT из фьючерс в спот был осуществлен");
                }
                else
                {
                    logger?.Error($"Перевод USDT из фьючерс в спот не был осуществлен");
                }
            }

            #endregion

            if (isDustSell || isCurrenciesSell)
            {
                List<string> finalCoins = new List<string>();

                // получаем информацию по фильтрам, они статичны и достаточно 1 раз получить
                (bool isSuccessGetExchangeInfo, BinanceExchangeInfo exchangeInfo) = await BinanceApi.GetExchangeInfo(settings: Settings, logger: logger);

                if (!isSuccessGetExchangeInfo)
                {
                    logger.Error("Ошибка получения информации минимальных требований по символам для перевода");
                    return false;
                }

                // получим все валюты в балансе аккаунта, кроме USDT и BNB
                (bool isSuccessGetAllCurrencies, List<BinanceBalance> currencies) = await BinanceApi.GetAllCurrenciesWithout_USDT_BNB_Async(exchangeInfo, settings: Settings, logger: logger);

                if (!isSuccessGetAllCurrencies || currencies == null)
                {
                    return false;
                }

                #region Продажа криптовалют

                if (isCurrenciesSell)
                {
                    logger.Trace($"Начинаем продажу монет {string.Join(", ", currencies.Select(x => x.Asset))}");
                    // пройдем по всем валютам, которые есть на аккаунте и попробуем продать их
                    foreach (var currency in currencies)
                    {
                        var finalCoin = await SellCoin(currenciesInfo, currency.Asset, exchangeInfo, settings: Settings, logger: logger);
                        if (!string.IsNullOrEmpty(finalCoin) && finalCoin != StaticClass.USDT && !finalCoins.Contains(finalCoin))
                        {
                            finalCoins.Add(finalCoin);
                        }
                    }
                    logger.Trace("Продажа монет выполнена");
                }

                #endregion

                #region Перевод монет с маленьким балансом в BNB

                if (isDustSell)
                {
                    logger.Trace("Начинаем перевод монет с маленьким балансом в BNB");
                    if (!finalCoins.Contains(StaticClass.BNB))
                    {
                        finalCoins.Add(StaticClass.BNB);
                    }

                    (bool isSuccessGetAllAfterSellCurrencies, List<(string asset, decimal quantity, bool isDust)> currenciesAfterSell) = await BinanceApi.GetAllCurrenciesWithoutUSDTWithQuantityAsync(exchangeInfo, settings: Settings, logger: logger);

                    if (isSuccessGetAllAfterSellCurrencies)
                    {
                        // перевод мелких монет в BNB
                        (bool isSuccessTransferDust, string messageTransferDust) = await BinanceApi.TransferDustAsync(currenciesAfterSell, settings: Settings, logger: logger);

                        if (!string.IsNullOrEmpty(messageTransferDust))
                        {
                            isDustSellEmail = $"<p>{messageTransferDust}</p>";

                            logger?.Trace($"{messageTransferDust}");
                        }
                    }

                    logger.Trace("Перевод монет с маленьким балансом в BNB закончен");
                }

                #endregion

                if (isCurrenciesSell)
                {
                    logger.Trace($"Начинаем продажу дополнительных монет: {string.Join(", ", finalCoins)}");

                    foreach (var finalCoin in finalCoins)
                    {
                        await SellCoin(currenciesInfo, finalCoin, exchangeInfo, settings: Settings, logger: logger);
                    }

                    logger.Trace($"Закончили продажу дополнительных монет");
                }
            }

            logger.Trace($"Успешно продались: {string.Join(", ", currenciesInfo.Where(y => y.IsSuccessSell).Select(x => x.Asset))}");

            #region Отправка уведомлений

            if (isNeedSendEmail)
            {
                var body = CreateEmailBody(currenciesInfo, isSuccessTransferSpotEmail, isDustSellEmail);

                logger?.Trace($"Уведомление:{Environment.NewLine}{body}");

                var emailsInfo = Settings.Emails.Split(',');
                foreach (var email in emailsInfo)
                {
                    HelperMethods.SendEmail(Settings.EmailLogin.Trim(), "Binex Admin", Settings.EmailPassword.Trim(), email.Trim(), "Продажа криптовалют", body);
                }
            }

            #endregion

            return true;
        }

        private async Task<string> SellCoin(List<CurrencyInfo> currenciesInfo, string currencyAsset, BinanceExchangeInfo exchangeInfo, SettingsFileInfo settings, Logger logger)
        {
            logger.Trace($"Продажа {currencyAsset}");

            if (string.IsNullOrEmpty(currencyAsset))
            {
                return null;
            }

            // получим валюту и определим пыль или нет, если нет, то сразу продадим ее
            (bool isSuccessCurrency, (string fromAsset, string toAsset, decimal quantity, bool isDust) currencyInfo) = await BinanceApi.GetСurrencyAsync(exchangeInfo, currencyAsset, settings: settings, logger: logger);

            if (!isSuccessCurrency || string.IsNullOrEmpty(currencyInfo.fromAsset) || string.IsNullOrEmpty(currencyInfo.toAsset))
            {
                logger.Trace($"Продажа {currencyAsset}: неудачное получение валюты.");
                return null;
            }

            if (!currencyInfo.isDust)
            {
                logger.Trace($"Продажа {currencyAsset}: выполним продажу.");

                bool isSuccessSell = await BinanceApi.SellCoinAsync(currencyInfo.quantity, currencyInfo.fromAsset, currencyInfo.toAsset, settings: settings, logger: logger);

                logger.Trace($"Продажа {currencyAsset}:{(isSuccessSell ? "" : " не")} выполнилась продажа по {currencyInfo.toAsset}.");
                currenciesInfo.Add(new CurrencyInfo() { Asset = currencyInfo.fromAsset, IsSuccessSell = isSuccessSell });
            }

            return currencyInfo.toAsset;
        }

        private string CreateEmailBody(List<CurrencyInfo> currenciesInfo, string transferSpot, string transferDust)
        {
            if (currenciesInfo.Any())
            {
                string bodyContent = string.Empty;

                foreach (var currencyInfo in currenciesInfo)
                {
                    bodyContent += currencyInfo.GetElementHTML() + Environment.NewLine;
                }

                return $@"
<html>
<body style=""font-family: Helvetica;"">
	<p>Информация о продаже криптовалют</p>
    {transferSpot}
    {transferDust}
    <div style=""box-shadow: 0px 35px 50px rgba( 0, 0, 0, 0.2 );"">
		<table style=""border-collapse: collapse;"" width=""100%"">
			<thead>
			<tr>
				<th style=""text-align: center; padding: 8px; color: #ffffff; background: #324960;"" align=""center"">Криптовалюта</th>
				<th style=""text-align: center; padding: 8px; color: #ffffff; background: #324960;"" align=""center"">Успешная продажа</th>
			</tr>
			</thead>
			<tbody>
				{bodyContent}
			</tbody>
		</table>
	</div>
</body>
</html>";
            }
            else
            {
                return $@"
<html>
<body style=""font-family: Helvetica;"">
    {transferSpot}
    {transferDust}
	<p>Не продалась ни одна криптовалюта.</p>
</body>
</html>";
            }
        }
    }
}