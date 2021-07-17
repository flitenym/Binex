using Binance.Net.SubClients;
using SharedLibrary.Helper;
using System.ComponentModel;
using System.Reflection;

namespace binex.ViewModel
{
    public class TestViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public string ApiKey => "";

        public TestViewModel()
        {
            new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials("Test", "Test"),
                AutoTimestamp = false
            };
        }
    }
}
