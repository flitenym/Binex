using System;
using System.Windows.Controls;
using SharedLibrary.AbstractClasses;
using binex.Helper.StaticInfo;
using binex.View;
using binex.ViewModel;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace binex.Model
{
    class TestModel : ModuleBase
    {
        public override string Name => Types.ViewData.Test.Name;

        public override int Num => Types.ViewData.Test.Num;

        public override bool IsActive => Types.ViewData.Test.IsActive;

        public override bool IsNeedToDeactivate => Types.ViewData.Test.IsNeedToDeactivate;

        public override Guid ID => Types.ViewData.Test.View;

        public override ModelBaseClasses modelClass => Types.ViewData.Test.ModelClass;

        public override Guid? ParentID => null;

        protected override UserControl CreateViewAndViewModel()
        {
            return new TestView() { DataContext = new TestViewModel() };
        }
    }
}
