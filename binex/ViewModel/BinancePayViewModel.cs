using Binance.Net;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.MarketData;
using Binance.Net.Objects.Spot.SpotData;
using Binex.Api;
using Binex.Helper.StaticInfo;
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
            Task.Factory.StartNew(async () => await GetMainInfoAsync());
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



        #region Вычисление валюты

        public async Task<bool> GetCurrencyAsync()
        {
            try
            {
                DateTime endDate = new DateTime(DateTime.UtcNow.Date.Year, DateTime.UtcNow.Date.Month, DateTime.UtcNow.Date.Day, 0, 0, 0);
                DateTime startDate = endDate.AddDays(-6);

                (bool IsSuccess, decimal? Average) = await BinanceApi.GetAverageBetweenCurreniesAsync(
                    StaticClass.BTC,
                    StaticClass.USDT,
                    Binance.Net.Enums.KlineInterval.OneDay,
                    startDate,
                    endDate);

                if (!IsSuccess)
                {
                    return false;
                }

                UsdtCurrencyByBTC = Average.Value;

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

        public async Task GetPayInfoDataAsync()
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

        public async Task<bool> GetMainInfoAsync()
        {
            if (!await GetCurrencyAsync()) return false;
            await GetPayInfoDataAsync();
            return true;
        }

        #endregion

        #region Комманда для методов

        private AsyncCommand updateDataCommand;

        public AsyncCommand UpdateDataCommand => updateDataCommand ?? (updateDataCommand = new AsyncCommand(x => UpdateDataAsync()));

        private async Task UpdateDataAsync()
        {
            if (await GetMainInfoAsync())
            {
                await HelperMethods.Message("Данные обновлены");
            }
        }

        #endregion

        #region Комманда для оплаты

        private AsyncCommand binancePayCommand;

        public AsyncCommand BinancePayCommand => binancePayCommand ?? (binancePayCommand = new AsyncCommand(x => BinancePayAsync()));

        private async Task BinancePayAsync()
        {
            
        }

        #endregion
    }
}