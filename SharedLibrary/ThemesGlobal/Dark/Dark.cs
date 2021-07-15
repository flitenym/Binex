using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.ThemesGlobal.Dark
{
    class Dark : ThemeBase
    {
        public override string Title => "Темная тема";
        public override string Name => nameof(Dark);
        public override string UriPath => @"/SharedLibrary;component/ThemesGlobal/Dark/Dark.xaml"; 
        public override int Num => 0; 
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GlobalTheme;
    }
}
