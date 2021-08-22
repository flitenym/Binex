using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Themes.Lime
{
    class Lime : ThemeBase
    {  
        public override string Title => "Салатовая тема"; 
        public override string Name => nameof(Lime);
        public override string UriPath => @"/SharedLibrary;component/Themes/Lime/Lime.xaml";
        public override int Num => 11;
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GeneralTheme;
    }
}
