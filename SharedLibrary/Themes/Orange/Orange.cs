using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Themes.Orange
{
    class Orange : ThemeBase
    {  
        public override string Title => "Оранжевая тема"; 
        public override string Name => nameof(Orange);
        public override string UriPath => @"/SharedLibrary;component/Themes/Orange/Orange.xaml";
        public override int Num => 12;
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GeneralTheme;
    }
}
