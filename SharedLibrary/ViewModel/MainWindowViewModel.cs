using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using SharedLibrary.Helper.Classes;
using static SharedLibrary.Helper.StaticInfo.Enums;
using System.Collections.ObjectModel;
using System;
using SharedLibrary.AbstractClasses;
using Dragablz;
using MaterialDesignThemes.Wpf;
using SharedLibrary.Helper.StaticInfo;
using SharedLibrary.DataBase;
using SharedLibrary.LocalDataBase;
using System.Data.SQLite;
using SharedLibrary.LocalDataBase.Models;
using Dapper;
using System.Threading.Tasks;
using SharedLibrary.Provider;
using SharedLibrary.Commands;

namespace SharedLibrary.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public MainWindowViewModel()
        {
            isMessagePanelContent = new SnackbarMessageQueue(TimeSpan.FromMilliseconds(1200));

            Modules = SharedProvider.GetFromDictionaryByKey(InfoKeys.ModulesKey) as List<ModuleBase>;

            var leftMenu = Modules.Where(x => x.modelClass == ModelBaseClasses.LeftMenu).OrderBy(x => x.Num);
            LeftMenuNodes = GetTreeViewItems(leftMenu);

            var rightMenu = Modules.Where(x => x.modelClass == ModelBaseClasses.RightMenu).OrderBy(x => x.Num);
            RightMenuNodes = GetTreeViewItems(rightMenu);

            SetViewModels();

            Task.Factory.StartNew(async () =>
                //загрузка тем из локальной БД
                await GetThemesAsync()
            );

            Task.Factory.StartNew(async () =>
                //подключение к внешней БД (загрузка connectionString из локальной БД)
                await DatabaseOperation.ConnectionStringAsync()
            );
        }

        #region Fields

        public List<ThemeBase> Themes { get; private set; }

        public List<ThemeBase> DarkLightThemes { get; private set; }

        public ObservableCollection<TabControlData> ViewModels { get; set; } = new ObservableCollection<TabControlData>();

        private IList<ModuleBase> Modules { get; set; }

        #region Выбранный Таб Контрол

        private TabControlData _selectedViewModel;

        public TabControlData SelectedViewModel
        {
            get { return _selectedViewModel; }
            set
            {
                if (_selectedViewModel == value) return;
                _selectedViewModel = value;

                PropertyChanged(this, new PropertyChangedEventArgs(nameof(SelectedViewModel)));
            }
        }

        #endregion

        #region Дерево для левого меню

        private ObservableCollection<Node> leftNodeData = new ObservableCollection<Node>();
        public ObservableCollection<Node> LeftMenuNodes
        {
            get { return leftNodeData; }
            set
            {
                leftNodeData = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(LeftMenuNodes)));
            }
        }

        #endregion

        #region Дерево для правого меню

        private ObservableCollection<Node> rightNodeData = new ObservableCollection<Node>();
        public ObservableCollection<Node> RightMenuNodes
        {
            get { return rightNodeData; }
            set
            {
                rightNodeData = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(RightMenuNodes)));
            }
        }

        #endregion

        #region Видимость прогресс бара бесконечного

        public bool isLoadingPanelVisible = false;

        public bool IsLoadingPanelVisible
        {
            get
            {
                return isLoadingPanelVisible;
            }
            set
            {
                isLoadingPanelVisible = SharedProvider.AnyActiveTask();
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsLoadingPanelVisible)));
            }
        }

        #endregion

        #region Сообщение в SnackBar

        public SnackbarMessageQueue isMessagePanelContent;

        public SnackbarMessageQueue IsMessagePanelContent
        {
            get
            {
                return isMessagePanelContent;
            }
            set
            {
                isMessagePanelContent.IgnoreDuplicate = true;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsMessagePanelContent)));
            }
        }

        #endregion

        #region Обычная тема

        private ThemeBase _SelectedTheme;
        public ThemeBase SelectedTheme
        {
            get { return _SelectedTheme; }
            set
            {
                if (value == _SelectedTheme)
                    return;
                if (_SelectedTheme != null)
                    _SelectedTheme.Deactivate();

                _SelectedTheme = value;
                _SelectedTheme.SelectTheme();
            }
        }

        #endregion

        #region Глобальная тема

        private ThemeBase _SelectedThemeDarkOrLight;
        public ThemeBase SelectedThemeDarkOrLight
        {
            get { return _SelectedThemeDarkOrLight; }
            set
            {
                if (value == _SelectedThemeDarkOrLight)
                    return;
                if (_SelectedThemeDarkOrLight != null)
                    _SelectedThemeDarkOrLight.Deactivate();

                _SelectedThemeDarkOrLight = value;
                _SelectedThemeDarkOrLight.SelectTheme();
            }
        }

        #endregion

        #endregion

        #region Methods

        public async Task GetThemesAsync()
        {
            var themes = SharedProvider.GetFromDictionaryByKey(InfoKeys.ThemesKey) as List<ThemeBase>;

            Settings selectedThemeData;
            Settings selectedThemeDarkOrLightData;

            using (var slc = new SQLiteConnection(SQLExecutor.LoadConnectionString))
            {
                await slc.OpenAsync();
                //обычная тема
                selectedThemeData = (await slc.QueryAsync<Settings>($"SELECT * FROM {nameof(Settings)} Where Name = '{InfoKeys.SelectedThemeKey}'")).FirstOrDefault();
            }

            if (selectedThemeData != null && !string.IsNullOrEmpty(selectedThemeData.Value))
            {
                SelectedTheme = themes.FirstOrDefault(x => x.ThemeClass == ThemeBaseClasses.GeneralTheme && x.Name == selectedThemeData.Value);
            }
            else
            {
                SelectedTheme = themes.FirstOrDefault(x => x.ThemeClass == ThemeBaseClasses.GeneralTheme);
                selectedThemeData = new Settings() { Name = InfoKeys.SelectedThemeKey, Value = SelectedTheme.Name };
                await SQLExecutor.InsertExecutorAsync(selectedThemeData, selectedThemeData);
            }

            using (var slc = new SQLiteConnection(SQLExecutor.LoadConnectionString))
            {
                await slc.OpenAsync();
                //глобальная тема
                selectedThemeDarkOrLightData = (await slc.QueryAsync<Settings>($"SELECT * FROM {nameof(Settings)} Where Name = '{InfoKeys.SelectedThemeDarkOrLightKey}'")).FirstOrDefault();
            }

            if (selectedThemeDarkOrLightData != null && !string.IsNullOrEmpty(selectedThemeDarkOrLightData.Value))
            {
                SelectedThemeDarkOrLight = themes.FirstOrDefault(x => x.ThemeClass == ThemeBaseClasses.GlobalTheme && x.Name == selectedThemeDarkOrLightData.Value);
            }
            else
            {
                SelectedThemeDarkOrLight = themes.FirstOrDefault(x => x.ThemeClass == ThemeBaseClasses.GlobalTheme);
                selectedThemeDarkOrLightData = new Settings() { Name = InfoKeys.SelectedThemeDarkOrLightKey, Value = SelectedThemeDarkOrLight.Name };
                await SQLExecutor.InsertExecutorAsync(selectedThemeDarkOrLightData, selectedThemeDarkOrLightData);
            }
        }

        public void SetViewModels(TabControlData viewModel = null)
        {
            var mainmenu = Modules.FirstOrDefault(x => x.modelClass == ModelBaseClasses.Main);

            if (ViewModels.FirstOrDefault(x => x.ModuleBaseItem == mainmenu) == null)
            {
                mainmenu = mainmenu ?? Modules.FirstOrDefault();

                var tabControlViewModel = new TabControlData { Name = mainmenu.Name, ViewContent = mainmenu.UserInterface, ID = mainmenu.ID, ModuleBaseItem = mainmenu };

                ViewModels.Add(tabControlViewModel);

                SelectedViewModel = tabControlViewModel;

                if (viewModel != null)
                {
                    viewModel.ModuleBaseItem.Deactivate();

                    ViewModels.Remove(viewModel);
                }
            }
        }

        /// <summary>
        /// Выбор всех элементов в виде дерева
        /// </summary>
        public ObservableCollection<Node> GetTreeViewItems(IEnumerable<ModuleBase> items)
        {

            var nodes = new ObservableCollection<Node>();

            foreach (var module in items)
            {
                if (module.ParentID == null)
                {
                    var node = new Node
                    {
                        ID = module.ID,
                        Name = module.Name,
                        ParentName = string.Empty,
                        View = module,
                        Nodes = GetNodes(module.ID)
                    };

                    nodes.Add(node);
                }
            }

            return nodes;
        }

        /// <summary>
        /// Добавляет корни в дерево
        /// </summary>
        public ObservableCollection<Node> GetNodes(Guid moduleID)
        {
            var nodes = new ObservableCollection<Node>();

            foreach (var module in Modules)
            {
                if (module.ParentID != null && module.ParentID == moduleID)
                {
                    var node = new Node
                    {
                        ID = module.ID,
                        Name = module.Name,
                        ParentName = Modules.FirstOrDefault(x => x.ID == moduleID).Name,
                        View = module,
                        Nodes = GetNodes(module.ID)
                    };

                    nodes.Add(node);
                }
            }

            return nodes;
        }

        #endregion

        #region Команда для вызова настроек

        private RelayCommand setSettingsView;
        public RelayCommand SetSettingsView => setSettingsView ?? (setSettingsView = new RelayCommand(x => SetSettings()));

        private void SetSettings()
        { 
            var settings = Modules.Where(x => x.modelClass == Enums.ModelBaseClasses.Settings).FirstOrDefault();
            AddItemInTabControl(settings.Name, settings.UserInterface, settings.ID, settings); 
        }

        #endregion

        #region Команда для добавления в ТабКонтрол

        private RelayCommand setSelectedTreeViewItem;
        public RelayCommand SetSelectedTreeViewItem => setSelectedTreeViewItem ?? (setSelectedTreeViewItem = new RelayCommand(obj => SetSelectedFunction(obj)));

        public void SetSelectedFunction(object obj)
        { 
            var Node = (Node)obj;
            if (Node != null)
            {
                AddItemInTabControl(Node.Name, Node.View.UserInterface, Node.ID, Node.View);
                Node.Selected = false;
            } 
        }

        private void AddItemInTabControl(string name, UserControl userInterface, Guid id, ModuleBase moduleBase)
        {
            var existingViewModel = ViewModels.FirstOrDefault(x => x.ID == id);
            int? index = null;
            if (existingViewModel != null)
            {
                index = ViewModels.IndexOf(existingViewModel);
            }

            if (index == null)
            {
                var mainmenu = ViewModels.Count() == 1 ? ViewModels.FirstOrDefault(x => x.ModuleBaseItem == Modules.FirstOrDefault(y => y.modelClass == ModelBaseClasses.Main)) : null;

                var TabControlViewModel = new TabControlData() { Name = name, ViewContent = userInterface, ID = id, ModuleBaseItem = moduleBase };
                ViewModels.Add(TabControlViewModel);
                SelectedViewModel = TabControlViewModel;

                if (mainmenu != null)
                {
                    ViewModels.Remove(mainmenu);
                }
            }
            else
            {
                SelectedViewModel = existingViewModel;
            }
        }

        #endregion

        #region Команда для переопределения удаления вкладки

        public ItemActionCallback ClosingTabItemHandler
        {
            get { return ClosingTabItemHandlerImpl; }
        }

        private void ClosingTabItemHandlerImpl(ItemActionCallbackArgs<TabablzControl> args)
        {
            var viewModel = args.DragablzItem.DataContext as TabControlData;
            if (ViewModels.Count() == 1)
            {
                SetViewModels(viewModel);
                args.Cancel();
            }
            else
            {
                viewModel.ModuleBaseItem.Deactivate();
                ViewModels.Remove(viewModel);
            }
        }

        #endregion

    }
}
