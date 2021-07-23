using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using SharedLibrary.Commands;
using SharedLibrary.DataBase;
using SharedLibrary.LocalDataBase;
using SharedLibrary.LocalDataBase.Models;
using SharedLibrary.View;
using SharedLibrary.Provider;

namespace SharedLibrary.ViewModel
{
    public class InfoViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public InfoViewModel()
        {
            //При инициализации будем удалять все записи по ошибкам если дата ошибки уже больше недели
            DeleteStackTraceCommand.Execute(null);
        }

        public ObservableCollection<string> ActiveTasks { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<Errors> StackTraces { get; set; } = new ObservableCollection<Errors>();

        public void UpdateActiveTasks(Func<object, Task> task, bool isAdd)
        {
            var funcName = task.Method.Name.Substring(0, task.Method.Name.IndexOf('>')).Replace("<get_", "");

            if (isAdd)
            {
                ActiveTasks.Add(funcName);
            }
            else
            {
                var alreadyExist = ActiveTasks.FirstOrDefault(x => x == funcName);
                if (alreadyExist != null)
                {
                    ActiveTasks.Remove(alreadyExist);
                }
            }

            PropertyChanged(this, new PropertyChangedEventArgs(nameof(ActiveTasks)));
        }

        public void UpdateStackTrace(string stackTrace = null)
        {
            if (!string.IsNullOrEmpty(stackTrace))
            {
                InsertStackTraceCommand.Execute(stackTrace);
            }
            else
            {
                SelectStackTraceCommand.Execute(null);
            }
        }

        private AsyncCommand insertStackTraceCommand;
        public AsyncCommand InsertStackTraceCommand => insertStackTraceCommand ?? (insertStackTraceCommand = new AsyncCommand(x => InsertStackTraceAsync(x as string)));

        public async Task InsertStackTraceAsync(string str)
        {
            var newStackTrace = new Errors() { Date = DateTime.Now, StackTrace = str };
            await SQLExecutor.InsertExecutorAsync(newStackTrace, newStackTrace);

            SelectStackTraceCommand.Execute(null);
        }

        private AsyncCommand selectStackTraceCommand;
        public AsyncCommand SelectStackTraceCommand => selectStackTraceCommand ?? (selectStackTraceCommand = new AsyncCommand(x => SelectStackTraceAsync()));

        public async Task SelectStackTraceAsync()
        {
            var DBstackRaces = (await SQLExecutor.SelectExecutorAsync<Errors>(nameof(Errors), "order by Date desc LIMIT 10"));
            StackTraces = new ObservableCollection<Errors>(DBstackRaces);

            PropertyChanged(this, new PropertyChangedEventArgs(nameof(StackTraces)));
        }

        private AsyncCommand deleteStackTraceCommand;
        public AsyncCommand DeleteStackTraceCommand => deleteStackTraceCommand ?? (deleteStackTraceCommand = new AsyncCommand(x => DeleteStackTraceAsync()));

        public async Task DeleteStackTraceAsync()
        {
            await SQLExecutor.DeleteExecutorAsync(nameof(Errors), "where (julianday('now','localtime') - julianday(Date))>=7");

            SelectStackTraceCommand.Execute(null);
        }
    }
}