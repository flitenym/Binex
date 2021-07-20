using Binance.Net;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.WalletData;
using Binance.Net.SubClients;
using CryptoExchange.Net.Authentication;
using SharedLibrary.Commands;
using SharedLibrary.Helper;
using SharedLibrary.LocalDataBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace binex.ViewModel
{
    public class PayInfo
    {
        public int ID { get; set; }
        public string UserID { get; set; }
        public decimal AgentEarnBtc { get; set; }
        public string BTS { get; set; }
        public bool IsSelected { get; set; }
        public PayInfo ShallowCopy()
        {
            return (PayInfo)this.MemberwiseClone();
        }
    }

    public class BinancePayViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        //public string ApiKey => "Jo0NUe99OoQnUBXnMp5VukUQR3jN2U5RMCbL6cZBYt7SQ2HU2MEx6WkRyPJvM0wt";
        //public string ApiSecret => "vsRyg5fjXjiR8pP02iVpXEnmcemiMczsCDcvrb1g3PL7ky8EWJIE2znNvBzJPFfN";
        //public string Address => "0x8d45a948624ac5fe2326596f4a7d040fb3e8a85e";

        public BinancePayViewModel()
        {
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
            ////var result = client.WithdrawDeposit.Withdraw("BTC", Address, amount, network: "BSC");
            ///
            Task.Factory.StartNew(async () => await SetTransList());
        }

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

        public async Task SetTransList()
        {
            var paysInfo = await SQLExecutor.SelectExecutorAsync<PayInfo>(@"
WITH VarTable AS (
	SELECT di.ID, di.UserID, di.AgentEarnBtc, ui.BTS, True as IsSelected FROM DataInfo di
	LEFT JOIN UserInfo ui ON ui.UserID = di.UserID
	WHERE LoadingDateTime IN (SELECT LoadingDateTime FROM DataInfo GROUP BY LoadingDateTime LIMIT 2) 
)

select * from VarTable
WHERE UserID in 
(
select UserID from VarTable
GROUP BY UserID
HAVING(count(UserID) > 1)
)
");
            List<PayInfo> resultPayInfo = new List<PayInfo>();

            foreach (var payInfo in paysInfo)
            {
                if (!resultPayInfo.Any(x => x.UserID == payInfo.UserID))
                {
                    var allUserInfo = paysInfo.Where(x => x.UserID == payInfo.UserID);
                    var payInfoData = payInfo.ShallowCopy();
                    payInfoData.AgentEarnBtc = Math.Abs(allUserInfo.First().AgentEarnBtc - allUserInfo.Last().AgentEarnBtc);
                    resultPayInfo.Add(payInfoData);
                }
            }

            foreach(var payInfoCollectionItem in PayInfoCollection)
            {
                if (payInfoCollectionItem.IsSelected == false)
                {
                    foreach(var resultPayInfoItem in resultPayInfo)
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

        #region Комманда для методов

        private AsyncCommand updateDataCommand;

        public AsyncCommand UpdateDataCommand => updateDataCommand ?? (updateDataCommand = new AsyncCommand(x => UpdateData()));

        private async Task UpdateData()
        {
            await SetTransList();
            await HelperMethods.Message("Данные обновлены");
        }

        #endregion
    }
}