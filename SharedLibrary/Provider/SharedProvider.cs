using SharedLibrary.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using SharedLibrary.View;
using System.ComponentModel;

namespace SharedLibrary.Provider
{
    public static class SharedProvider
    {
        public static void SetToSingleton(string key, object obj)
        {
            Singleton.GetInstance().SetToDictionary(key, obj);
        }

        public static object GetFromDictionaryByKey(string key)
        {
            return Singleton.GetInstance().GetFromDictionary(key);
        }

        public static void SetActiveTask(Func<object, Task> activeTask)
        {
            Singleton.GetInstance().SetToActiveTask(activeTask);

            (GetFromDictionaryByKey(nameof(InfoViewModel)) as InfoViewModel)?.UpdateActiveTasks(activeTask, true);
        }

        public static void RemoveActiveTask(Func<object, Task> activeTask)
        {
            Singleton.GetInstance().RemoveFromActiveTask(activeTask);

            (GetFromDictionaryByKey(nameof(InfoViewModel)) as InfoViewModel)?.UpdateActiveTasks(activeTask, false);
        }

        public static bool AnyActiveTask()
        {
            return Singleton.GetInstance().GetActiveTasks().Any();
        }

        public static List<Func<object, Task>> GetActiveTask()
        {
            return Singleton.GetInstance().GetActiveTasks().ToList();
        }
    }

    public class Singleton
    {
        private static readonly Lazy<Singleton> lazy = new Lazy<Singleton>(() => new Singleton());

        private readonly Dictionary<string, object> Mapping = new Dictionary<string, object>();

        private readonly ObservableCollection<Func<object, Task>> ActiveTasks;

        private Singleton()
        {
            ActiveTasks = new ObservableCollection<Func<object, Task>>();
            ActiveTasks.CollectionChanged += CollectionChanged;
        }

        ~Singleton()
        {
            ActiveTasks.CollectionChanged -= CollectionChanged;
        }

        public static Singleton GetInstance()
        {
            return lazy.Value;
        }

        #region ActiveTasks

        public ObservableCollection<Func<object, Task>> GetActiveTasks()
        {
            return ActiveTasks;
        }

        public void SetToActiveTask(Func<object, Task> activeTask)
        {
            ActiveTasks.Add(activeTask);
        }
        public void RemoveFromActiveTask(Func<object, Task> activeTask)
        {
            ActiveTasks.Remove(activeTask);
        }

        public void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (SharedProvider.GetFromDictionaryByKey(nameof(MainWindowViewModel)) is MainWindowViewModel mainWindowViewModel)
            {
                //просто затригерим выполнение "set" у "IsLoadingPanelVisible", могли и false написать, без разницы.
                mainWindowViewModel.IsLoadingPanelVisible = true;
            }
        }

        #endregion

        #region Dictionary

        public void SetToDictionary(string key, object obj)
        {
            if (!Mapping.ContainsKey(key))
            {
                Mapping.Add(key, obj);
            }
        }

        public object GetFromDictionary(string key)
        {
            if (Mapping.TryGetValue(key, out var obj))
            {
                return obj;
            }

            return null;
        }

        #endregion
    }
}
