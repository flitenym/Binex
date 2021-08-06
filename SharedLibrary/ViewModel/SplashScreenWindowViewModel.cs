using SharedLibrary.Helper;
using System.ComponentModel;
using System.Reflection;

namespace SharedLibrary.ViewModel
{
    public class SplashScreenWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public string AppVersion => HelperMethods.GetVersion();  
    }
}
