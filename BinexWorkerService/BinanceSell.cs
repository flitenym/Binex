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
                _logger.Trace($"������ ������ {nameof(BinanceSell)}");
            }
            else
            {
                _logger.Trace($"��� ��������");
                await base.StopAsync(cancellationToken);
            }
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            if (!IsActivated) return;

            _logger.Trace($"������ �������");
            try
            {
                bool isSuccess = await Sell(_logger);
                if (isSuccess)
                {
                    _logger.Trace($"������� ������ �������");
                }
                else
                {
                    _logger.Info($"������� ������ ��������");
                }
            }
            catch (Exception ex)
            {
                _logger.Info($"������� ������ � �������� {ex.Message}");
            }
            
            return;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Trace($"��������� ������ {nameof(BinanceSell)}");
            return base.StopAsync(cancellationToken);
        }

        #endregion

        private async Task<bool> Sell(Logger logger)
        {
            var apiData = await BinanceApi.GetApiDataAsync(logger);
            if (!apiData.IsSuccess)
            {
                logger.Error($"��� ��������� ������ ApiKey � ����������.");
                return false;
            }

            bool isNeedSendEmail = true;

            var emails = await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailsKey);
            if (string.IsNullOrEmpty(emails?.Value))
            {
                logger.Error($"��� ��������� �������� ������� � ����������.");
                isNeedSendEmail = false;
            }
            var emailLogin = await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailLoginKey);
            var emailPassword = await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailPasswordKey);
            if (string.IsNullOrEmpty(emailLogin?.Value))
            {
                logger.Error($"�� ������� ����� ����������� � ����������.");
                isNeedSendEmail = false;
            }
            if (string.IsNullOrEmpty(emailPassword?.Value))
            {
                logger.Error($"�� ������ ������ �� ����� ����������� � ����������.");
                isNeedSendEmail = false;
            }

            // ������� USDT �� ������� � ����
            bool isSuccessTransferSpot = await BinanceApi.TransferSpotToUsdtAsync(logger: logger);

            // ������� ������ ����� � BNB
            (bool isSuccessTransferDust, string messageTransferDust) = await BinanceApi.TransferDustAsync(logger: logger);            

            // ������� ��� �������
            (bool isSuccess, List<BinanceBalance> currencies) = await BinanceApi.GetAllCurrenciesAsync(logger: logger);

            if (!isSuccess)
            {
                return false;
            }

            List<CurrencyInfo> currenciesInfo = new List<CurrencyInfo>();

            foreach (var currency in currencies)
            {
                // ���� ���� ����� ������� ������� � USDT ����������, �� ������ ����� ��������
                (bool isSuccessPriceUSDT, BinancePrice priceUSDT) = await BinanceApi.GetPrice(currency.Asset, StaticClass.USDT, logger: logger);
                if (isSuccessPriceUSDT)
                {
                    bool isSuccessSellUSDT = await BinanceApi.SellCoinAsync(currency.Free, currency.Asset, StaticClass.USDT, logger: logger);
                    currenciesInfo.Add(new CurrencyInfo() { Asset = currency.Asset, IsSuccess = true, IsSuccessSell = isSuccessSellUSDT, IsWasNeedToBTC = false });
                }
                else
                {
                    // � ������ ���� � ��� ���� �� BTC, ����� ��� ����� ��������� � BTC, � ����� ������������ BTC ������� � USDT
                    // GetAllCurrenciesAsync - ������ �� ������ ����� BTC, ������� ����� ����� ���������� � ���������
                    (bool isSuccessPriceBTC, BinancePrice priceBTC) = await BinanceApi.GetPrice(currency.Asset, StaticClass.BTC, logger: logger);
                    if (isSuccessPriceBTC)
                    {
                        // � ������ ���� ���������� ��� ����� ��������� �� ������� BTC, ����� ��� �������� � ������� �������, �������� ����� ��������
                        await Task.Delay(2000);
                        // ������� BTC �������� �������� � ��������� ������� ���
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
                var body = CreateEmailBody(currenciesInfo, (isSuccessTransferSpot ? "<p>������� USDT �� ������� � ���� ��� �����������.</p>" : ""), (!string.IsNullOrEmpty(messageTransferDust) ? $"<p>{messageTransferDust}</p>" : ""));
                var emailsInfo = emails.Value.Split(',');
                foreach (var email in emailsInfo)
                {
                    HelperMethods.SendEmail(emailLogin.Value.Trim(), "Binex Admin", emailPassword.Value.Trim(), email.Trim(), "������� �����������", body);
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
	<p>���������� � ������� �����������</p>
    {transferSpot}
    {transferDust}
    <div style=""box-shadow: 0px 35px 50px rgba( 0, 0, 0, 0.2 );"">
		<table style=""border-collapse: collapse;"" width=""100%"">
			<thead>
			<tr>
				<th style=""text-align: center; padding: 8px; color: #ffffff; background: #324960;"" align=""center"">������������</th>
				<th style=""text-align: center; padding: 8px; color: #ffffff; background: #324960;"" align=""center"">�������� �������</th>
				<th style=""text-align: center; padding: 8px; color: #ffffff; background: #324960;"" align=""center"">��� ������� � BTC</th>
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
	<p>�� ��������� �� ���� ������������, ���������� ����.</p>
</body>
</html>";
            }
        }
    }
}