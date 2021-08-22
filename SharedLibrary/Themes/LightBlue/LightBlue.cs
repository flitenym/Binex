using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Themes.LightBlue
{
    class LightBlue : ThemeBase
    {  
        public override string Title => "Светло-синяя тема"; 
        public override string Name => nameof(LightBlue);
        public override string UriPath => @"/SharedLibrary;component/Themes/LightBlue/LightBlue.xaml";
        public override int Num => 9;
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GeneralTheme;
    }
}
