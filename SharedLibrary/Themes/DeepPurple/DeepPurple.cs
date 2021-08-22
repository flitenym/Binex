using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Themes.DeepPurple
{
    class DeepPurple : ThemeBase
    {  
        public override string Title => "Темно-фиолетовая тема"; 
        public override string Name => nameof(DeepPurple);
        public override string UriPath => @"/SharedLibrary;component/Themes/DeepPurple/DeepPurple.xaml";
        public override int Num => 6;
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GeneralTheme;
    }
}
