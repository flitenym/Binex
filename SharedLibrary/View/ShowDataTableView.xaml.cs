using SharedLibrary.Controls;

namespace SharedLibrary.View
{
    /// <summary>
    /// Логика взаимодействия для ShowDataTableView.xaml
    /// </summary>
    public partial class ShowDataTableView : WindowControl
    {
        public ShowDataTableView()
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