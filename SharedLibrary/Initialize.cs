using SharedLibrary.AbstractClasses;
using SharedLibrary.Helper;
using SharedLibrary.Helper.StaticInfo;
using SharedLibrary.Provider;
using SharedLibrary.View;
using SharedLibrary.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace SharedLibrary
{
    public static class Initialize
    {
        public static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Task.Factory.StartNew(async () => await HelperMethods.Message($"Глобальная ошибка: {e.Exception.Message}"));

            var infoVM = SharedProvider.GetFromDictionaryByKey(nameof(InfoViewModel)) as InfoViewModel ?? new InfoViewModel();
            infoVM.UpdateStackTrace(e.Exception.StackTrace);
            e.Handled = true;
        }

        public static bool LicenseVerify()
        {
            string path = System.IO.Path.Combine(Environment.CurrentDirectory, "license.xml");
            if (!System.IO.File.Exists(path))
            {
                throw new ApplicationException($"Ваша копия программы не лицензирована! Не найден файл лицензии License.xml. Обратитесь к автору.");
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            try
            {
                string Name = doc.ChildNodes[0].SelectSingleNode(@"/license/Name", null).InnerText;
                string Date = doc.ChildNodes[0].SelectSingleNode(@"/license/Date", null).InnerText;
                string signature = doc.ChildNodes[0].SelectSingleNode(@"/license/Signature", null).InnerText;
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] data = System.Text.Encoding.UTF8.GetBytes(Name + Date +
                        "B37F00E131967D5888350F2307D26FEA9EA86A451C816C3C1E221DD476279EB3D5CC57B28C85AE8662B379C30545F84CBD4262D6DFB7653B8939D6D28D14D3C4");
                byte[] hash = md5.ComputeHash(data);
                var sigResult = Convert.ToBase64String(hash);

                if (sigResult != signature)
                {
                    throw new ApplicationException($"Ваша копия программы не лицензирована! Ошибка чтения файла лицензии.");
                }
                if (DateTime.Now > Convert.ToDateTime(Date))
                {
                    throw new ApplicationException($"Ваша копия программы не лицензирована! Закончилось время лицензии.");
                }
                return true;
            }
            catch (Exception)
            {
                throw new ApplicationException($"Ваша копия программы не лицензирована! Закончилось время лицензии.");
            }
        }

        public static void Start(object sender, StartupEventArgs e)
        {
            var splashScreen = new SplashScreenWindowView();
            Application.Current.MainWindow = splashScreen;
            splashScreen.Show();

            if (!LicenseVerify())
            {
                return;
            }

            Application.Current.Dispatcher.UnhandledException += OnDispatcherUnhandledException;

            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                //Все вьюшки
                SharedProvider.SetToSingleton(
                InfoKeys.ModulesKey,
                HelperMethods.GetAllInstancesOf(new List<ModuleBase>()).Where(x => x.IsActive).ToList());

                //Все темы
                SharedProvider.SetToSingleton(
                InfoKeys.ThemesKey,
                HelperMethods.GetAllInstancesOf(new List<ThemeBase>()).ToList());

                //Основная ViewModel
                var vm = new MainWindowViewModel();
                SharedProvider.SetToSingleton(nameof(MainWindowViewModel), vm);

                var infovm = new InfoViewModel();
                SharedProvider.SetToSingleton(nameof(InfoViewModel), infovm);

                var apiKey = await HelperMethods.GetByKeyInDB(InfoKeys.ApiKeyBinanceKey);
                SharedProvider.SetToSingleton(InfoKeys.ApiKeyBinanceKey, apiKey?.Value);

                var apiSecret = await HelperMethods.GetByKeyInDB(InfoKeys.ApiSecretBinanceKey);
                SharedProvider.SetToSingleton(InfoKeys.ApiSecretBinanceKey, apiSecret?.Value);

                var binancePercent = await HelperMethods.GetByKeyInDB(InfoKeys.BinancePercentKey);
                SharedProvider.SetToSingleton(InfoKeys.BinancePercentKey, binancePercent?.Value);

                var mainWindow = new MainWindowView();

                mainWindow.DataContext = vm;
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
                splashScreen.Close();
                mainWindow.Closing += async (s, args) =>
                {
                    try
                    {
                        if (vm.SelectedTheme != null)
                            vm.SelectedTheme.Deactivate();

                        if (vm.SelectedThemeDarkOrLight != null)
                            vm.SelectedThemeDarkOrLight.Deactivate();

                        if (vm.SelectedViewModel != null)
                            vm.SelectedViewModel.ModuleBaseItem.Deactivate();

                        Application.Current.Dispatcher.UnhandledException -= OnDispatcherUnhandledException;
                    }
                    catch (Exception ex)
                    {
                        await HelperMethods.Message($"{ex.Message}");
                    }
                };
            });
        }
    }
}