using ExcelDataReader;
using SharedLibrary.AbstractClasses;
using SharedLibrary.Commands;
using SharedLibrary.Helper;
using SharedLibrary.LocalDataBase;
using SharedLibrary.LocalDataBase.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SharedLibrary.ViewModel
{
    public class ModalOKViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public ModalOKViewModel(string message)
        {
            Message = message;
        }

        #region Fields

        #region Обновить лицензию

        private string message;

        public string Message
        {
            get
            {
                return message;
            }
            set
            {
                message = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Message)));
            }
        }

        #endregion

        #endregion

        #region Команда для закрытия

        private RelayCommand okCommand;
        public RelayCommand OkCommand => okCommand ?? (okCommand = new RelayCommand(obj => CancelFunction(obj)));

        public void CancelFunction(object obj)
        {
            (obj as System.Windows.Window).DialogResult = false;
        }

        #endregion
    }
}
