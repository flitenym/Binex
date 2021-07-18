using Binance.Net;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.WalletData;
using Binance.Net.SubClients;
using CryptoExchange.Net.Authentication;
using SharedLibrary.Helper;
using System.ComponentModel;
using System.Reflection;

namespace binex.ViewModel
{
    public class TestViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public string ApiKey => "Jo0NUe99OoQnUBXnMp5VukUQR3jN2U5RMCbL6cZBYt7SQ2HU2MEx6WkRyPJvM0wt";
        public string ApiSecret => "vsRyg5fjXjiR8pP02iVpXEnmcemiMczsCDcvrb1g3PL7ky8EWJIE2znNvBzJPFfN";
        public string Address => "0x8d45a948624ac5fe2326596f4a7d040fb3e8a85e";

        public TestViewModel()
        {
            var order = new BinanceWithdrawalPlaced()
            {
                Success = true,
                Message = "Test"
            };

            var options = new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(ApiKey, ApiSecret),
                AutoTimestamp = false
            };

            var client = new BinanceClient(options);
            decimal amount = 9.6E-6m;
            //var result = client.WithdrawDeposit.Withdraw("BTC", Address, amount, network: "BSC");
        }
    }
}
