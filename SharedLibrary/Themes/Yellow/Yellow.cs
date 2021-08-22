using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Themes.Yellow
{
    class Yellow : ThemeBase
    {  
        public override string Title => "Желтая тема"; 
        public override string Name => nameof(Yellow);
        public override string UriPath => @"/SharedLibrary;component/Themes/Yellow/Yellow.xaml";
        public override int Num => 16;
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GeneralTheme;
    }
}
