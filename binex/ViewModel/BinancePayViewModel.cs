using Binance.Net;
using Binance.Net.Objects.Spot;
using CryptoExchange.Net.Authentication;
using SharedLibrary.Commands;
using SharedLibrary.Helper;
using SharedLibrary.Helper.StaticInfo;
using SharedLibrary.LocalDataBase;
using SharedLibrary.LocalDataBase.Models;
using SharedLibrary.Provider;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Binex.ViewModel
{
    public class PayInfo
    {
        public int ID { get; set; }
        public string UserID { get; set; }
        public decimal AgentEarnBtc { get; set; }
        public decimal? AgentEarnUSDT { get; set; }
        public decimal? UsdtToPay { get; set; }
        public string BTS { get; set; }
        public string IsUnique { get; set; }
        public bool IsSelected { get; set; }
        public PayInfo ShallowCopy()
        {
            return (PayInfo)this.MemberwiseClone();
        }
    }

    public class BinancePayViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public BinancePayViewModel()
        {
            Task.Factory.StartNew(async () => await GetMainInfo());
        }

        #region Fields

        #region Информация для оплаты

        private ObservableCollection<PayInfo> payInfoCollection = new ObservableCollection<PayInfo>();

        public ObservableCollection<PayInfo> PayInfoCollection
        {
            get { return payInfoCollection; }
            set
            {
                payInfoCollection = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(PayInfoCollection)));
            }
        }

        #endregion

        #region USDT относительно BTC

        private decimal usdtCurrencyByBTC;

        public decimal UsdtCurrencyByBTC
        {
            get { return usdtCurrencyByBTC; }
            set
            {
                usdtCurrencyByBTC = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(UsdtCurrencyByBTC)));
            }
        }

        #endregion

        #endregion

        private async Task<(bool IsSuccess, string ApiKey, string ApiSecret)> GetApiData()
        {
            if (SharedProvider.GetFromDictionaryByKey(InfoKeys.ApiKeyBinanceKey) is string apiKeyValue &&
                SharedProvider.GetFromDictionaryByKey(InfoKeys.ApiSecretBinanceKey) is string apiSecretValue)
            {
                return (true, apiKeyValue, apiSecretValue);
            }

            await HelperMethods.Message("Не удалось получить данные Api, проверьте настройки");
            return (false, null, null);
        }

        #region Вычисление валюты

        public async Task<bool> GetCurrency()
        {
            try
            {
                var apiData = await GetApiData();

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

                DateTime endDate = new DateTime(DateTime.UtcNow.Date.Year, DateTime.UtcNow.Date.Month, DateTime.UtcNow.Date.Day, 0, 0, 0);
                DateTime startDate = endDate.AddDays(-6);

                var data = await client.Spot.Market.GetKlinesAsync("BTCUSDT", Binance.Net.Enums.KlineInterval.OneDay, startDate, endDate);

                if (!data.Success)
                {
                    await HelperMethods.Message("Не удалось получить данные по валюте");
                    return false;
                }

                UsdtCurrencyByBTC = data.Data.Average(x => x.Open);

                return true;
            }
            catch (Exception ex)
            {
                await HelperMethods.Message($"Вычисление валюты {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Вычисление данные в таблице

        public async Task GetPayInfoData()
        {
            var scaleInfo = await SQLExecutor.SelectExecutorAsync<ScaleInfo>(nameof(ScaleInfo), "order by FromValue desc");

            double binancePercent;

            if (!(SharedProvider.GetFromDictionaryByKey(InfoKeys.BinancePercentKey) is string binancePercentValue &&
                double.TryParse(binancePercentValue.Replace('.', ','), out binancePercent)))
            {
                await HelperMethods.Message("Не задан процент по умолчанию в настройках");
                return;
            }

            var paysInfo = await SQLExecutor.SelectExecutorAsync<PayInfo>(@"
WITH VarTable AS (
	SELECT di.ID, di.UserID, di.AgentEarnBtc, ui.BTS, ui.IsUnique, True as IsSelected FROM DataInfo di
	LEFT JOIN UserInfo ui ON ui.UserID = di.UserID
	WHERE LoadingDateTime IN (SELECT LoadingDateTime FROM DataInfo GROUP BY LoadingDateTime LIMIT 2) 
)

select * from VarTable
WHERE UserID in 
(
select UserID from VarTable
GROUP BY UserID
)
");
            List<PayInfo> resultPayInfo = new List<PayInfo>();

            foreach (var payInfo in paysInfo)
            {
                if (!resultPayInfo.Any(x => x.UserID == payInfo.UserID))
                {
                    var allUserInfo = paysInfo.Where(x => x.UserID == payInfo.UserID);
                    var payInfoData = payInfo.ShallowCopy();
                    var first = allUserInfo.First();
                    var lastBtc = allUserInfo.LastOrDefault(x => x.ID != first.ID)?.AgentEarnBtc ?? 0;
                    payInfoData.AgentEarnBtc = Math.Abs(allUserInfo.First().AgentEarnBtc - lastBtc);
                    payInfoData.AgentEarnUSDT = payInfoData.AgentEarnBtc * UsdtCurrencyByBTC;
                    var hungPercent = (double)payInfoData.AgentEarnUSDT.Value / binancePercent * 100;
                    var percent = string.IsNullOrEmpty(payInfo.IsUnique?.Trim()) ? (scaleInfo.FirstOrDefault(x => x.FromValue <= hungPercent)?.Percent ?? 20) / 100 : binancePercent / 100;

                    payInfoData.UsdtToPay = (decimal)hungPercent * (decimal)percent;

                    resultPayInfo.Add(payInfoData);
                }
            }

            foreach (var payInfoCollectionItem in PayInfoCollection)
            {
                if (payInfoCollectionItem.IsSelected == false)
                {
                    foreach (var resultPayInfoItem in resultPayInfo)
                    {
                        if (resultPayInfoItem.UserID == payInfoCollectionItem.UserID)
                        {
                            resultPayInfoItem.IsSelected = false;
                        }
                    }
                }
            }

            PayInfoCollection = new ObservableCollection<PayInfo>(resultPayInfo);
        }

        #endregion

        #region Получение основной информации для оплаты

        public async Task<bool> GetMainInfo()
        {
            if (!await GetCurrency()) return false;
            await GetPayInfoData();
            return true;
        }

        #endregion

        #region Комманда для методов

        private AsyncCommand updateDataCommand;

        public AsyncCommand UpdateDataCommand => updateDataCommand ?? (updateDataCommand = new AsyncCommand(x => UpdateData()));

        private async Task UpdateData()
        {
            if (await GetMainInfo())
            {
                await HelperMethods.Message("Данные обновлены");
            }
        }

        #endregion

        #region Комманда для оплаты

        private AsyncCommand binancePayCommand;

        public AsyncCommand BinancePayCommand => binancePayCommand ?? (binancePayCommand = new AsyncCommand(x => BinancePay()));

        private async Task BinancePay()
        {
            var apiData = await GetApiData();

            if (!apiData.IsSuccess)
            {
                return;
            }

            //var order = new BinanceWithdrawalPlaced()
            //{
            //    Success = true,
            //    Message = "Test"
            //};

            //var options = new BinanceClientOptions()
            //{
            //    ApiCredentials = new ApiCredentials(ApiKey, ApiSecret),
            //    AutoTimestamp = false
            //};

            //var client = new BinanceClient(options);
            //decimal amount = 9.6E-6m;
            //var result = client.WithdrawDeposit.Withdraw("BTC", Address, amount, network: "BSC");

            await HelperMethods.Message("Оплата совершена");
            await HelperMethods.Message("Оплата совершена");
        }

        #endregion
    }
}