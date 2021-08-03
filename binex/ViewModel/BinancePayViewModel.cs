using Binance.Net.Objects.Spot.WalletData;
using Binex.Api;
using Binex.Helper.StaticInfo;
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
        public decimal AgentEarnUsdt { get; set; }
        public decimal? UsdtToPay { get; set; }
        public string Address { get; set; }
        public string IsUnique { get; set; }
        public bool IsSelected { get; set; }
        public bool IsPaid { get; set; }
        public PayInfo ShallowCopy()
        {
            return (PayInfo)this.MemberwiseClone();
        }
    }

    public class Scale
    {
        public int ID { get; set; }
        public double FromValue { get; set; }
        public double Percent { get; set; }
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

        #endregion

        #region Получение данных

        private async Task GetMainInfoAsync()
        {
            var apiData = await BinanceApi.GetApiDataAsync();

            if (!apiData.IsSuccess)
            {
                return;
            }
            LoadInfoCommand.Execute(null);
        }

        #endregion

        #region Вычисление данные в таблице

        private List<PayInfo> ConcatLists(List<PayInfo> first, List<PayInfo> second)
        {
            List<PayInfo> result = new List<PayInfo>(first);

            foreach (var secondItem in second)
            {
                var itemIndex = result.FindLastIndex(x => x.UserID == secondItem.UserID);
                if (itemIndex == -1)
                {
                    result.Add(secondItem);
                }
                else
                {
                    result[itemIndex].AgentEarnBtc += secondItem.AgentEarnBtc;
                    result[itemIndex].AgentEarnUsdt += secondItem.AgentEarnUsdt;
                    result[itemIndex].UsdtToPay += secondItem.UsdtToPay;
                }
            }

            return result;
        }

        private async Task<List<PayInfo>> GetResultPaysInfo(string dataInfoTableName, string scaleTableName, double settingsPercent, double defaultPercent)
        {
            var scale = await SQLExecutor.SelectExecutorAsync<Scale>(scaleTableName, "order by FromValue desc");

            var paysInfo = await SQLExecutor.SelectExecutorAsync<PayInfo>($@"
WITH VarTable AS (
	SELECT 
        di.ID, 
        di.UserID, 
        di.AgentEarnBtc,
        di.AgentEarnUsdt,
        ui.Address, 
        ui.IsUnique, 
        True as IsSelected, 
        case di.IsPaid
            when 'Нет'
                then False
            else True
        end IsPaid
    FROM {dataInfoTableName} di
	LEFT JOIN UserInfo ui ON ui.UserID = di.UserID
	WHERE LoadingDateTime IN (SELECT LoadingDateTime FROM {dataInfoTableName} GROUP BY LoadingDateTime LIMIT 2) 
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
                if (payInfo.IsPaid) { continue; }
                if (!resultPayInfo.Any(x => x.UserID == payInfo.UserID))
                {
                    var allUserInfo = paysInfo.Where(x => x.UserID == payInfo.UserID && !x.IsPaid);
                    var payInfoData = payInfo.ShallowCopy();
                    var first = allUserInfo.First();
                    // вычисление оплаты btc
                    var lastBtc = allUserInfo.LastOrDefault(x => x.ID != first.ID)?.AgentEarnBtc ?? 0;
                    payInfoData.AgentEarnBtc = Math.Abs(first.AgentEarnBtc - lastBtc);
                    // вычисление оплаты usdt
                    var lastUsdt = allUserInfo.LastOrDefault(x => x.ID != first.ID)?.AgentEarnUsdt ?? 0;
                    payInfoData.AgentEarnUsdt = Math.Abs(first.AgentEarnUsdt - lastUsdt);

                    var hungPercent = (double)payInfoData.AgentEarnUsdt / settingsPercent * 100;
                    var percent = string.IsNullOrEmpty(payInfo.IsUnique?.Trim()) ? (scale.FirstOrDefault(x => x.FromValue <= hungPercent)?.Percent ?? defaultPercent) / 100 : settingsPercent / 100;

                    payInfoData.UsdtToPay = (decimal)hungPercent * (decimal)percent;

                    resultPayInfo.Add(payInfoData);
                }
            }

            return resultPayInfo;
        }

        public async Task GetPayInfoDataAsync()
        {
            double binancePercent;
            double binanceFuturesPercent;

            if (!(SharedProvider.GetFromDictionaryByKey(InfoKeys.BinancePercentKey) is string binancePercentValue &&
                double.TryParse(binancePercentValue.Replace('.', ','), out binancePercent)))
            {
                await HelperMethods.Message("Не задан процент по умолчанию в настройках");
                return;
            }

            if (!(SharedProvider.GetFromDictionaryByKey(InfoKeys.BinanceFuturesPercentKey) is string binanceFuturesPercentValue &&
                double.TryParse(binanceFuturesPercentValue.Replace('.', ','), out binanceFuturesPercent)))
            {
                await HelperMethods.Message("Не задан процент фьючерс по умолчанию в настройках");
                return;
            }

            var payInfo = await GetResultPaysInfo(nameof(DataInfo), nameof(ScaleInfo), binancePercent, StaticClass.DefaultUniquePercent);
            var futuresPayInfo = await GetResultPaysInfo(nameof(FuturesDataInfo), nameof(FuturesScaleInfo), binanceFuturesPercent, StaticClass.DefaultUniqueFuturesPercent);

            var resultPayInfo = ConcatLists(payInfo, futuresPayInfo);

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

        #region Загрузка данных

        private AsyncCommand loadInfoCommand;

        public AsyncCommand LoadInfoCommand => loadInfoCommand ?? (loadInfoCommand = new AsyncCommand(x => LoadInfoAsync()));

        public async Task LoadInfoAsync()
        {
            await GetPayInfoDataAsync();
        }

        #endregion

        #region Комманда для методов

        private AsyncCommand updateDataCommand;

        public AsyncCommand UpdateDataCommand => updateDataCommand ?? (updateDataCommand = new AsyncCommand(x => UpdateDataAsync()));

        private async Task UpdateDataAsync()
        {
            await LoadInfoAsync();
            await HelperMethods.Message("Данные обновлены");
        }

        #endregion

        #region Комманда для оплаты

        private AsyncCommand binancePayCommand;

        public AsyncCommand BinancePayCommand => binancePayCommand ?? (binancePayCommand = new AsyncCommand(x => BinancePayAsync()));

        private async Task BinancePayAsync()
        {
            if (!PayInfoCollection.Any(x => x.IsSelected))
            {
                await HelperMethods.Message($"Не выбраны данные для отправки");
                return;
            }

            var coinInfo = await BinanceApi.GetCoinAsync(StaticClass.USDT);

            if (coinInfo.Currency == null)
            {
                await HelperMethods.Message($"Ошибка. Не найден {StaticClass.USDT}");
                return;
            }

            BinanceNetwork network = null;

            if (coinInfo.Currency.NetworkList.Any())
            {
                foreach (var networkInfo in coinInfo.Currency.NetworkList)
                {
                    if (networkInfo.Network == StaticClass.USDT_ADDRESS)
                    {
                        network = networkInfo;
                    }
                }
            }

            if (network == null)
            {
                await HelperMethods.Message($"Не удалось определить сеть для вывода");
                return;
            }

            if (!network.WithdrawEnabled)
            {
                await HelperMethods.Message($"Для {network.Network} запрещен вывод");
                return;
            }

            var numberPay = await SQLExecutor.SelectFirstExecutorAsync<int?>("SELECT NumberPay FROM PayHistory ORDER BY NumberPay DESC LIMIT 1") ?? 1;

            foreach (var payInfo in PayInfoCollection)
            {
                if (payInfo.IsSelected && !string.IsNullOrEmpty(payInfo.Address) && payInfo.UsdtToPay.HasValue && payInfo.UsdtToPay >= network.WithdrawMin)
                {
                    var isSuccessWithrawal = await BinanceApi.WithdrawalPlacedAsync(StaticClass.USDT, StaticClass.USDT, payInfo.UsdtToPay.Value, payInfo.Address, network.Network);
                    if (isSuccessWithrawal)
                    {
                        await SQLExecutor.QueryExecutorAsync($@"
UPDATE {nameof(DataInfo)}
SET IsPaid = 'Да'
WHERE UserID = {payInfo.UserID} and IsPaid = 'Нет'
");
                        await SQLExecutor.QueryExecutorAsync($@"
UPDATE {nameof(FuturesDataInfo)}
SET IsPaid = 'Да'
WHERE UserID = {payInfo.UserID} and IsPaid = 'Нет'
");

                        var payHistory = new PayHistory() { UserID = payInfo.UserID, SendedUsdt = Math.Round(payInfo.UsdtToPay.Value, 4), PayTime = DateTime.Now.ToString(), NumberPay = numberPay };
                        await SQLExecutor.InsertExecutorAsync(payHistory, payHistory);
                    }
                }
            }

            await HelperMethods.Message("Оплата выполнена");

            LoadInfoCommand.Execute(null);
        }

        #endregion
    }
}