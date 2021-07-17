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
        } 
    }
}
