using System;
using System.ComponentModel;
using SharedLibrary.AbstractClasses;
using SharedLibrary.Helper.Attributes;

namespace SharedLibrary.LocalDataBase.Models
{
    [ModelClass(TableTitle = "Ошибки", IsVisible = false, CanDelete = false, CanInsert = false, CanUpdate = false, CanLoad = false)]
    public class Errors : ModelClass
    {
        [Description("Stack")]
        public string StackTrace { get; set; }

        [Description("Название")]
        public DateTime Date { get; set; }
    }
}