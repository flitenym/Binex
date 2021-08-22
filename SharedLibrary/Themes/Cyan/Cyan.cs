using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Themes.Cyan
{
    class Cyan : ThemeBase
    {  
        public override string Title => "Голубая тема"; 
        public override string Name => nameof(Cyan);
        public override string UriPath => @"/SharedLibrary;component/Themes/Cyan/Cyan.xaml";
        public override int Num => 4;
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GeneralTheme;
    }
}
