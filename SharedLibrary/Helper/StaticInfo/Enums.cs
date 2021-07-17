using System;
using System.ComponentModel;
using System.Reflection;

namespace SharedLibrary.Helper.StaticInfo
{
    public static class Enums
    {
        public static string GetDescription(this Enum en)
        {
            Type type = en.GetType();

            MemberInfo[] memInfo = type.GetMember(en.ToString());

            if (memInfo != null && memInfo.Length > 0)
            {
                object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs != null && attrs.Length > 0)
                {
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }

            return en.ToString();
        }
        public enum ModelBaseClasses
        {
            /// <summary>
            /// Класс для настроек, такая модель одна
            /// </summary>
            [Description("Настройки")]
            Settings = 0,
            /// <summary>
            /// Класс для меню слева
            /// </summary>
            [Description("Левое меню")]
            LeftMenu = 1,
            /// <summary>
            /// Класс для меню справа
            /// </summary>
            [Description("Правое меню")]
            RightMenu = 2,
            /// <summary>
            /// Рабочее место
            /// </summary>
            [Description("Рабочее место")]
            Main = 3
        }

        public enum ThemeBaseClasses
        {
            /// <summary>
            /// Темная/светлая тема
            /// </summary>
            [Description("Темная/светлая тема")]
            GlobalTheme = 0,
            /// <summary>
            /// Обычная тема
            /// </summary>
            [Description("Обычная тема")]
            GeneralTheme = 1
        }

        public enum YesOrNo
        {
            /// <summary>
            /// Да
            /// </summary>
            [Description("Да")]
            Yes,
            /// <summary>
            /// Нет
            /// </summary>
            [Description("Нет")]
            No
        }
    }
}
