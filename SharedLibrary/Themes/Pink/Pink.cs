using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Themes.Pink
{
    class Pink : ThemeBase
    {  
        public override string Title => "Розовая тема"; 
        public override string Name => nameof(Pink);
        public override string UriPath => @"/SharedLibrary;component/Themes/Pink/Pink.xaml";
        public override int Num => 13;
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GeneralTheme;
    }
}
