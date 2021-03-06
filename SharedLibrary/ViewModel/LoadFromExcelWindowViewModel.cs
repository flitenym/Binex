using ExcelDataReader;
using SharedLibrary.AbstractClasses;
using SharedLibrary.Commands;
using SharedLibrary.Helper;
using SharedLibrary.LocalDataBase;
using SharedLibrary.LocalDataBase.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SharedLibrary.ViewModel
{
    public class LoadFromExcelWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public LoadFromExcelWindowViewModel(string fileName, ModelClass modelClassItem, Type type)
        {
            this.type = type;
            this.modelClassItem = modelClassItem;
            this.fileName = fileName;

            GetDataTableData();
        }

        #region Fields

        public string fileName;
        public Type type;
        public ModelClass modelClassItem;

        /// <summary>
        /// Инструкция по загрузке
        /// </summary>
        public string Instruction =>
$@"При загрузке из Excel следует придерживаться правил:
1. Загрузка будет работать только над объектом, который выбран в странице ""{Helper.StaticInfo.Types.ViewData.DataBaseBrowsing.Name}"".
2. Необходимо следовать изначальной последовательности колонок.
3. В случае если у нет названия колонок в Excel файле, то следует снять флаг.";

        #region Игнорирование первой строки в листе Excel

        private bool ignoreFirstRow = true;
        public bool IgnoreFirstRow
        {
            get { return ignoreFirstRow; }
            set
            {
                ignoreFirstRow = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(IgnoreFirstRow)));
            }
        }

        #endregion

        #region Выбранный лист

        private string workSheet;
        public string WorkSheet
        {
            get { return workSheet; }
            set
            {
                workSheet = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(WorkSheet)));
            }
        }

        #endregion

        #region Все доступные листы в Excel

        private List<string> workSheets = new List<string>();
        public List<string> WorkSheets
        {
            get { return workSheets; }
            set
            {
                workSheets = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(WorkSheets)));
            }
        }

        #endregion

        #endregion

        #region Команда для старта загрузки

        private AsyncCommand startCommand;
        public AsyncCommand StartCommand => startCommand ?? (startCommand = new AsyncCommand(obj => StartLoadingAsync(obj as System.Windows.Window)));

        public async Task StartLoadingAsync(System.Windows.Window window)
        {
            await LoadAsync();

            window.DialogResult = true;
        }

        /// <summary>
        /// Проверим загружаемые данные
        /// </summary>
        /// <returns></returns>
        private async Task<bool> CheckMinimumBeforeAsync(List<object> listObj)
        {
            string tableName = string.Empty;
            if (modelClassItem is DataInfo)
            {
                var dataInfos = await SQLExecutor.SelectExecutorAsync<DataInfo>(nameof(DataInfo), $"WHERE LoadingDateTime = (SELECT di.LoadingDateTime FROM {nameof(DataInfo)} di ORDER BY di.LoadingDateTime DESC LIMIT 1)");
                if (dataInfos.Any())
                {
                    for (int i = 0; i < listObj.Count; i++)
                    {
                        var objUserID = (string)((ExpandoObject)listObj[i]).FirstOrDefault(x => x.Key == "UserID").Value;
                        var objAgentEarnBtc = (decimal?)((ExpandoObject)listObj[i]).FirstOrDefault(x => x.Key == "AgentEarnBtc").Value;
                        if (objAgentEarnBtc != null)
                        {
                            var objLikeInDataInfo = dataInfos.FirstOrDefault(x => x.UserID == objUserID && x.AgentEarnBtc > objAgentEarnBtc);
                            if (objLikeInDataInfo != null)
                            {
                                await HelperMethods.Message($"Данные не загружены. UserID {objUserID} было {objLikeInDataInfo.AgentEarnBtc} стало {objAgentEarnBtc}");
                                return false;
                            }
                        }
                    }
                }
            }
            else if (modelClassItem is FuturesDataInfo)
            {
                var dataInfos = await SQLExecutor.SelectExecutorAsync<FuturesDataInfo>(nameof(FuturesDataInfo), $"WHERE LoadingDateTime = (SELECT di.LoadingDateTime FROM {nameof(FuturesDataInfo)} di ORDER BY di.LoadingDateTime DESC LIMIT 1)");
                if (dataInfos.Any())
                {
                    for (int i = 0; i < listObj.Count; i++)
                    {
                        var objUserID = (string)((ExpandoObject)listObj[i]).FirstOrDefault(x => x.Key == "UserID").Value;
                        var objAgentEarnBtc = (decimal?)((ExpandoObject)listObj[i]).FirstOrDefault(x => x.Key == "AgentEarnBtc").Value;
                        if (objAgentEarnBtc != null)
                        {
                            var objLikeInDataInfo = dataInfos.FirstOrDefault(x => x.UserID == objUserID && x.AgentEarnBtc > objAgentEarnBtc);
                            if (objLikeInDataInfo != null)
                            {
                                await HelperMethods.Message($"Данные не загружены. UserID {objUserID} было {objLikeInDataInfo.AgentEarnBtc} стало {objAgentEarnBtc}");
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Метод, позволяющий делать только 2 итерации загрузки
        /// </summary>
        /// <returns></returns>
        private async Task DeleteBeforeDataAsync()
        {
            string tableName = string.Empty;
            if (modelClassItem is DataInfo)
            {
                await SQLExecutor.QueryExecutorAsync($@"
DELETE FROM {nameof(DataInfo)}
WHERE LoadingDateTime not in (
	SELECT LoadingDateTime from {nameof(DataInfo)}
	GROUP BY LoadingDateTime
	ORDER BY LoadingDateTime DESC
	LIMIT(2)
)
");
            }
            else if (modelClassItem is FuturesDataInfo)
            {
                await SQLExecutor.QueryExecutorAsync($@"
DELETE FROM {nameof(FuturesDataInfo)}
WHERE LoadingDateTime not in (
	SELECT LoadingDateTime from {nameof(FuturesDataInfo)}
	GROUP BY LoadingDateTime
	ORDER BY LoadingDateTime DESC
	LIMIT(2)
)
");
            }
        }

        public async Task LoadAsync()
        {
            try
            {
                var result = GetDataSet();

                DataTable dataTable = new DataTable();

                for (int i = 0; i < result.Tables.Count; i++)
                {
                    if (result.Tables[i].TableName == workSheet)
                    {
                        dataTable = result.Tables[i];
                        break;
                    }
                }

                List<object> listObj = new List<object>();
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    listObj.Add(dataTable.Rows[i].ToObjectLoad(type));
                }
                if (listObj.Count > 0)
                {
                    // в случае если у нас таблица Данные, то нам нужно проверить BTC
                    // если BTC у нас по новым данным для загрузки
                    if (!await CheckMinimumBeforeAsync(listObj))
                    {
                        return;
                    }

                    await HelperMethods.Message($"Найдено {listObj.Count} строк, выполняется загрузка в БД");

                    await SQLExecutor.InsertSeveralExecutorAsync(modelClassItem, listObj);

                    await DeleteBeforeDataAsync();
                }
                else
                {
                    await HelperMethods.Message($"Данные не найдены");
                }
            }
            catch (Exception ex)
            {
                await HelperMethods.Message($"{ex.ToString()}");
            }
        }

        public void GetDataTableData()
        {
            DataSet result = GetDataSet();

            WorkSheets.Clear();

            for (int i = 0; i < result.Tables.Count; i++)
            {
                workSheets.Add(result.Tables[i].TableName);
            }

            workSheet = workSheets.FirstOrDefault();
        }

        public DataSet GetDataSet()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = File.Open(fileName, FileMode.Open, FileAccess.Read))
            {
                if (Path.GetExtension(fileName) == ".csv")
                {
                    using (IExcelDataReader reader = ExcelReaderFactory.CreateCsvReader(stream))
                    {
                        return reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = ignoreFirstRow }
                        });
                    }
                }
                else
                {
                    using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        return reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = ignoreFirstRow }
                        });
                    }
                }
            }
        }

        #endregion

        #region Команда для отмены загрузки

        private RelayCommand cancelCommand;
        public RelayCommand CancelCommand => cancelCommand ?? (cancelCommand = new RelayCommand(obj => CancelFunction(obj)));

        public void CancelFunction(object obj)
        {
            (obj as System.Windows.Window).DialogResult = false;
        }

        #endregion

    }
}
