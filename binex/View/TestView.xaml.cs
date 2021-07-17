using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace binex.View
{
    /// <summary>
    /// Логика взаимодействия для TestView.xaml
    /// </summary>
    public partial class TestView : UserControl, IDisposable
    {
        public TestView()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
        }
    }
}