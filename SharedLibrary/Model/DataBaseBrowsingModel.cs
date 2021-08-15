using System;
using System.Windows.Controls;
using SharedLibrary.AbstractClasses;
using SharedLibrary.View;
using SharedLibrary.ViewModel;
using SharedLibrary.Helper.StaticInfo;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Model
{
    class DataBaseBrowsingModel : ModuleBase
    {
        public override string Name => Types.ViewData.DataBaseBrowsing.Name;

        public override int Num => Types.ViewData.DataBaseBrowsing.Num;

        public override bool IsActive => Types.ViewData.DataBaseBrowsing.IsActive;

        public override bool IsNeedToDeactivate => Types.ViewData.DataBaseBrowsing.IsNeedToDeactivate;

        public override Guid ID => Types.ViewData.DataBaseBrowsing.View;

        public override ModelBaseClasses modelClass => Types.ViewData.DataBaseBrowsing.ModelClass;

        public override Guid? ParentID => null;

        protected override UserControl CreateViewAndViewModel()
        {
            return new LocalDataBaseView() { DataContext = new LocalDataBaseViewModel() };
        }

    }
}
