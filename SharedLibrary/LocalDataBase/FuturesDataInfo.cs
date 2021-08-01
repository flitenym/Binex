using System.ComponentModel;
using SharedLibrary.AbstractClasses;
using SharedLibrary.Helper.Attributes;

namespace SharedLibrary.LocalDataBase.Models
{
    [ModelClass(TableTitle = "Данные Фьючерс", IsVisible = true, CanDelete = true, CanInsert = true, CanUpdate = true, CanLoad = true)]
    public class FuturesDataInfo : ModelClass
    {
        [ColumnData(ShowInTable = false)]
        public int ID { get; set; }

        [Description("Тип аккаунта")]
        public string AccountType { get; set; }

        [Description("Был трейдинг")]
        public string HasTrading { get; set; }

        [Description("Идентификатор пользователя")]
        public string UserID { get; set; }

        [Description("BTC")]
        public decimal AgentEarnBtc { get; set; }

        [Description("Дата приглашения")]
        public string InviteTime { get; set; }

        [ColumnData(IsReadOnly = true, DefaultValue = "", IsNullable = false)]
        [Description("Время загрузки")]
        public string LoadingDateTime { get; set; }

        [ColumnData(IsReadOnly = true, DefaultValue = "Нет", IsNullable = false)]
        [Description("Оплачено")]
        public string IsPaid { get; set; }
    }
}