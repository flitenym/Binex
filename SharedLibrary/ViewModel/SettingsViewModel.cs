using System.ComponentModel;
using System.Linq;
using System.Windows;
using SharedLibrary.AbstractClasses;
using static SharedLibrary.Helper.HelperMethods;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Net.Mime;
using SharedLibrary.LocalDataBase.Models;
using SharedLibrary.Helper.StaticInfo;
using SharedLibrary.LocalDataBase;
using System.Data.SQLite;
using Dapper;
using SharedLibrary.Provider;
using System.Collections.Generic;
using SharedLibrary.Commands;

namespace SharedLibrary.ViewModel
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        #region Темы

        #region Команда для применения обычной темы

        private AsyncCommand themeAcceptor;

        public AsyncCommand ThemeAcceptor => themeAcceptor ?? (themeAcceptor = new AsyncCommand(obj => SelectThemeAsync(obj, false)));

        #endregion

        #region Команда для применения глобальной темы

        private AsyncCommand globalThemeAcceptor;

        public AsyncCommand GlobalThemeAcceptor => globalThemeAcceptor ?? (globalThemeAcceptor = new AsyncCommand(obj => SelectThemeAsync(obj, true)));

        #endregion 

        #region Shared Methods

        public async Task SelectThemeAsync(object obj, bool isGlobal)
        {
            if (obj != null && obj is string ThemeName)
            {
                await AcceptThemeAsync(ThemeName, isGlobal);
            }
        }

        public async Task AcceptThemeAsync(string ThemeName, bool isGlobal)
        {
            if (SharedProvider.GetFromDictionaryByKey(nameof(MainWindowViewModel)) is MainWindowViewModel mainWindowViewModel)
            {
                var themes = SharedProvider.GetFromDictionaryByKey(InfoKeys.ThemesKey) as List<ThemeBase>;

                var selectedTheme = themes.FirstOrDefault(x => x.Name == ThemeName);

                var parameters = new { themeName = ThemeName };

                if (selectedTheme == null) return;

                if (isGlobal)
                {
                    if (mainWindowViewModel.SelectedThemeDarkOrLight.UriPath != selectedTheme.UriPath)
                    {
                        mainWindowViewModel.SelectedThemeDarkOrLight = selectedTheme;

                        using (var slc = new SQLiteConnection(SQLExecutor.LoadConnectionString))
                        {
                            await slc.OpenAsync();
                            await slc.ExecuteAsync($@"UPDATE {nameof(Settings)} SET Value = @themeName WHERE Name = '{InfoKeys.SelectedThemeDarkOrLightKey}' ", parameters);
                        }

                        await Message("Тема изменена");
                    }
                    else
                    {
                        await Message("Тема уже применена");
                    }
                }
                else
                {
                    if (mainWindowViewModel.SelectedTheme.UriPath != selectedTheme.UriPath)
                    {
                        mainWindowViewModel.SelectedTheme = selectedTheme;

                        using (var slc = new SQLiteConnection(SQLExecutor.LoadConnectionString))
                        {
                            await slc.OpenAsync();
                            await slc.ExecuteAsync($@"UPDATE {nameof(Settings)} SET Value = @themeName WHERE Name = '{InfoKeys.SelectedThemeKey}' ", parameters);
                        }

                        await Message("Тема изменена");
                    }
                    else
                    {
                        await Message("Тема уже применена");
                    }
                }
            }
        }

        #endregion

        #endregion

        #region Обновление

        #region Fields

        /// <summary>
        /// Используется для вычисления скорости
        /// </summary>
        public Stopwatch sw;

        /// <summary>
        /// Используется для скачивания
        /// </summary>
        public WebClient client;

        /// <summary>
        /// Показывает версию текущую
        /// </summary>
        public string AppVersion => "Version: " + GetVersion();

        /// <summary>
        /// Правила для формирования обновления
        /// </summary>
        public string LinkRules =>
$@"1. Файл должен скачиваться по ссылке из интернета или локального компьютера.
2. Файл должен содержать сразу все файлы программы, а не быть с папкой и вложенными файлами.
3. Файл должен быть Zip архивом без пароля";

        /// <summary>
        /// Путь ко временной таблице Temp
        /// </summary>
        public string TempFolderPath { get; set; }

        /// <summary>
        /// Путь ко временной таблице Temp со скаченным туда файлом из ссылки
        /// </summary>
        public string TempFolderWithFilePath { get; set; }

        /// <summary>
        /// Путь где в текущий момент находится исполняемый файл
        /// </summary>
        public string ProgramFolderWithFilePath { get; set; }

        public string CurrentProgramm { get; set; } = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName);

        /// <summary>
        /// Сколько байтов весит файл
        /// </summary>
        public long TotalBytes { get; set; }

        #region Ссылка для файла

        private bool isHavePath => !string.IsNullOrEmpty(LinkData);

        /// <summary>
        /// Ссылка для файла
        /// </summary>
        public bool IsHavePath
        {
            get
            {
                return isHavePath;
            }
            set
            {
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsHavePath)));
            }
        }

        #endregion

        #region Ссылка для файла

        private string linkData = string.Empty;

        /// <summary>
        /// Ссылка для файла
        /// </summary>
        public string LinkData
        {
            get
            {
                return linkData;
            }
            set
            {
                linkData = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(LinkData)));
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsHavePath)));
            }
        }

        #endregion

        #region Показывает текущую скорость

        private string downloadSpeed = "0 kb/s";

        /// <summary>
        /// Показывает текущую скорость
        /// </summary>
        public string DownloadSpeed
        {
            get
            {
                return downloadSpeed;
            }
            set
            {
                downloadSpeed = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(DownloadSpeed)));
            }
        }

        #endregion

        #region Показывает выполнение в процентах

        private string downloadPercent = "0%";

        /// <summary>
        /// Показывает выполнение в процентах
        /// </summary>
        public string DownloadPercent
        {
            get
            {
                return downloadPercent;
            }
            set
            {
                downloadPercent = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(DownloadPercent)));
            }
        }

        #endregion

        #region Показывает сколько скачано из скольки

        private string download = "0,00 MB's / 0,00 MB's";

        /// <summary>
        /// Показывает сколько скачано из скольки
        /// </summary>
        public string Download
        {
            get
            {
                return download;
            }
            set
            {
                download = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Download)));
            }
        }

        #endregion

        #region Используется для показа информации о скачивании

        private bool isDownload = false;

        /// <summary>
        /// Используется для показа информации о скачивании
        /// </summary>
        public bool IsDownload
        {
            get
            {
                return isDownload;
            }
            set
            {
                isDownload = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsDownload)));
            }
        }

        #endregion

        #region Значение задаваемое для ProgressBar

        private double progressBarValue = 0;

        /// <summary>
        /// Значение задаваемое для ProgressBar
        /// </summary>
        public double ProgressBarValue
        {
            get
            {
                return progressBarValue;
            }
            set
            {
                progressBarValue = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ProgressBarValue)));
            }
        }

        #endregion

        #endregion

        #region Команда для начала обновления программы

        private AsyncCommand updateCommand;
        public AsyncCommand UpdateCommand => updateCommand ?? (updateCommand = new AsyncCommand(x => UpdateProgramAsync(), o => CanUpdate()));

        public bool CanUpdate()
        {
            return !string.IsNullOrEmpty(LinkData);
        }

        /// <summary>
        /// Получение пути исполняемого файла
        /// </summary>
        public string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        /// <summary>
        /// Получение длинны файла из потока
        /// </summary>
        /// <param name="stream">Поток</param>
        public long GetLength(Stream stream)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                int count = 0;
                do
                {
                    byte[] buf = new byte[1024];
                    count = stream.Read(buf, 0, 1024);
                    ms.Write(buf, 0, count);
                } while (stream.CanRead && count > 0);

                return ms.Length;
            }
        }

        /// <summary>
        /// Обновление по локальному пути
        /// </summary>
        private async Task UpdateProgramCommandAsync()
        {
            try
            {
                Process pc = new Process();
                pc.StartInfo.FileName = "cmd.exe";

                string cdC = $@"/C cd C:\\";//подключимся к диску С
                string timeout = $@"timeout /t 1"; //ожидание
                string removeProgramFolderWithFilePath = $@"powershell Remove-Item {ProgramFolderWithFilePath}\\* -Recurse -Force"; //удалим текущую папку где существует exe, по сути откуда сейчас работаем
                string expandArchive = $@"powershell Expand-Archive {TempFolderWithFilePath} -DestinationPath {ProgramFolderWithFilePath}"; //разархивация архива zip
                string startProgramm = $@"start /D ""{ProgramFolderWithFilePath}\"" {CurrentProgramm}.exe"; //запуск программы
                string removeTempFolder = $@"powershell Remove-Item {TempFolderPath} -Recurse -Force"; //удалим временную папку куда шла разархивация

                pc.StartInfo.Arguments = $"{cdC} && {removeProgramFolderWithFilePath} && {expandArchive} && {timeout}  && {startProgramm} && {removeTempFolder}";
                pc.Start();

                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Application.Current.MainWindow.Close();
                }));
            }
            catch (Exception ex)
            {
                await Message(ex.Message);
            }
        }

        /// <summary>
        /// Обновление программы
        /// </summary>
        public async Task UpdateProgramAsync()
        {
            ProgramFolderWithFilePath = AssemblyDirectory;
            
            Stream contentStream = null;
            string fileName = string.Empty;
            TempFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\" + CurrentProgramm + @"\Temp";

            if (File.Exists(LinkData))
            {
                var fileStream = File.OpenRead(LinkData);
                contentStream = fileStream as Stream;
                fileName = Path.GetFileNameWithoutExtension(fileStream.Name);

                TempFolderWithFilePath = fileStream.Name;
                CheckFolder(TempFolderPath);
                await UpdateProgramCommandAsync();
            }
            else
            {
                try
                {
                    IsDownload = true; 
                    client = new WebClient();
                    contentStream = client.OpenRead(LinkData);
                    string header_contentDisposition = client.ResponseHeaders["content-disposition"];
                    fileName = new ContentDisposition(header_contentDisposition).FileName.Replace(' ', '_');

                    TotalBytes = GetLength(contentStream);

                    Download = $"{0:0.00} MB's / {TotalBytes / 1024d / 1024d:0.00} MB's";

                    TempFolderWithFilePath = $@"{TempFolderPath}\{fileName}";
                    CheckFolder(TempFolderPath);

                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(CompletedAsync);
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
                    sw = new Stopwatch();
                    sw.Start();
                    await client.DownloadFileTaskAsync(new Uri(LinkData), TempFolderWithFilePath);
                }
                catch (Exception ex)
                {
                    await Message(ex.Message);
                    IsDownload = false;
                }
                finally
                {
                    client.Dispose();
                }
            } 
        }

        /// <summary>
        /// Событие скачивания
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadSpeed = string.Format("{0} kb/s", (e.BytesReceived / 1024d / sw.Elapsed.TotalSeconds).ToString("0.00"));

            var received = e.BytesReceived / 1024d / 1024d;
            var total = TotalBytes / 1024d / 1024d;

            ProgressBarValue = (int)((e.BytesReceived * 100) / TotalBytes);

            DownloadPercent = ProgressBarValue.ToString() + "%";

            Download = $"{received:0.00} MB's / {total:0.00} MB's";
        }

        /// <summary>
        /// Завершение скачивания
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CompletedAsync(object sender, AsyncCompletedEventArgs e)
        {
            IsDownload = false;
            sw.Reset();
            sw.Stop();
            if (e.Cancelled == true)
            {
                await Message("Скачивание отменено");
                if (Directory.Exists(TempFolderPath)) Directory.Delete(TempFolderPath, true);
                client.Dispose();

                DownloadSpeed = "0 kb/s";
                DownloadPercent = "0%";
                Download = "0,00 MB's / 0,00 MB's";
                ProgressBarValue = 0;

                return;
            }
            else if (e.Error != null)
            {
                string error = e.Error.ToString();
                await Message(error);
                if (Directory.Exists(TempFolderPath)) Directory.Delete(TempFolderPath, true);
                return;
            }
            else
            {
                await Message("Скачивание завершено");
                await UpdateProgramCommandAsync();
            }
        }

        /// <summary>
        /// Очистка папки от файлов
        /// </summary>
        /// <param name="FolderName"></param>
        private void ClearFolder(string FolderName)
        {
            DirectoryInfo dir = new DirectoryInfo(FolderName);

            foreach (FileInfo fi in dir.GetFiles())
            {
                fi.Delete();
            }

            foreach (DirectoryInfo di in dir.GetDirectories())
            {
                ClearFolder(di.FullName);
                di.Delete();
            }
        }

        /// <summary>
        /// Создание папки если не существует и если существует то очистить ее
        /// </summary>
        /// <param name="path"></param>
        private void CheckFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                ClearFolder(path);
            }
        }

        #endregion

        #region Команда для отмены обновления

        private RelayCommand cancelDownloadCommand;
        public RelayCommand CancelDownloadCommand => cancelDownloadCommand ?? (cancelDownloadCommand = new RelayCommand(x => Cancel(), y => CancelCanUse()));
        public bool CancelCanUse()
        {
            if (ProgressBarValue >= 0 && ProgressBarValue < 100)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Отмена скачивания
        /// </summary>
        private void Cancel()
        {
            if (this.client != null)
            {
                this.client.CancelAsync();
            }
        }

        #endregion

        #region Команда для Drag and Drop

        private AsyncCommand previewDropCommand;
        public AsyncCommand PreviewDropCommand => previewDropCommand ?? (previewDropCommand = new AsyncCommand(obj => DragAndDropAsync(obj)));
        public async Task DragAndDropAsync(object obj)
        {
            var list = (obj as DataObject).GetFileDropList();
            if (list.Count == 0)
            {
                await Message("Ни один файл не был загружен");
                return;
            }

            LinkData = list[0];
            UpdateCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #endregion

    }
}
