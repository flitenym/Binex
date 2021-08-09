using SharedLibrary.Commands;
using System.ComponentModel;

namespace SharedLibrary.ViewModel
{
    public class ModalYesNoViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public ModalYesNoViewModel(string message)
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

        #region Команда Нет

        private RelayCommand noCommand;
        public RelayCommand NoCommand => noCommand ?? (noCommand = new RelayCommand(obj => NoFunction(obj)));

        public void NoFunction(object obj)
        {
            (obj as System.Windows.Window).DialogResult = false;
        }

        #endregion

        #region Команда Да

        private RelayCommand yesCommand;
        public RelayCommand YesCommand => yesCommand ?? (yesCommand = new RelayCommand(obj => YesFunction(obj)));

        public void YesFunction(object obj)
        {
            (obj as System.Windows.Window).DialogResult = true;
        }

        #endregion
    }
}
