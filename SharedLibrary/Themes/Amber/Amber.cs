using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Themes.Amber
{
    class Amber : ThemeBase
    {  
        public override string Title => "Янтарная тема"; 
        public override string Name => nameof(Amber);
        public override string UriPath => @"/SharedLibrary;component/Themes/Amber/Amber.xaml";
        public override int Num => 2;
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GeneralTheme;
    }
}
