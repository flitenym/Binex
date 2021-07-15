using System;
using System.Windows;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.AbstractClasses
{
    public abstract class ThemeBase
    {
        public abstract string Title { get; }

        public abstract string Name { get; }

        public abstract int Num { get; }

        public abstract ThemeBaseClasses ThemeClass { get; }

        public abstract string UriPath { get; }

        private ResourceDictionary resource;

        public ResourceDictionary ResourceDictionary
        {
            get
            {
                if (resource == null)
                {
                    resource = CreateView();
                }
                return resource;
            }
        }

        public void Deactivate()
        {
            // очищаем коллекцию ресурсов приложения
            Application.Current.Resources.MergedDictionaries.Remove(ResourceDictionary);
        }

        public void SelectTheme()
        {
            // добавляем загруженный словарь ресурсов
            Application.Current.Resources.MergedDictionaries.Add(ResourceDictionary);
        }

        public ResourceDictionary CreateView()
        {
            return Application.LoadComponent(new Uri(UriPath, UriKind.Relative)) as ResourceDictionary;
        }
    }
}
