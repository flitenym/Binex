using System;
using System.ComponentModel;
using SharedLibrary.AbstractClasses;
using SharedLibrary.Helper.Attributes;
using SharedLibrary.Helper.StaticInfo;

namespace SharedLibrary.LocalDataBase.Models
{
    [ModelClass(TableTitle = "Пользователи", IsVisible = true, CanDelete = true, CanInsert = true, CanUpdate = true, CanLoad = true)]
    public class UserInfo : ModelClass
    {
        [ColumnData(ShowInTable = false)]
        public int ID { get; set; }

        [Description("Идентификатор пользователя")]
        public string UserID { get; set; }

        [Description("BTS кошелек")]
        public string BTS { get; set; }
    }
}