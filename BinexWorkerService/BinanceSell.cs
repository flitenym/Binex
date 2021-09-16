using Binance.Net.Objects.Spot.MarketData;
using Binance.Net.Objects.Spot.SpotData;
using Binex.Api;
using Binex.Helper.StaticInfo;
using BinexWorkerService.Models;
using Microsoft.Extensions.Logging;
using NLog;
using SharedLibrary.Helper;
using SharedLibrary.Helper.StaticInfo;
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

            var apiData = await BinanceApi.GetApiDataAsync(logger);
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

            var emails = await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailsKey);
            if (string.IsNullOrEmpty(emails?.Value))
            {
                logger?.Error($"Нет указанных почтовых адресов в настройках.");
                isNeedSendEmail = false;
            }
            else
            {
                logger?.Trace($"Почта указана. {emails.Value}");
            }

            var emailLogin = await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailLoginKey);
            var emailPassword = await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailPasswordKey);
            if (string.IsNullOrEmpty(emailLogin?.Value))
            {
                logger?.Error($"Не указана почта отправителя в настройках.");
                isNeedSendEmail = false;
            }
            else
            {
                logger?.Trace($"Почта отправителя указана. {emailLogin.Value}");
            }

            if (string.IsNullOrEmpty(emailPassword?.Value))
            {
                logger?.Error($"Не указан пароль от почты отправителя в настройках.");
                isNeedSendEmail = false;
            }
            else
            {
                logger?.Trace($"Пароль от почты отправителя указан. {emailPassword.Value}");
            }

            #endregion

            #region Ключи из настроек для работы алгоритма

            var isTransferFromFuturesToSpot = (await HelperMethods.GetByKeyInDBAsync(InfoKeys.IsTransferFromFuturesToSpotKey))?.Value == bool.TrueString;
            if (!isTransferFromFuturesToSpot)
            {
                logger?.Error($"\"Перевод USDT из Фьючерс в Спот\" выключена в настройках.");
            }
            else
            {
                logger?.Trace($"\"Перевод USDT из Фьючерс в Спот\" указан");
            }

            var isDustSell = (await HelperMethods.GetByKeyInDBAsync(InfoKeys.IsDustSellKey))?.Value == bool.TrueString;
            if (!isDustSell)
            {
                logger?.Error($"\"Перевод монет с маленьким балансом в BNB\" выключена в настройках.");
            }
            else
            {
                logger?.Trace($"\"Перевод монет с маленьким балансом в BNB\" указан");
            }

            var isCurrenciesSell = (await HelperMethods.GetByKeyInDBAsync(InfoKeys.IsCurrenciesSellKey))?.Value == bool.TrueString;
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
                bool isSuccessTransferSpot = await BinanceApi.TransferSpotToUsdtAsync(logger: logger);

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
                // получаем информацию по фильтрам, они статичны и достаточно 1 раз получить
                (bool isSuccessGetExchangeInfo, BinanceExchangeInfo exchangeInfo) = await BinanceApi.GetExchangeInfo(logger: logger);

                if (!isSuccessGetExchangeInfo)
                {
                    logger.Error("Ошибка получения информации минимальных требований по символам для перевода");
                    return false;
                }

                // получим все валюты в балансе аккаунта, кроме USDT
                (bool isSuccessGetAllCurrencies, List<BinanceBalance> currencies) = await BinanceApi.GetAllCurrenciesWithout_USDT_BTC_BNB_Async(exchangeInfo, logger: logger);

                if (!isSuccessGetAllCurrencies || currencies == null)
                {
                    return false;
                }

                #region Продажа криптовалют

                if (isCurrenciesSell)
                {
                    logger.Trace($"Начинаем продажу монет {string.Join(", ", currencies.Select(x=>x.Asset))}");
                    // пройдем по всем валютам, которые есть на аккаунте и попробуем продать их
                    foreach (var currency in currencies)
                    {
                        await SellCoin(currenciesInfo, currency.Asset, exchangeInfo, logger);
                    }
                    logger.Trace("Продажа монет выполнена");
                }

                #endregion

                #region Перевод монет с маленьким балансом в BNB

                if (isDustSell)
                {
                    logger.Trace("Начинаем перевод монет с маленьким балансом в BNB");

                    (bool isSuccessGetAllAfterSellCurrencies, List<(string asset, decimal quantity, bool isDust)> currenciesAfterSell) = await BinanceApi.GetAllCurrenciesWithoutUSDTWithQuantityAsync(exchangeInfo, logger: logger);

                    if (isSuccessGetAllAfterSellCurrencies)
                    {
                        // перевод мелких монет в BNB
                        (bool isSuccessTransferDust, string messageTransferDust) = await BinanceApi.TransferDustAsync(currenciesAfterSell, logger: logger);

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
                    logger.Trace($"Начинаем продажу BNB и BTC");

                    // т.к. информация по валютам пришла без BNB и перевод пыли производится в BNB, то получем его и включим в обработку.
                    await SellCoin(currenciesInfo, StaticClass.BNB, exchangeInfo, logger);
                    // т.к. продажи были и по BTC, то и его продадим.
                    await SellCoin(currenciesInfo, StaticClass.BTC, exchangeInfo, logger);

                    logger.Trace($"Закончили продажу BNB и BTC");
                }
            }

            #region Отправка уведомлений

            if (isNeedSendEmail)
            {
                var body = CreateEmailBody(currenciesInfo, isSuccessTransferSpotEmail, isDustSellEmail);

                logger?.Trace($"Уведомление:{Environment.NewLine}{body}");

                var emailsInfo = emails.Value.Split(',');
                foreach (var email in emailsInfo)
                {
                    HelperMethods.SendEmail(emailLogin.Value.Trim(), "Binex Admin", emailPassword.Value.Trim(), email.Trim(), "Продажа криптовалют", body);
                }
            }

            #endregion

            return false;
        }

        private async Task SellCoin(List<CurrencyInfo> currenciesInfo, string currencyAsset, BinanceExchangeInfo exchangeInfo, Logger logger)
        {
            logger.Trace($"Продажа {currencyAsset}");

            if (string.IsNullOrEmpty(currencyAsset))
            {
                return;
            }

            // получим валюту и определим пыль или нет, если нет, то сразу продадим ее
            (bool isSuccessCurrency, (string asset, decimal quantity, bool isDust) currencyInfo) = await BinanceApi.GetСurrencyAsync(exchangeInfo, currencyAsset, logger: logger);

            if (!isSuccessCurrency || string.IsNullOrEmpty(currencyInfo.asset))
            {
                logger.Trace($"Продажа {currencyAsset}: неудачное получение валюты.");
                return;
            }

            if (!currencyInfo.isDust)
            {
                logger.Trace($"Продажа {currencyAsset}: выполним продажу.");

                bool isSuccessSellUSDT = await BinanceApi.SellCoinAsync(exchangeInfo, currencyInfo.quantity, currencyInfo.asset, StaticClass.USDT, logger: logger);

                if (isSuccessSellUSDT)
                {
                    logger.Trace($"Продажа {currencyAsset}: выполнилась продажа по USDT.");
                    currenciesInfo.Add(new CurrencyInfo() { Asset = currencyInfo.asset, IsSuccess = true, IsSuccessSell = isSuccessSellUSDT, IsWasNeedToBTC = false });
                }
                else if (currencyInfo.asset != StaticClass.BTC)
                {
                    bool isSuccessSellBTC = await BinanceApi.SellCoinAsync(exchangeInfo, currencyInfo.quantity, currencyInfo.asset, StaticClass.BTC, logger: logger);
                    if (isSuccessSellBTC)
                    {
                        logger.Trace($"Продажа {currencyAsset}: выполнилась продажа по BTC.");
                    }
                }
            }
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
				<th style=""text-align: center; padding: 8px; color: #ffffff; background: #324960;"" align=""center"">Был перевод в BTC</th>
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