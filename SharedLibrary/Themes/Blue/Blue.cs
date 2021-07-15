using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Themes.Blue
{
    class Blue : ThemeBase
    {  
        public override string Title => "Голубая тема"; 
        public override string Name => nameof(Blue);
        public override string UriPath => @"/SharedLibrary;component/Themes/Blue/Blue.xaml";
        public override int Num => 0;
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GeneralTheme;
    }
}
