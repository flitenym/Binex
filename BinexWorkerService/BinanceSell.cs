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
            }
            else
            {
                _logger.Trace($"Нет лицензии");
                await base.StopAsync(cancellationToken);
            }
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            if (!IsActivated) return;

            _logger.Trace($"Запуск продажи");
            try
            {
                bool isSuccess = await Sell(_logger);
                if (isSuccess)
                {
                    _logger.Trace($"Продажа прошла успешно");
                }
                else
                {
                    _logger.Info($"Продажа прошла неудачно");
                }
            }
            catch (Exception ex)
            {
                _logger.Info($"Продажа прошла с ошибками {ex.Message}");
            }
            
            return;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Trace($"Остановка службы {nameof(BinanceSell)}");
            return base.StopAsync(cancellationToken);
        }

        #endregion

        private async Task<bool> Sell(Logger logger)
        {
            var apiData = await BinanceApi.GetApiDataAsync(logger);
            if (!apiData.IsSuccess)
            {
                logger.Error($"Нет указанных данных ApiKey в настройках.");
                return false;
            }

            bool isNeedSendEmail = true;

            var emails = await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailsKey);
            if (string.IsNullOrEmpty(emails?.Value))
            {
                logger.Error($"Нет указанных почтовых адресов в настройках.");
                isNeedSendEmail = false;
            }
            var emailLogin = await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailLoginKey);
            var emailPassword = await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailPasswordKey);
            if (string.IsNullOrEmpty(emailLogin?.Value))
            {
                logger.Error($"Не указана почта отправителя в настройках.");
                isNeedSendEmail = false;
            }
            if (string.IsNullOrEmpty(emailPassword?.Value))
            {
                logger.Error($"Не указан пароль от почты отправителя в настройках.");
                isNeedSendEmail = false;
            }

            // перевод USDT из фьючерс в спот
            bool isSuccessTransferSpot = await BinanceApi.TransferSpotToUsdtAsync(logger: logger);

            // перевод мелких монет в BNB
            (bool isSuccessTransferDust, string messageTransferDust) = await BinanceApi.TransferDustAsync(logger: logger);            

            // продаем все монетки
            (bool isSuccess, List<BinanceBalance> currencies) = await BinanceApi.GetAllCurrenciesAsync(logger: logger);

            if (!isSuccess)
            {
                return false;
            }

            List<CurrencyInfo> currenciesInfo = new List<CurrencyInfo>();

            foreach (var currency in currencies)
            {
                // если цена между валютой текущей и USDT существует, то только тогда продадим
                (bool isSuccessPriceUSDT, BinancePrice priceUSDT) = await BinanceApi.GetPrice(currency.Asset, StaticClass.USDT, logger: logger);
                if (isSuccessPriceUSDT)
                {
                    bool isSuccessSellUSDT = await BinanceApi.SellCoinAsync(currency.Free, currency.Asset, StaticClass.USDT, logger: logger);
                    currenciesInfo.Add(new CurrencyInfo() { Asset = currency.Asset, IsSuccess = true, IsSuccessSell = isSuccessSellUSDT, IsWasNeedToBTC = false });
                }
                else
                {
                    // в случае если у нас есть по BTC, тогда нам нужно перевести в BTC, а затем получившийся BTC продать в USDT
                    // GetAllCurrenciesAsync - ставит на первое место BTC, поэтому можем смело переводить и продавать
                    (bool isSuccessPriceBTC, BinancePrice priceBTC) = await BinanceApi.GetPrice(currency.Asset, StaticClass.BTC, logger: logger);
                    if (isSuccessPriceBTC)
                    {
                        // в случае если транзакцию нам нужно выполнять по продаже BTC, может она работает в течении секунды, поставим лучше ожидание
                        await Task.Delay(2000);
                        // получим BTC текущего кошелька и попробуем продать его
                        (bool isSuccessBTC, List<BinanceBalance> curreniesBTC) = await BinanceApi.GetAllCurrenciesAsync(StaticClass.BTC, logger: logger);
                        if (isSuccessBTC && curreniesBTC.Any())
                        {
                            var currencyBTC = curreniesBTC.First();
                            bool isSuccessSellBTC = await BinanceApi.SellCoinAsync(currencyBTC.Free, currencyBTC.Asset, StaticClass.BTC, logger: logger);
                            currenciesInfo.Add(new CurrencyInfo() { Asset = currency.Asset, IsSuccess = true, IsSuccessSell = isSuccessSellBTC, IsWasNeedToBTC = true });
                        }
                        else
                        {
                            currenciesInfo.Add(new CurrencyInfo() { Asset = currency.Asset, IsSuccess = false, IsSuccessSell = false, IsWasNeedToBTC = true });
                        }
                    }
                }
            }

            if (isNeedSendEmail)
            {
                var body = CreateEmailBody(currenciesInfo, (isSuccessTransferSpot ? "<p>Перевод USDT из фьючерс в спот был осуществлен.</p>" : ""), (!string.IsNullOrEmpty(messageTransferDust) ? $"<p>{messageTransferDust}</p>" : ""));
                var emailsInfo = emails.Value.Split(',');
                foreach (var email in emailsInfo)
                {
                    HelperMethods.SendEmail(emailLogin.Value.Trim(), "Binex Admin", emailPassword.Value.Trim(), email.Trim(), "Продажа криптовалют", body);
                }
            }

            return false;
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
	<p>Не продалась ни одна криптовалюта, посмотрите логи.</p>
</body>
</html>";
            }
        }
    }
}