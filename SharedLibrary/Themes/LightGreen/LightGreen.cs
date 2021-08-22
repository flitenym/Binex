using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Themes.LightGreen
{
    class LightGreen : ThemeBase
    {  
        public override string Title => "Светло-зеленая тема"; 
        public override string Name => nameof(LightGreen);
        public override string UriPath => @"/SharedLibrary;component/Themes/LightGreen/LightGreen.xaml";
        public override int Num => 10;
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GeneralTheme;
    }
}
