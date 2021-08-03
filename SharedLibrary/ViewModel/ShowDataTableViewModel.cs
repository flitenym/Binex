using SharedLibrary.Helper;
using System.ComponentModel;
using System.Data;
using System.Reflection;

namespace SharedLibrary.ViewModel
{
    public class ShowDataTableViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public ShowDataTableViewModel(DataTable table)
        {
            TableData = table;
        }

        #region Отображение данных в таблице

        private DataTable tableData;

        public DataTable TableData
        {
            get { return tableData; }
            set
            {
                if (value == null) return;
                tableData?.Clear();
                tableData = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(TableData)));
            }
        }

        #endregion

    }
}
