using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Themes.Purple
{
    class Purple : ThemeBase
    {  
        public override string Title => "Фиолетовая тема"; 
        public override string Name => nameof(Purple);
        public override string UriPath => @"/SharedLibrary;component/Themes/Purple/Purple.xaml";
        public override int Num => 14;
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GeneralTheme;
    }
}
