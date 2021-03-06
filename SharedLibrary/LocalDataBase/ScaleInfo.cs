using System.ComponentModel;
using SharedLibrary.AbstractClasses;
using SharedLibrary.Helper.Attributes;

namespace SharedLibrary.LocalDataBase.Models
{
    [ModelClass(TableTitle = "Линейка Спот", IsVisible = true, CanDelete = true, CanInsert = true, CanUpdate = true, CanLoad = true, Order = 3)]
    public class ScaleInfo : ModelClass
    {
        [ColumnData(ShowInTable = false)]
        public int ID { get; set; }

        [Description("От")]
        public double FromValue { get; set; }

        [Description("Коммисия (%)")]
        public double Percent { get; set; }
    }
}