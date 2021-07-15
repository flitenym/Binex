using System;
using System.Windows.Controls;
using SharedLibrary.AbstractClasses;
using SharedLibrary.View;
using SharedLibrary.ViewModel;
using SharedLibrary.Helper.StaticInfo;
using static SharedLibrary.Helper.StaticInfo.Enums;
using SharedLibrary.Provider;

namespace Van.Model
{
    class InfoModel : ModuleBase
    {
        public override string Name => Types.ViewData.Info.Name;

        public override int Num => Types.ViewData.Info.Num;

        public override bool IsActive => Types.ViewData.Info.IsActive;

        public override bool IsNeedToDeactivate => Types.ViewData.Info.IsNeedToDeactivate;

        public override Guid ID => Types.ViewData.Info.View;

        public override ModelBaseClasses modelClass => Types.ViewData.Info.ModelClass;

        public override Guid? ParentID => null;

        protected override UserControl CreateViewAndViewModel()
        {
            if (IsNeedToDeactivate == false)
            {
                return new InfoView()
                {
                    DataContext = SharedProvider.GetFromDictionaryByKey(nameof(InfoViewModel)) ?? new InfoViewModel()
                };
            }

            return new InfoView()
            {
                DataContext = new InfoViewModel()
            };
        }
    }
}