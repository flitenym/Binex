using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Themes.Indigo
{
    class Indigo : ThemeBase
    {  
        public override string Title => "Индиго тема"; 
        public override string Name => nameof(Indigo);
        public override string UriPath => @"/SharedLibrary;component/Themes/Indigo/Indigo.xaml";
        public override int Num => 8;
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GeneralTheme;
    }
}
