using SharedLibrary.Helper;
using SharedLibrary.Helper.Attributes;

namespace SharedLibrary.AbstractClasses
{
    public abstract class ModelClass
    {
        [ColumnData(ShowInTable = false)]
        public string Title { get; set; }

        [ColumnData(ShowInTable = false)]
        public int Order { get; set; }

        #region IsVisible

        private bool IsVisible { get; set; }

        public bool GetIsVisible() => IsVisible;
        public void SetIsVisible(bool value) { IsVisible = value; }

        #endregion

        #region CanInsert

        private bool CanInsert { get; set; }

        public bool GetCanInsert() => CanInsert;
        public void SetCanInsert(bool value) { CanInsert = value; }

        #endregion

        #region CanDelete

        private bool CanDelete { get; set; }

        public bool GetCanDelete() => CanDelete;
        public void SetCanDelete(bool value) { CanDelete = value; }

        #endregion

        #region CanUpdate

        private bool CanUpdate { get; set; }

        public bool GetCanUpdate() => CanUpdate;
        public void SetCanUpdate(bool value) { CanUpdate = value; }

        #endregion

        #region CanLoad

        private bool CanLoad { get; set; }

        public bool GetCanLoad() => CanLoad;
        public void SetCanLoad(bool value) { CanLoad = value; }

        #endregion

        public string InsertQuery(object obj, bool needReturnIndex = true)
        {
            var typeProperties = obj.GetType().GetProperties();
            var properties = HelperMethods.GetProperties(typeProperties);

            string fields = string.Empty;
            string values = string.Empty;

            for (int i = 0; i < properties.Length; i++)
            {
                fields += $", {properties[i].Name}";
                values += $", @{properties[i].Name}";
            }

            fields = fields.Substring(2, fields.Length - 2);
            values = values.Substring(2, values.Length - 2);

            return $"INSERT INTO {obj.GetType().Name} ({fields}) VALUES ({values});  {(needReturnIndex ? "select last_insert_rowid()" : "")}";
        }

        public string UpdateQuery(object obj, object ID)
        {
            var typeProperties = obj.GetType().GetProperties();
            var properties = HelperMethods.GetProperties(typeProperties);

            string fieldsQuery = string.Empty;

            for (int i = 0; i < properties.Length; i++)
            {
                fieldsQuery += $", {properties[i].Name} = @{properties[i].Name}";
            }

            fieldsQuery = fieldsQuery.Substring(2, fieldsQuery.Length - 2);

            return $"UPDATE {obj.GetType().Name} SET {fieldsQuery} WHERE ID = {ID}";
        }
    }
}