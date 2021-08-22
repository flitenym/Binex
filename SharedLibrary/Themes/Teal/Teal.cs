using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Themes.Teal
{
    class Teal : ThemeBase
    {  
        public override string Title => "Бирюзовая тема"; 
        public override string Name => nameof(Teal);
        public override string UriPath => @"/SharedLibrary;component/Themes/Teal/Teal.xaml";
        public override int Num => 15;
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GeneralTheme;
    }
}
