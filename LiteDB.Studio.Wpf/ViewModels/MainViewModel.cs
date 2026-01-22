using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB.Studio.Wpf.Services;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LiteDB.Studio.Wpf.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDatabaseService _dbService;

        [ObservableProperty]
        private string _cursorText = string.Empty;

        [ObservableProperty]
        private string _elapsedText = string.Empty;

        [ObservableProperty]
        private bool _isConnected = false;

        [ObservableProperty]
        private string _currentDatabase = string.Empty;

        [ObservableProperty]
        private bool _transactionActive = false;

        private bool _loadLastDatabaseOnStartup = false;
        private TabViewModel? _selectedTab;

        public ObservableCollection<TabViewModel> Tabs { get; } = new ObservableCollection<TabViewModel>();
        public ObservableCollection<string> RecentDatabases { get; } = new ObservableCollection<string>();

        public DatabaseTreeViewModel Tree { get; }
        public DataTable CurrentResults { get; } = new DataTable();

        public MainViewModel(IDatabaseService dbService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            Tree = new DatabaseTreeViewModel(dbService);
            Tree.InsertSnippetRequested += (s, snippet) => InsertSnippet(snippet);

            _dbService.ConnectionStateChanged += OnConnectionStateChanged;
            _dbService.TransactionStateChanged += OnTransactionStateChanged;

            ConnectCommand = new AsyncRelayCommand(ConnectAsync);
            DisconnectCommand = new RelayCommand(Disconnect);
            RunCommand = new RelayCommand(Run);
            NewTabCommand = new RelayCommand(AddNewTab);
            CloseTabCommand = new RelayCommand<TabViewModel>(CloseTab);
            OpenRecentCommand = new AsyncRelayCommand<object>(OpenRecentAsync);
                OpenRecentWrapperCommand = new RelayCommand<object>(p =>
                {
                    // ensure we only forward string parameters to the async handler
                    if (p is string s && !string.IsNullOrEmpty(s)) _ = OpenRecentAsync(s);
                });
            ClearRecentCommand = new RelayCommand(ClearRecentList);
            ValidateRecentCommand = new RelayCommand(ValidateRecentList);
            RefreshTreeCommand = new AsyncRelayCommand(RefreshTreeAsync);
            InsertSnippetCommand = new RelayCommand<string>(InsertSnippet);

            // create initial + tab
            Tabs.Add(new TabViewModel(_dbService) { Title = "+", IsPlus = true });
        }

        public void Initialize()
        {
            // load persisted recent list
            foreach (var cs in LiteDB.Studio.Wpf.Util.AppSettingsManager.ApplicationSettings.RecentConnectionStrings)
            {
                RecentDatabases.Add(cs.Filename);
            }

            // auto-open last DB if requested
            if (LoadLastDatabaseOnStartup && LiteDB.Studio.Wpf.Util.AppSettingsManager.IsLastDbExist())
            {
                var last = LiteDB.Studio.Wpf.Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings?.Filename;
                if (!string.IsNullOrEmpty(last))
                {
                    _ = OpenRecentAsync(last);
                }
            }
        }

        public IAsyncRelayCommand ConnectCommand { get; }
        public IRelayCommand DisconnectCommand { get; }
        public IRelayCommand RunCommand { get; }
        public IRelayCommand NewTabCommand { get; }
        public IRelayCommand<TabViewModel> CloseTabCommand { get; }
        public IAsyncRelayCommand<object> OpenRecentCommand { get; }
        public IRelayCommand<object> OpenRecentWrapperCommand { get; }
        public IRelayCommand ClearRecentCommand { get; }
        public IRelayCommand ValidateRecentCommand { get; }
        public IAsyncRelayCommand RefreshTreeCommand { get; }
        public IRelayCommand<string> InsertSnippetCommand { get; }

        public TabViewModel? SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (SetProperty(ref _selectedTab, value))
                {
                    if (_selectedTab != null && _selectedTab.Title == "+")
                    {
                        AddNewTab();
                    }
                }
            }
        }

        public bool LoadLastDatabaseOnStartup
        {
            get => _loadLastDatabaseOnStartup;
            set
            {
                if (SetProperty(ref _loadLastDatabaseOnStartup, value))
                {
                    LiteDB.Studio.Wpf.Util.AppSettingsManager.ApplicationSettings.LoadLastDbOnStartup = value;
                    LiteDB.Studio.Wpf.Util.AppSettingsManager.PersistData();
                }
            }
        }

        private async Task ConnectAsync()
        {
            // If already connected, perform disconnect instead (toggle behavior)
            if (_dbService.IsConnected)
            {
                _dbService.Disconnect();
                IsConnected = false;
                CursorText = "Disconnected";
                return;
            }
            // show connection manager dialog
            var vm = new ConnectionManagerViewModel();
            var win = new LiteDB.Studio.Wpf.Views.ConnectionManagerWindow
            {
                Owner = System.Windows.Application.Current?.MainWindow,
                DataContext = vm
            };

            var shown = win.ShowDialog();
            if (shown != true) return;

            var filename = vm.Filename;
            if (string.IsNullOrEmpty(filename)) return;

            var cs = new LiteDB.ConnectionString(filename);

            // map ConnectionManagerViewModel -> ConnectionString (same logic as WinForms ConnectionForm)
            try
            {
                cs.Connection = vm.Mode == ConnectionMode.Direct ? LiteDB.ConnectionType.Direct : LiteDB.ConnectionType.Shared;

                cs.Filename = vm.Filename;
                cs.ReadOnly = vm.ReadOnly;
                cs.Upgrade = vm.UpgradeFromV4;

                cs.Password = !string.IsNullOrWhiteSpace(vm.Password) ? vm.Password.Trim() : null;

                const long MB = 1024 * 1024;
                if (vm.InitialSize > 0)
                {
                    cs.InitialSize = vm.InitialSize * MB;
                }

                if (!string.IsNullOrWhiteSpace(vm.CollationLeft))
                {
                    var collation = vm.CollationLeft;
                    if (!string.IsNullOrWhiteSpace(vm.CollationRight))
                    {
                        collation += "/" + vm.CollationRight;
                    }

                    cs.Collation = new LiteDB.Collation(collation);
                }

                CursorText = "Opening " + filename;
                ElapsedText = "Reading...";

                await _dbService.ConnectAsync(cs.ToString(), System.Threading.CancellationToken.None);

                // persist last connection and recent list using same AppSettingsManager calls
                LiteDB.Studio.Wpf.Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings = cs;
                LiteDB.Studio.Wpf.Util.AppSettingsManager.AddToRecentList(cs);

                IsConnected = true;
                CurrentDatabase = filename;

                // populate tree view to match WinForms behavior
                try
                {
                    await Tree.LoadRootNodesAsync();
                }
                catch
                {
                    // non-fatal, ignore tree population errors
                }

                if (!RecentDatabases.Contains(filename))
                {
                    RecentDatabases.Insert(0, filename);
                }
            }
            catch (Exception ex)
            {
                CursorText = "Error: " + ex.Message;
                IsConnected = false;
                CurrentDatabase = string.Empty;
            }
            finally
            {
                ElapsedText = string.Empty;
            }
        }

        private async Task RefreshTreeAsync()
        {
            Tree.RootNodes.Clear();
            await Tree.LoadRootNodesAsync();
        }

        private void InsertSnippet(string snippet)
        {
            if (SelectedTab == null || string.IsNullOrEmpty(snippet)) return;

            var text = SelectedTab.EditorText ?? string.Empty;
            var offset = SelectedTab.CaretOffset;

            // Ensure offset is within bounds
            if (offset < 0) offset = 0;
            if (offset > text.Length) offset = text.Length;

            SelectedTab.EditorText = text.Insert(offset, snippet);
            SelectedTab.IsModified = true;
        }

        private void Disconnect()
        {
            var unsavedTabs = Tabs.Where(t => t.IsModified && !t.IsPlus).ToList();
            if (unsavedTabs.Any())
            {
                var result = MessageBox.Show("You have unsaved changes in some tabs. Do you want to disconnect anyway?", "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;
            }

            try
            {
                _dbService.Disconnect();
            }
            finally
            {
                IsConnected = false;
                CurrentDatabase = string.Empty;
                TransactionActive = false;
                CursorText = "Disconnected";
            }
        }

        private void Run()
        {
            // Execute current editor SQL (stub)
            var sql = SelectedTab?.EditorText ?? string.Empty;
            // populate fake results table to show in grid
            PopulateSampleResults(sql);
            ElapsedText = "0s";
        }

        private void AddNewTab()
        {
            var newTab = new TabViewModel(_dbService) { Title = $"Query {Tabs.Count}" };
            // insert before plus tab
            var plus = Tabs.FirstOrDefault(t => t.Title == "+");
            if (plus != null)
            {
                var idx = Tabs.IndexOf(plus);
                Tabs.Insert(idx, newTab);
                SelectedTab = newTab;
            }
            else
            {
                Tabs.Add(newTab);
                SelectedTab = newTab;
            }
        }

        public void AddSqlSnippet(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return;

            // if there's no selected tab or selected tab is the plus tab, or current content is empty -> set into current
            if (SelectedTab == null || SelectedTab.Title == "+" || string.IsNullOrWhiteSpace(SelectedTab.EditorText))
            {
                // ensure there's a non-plus tab to place content
                if (SelectedTab == null || SelectedTab.Title == "+")
                {
                    AddNewTab();
                }

                SelectedTab.EditorText = sql.Replace("\\n", "\n");
            }
            else
            {
                // insert new tab before plus
                var plus = Tabs.FirstOrDefault(t => t.Title == "+");
                var newTab = new TabViewModel(_dbService) { Title = $"Query {Tabs.Count}", EditorText = sql.Replace("\\n", "\n") };
                if (plus != null)
                {
                    var idx = Tabs.IndexOf(plus);
                    Tabs.Insert(idx, newTab);
                    SelectedTab = newTab;
                }
                else
                {
                    Tabs.Add(newTab);
                    SelectedTab = newTab;
                }
            }
        }















        private void CloseTab(TabViewModel tab)
        {
            if (tab == null || tab.IsPlus) return;
            var idx = Tabs.IndexOf(tab);
            Tabs.Remove(tab);
            if (Tabs.Count > 0)
            {
                SelectedTab = Tabs[Math.Max(0, idx - 1)];
            }
        }

        public async System.Threading.Tasks.Task OpenRecentAsync(object? filename)
        {
            var fname = filename as string;
            if (string.IsNullOrEmpty(fname)) return;

            CursorText = "Opening: " + fname;

            try
            {
                var cs = new LiteDB.ConnectionString(fname);
                await _dbService.ConnectAsync(cs.ToString(), System.Threading.CancellationToken.None);

                LiteDB.Studio.Wpf.Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings = cs;
                LiteDB.Studio.Wpf.Util.AppSettingsManager.AddToRecentList(cs);

                IsConnected = true;
                CurrentDatabase = fname;
            }
            catch (Exception ex)
            {
                CursorText = "Error: " + ex.Message;
                IsConnected = false;
                CurrentDatabase = string.Empty;
            }
        }

        private void ClearRecentList()
        {
            LiteDB.Studio.Wpf.Util.AppSettingsManager.ClearRecentList();
            RecentDatabases.Clear();
        }

        private void ValidateRecentList()
        {
            LiteDB.Studio.Wpf.Util.AppSettingsManager.ValidateRecentList();
            RecentDatabases.Clear();
            foreach (var cs in LiteDB.Studio.Wpf.Util.AppSettingsManager.ApplicationSettings.RecentConnectionStrings)
            {
                RecentDatabases.Add(cs.Filename);
            }
        }

        private void PopulateSampleResults(string sql)
        {
            CurrentResults.Clear();
            CurrentResults.Columns.Clear();
            CurrentResults.Columns.Add("Id");
            CurrentResults.Columns.Add("Content");

            for (int i = 0; i < 10; i++)
            {
                var row = CurrentResults.NewRow();
                row[0] = i;
                row[1] = sql + " - row " + i;
                CurrentResults.Rows.Add(row);
            }
        }

        private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
        {
            // Handle connection state changes if needed
        }

        private void OnTransactionStateChanged(object? sender, TransactionStateChangedEventArgs e)
        {
            TransactionActive = e.TransactionActive;
        }
    }
}
