using System.ComponentModel;
using SharedLibrary.AbstractClasses;
using SharedLibrary.Helper.Attributes;

namespace SharedLibrary.LocalDataBase.Models
{
    [ModelClass(TableTitle = "История об оплате", IsVisible = true, CanDelete = true, CanInsert = true, CanUpdate = true, CanLoad = true)]
    public class PayHistory : ModelClass
    {
        [ColumnData(ShowInTable = false)]
        public int ID { get; set; }

        [Description("Идентификатор пользователя")]
        public string UserID { get; set; }

        [Description("USDT")]
        public decimal SendedUsdt { get; set; }

        [Description("Дата оплаты")]
        public string PayTime { get; set; }

        [Description("Номер операции оплаты")]
        public int? NumberPay { get; set; }
    }
}