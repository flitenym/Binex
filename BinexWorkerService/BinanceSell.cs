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
                //� ������, ���� ��������� ����������
                Settings = await FileOperations.GetFileInfo(_logger);

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

            var apiData = await BinanceApi.GetApiDataAsync(settings: Settings, logger: logger);
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

            if (string.IsNullOrEmpty(Settings.Emails))
            {
                logger?.Error($"��� ��������� �������� ������� � ����������.");
                isNeedSendEmail = false;
            }
            else
            {
                logger?.Trace($"����� �������. {Settings.Emails}");
            }

            if (string.IsNullOrEmpty(Settings.EmailLogin))
            {
                logger?.Error($"�� ������� ����� ����������� � ����������.");
                isNeedSendEmail = false;
            }
            else
            {
                logger?.Trace($"����� ����������� �������. {Settings.EmailLogin}");
            }

            if (string.IsNullOrEmpty(Settings.EmailPassword))
            {
                logger?.Error($"�� ������ ������ �� ����� ����������� � ����������.");
                isNeedSendEmail = false;
            }
            else
            {
                logger?.Trace($"������ �� ����� ����������� ������. {Settings.EmailPassword}");
            }

            #endregion

            #region ����� �� �������� ��� ������ ���������

            var isTransferFromFuturesToSpot = Settings.IsTransferFromFuturesToSpot == bool.TrueString;
            if (!isTransferFromFuturesToSpot)
            {
                logger?.Error($"\"������� USDT �� ������� � ����\" ��������� � ����������.");
            }
            else
            {
                logger?.Trace($"\"������� USDT �� ������� � ����\" ������");
            }

            var isDustSell = Settings.IsDustSell == bool.TrueString;
            if (!isDustSell)
            {
                logger?.Error($"\"������� ����� � ��������� �������� � BNB\" ��������� � ����������.");
            }
            else
            {
                logger?.Trace($"\"������� ����� � ��������� �������� � BNB\" ������");
            }

            var isCurrenciesSell = Settings.IsCurrenciesSell == bool.TrueString;
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
                bool isSuccessTransferSpot = await BinanceApi.TransferSpotToUsdtAsync(settings: Settings, logger: logger);

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
                List<string> finalCoins = new List<string>();

                // �������� ���������� �� ��������, ��� �������� � ���������� 1 ��� ��������
                (bool isSuccessGetExchangeInfo, BinanceExchangeInfo exchangeInfo) = await BinanceApi.GetExchangeInfo(settings: Settings, logger: logger);

                if (!isSuccessGetExchangeInfo)
                {
                    logger.Error("������ ��������� ���������� ����������� ���������� �� �������� ��� ��������");
                    return false;
                }

                // ������� ��� ������ � ������� ��������, ����� USDT � BNB
                (bool isSuccessGetAllCurrencies, List<BinanceBalance> currencies) = await BinanceApi.GetAllCurrenciesWithout_USDT_BNB_Async(exchangeInfo, settings: Settings, logger: logger);

                if (!isSuccessGetAllCurrencies || currencies == null)
                {
                    return false;
                }

                #region ������� �����������

                if (isCurrenciesSell)
                {
                    logger.Trace($"�������� ������� ����� {string.Join(", ", currencies.Select(x => x.Asset))}");
                    // ������� �� ���� �������, ������� ���� �� �������� � ��������� ������� ��
                    foreach (var currency in currencies)
                    {
                        var finalCoin = await SellCoin(currenciesInfo, currency.Asset, exchangeInfo, settings: Settings, logger: logger);
                        if (!string.IsNullOrEmpty(finalCoin) && finalCoin != StaticClass.USDT && !finalCoins.Contains(finalCoin))
                        {
                            finalCoins.Add(finalCoin);
                        }
                    }
                    logger.Trace("������� ����� ���������");
                }

                #endregion

                #region ������� ����� � ��������� �������� � BNB

                if (isDustSell)
                {
                    logger.Trace("�������� ������� ����� � ��������� �������� � BNB");
                    if (!finalCoins.Contains(StaticClass.BNB))
                    {
                        finalCoins.Add(StaticClass.BNB);
                    }

                    (bool isSuccessGetAllAfterSellCurrencies, List<(string asset, decimal quantity, bool isDust)> currenciesAfterSell) = await BinanceApi.GetAllCurrenciesWithoutUSDTWithQuantityAsync(exchangeInfo, settings: Settings, logger: logger);

                    if (isSuccessGetAllAfterSellCurrencies)
                    {
                        // ������� ������ ����� � BNB
                        (bool isSuccessTransferDust, string messageTransferDust) = await BinanceApi.TransferDustAsync(currenciesAfterSell, settings: Settings, logger: logger);

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
                    logger.Trace($"�������� ������� �������������� �����: {string.Join(", ", finalCoins)}");

                    foreach (var finalCoin in finalCoins)
                    {
                        await SellCoin(currenciesInfo, finalCoin, exchangeInfo, settings: Settings, logger: logger);
                    }

                    logger.Trace($"��������� ������� �������������� �����");
                }
            }

            logger.Trace($"������� ���������: {string.Join(", ", currenciesInfo.Where(y => y.IsSuccessSell).Select(x => x.Asset))}");

            #region �������� �����������

            if (isNeedSendEmail)
            {
                var body = CreateEmailBody(currenciesInfo, isSuccessTransferSpotEmail, isDustSellEmail);

                logger?.Trace($"�����������:{Environment.NewLine}{body}");

                var emailsInfo = Settings.Emails.Split(',');
                foreach (var email in emailsInfo)
                {
                    HelperMethods.SendEmail(Settings.EmailLogin.Trim(), "Binex Admin", Settings.EmailPassword.Trim(), email.Trim(), "������� �����������", body);
                }
            }

            #endregion

            return true;
        }

        private async Task<string> SellCoin(List<CurrencyInfo> currenciesInfo, string currencyAsset, BinanceExchangeInfo exchangeInfo, SettingsFileInfo settings, Logger logger)
        {
            logger.Trace($"������� {currencyAsset}");

            if (string.IsNullOrEmpty(currencyAsset))
            {
                return null;
            }

            // ������� ������ � ��������� ���� ��� ���, ���� ���, �� ����� �������� ��
            (bool isSuccessCurrency, (string fromAsset, string toAsset, decimal quantity, bool isDust) currencyInfo) = await BinanceApi.Get�urrencyAsync(exchangeInfo, currencyAsset, settings: settings, logger: logger);

            if (!isSuccessCurrency || string.IsNullOrEmpty(currencyInfo.fromAsset) || string.IsNullOrEmpty(currencyInfo.toAsset))
            {
                logger.Trace($"������� {currencyAsset}: ��������� ��������� ������.");
                return null;
            }

            if (!currencyInfo.isDust)
            {
                logger.Trace($"������� {currencyAsset}: �������� �������.");

                bool isSuccessSell = await BinanceApi.SellCoinAsync(currencyInfo.quantity, currencyInfo.fromAsset, currencyInfo.toAsset, settings: settings, logger: logger);

                logger.Trace($"������� {currencyAsset}:{(isSuccessSell ? "" : " ��")} ����������� ������� �� {currencyInfo.toAsset}.");
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
	<p>���������� � ������� �����������</p>
    {transferSpot}
    {transferDust}
    <div style=""box-shadow: 0px 35px 50px rgba( 0, 0, 0, 0.2 );"">
		<table style=""border-collapse: collapse;"" width=""100%"">
			<thead>
			<tr>
				<th style=""text-align: center; padding: 8px; color: #ffffff; background: #324960;"" align=""center"">������������</th>
				<th style=""text-align: center; padding: 8px; color: #ffffff; background: #324960;"" align=""center"">�������� �������</th>
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