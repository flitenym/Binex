using SharedLibrary.AbstractClasses;
using SharedLibrary.Helper;
using SharedLibrary.Helper.StaticInfo;
using SharedLibrary.Provider;
using SharedLibrary.View;
using SharedLibrary.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SharedLibrary
{
    public static class Initialize
    {
        public static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Task.Factory.StartNew(() => HelperMethods.Message($"Глобальная ошибка: {e.Exception.Message}"));

            var infoVM = SharedProvider.GetFromDictionaryByKey(nameof(InfoViewModel)) as InfoViewModel ?? new InfoViewModel();
            infoVM.UpdateStackTrace(e.Exception.StackTrace);
            e.Handled = true;
        }

        public static void Start(object sender, StartupEventArgs e)
        {
            Application.Current.Dispatcher.UnhandledException += OnDispatcherUnhandledException;

            var splashScreen = new SplashScreenWindowView();
            Application.Current.MainWindow = splashScreen;
            splashScreen.Show();

            Task.Factory.StartNew(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    //Все вьюшки
                    SharedProvider.SetToSingleton(
                        InfoKeys.ModulesKey,
                        HelperMethods.GetAllInstancesOf<ModuleBase>(new List<ModuleBase>()).Where(x => x.IsActive).ToList());

                    //Все темы
                    SharedProvider.SetToSingleton(
                        InfoKeys.ThemesKey,
                        HelperMethods.GetAllInstancesOf<ThemeBase>(new List<ThemeBase>()).ToList());

                    //Основная ViewModel
                    var vm = new MainWindowViewModel();
                    SharedProvider.SetToSingleton(nameof(MainWindowViewModel), vm);

                    var infovm = new InfoViewModel();
                    SharedProvider.SetToSingleton(nameof(InfoViewModel), infovm);

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
            });
        }
    }
}
