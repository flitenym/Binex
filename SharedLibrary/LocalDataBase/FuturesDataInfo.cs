using System.ComponentModel;
using SharedLibrary.AbstractClasses;
using SharedLibrary.Helper.Attributes;

namespace SharedLibrary.LocalDataBase.Models
{
    [ModelClass(TableTitle = "Данные Фьючерс", IsVisible = true, CanDelete = true, CanInsert = false, CanUpdate = false, CanLoad = true, Order = 2)]
    public class FuturesDataInfo : ModelClass
    {
        [ColumnData(ShowInTable = false, IsReadOnly = true)]
        public int ID { get; set; }

        [ColumnData(IsReadOnly = true, IsNullable = false)]
        [Description("Тип аккаунта")]
        public string AccountType { get; set; }

        [ColumnData(IsReadOnly = true, IsNullable = false)]
        [Description("Идентификатор пользователя")]
        public string UserID { get; set; }

        [ColumnData(IsReadOnly = true, IsNullable = false)]
        [Description("BTC")]
        public decimal AgentEarnBtc { get; set; }

        [ColumnData(IsReadOnly = true, IsNullable = false)]
        [Description("USDT")]
        public decimal AgentEarnUsdt { get; set; }

        [ColumnData(IsReadOnly = true, IsNullable = false)]
        [Description("Был трейдинг")]
        public string HasTrading { get; set; }

        [ColumnData(IsReadOnly = true, IsNullable = false)]
        [Description("Счет больше 1$")]
        public string AccountBalance { get; set; }

        [ColumnData(IsReadOnly = true, IsNullable = false)]
        [Description("Дата приглашения")]
        public string InviteTime { get; set; }

        [ColumnData(ShowInTable = false, IsReadOnly = true, IsNullable = false)]
        [Description("Бонус")]
        public string TotalReferralBonusEarn { get; set; }

        [ColumnData(IsReadOnly = true, DefaultValue = "", IsNullable = false)]
        [Description("Время загрузки")]
        public string LoadingDateTime { get; set; }

        [ColumnData(IsReadOnly = true, DefaultValue = "Нет", IsNullable = false)]
        [Description("Оплачено")]
        public string IsPaid { get; set; }
    }
}