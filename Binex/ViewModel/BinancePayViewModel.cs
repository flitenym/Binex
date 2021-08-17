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
        public double? UserID { get; set; }
        public string UserName { get; set; }
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
            LoadInfoCommand.Execute(null);
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
                IsSelectedCommand.Execute(null);
            }
        }

        #endregion

        #region Api данные добавлены

        private bool isSuccessApiData = false;

        public bool IsSuccessApiData
        {
            get { return isSuccessApiData; }
            set
            {
                isSuccessApiData = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsSuccessApiData)));
            }
        }

        #endregion

        #region Сумма всех пользователей

        public string SumAllUsersText => SumAllUsers.HasValue ? $"Сумма для оплаты: {Math.Round(SumAllUsers.Value, 2)}$" : $"Нет загруженных данных.";

        private decimal? sumAllUsers = null;

        public decimal? SumAllUsers
        {
            get { return sumAllUsers; }
            set
            {
                sumAllUsers = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(SumAllUsers)));
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(SumAllUsersText)));
            }
        }

        #endregion

        #region Баланс пользователя

        public string BalanceText => Balance.HasValue ? $"Баланс USDT: {Math.Round(Balance.Value, 2)}$" : $"Нет Binance данных Api.";

        private decimal? balance = null;

        public decimal? Balance
        {
            get { return balance; }
            set
            {
                balance = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Balance)));
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(BalanceText)));
            }
        }

        #endregion

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
            var uniqueScale = await SQLExecutor.SelectExecutorAsync<Scale>($"Unique{scaleTableName}", "order by FromValue desc");

            var paysInfo = await SQLExecutor.SelectExecutorAsync<PayInfo>($@"
WITH VarTable AS (
	SELECT 
        di.ID, 
        di.UserID,
        ui.UserName,
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

                    double percent;

                    if (string.IsNullOrEmpty(payInfo.IsUnique?.Trim()))
                    {
                        percent = scale.FirstOrDefault(x => x.FromValue <= hungPercent)?.Percent ?? defaultPercent;
                    }
                    else
                    {
                        percent = uniqueScale.FirstOrDefault(x => x.FromValue <= hungPercent)?.Percent ?? defaultPercent;
                    }

                    percent /= 100;

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
            var apiData = await BinanceApi.GetApiDataAsync();

            IsSuccessApiData = apiData.IsSuccess;
            BinancePayCommand.RaiseCanExecuteChanged();

            await GetPayInfoDataAsync();

            if (IsSuccessApiData)
            {
                BinanceBalanceCommand.Execute(null);
            }
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

        public AsyncCommand BinancePayCommand => binancePayCommand ?? (binancePayCommand = new AsyncCommand(x => BinancePayAsync(), y => CanPay()));

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

                        var payHistory = new PayHistory() { UserID = payInfo.UserID.ToString(), SendedUsdt = Math.Round(payInfo.UsdtToPay.Value, 4), PayTime = DateTime.Now.ToString(), NumberPay = numberPay };
                        await SQLExecutor.InsertExecutorAsync(payHistory, payHistory);
                    }
                }
            }

            await HelperMethods.Message("Оплата выполнена");

            LoadInfoCommand.Execute(null);
        }

        private bool CanPay()
        {
            return IsSuccessApiData && SumAllUsers.HasValue && SumAllUsers != 0;
        }

        #endregion

        #region Команда для получения баланса USDT

        private AsyncCommand binanceBalanceCommand;

        public AsyncCommand BinanceBalanceCommand => binanceBalanceCommand ?? (binanceBalanceCommand = new AsyncCommand(x => BinanceBalanceAsync()));

        private async Task BinanceBalanceAsync()
        {
            var apiData = await BinanceApi.GetApiDataAsync();

            if (!apiData.IsSuccess)
            {
                return;
            }

            var coinInfo = await BinanceApi.GetCoinAsync(StaticClass.USDT);

            if (coinInfo.Currency == null)
            {
                await HelperMethods.Message($"Ошибка. Не найден {StaticClass.USDT}");
                return;
            }

            Balance = coinInfo.Currency.Free;
        }

        #endregion

        #region Команда для получения суммы всех пользователей

        private RelayCommand isSelectedCommand;

        public RelayCommand IsSelectedCommand => isSelectedCommand ?? (isSelectedCommand = new RelayCommand(x => IsSelected()));

        private void IsSelected()
        {
            SumAllUsers = PayInfoCollection.Any() ? PayInfoCollection.Where(x => x.IsSelected && !string.IsNullOrEmpty(x.Address) && x.UsdtToPay.HasValue).Sum(x => x.UsdtToPay) : null;
        }

        #endregion

        #region Команда для получения суммы всех пользователей

        private AsyncCommand testCommand;

        public AsyncCommand TestCommand => testCommand ?? (testCommand = new AsyncCommand(x => TestMehtod()));

        private async Task TestMehtod()
        {
            //var x = await BinanceApi.GetExchangeInfo();
        }

        #endregion

        
    }
}