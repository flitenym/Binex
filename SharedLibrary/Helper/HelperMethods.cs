using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Dapper;
using NLog;
using SharedLibrary.AbstractClasses;
using SharedLibrary.Helper.Attributes;
using SharedLibrary.Helper.StaticInfo;
using SharedLibrary.LocalDataBase;
using SharedLibrary.LocalDataBase.Models;
using SharedLibrary.Provider;
using SharedLibrary.ViewModel;

namespace SharedLibrary.Helper
{
    public static class HelperMethods
    {
        #region SnackBar

        /// <summary>
        /// Отправить сообщение через SnackBar
        /// </summary>
        /// <param name="content">Сообщение</param>
        /// <param name="isNoDuplicateConsider">Если true и будет дубликаты сообщений, то они каждый все равно вызовет новое уведомление, если false то выйдет повторное сообщение 1 раз</param>
        public static Task Message(string content, bool isNoDuplicateConsider = false, Logger logger = null)
        {
            return Task.Run(
                () =>
                {
                    if (logger == null)
                    {
                        if (SharedProvider.GetFromDictionaryByKey(nameof(MainWindowViewModel)) is MainWindowViewModel mainWindowViewModel)
                        {
                            mainWindowViewModel.IsMessagePanelContent.Enqueue(
                            content,
                            "OK",
                            param => Trace.WriteLine("Actioned: " + param),
                            null,
                            false,
                            isNoDuplicateConsider);
                        }
                    }
                    else
                    {
                        logger.Trace(content);
                    }
                });
        }

        #endregion

        #region DataGrid and DataTable

        public static void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // В случае если референсы или сами ID то будем скрывать, они не нужны пользователю 
            if (!(sender is DataGrid dGrid)) return;
            if (!(dGrid.ItemsSource is DataView view)) return;
            var table = view.Table;
            var column = table.Columns[e.Column.Header as string];
            e.Column.Header = table.Columns[e.Column.Header as string].Caption;

            if (GetProperty(column, InfoKeys.ExtendedPropertiesShowInTableKey))
            {
                e.Column.Visibility = Visibility.Collapsed;
            }

            if (GetProperty(column, InfoKeys.ExtendedPropertiesIsReadOnlyKey))
            {
                e.Column.IsReadOnly = true;
            }
        }

        public static void SetProperty(DataColumn column, string key)
        {
            column.ExtendedProperties.Add(key, true);
        }

        public static bool GetProperty(DataColumn column, string key)
        {
            if ((bool?)column.ExtendedProperties[key] is bool)
                return true;
            return false;
        }

        public static DataTable ToDataTable<T>(this List<T> items)
        {
            var tb = new DataTable(typeof(T).Name);

            PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                tb.Columns.Add(prop.Name, prop.PropertyType);
            }

            foreach (var item in items)
            {
                var values = new object[props.Length];
                for (var i = 0; i < props.Length; i++)
                {
                    values[i] = props[i].GetValue(item, null);
                }

                tb.Rows.Add(values);
            }

            return tb;
        }

        public static DataTable ToDataTable<T>(this IList<T> data, Type type)
        {
            DataTable table = new DataTable();

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(type);

            foreach (PropertyDescriptor prop in properties)
            {
                DataColumn column = new DataColumn(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType)
                {
                    Caption = string.IsNullOrEmpty(prop.Description) ? prop.Name : prop.Description
                };

                for (int i = 0; i < prop.Attributes.Count; i++)
                {
                    if (prop.Attributes[i].GetType() == typeof(ColumnDataAttribute))
                    {
                        if (((ColumnDataAttribute)prop.Attributes[i]).ShowInTable == false)
                        {
                            SetProperty(column, InfoKeys.ExtendedPropertiesShowInTableKey);
                        }
                        if (((ColumnDataAttribute)prop.Attributes[i]).IsReadOnly == true)
                        {
                            SetProperty(column, InfoKeys.ExtendedPropertiesIsReadOnlyKey);
                        }
                    }
                }

                table.Columns.Add(column);
            }

            foreach (T item in data)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                {
                    if (prop.GetValue(item) is double doubleValue)
                    {
                        row[prop.Name] = Math.Round(doubleValue, SettingsDictionary.round);
                    }
                    else
                    {
                        row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                    }
                }

                table.Rows.Add(row);
            }
            return table;
        }

        public static object ToObject(this DataRow row, Type type)
        {
            var expandoDict = new ExpandoObject() as IDictionary<string, object>;
            foreach (PropertyInfo property in type.GetProperties())
            {
                if (row.Table.Columns.Contains(property.Name) && property.DeclaringType != typeof(ModelClass))
                {
                    expandoDict.Add(property.Name.ToString(), row[property.Name] == DBNull.Value ? null : row[property.Name]);
                }
            }

            return expandoDict;
        }

        public static object ToObjectLoad(this DataRow row, Type type)
        {
            var expandoDict = new ExpandoObject() as IDictionary<string, object>;
            var typeProperties = type.GetProperties();
            var properties = GetProperties(typeProperties);

            int j = 0; //индекс столбца в excel
            for (int i = 0; i < properties.Length; i++)
            {
                object value;
                var databaseType = properties[i].Name.GetType();

                (bool isHaveDefault, object defaultValue) = GetDefaultValue(properties[i], DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));

                //в случае если столбцов в Excel меньше чем в БД, тогда искусственно заполним default значениями
                if (j >= row.Table.Columns.Count)
                {
                    if (isHaveDefault)
                    {
                        value = defaultValue;
                    }
                    else if (type.IsValueType)
                    {
                        value = Activator.CreateInstance(type);
                    }
                    else
                    {
                        value = null;
                    }
                }
                else
                {
                    var columnName = row.Table.Columns[j].ColumnName;

                    if (isHaveDefault)
                    {
                        value = defaultValue;
                    }
                    else if (row[columnName] == DBNull.Value)
                    {
                        value = null;
                    }
                    else
                    {
                        var excelType = row[columnName].GetType();

                        //в excel сложно задать string/double даже в одном столбце, поэтому создаются value.0 в БД, 
                        //поэтому применим такой hack
                        if (!databaseType.Equals(excelType) && databaseType.Equals(typeof(System.String)))
                        {
                            value = row[columnName].ToString();
                        }
                        else
                        {
                            value = row[columnName];
                        }
                    }
                }

                expandoDict.Add(properties[i].Name.ToString(), value);
                j++;
            }

            return expandoDict;
        }

        public static PropertyInfo[] GetProperties(PropertyInfo[] typeProperties)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>();

            for (int i = 0; i < typeProperties.Length; i++)
            {
                var columnDataAttributes = (ColumnDataAttribute)typeProperties[i].GetCustomAttributes(typeof(ColumnDataAttribute), true).FirstOrDefault();
                if ((columnDataAttributes == null || columnDataAttributes.IsNullable == false) &&
                    typeProperties[i].DeclaringType != typeof(ModelClass) &&
                    typeProperties[i].CanWrite)
                {
                    properties.Add(typeProperties[i]);
                }
            }

            return properties.ToArray();
        }

        public static (bool isHaveDefault, object defaultValue) GetDefaultValue(PropertyInfo property, object defaultValueWhenStringEmpty = null)
        {
            var columnDataAttributes = (ColumnDataAttribute)property.GetCustomAttributes(typeof(ColumnDataAttribute), true).FirstOrDefault();
            if (columnDataAttributes != null && columnDataAttributes.DefaultValue != null)
            {
                if (columnDataAttributes.DefaultValue is string defaultValue && defaultValue == "" && defaultValueWhenStringEmpty != null)
                {
                    return (true, defaultValueWhenStringEmpty);
                }

                return (true, columnDataAttributes.DefaultValue);
            }

            return (false, null);
        }

        public static bool ClearDataTable(DataTable dataTable, object value)
        {
            if (value == null) return false;

            dataTable?.Clear();
            dataTable?.Columns.Clear();
            dataTable?.Rows.Clear();

            return true;
        }

        #endregion

        #region Clone

        /// <summary>
        /// Клонирование Списка
        /// </summary>
        public static List<T> Clone<T>(this List<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }

        #endregion

        #region Generics

        public static T TryGet<T>(this IDictionary<string, object> storage, string key, T defaultValue = default)
        {
            if (storage == null)
                throw new ArgumentNullException(nameof(storage));
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            object obj;
            if (!storage.TryGetValue(key, out obj))
                return defaultValue;
            try
            {
                return (T)obj;
            }
            catch (Exception ex)
            {
                throw new InvalidCastException("Ошибка " + ex.Message);
            }
        }

        public static IEnumerable<T> GetAllInstancesOf<T>(List<T> result)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            Assembly[] assems = currentDomain.GetAssemblies();

            foreach (var assm in assems)
            {
                result.AddRange(
                        assm.GetTypes()
                        .Where(t => typeof(T).IsAssignableFrom(t))
                        .Where(t => !t.IsAbstract && t.IsClass)
                        .Select(t => (T)Activator.CreateInstance(t))
                );
            }

            return result;
        }

        #endregion

        public static string GetVersion()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == AppDomain.CurrentDomain.FriendlyName.Replace(".exe", "")) ?? Assembly.GetExecutingAssembly();

            return assembly.GetName().Version.ToString();
        }

        #region DB operations by Key

        public static async Task UpdateByKeyInDBAsync(string key, string value, Settings settingsData = null)
        {
            if (settingsData == null)
            {
                using (var slc = new SQLiteConnection(SQLExecutor.LoadConnectionString))
                {
                    await slc.OpenAsync();
                    settingsData = (await slc.QueryAsync<Settings>($"SELECT * FROM {nameof(Settings)} Where Name = '{key}'")).FirstOrDefault();
                }

                if (settingsData == null)
                {
                    settingsData = new Settings() { Name = key, Value = value };
                    await SQLExecutor.InsertExecutorAsync(settingsData, settingsData);
                }
            }

            if (settingsData != null && settingsData.Value != value)
            {
                settingsData.Value = value;
                await SQLExecutor.UpdateExecutorAsync(settingsData, settingsData, settingsData.ID);
            }
        }

        public static async Task<Settings> GetByKeyInDBAsync(string key)
        {
            using (var slc = new SQLiteConnection(SQLExecutor.LoadConnectionString))
            {
                await slc.OpenAsync();
                return (await slc.QueryAsync<Settings>($"SELECT * FROM {nameof(Settings)} Where Name = '{key}'")).FirstOrDefault();
            }
        }

        #endregion

        #region Email

        public static void SendEmail(string addressFrom, string addressFromName, string addressFromPassword, string addressTo, string subject, string body, bool enableSsl = true, string smtpHost = "smtp.gmail.com", int smtpPort = 587)
        {
            // отправитель - устанавливаем адрес и отображаемое в письме имя
            MailAddress from = new MailAddress(addressFrom, addressFromName);
            // кому отправляем
            MailAddress to = new MailAddress(addressTo);
            // создаем объект сообщения
            MailMessage m = new MailMessage(from, to);
            // тема письма
            m.Subject = subject;
            // текст письма
            m.Body = body;
            // письмо представляет код html
            m.IsBodyHtml = true;
            // адрес smtp-сервера и порт, с которого будем отправлять письмо
            SmtpClient smtp = new SmtpClient(smtpHost, smtpPort);
            // логин и пароль
            smtp.Credentials = new NetworkCredential(addressFrom, addressFromPassword);
            smtp.EnableSsl = enableSsl;
            smtp.Send(m);
        }

        #endregion

        #region License

        public static bool LicenseVerify()
        {
            string[] files = System.IO.Directory.GetFiles(Environment.CurrentDirectory, "*.lic");

            if (files.Length == 0)
            {
                //TODO создать окно красивое для показа инфы по лицензии
                //throw new ApplicationException($"Ваша копия программы не лицензирована! Не найден файл лицензии с расширением \".lic\". Обратитесь к автору.");
                return false;
            }

            XDocument doc = XDocument.Load(files.First());

            (bool isValid, DateTime startDate, string endDate, string userName, string productName, string Message) = LicenseGenerator.License.IsValidLicense(doc);

            if (isValid)
            {
                if (string.IsNullOrEmpty(Message))
                {
                    return true;
                }

                //TODO создать окно красивое для показа инфы по лицензии

                return true;
            }
            else
            {
                //TODO создать окно красивое для показа инфы по лицензии

                return false;
            }
        }

        #endregion
    }
}