using System;

namespace SharedLibrary.Helper.Attributes
{
    /// <summary>
    /// Если отсутствует TableTitle, то будет дефолтное название модели, если флаги отсутствуют, то все будет разрешено
    /// </summary>
    public class ModelClassAttribute : Attribute
    {
        public string TableTitle { get; set; }
        public bool CanInsert { get; set; } = true;
        public bool CanDelete { get; set; } = true;
        public bool CanUpdate { get; set; } = true;
        public bool CanLoad { get; set; } = true;
        public bool IsVisible { get; set; } = true;
    }
}
