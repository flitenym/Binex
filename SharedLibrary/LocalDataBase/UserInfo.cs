using System;
using System.ComponentModel;
using SharedLibrary.AbstractClasses;
using SharedLibrary.Helper.Attributes;
using SharedLibrary.Helper.StaticInfo;

namespace SharedLibrary.LocalDataBase.Models
{
    [ModelClass(TableTitle = "Пользователи", IsVisible = true, CanDelete = true, CanInsert = false, CanUpdate = true, CanLoad = false, Order = 7)]
    public class UserInfo : ModelClass
    {
        [ColumnData(ShowInTable = false)]
        public int ID { get; set; }

        [Description("Идентификатор пользователя")]
        public string UserID { get; set; }

        [Description("Кошелек")]
        public string Address { get; set; }

        [Description("Уникальный")]
        public string IsUnique { get; set; }
    }
}