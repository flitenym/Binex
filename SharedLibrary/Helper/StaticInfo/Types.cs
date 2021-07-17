using SharedLibrary.Helper.Classes;
using System;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Helper.StaticInfo
{
    public static class Types
    {
        public static class ViewData
        {
            #region Настройки

            public static ViewInfo Settings = new ViewInfo { Name = "Настройки", View = new Guid("C475B3A2-B969-4B30-B08A-F669C8B6AFF9"), Num = 0, ModelClass = ModelBaseClasses.Settings};

            #endregion

            #region Главное меню

            public static ViewInfo MainMenu = new ViewInfo { Name = "Главное меню", View = new Guid("C7ECA40A-DACF-4524-B0F7-443860448FFA"), Num = 0, ModelClass = ModelBaseClasses.Main };

            #endregion

            #region Правое меню

            public static ViewInfo ConnectionDataBase = new ViewInfo { Name = "Подключение к БД", View = new Guid("2340A6C5-1468-4CF6-BF1D-5DDE403F8034"), Num = 1, IsActive = false, ModelClass = ModelBaseClasses.RightMenu };

            public static ViewInfo DataBaseBrowsing = new ViewInfo { Name = "Локальные данные", View = new Guid("12011025-144B-4BE8-96C4-DAD33B8778FC"), Num = 2, ModelClass = ModelBaseClasses.RightMenu };

            public static ViewInfo Info = new ViewInfo { Name = "Просмотр доп. информации", View = new Guid("94E80C76-219B-4633-B35D-D8A39A5E80E0"), Num = 3, IsNeedToDeactivate = false, ModelClass = ModelBaseClasses.RightMenu };

            #endregion 

        } 
    }
}
