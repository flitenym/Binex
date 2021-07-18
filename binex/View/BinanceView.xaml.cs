using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace binex.View
{
    /// <summary>
    /// Логика взаимодействия для BinanceView.xaml
    /// </summary>
    public partial class BinanceView : UserControl, IDisposable
    {
        public BinanceView()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
        }
    }
}