using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;


namespace SharedLibrary.View
{
    public partial class LocalDataBaseView : UserControl, IDisposable
    {
        public LocalDataBaseView()
        {
            InitializeComponent();
            DataGridTable.AutoGeneratingColumn += Helper.HelperMethods.DataGrid_AutoGeneratingColumn;
        }

        public void Dispose()
        {
            DataGridTable.AutoGeneratingColumn -= Helper.HelperMethods.DataGrid_AutoGeneratingColumn;
        }
    }
}
