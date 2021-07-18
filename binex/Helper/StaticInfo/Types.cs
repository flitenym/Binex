using SharedLibrary.Helper.Classes;
using System;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace binex.Helper.StaticInfo
{
    public static class Types
    {
        public static class ViewData
        {
            public static ViewInfo Test = new ViewInfo { Name = "Тестирование", View = new Guid("38E7686B-4F2C-4F72-95DA-08CF3CA249BC"), Num = 0, ModelClass = ModelBaseClasses.LeftMenu };
            public static ViewInfo Binance = new ViewInfo { Name = "Настройка Binance", View = new Guid("920A26E7-AF84-41D7-9040-7C522291F24A"), Num = 1, ModelClass = ModelBaseClasses.RightMenu };
        } 
    }
}
