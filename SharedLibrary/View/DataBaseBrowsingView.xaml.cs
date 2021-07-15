using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;


namespace SharedLibrary.View
{
    public partial class DataBaseBrowsingView : UserControl, IDisposable
    {
        public DataBaseBrowsingView()
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
