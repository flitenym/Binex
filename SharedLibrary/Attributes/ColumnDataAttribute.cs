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
        /// Запретить редактирование
        /// False по умолчанию
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

        /// <summary>
        /// Поле допускает значения null
        /// True по умолчанию
        /// </summary>
        public bool IsNullable { get; set; } = true;

        /// <summary>
        /// Значение по умолчанию
        /// </summary>
        public object DefaultValue { get; set; } = null;
    }
}
