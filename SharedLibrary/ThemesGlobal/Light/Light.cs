using SharedLibrary.AbstractClasses; 
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.ThemesGlobal.Light
{
    class Light : ThemeBase
    {
        public override string Title => "Светлая тема";
        public override string Name => nameof(Light);
        public override string UriPath => @"/SharedLibrary;component/ThemesGlobal/Light/Light.xaml";
        public override int Num => 1; 
        public override ThemeBaseClasses ThemeClass => ThemeBaseClasses.GlobalTheme;
    }
}
