using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Themes.Red
{
    class Red : ThemeBase
    {  
        public override string Title => "Красная тема"; 
        public override string Name => nameof(Red);
        public override string UriPath => @"/SharedLibrary;component/Themes/Red/Red.xaml";
        public override int Num => 1;
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GeneralTheme;
    }
}
