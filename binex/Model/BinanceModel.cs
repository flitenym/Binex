using System;
using System.Windows.Controls;
using SharedLibrary.AbstractClasses;
using binex.Helper.StaticInfo;
using binex.View;
using binex.ViewModel;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace binex.Model
{
    class BinanceModel : ModuleBase
    {
        public override string Name => Types.ViewData.Binance.Name;

        public override int Num => Types.ViewData.Binance.Num;

        public override bool IsActive => Types.ViewData.Binance.IsActive;

        public override bool IsNeedToDeactivate => Types.ViewData.Binance.IsNeedToDeactivate;

        public override Guid ID => Types.ViewData.Binance.View;

        public override ModelBaseClasses modelClass => Types.ViewData.Binance.ModelClass;

        public override Guid? ParentID => null;

        protected override UserControl CreateViewAndViewModel()
        {
            return new BinanceView() { DataContext = new BinanceViewModel() };
        }
    }
}
