using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Themes.DeepOrange
{
    class DeepOrange : ThemeBase
    {  
        public override string Title => "Темно-оранжевая тема"; 
        public override string Name => nameof(DeepOrange);
        public override string UriPath => @"/SharedLibrary;component/Themes/DeepOrange/DeepOrange.xaml";
        public override int Num => 5;
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GeneralTheme;
    }
}
