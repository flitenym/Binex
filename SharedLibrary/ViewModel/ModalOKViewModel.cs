using SharedLibrary.Commands;
using System.ComponentModel;

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

        #region Сообщение

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
