using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Binex.View
{
    /// <summary>
    /// Логика взаимодействия для TestView.xaml
    /// </summary>
    public partial class BinancePayView : UserControl, IDisposable
    {
        public BinancePayView()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
        }
    }
}