using System.Windows;
using SharedLibrary;

namespace binex
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Initialize.Start(sender, e);
        }
    }
}