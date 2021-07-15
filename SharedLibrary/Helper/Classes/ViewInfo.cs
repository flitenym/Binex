using System;
using static SharedLibrary.Helper.StaticInfo.Enums;

namespace SharedLibrary.Helper.Classes
{
    public class ViewInfo
    {
        public string Name { get; set; }
        public Guid View { get; set; }
        public int Num { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsNeedToDeactivate { get; set; } = true;
        public ModelBaseClasses ModelClass { get; set; }
    }
}
