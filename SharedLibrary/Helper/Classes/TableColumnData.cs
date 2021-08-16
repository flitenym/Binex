using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLibrary.Helper.Classes
{
    public class TableColumnData
    {
        public string ColumnName { get; set; }
        public string ColumnCaption { get; set; }
        public Type ColumnType { get; set; }
        public TableColumnData ShallowCopy()
        {
            return (TableColumnData)this.MemberwiseClone();
        }
    }
}