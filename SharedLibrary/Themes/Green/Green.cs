using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Themes.Green
{
    class Green : ThemeBase
    {  
        public override string Title => "Зеленая тема"; 
        public override string Name => nameof(Green);
        public override string UriPath => @"/SharedLibrary;component/Themes/Green/Green.xaml";
        public override int Num => 2;
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GeneralTheme;
    }
}
