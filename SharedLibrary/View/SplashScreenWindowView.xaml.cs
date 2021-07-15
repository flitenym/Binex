using System.Windows;
using System.Windows.Input;
using SharedLibrary.ViewModel;

namespace SharedLibrary.View
{
    /// <summary>
    /// Логика взаимодействия для SplashScreen.xaml
    /// </summary>
    public partial class SplashScreenWindowView : Window
    {
        public SplashScreenWindowView()
        {
            InitializeComponent(); 
            this.DataContext = new SplashScreenWindowViewModel();
        } 

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}
