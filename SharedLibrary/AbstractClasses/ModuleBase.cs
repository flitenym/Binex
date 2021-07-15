using System;
using System.Windows.Controls;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.AbstractClasses
{
    public abstract class ModuleBase
    {
        private UserControl view;

        protected abstract UserControl CreateViewAndViewModel();

        public abstract string Name { get; }

        public abstract int Num { get; }

        public abstract bool IsActive { get; }

        public abstract bool IsNeedToDeactivate { get; }

        public abstract Guid ID { get; }

        public abstract Guid? ParentID { get; }

        public abstract ModelBaseClasses modelClass { get; }

        public UserControl UserInterface
        {
            get
            {
                if (view == null)
                {
                    view = CreateViewAndViewModel();
                }
                return view;
            }
        }

        /// <summary>
        /// Удаление производить только если это нужно, если есть например карточка где отображается глобальная информация,
        /// то не нужно ее пересоздавать, а нужно хранить в singleton
        /// </summary>
        public void Deactivate()
        {
            if (view != null && IsNeedToDeactivate == true)
            {
                if (view is IDisposable d) d.Dispose();
                view.DataContext = null;
                view = null;
            }
        }

    }
}
