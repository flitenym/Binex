using System;
using System.Windows.Controls;
using SharedLibrary.AbstractClasses;
using Binex.Helper.StaticInfo;
using Binex.View;
using Binex.ViewModel;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace Binex.Model
{
    class BinancePayModel : ModuleBase
    {
        public override string Name => Types.ViewData.BinancePay.Name;

        public override int Num => Types.ViewData.BinancePay.Num;

        public override bool IsActive => Types.ViewData.BinancePay.IsActive;

        public override bool IsNeedToDeactivate => Types.ViewData.BinancePay.IsNeedToDeactivate;

        public override Guid ID => Types.ViewData.BinancePay.View;

        public override ModelBaseClasses modelClass => Types.ViewData.BinancePay.ModelClass;

        public override Guid? ParentID => null;

        protected override UserControl CreateViewAndViewModel()
        {
            return new BinancePayView() { DataContext = new BinancePayViewModel() };
        }
    }
}
