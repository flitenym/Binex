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
                await base.StartAsync(cancellationToken);
            }
            else
            {
                _logger.Trace($"��� ��������");
                await base.StopAsync(cancellationToken);
            }
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            _logger.Trace($"������ �������");
            try
            {
                bool isSuccess = await Sell(_logger);
                if (isSuccess)
                {
                    _logger?.Trace($"������� ������ �������");
                }
                else
                {
                    _logger?.Info($"������� ������ ��������");
                }
            }
            catch (Exception ex)
            {
                _logger?.Info($"������� ������ � �������� {ex.Message}");
            }

            return;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger?.Trace($"��������� ������ {nameof(BinanceSell)}");
            return base.StopAsync(cancellationToken);
        }

        #endregion

        private async Task<bool> Sell(Logger logger)
        {
            #region �������� Api ������

            var apiData = await BinanceApi.GetApiDataAsync(logger);
            if (!apiData.IsSuccess)
            {
                logger?.Error($"��� ��������� ������ ApiKey � ����������.");
                return false;
            }
            else
            {
                logger?.Trace($"ApiKey �������.");
            }

            #endregion

            #region ������������� ������ ��� �������� �����������

            bool isNeedSendEmail = true;

            var emails = await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailsKey);
            if (string.IsNullOrEmpty(emails?.Value))
            {
                logger?.Error($"��� ��������� �������� ������� � ����������.");
                isNeedSendEmail = false;
            }
            else
            {
                logger?.Trace($"����� �������. {emails.Value}");
            }

            var emailLogin = await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailLoginKey);
            var emailPassword = await HelperMethods.GetByKeyInDBAsync(InfoKeys.EmailPasswordKey);
            if (string.IsNullOrEmpty(emailLogin?.Value))
            {
                logger?.Error($"�� ������� ����� ����������� � ����������.");
                isNeedSendEmail = false;
            }
            else
            {
                logger?.Trace($"����� ����������� �������. {emailLogin.Value}");
            }

            if (string.IsNullOrEmpty(emailPassword?.Value))
            {
                logger?.Error($"�� ������ ������ �� ����� ����������� � ����������.");
                isNeedSendEmail = false;
            }
            else
            {
                logger?.Trace($"������ �� ����� ����������� ������. {emailPassword.Value}");
            }

            #endregion

            #region ����� �� �������� ��� ������ ���������

            var isTransferFromFuturesToSpot = (await HelperMethods.GetByKeyInDBAsync(InfoKeys.IsTransferFromFuturesToSpotKey))?.Value == bool.TrueString;
            if (!isTransferFromFuturesToSpot)
            {
                logger?.Error($"\"������� USDT �� ������� � ����\" ��������� � ����������.");
            }
            else
            {
                logger?.Trace($"\"������� USDT �� ������� � ����\" ������");
            }

            var isDustSell = (await HelperMethods.GetByKeyInDBAsync(InfoKeys.IsDustSellKey))?.Value == bool.TrueString;
            if (!isDustSell)
            {
                logger?.Error($"\"������� ����� � ��������� �������� � BNB\" ��������� � ����������.");
            }
            else
            {
                logger?.Trace($"\"������� ����� � ��������� �������� � BNB\" ������");
            }

            var isCurrenciesSell = (await HelperMethods.GetByKeyInDBAsync(InfoKeys.IsCurrenciesSellKey))?.Value == bool.TrueString;
            if (!isCurrenciesSell)
            {
                logger?.Error($"\"������� �����������\" ��������� � ����������.");
            }
            else
            {
                logger?.Trace($"\"������� �����������\" �������");
            }

            #endregion

            List<CurrencyInfo> currenciesInfo = new List<CurrencyInfo>();
            string isSuccessTransferSpotEmail = string.Empty;
            string isDustSellEmail = string.Empty;

            #region ������� USDT �� ������� � ����

            if (isTransferFromFuturesToSpot)
            {
                logger.Trace("�������� ������� USDT �� ������� � ����");

                // ������� USDT �� ������� � ����
                bool isSuccessTransferSpot = await BinanceApi.TransferSpotToUsdtAsync(logger: logger);

                if (isSuccessTransferSpot)
                {
                    isSuccessTransferSpotEmail = "<p>������� USDT �� ������� � ���� ��� �����������.</p>";

                    logger?.Trace($"������� USDT �� ������� � ���� ��� �����������");
                }
                else
                {
                    logger?.Error($"������� USDT �� ������� � ���� �� ��� �����������");
                }
            }

            #endregion

            if (isDustSell || isCurrenciesSell)
            {
                // �������� ���������� �� ��������, ��� �������� � ���������� 1 ��� ��������
                (bool isSuccessGetExchangeInfo, BinanceExchangeInfo exchangeInfo) = await BinanceApi.GetExchangeInfo(logger: logger);

                if (!isSuccessGetExchangeInfo)
                {
                    logger.Error("������ ��������� ���������� ����������� ���������� �� �������� ��� ��������");
                    return false;
                }

                // ������� ��� ������ � ������� ��������, ����� USDT
                (bool isSuccessGetAllCurrencies, List<BinanceBalance> currencies) = await BinanceApi.GetAllCurrenciesWithout_USDT_BTC_BNB_Async(exchangeInfo, logger: logger);

                if (!isSuccessGetAllCurrencies || currencies == null)
                {
                    return false;
                }

                #region ������� �����������

                if (isCurrenciesSell)
                {
                    logger.Trace($"�������� ������� ����� {string.Join(", ", currencies.Select(x=>x.Asset))}");
                    // ������� �� ���� �������, ������� ���� �� �������� � ��������� ������� ��
                    foreach (var currency in currencies)
                    {
                        await SellCoin(currenciesInfo, currency.Asset, exchangeInfo, logger);
                    }
                    logger.Trace("������� ����� ���������");
                }

                #endregion

                #region ������� ����� � ��������� �������� � BNB

                if (isDustSell)
                {
                    logger.Trace("�������� ������� ����� � ��������� �������� � BNB");

                    (bool isSuccessGetAllAfterSellCurrencies, List<(string asset, decimal quantity, bool isDust)> currenciesAfterSell) = await BinanceApi.GetAllCurrenciesWithoutUSDTWithQuantityAsync(exchangeInfo, logger: logger);

                    if (isSuccessGetAllAfterSellCurrencies)
                    {
                        // ������� ������ ����� � BNB
                        (bool isSuccessTransferDust, string messageTransferDust) = await BinanceApi.TransferDustAsync(currenciesAfterSell, logger: logger);

                        if (!string.IsNullOrEmpty(messageTransferDust))
                        {
                            isDustSellEmail = $"<p>{messageTransferDust}</p>";

                            logger?.Trace($"{messageTransferDust}");
                        }
                    }

                    logger.Trace("������� ����� � ��������� �������� � BNB ��������");
                }

                #endregion

                if (isCurrenciesSell)
                {
                    logger.Trace($"�������� ������� BNB � BTC");

                    // �.�. ���������� �� ������� ������ ��� BNB � ������� ���� ������������ � BNB, �� ������� ��� � ������� � ���������.
                    await SellCoin(currenciesInfo, StaticClass.BNB, exchangeInfo, logger);
                    // �.�. ������� ���� � �� BTC, �� � ��� ��������.
                    await SellCoin(currenciesInfo, StaticClass.BTC, exchangeInfo, logger);

                    logger.Trace($"��������� ������� BNB � BTC");
                }
            }

            #region �������� �����������

            if (isNeedSendEmail)
            {
                var body = CreateEmailBody(currenciesInfo, isSuccessTransferSpotEmail, isDustSellEmail);

                logger?.Trace($"�����������:{Environment.NewLine}{body}");

                var emailsInfo = emails.Value.Split(',');
                foreach (var email in emailsInfo)
                {
                    HelperMethods.SendEmail(emailLogin.Value.Trim(), "Binex Admin", emailPassword.Value.Trim(), email.Trim(), "������� �����������", body);
                }
            }

            #endregion

            return false;
        }

        private async Task SellCoin(List<CurrencyInfo> currenciesInfo, string currencyAsset, BinanceExchangeInfo exchangeInfo, Logger logger)
        {
            logger.Trace($"������� {currencyAsset}");

            if (string.IsNullOrEmpty(currencyAsset))
            {
                return;
            }

            // ������� ������ � ��������� ���� ��� ���, ���� ���, �� ����� �������� ��
            (bool isSuccessCurrency, (string asset, decimal quantity, bool isDust) currencyInfo) = await BinanceApi.Get�urrencyAsync(exchangeInfo, currencyAsset, logger: logger);

            if (!isSuccessCurrency || string.IsNullOrEmpty(currencyInfo.asset))
            {
                logger.Trace($"������� {currencyAsset}: ��������� ��������� ������.");
                return;
            }

            if (!currencyInfo.isDust)
            {
                logger.Trace($"������� {currencyAsset}: �������� �������.");

                bool isSuccessSellUSDT = await BinanceApi.SellCoinAsync(exchangeInfo, currencyInfo.quantity, currencyInfo.asset, StaticClass.USDT, logger: logger);

                if (isSuccessSellUSDT)
                {
                    logger.Trace($"������� {currencyAsset}: ����������� ������� �� USDT.");
                    currenciesInfo.Add(new CurrencyInfo() { Asset = currencyInfo.asset, IsSuccess = true, IsSuccessSell = isSuccessSellUSDT, IsWasNeedToBTC = false });
                }
                else if (currencyInfo.asset != StaticClass.BTC)
                {
                    bool isSuccessSellBTC = await BinanceApi.SellCoinAsync(exchangeInfo, currencyInfo.quantity, currencyInfo.asset, StaticClass.BTC, logger: logger);
                    if (isSuccessSellBTC)
                    {
                        logger.Trace($"������� {currencyAsset}: ����������� ������� �� BTC.");
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
    {transferSpot}
    {transferDust}
	<p>�� ��������� �� ���� ������������.</p>
</body>
</html>";
            }
        }
    }
}