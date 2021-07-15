using System;

namespace SharedLibrary.Helper.Attributes
{
    public class ColumnDataAttribute : Attribute
    {
        /// <summary>
        /// Отображать в таблице
        /// True по умолчанию
        /// </summary>
        public bool ShowInTable { get; set; } = true;

        /// <summary>
        /// Поле допускает значения null
        /// True по умолчанию
        /// </summary>
        public bool IsNullable { get; set; } = true;
    }
}
