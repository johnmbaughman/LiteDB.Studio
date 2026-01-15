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

namespace LiteDB.Studio.Wpf.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly IDatabaseService _dbService;

        private string _cursorText = string.Empty;
        private string _elapsedText = string.Empty;
        private bool _isConnected = false;
        private bool _loadLastDatabaseOnStartup = false;
        private TabViewModel? _selectedTab;

        public ObservableCollection<TabViewModel> Tabs { get; } = new ObservableCollection<TabViewModel>();
        public ObservableCollection<string> RecentDatabases { get; } = new ObservableCollection<string>();
        public ObservableCollection<DbTreeNode> DatabaseTree { get; } = new ObservableCollection<DbTreeNode>();
        public DataTable CurrentResults { get; } = new DataTable();

        public MainViewModel(IDatabaseService dbService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));

            ConnectCommand = new AsyncRelayCommand(ConnectAsync);
            DisconnectCommand = new RelayCommand(Disconnect);
            RunCommand = new RelayCommand(Run);
            AddTabCommand = new RelayCommand(AddNewTab);
            CloseTabCommand = new RelayCommand<TabViewModel>(CloseTab);
            OpenRecentCommand = new AsyncRelayCommand<object>(OpenRecentAsync);
                OpenRecentWrapperCommand = new RelayCommand<object>(p =>
                {
                    // ensure we only forward string parameters to the async handler
                    if (p is string s && !string.IsNullOrEmpty(s)) _ = OpenRecentAsync(s);
                });
            ClearRecentCommand = new RelayCommand(ClearRecentList);
            ValidateRecentCommand = new RelayCommand(ValidateRecentList);

            // create initial + tab
            Tabs.Add(new TabViewModel { Title = "+", IsPlus = true });
        }

        public IAsyncRelayCommand ConnectCommand { get; }
        public IRelayCommand DisconnectCommand { get; }
        public IRelayCommand RunCommand { get; }
        public IRelayCommand AddTabCommand { get; }
        public IRelayCommand<TabViewModel> CloseTabCommand { get; }
        public IAsyncRelayCommand<object> OpenRecentCommand { get; }
        public IRelayCommand<object> OpenRecentWrapperCommand { get; }
        public IRelayCommand ClearRecentCommand { get; }
        public IRelayCommand ValidateRecentCommand { get; }

        public TabViewModel? SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (SetProperty(ref _selectedTab, value))
                {
                    if (_selectedTab != null && _selectedTab.IsPlus)
                    {
                        AddNewTab();
                    }
                }
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public string CursorText
        {
            get => _cursorText;
            set => SetProperty(ref _cursorText, value);
        }

        public string ElapsedText
        {
            get => _elapsedText;
            set => SetProperty(ref _elapsedText, value);
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

                await _dbService.ConnectAsync(cs);

                // persist last connection and recent list using same AppSettingsManager calls
                LiteDB.Studio.Wpf.Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings = cs;
                LiteDB.Studio.Wpf.Util.AppSettingsManager.AddToRecentList(cs);

                IsConnected = true;

                // populate tree view to match WinForms behavior
                try
                {
                    DatabaseTree.Clear();
                    if (_dbService.Database is LiteDB.LiteDatabase db)
                    {
                        BuildDatabaseTree(db, filename);
                    }
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
            }
            finally
            {
                ElapsedText = string.Empty;
            }
        }

        private void Disconnect()
        {
            try
            {
                _dbService.Disconnect();
            }
            finally
            {
                IsConnected = false;
                CursorText = "Disconnected";
            }
        }

        private void Run()
        {
            // Execute current editor SQL (stub)
            var sql = SelectedTab?.Content ?? string.Empty;
            // populate fake results table to show in grid
            PopulateSampleResults(sql);
            ElapsedText = "0s";
        }

        private void AddNewTab()
        {
            var newTab = new TabViewModel { Title = $"Query {Tabs.Count}", Content = "", IsPlus = false };
            // insert before plus tab
            var plus = Tabs.FirstOrDefault(t => t.IsPlus);
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
            if (SelectedTab == null || SelectedTab.IsPlus || string.IsNullOrWhiteSpace(SelectedTab.Content))
            {
                // ensure there's a non-plus tab to place content
                if (SelectedTab == null || SelectedTab.IsPlus)
                {
                    AddNewTab();
                }

                SelectedTab.Content = sql.Replace("\\n", "\n");
            }
            else
            {
                // insert new tab before plus
                var plus = Tabs.FirstOrDefault(t => t.IsPlus);
                var newTab = new TabViewModel { Title = $"Query {Tabs.Count}", Content = sql.Replace("\\n", "\n"), IsPlus = false };
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

        public void RefreshDatabaseTree()
        {
            try
            {
                DatabaseTree.Clear();
                if (_dbService.Database is LiteDB.LiteDatabase db)
                {
                    // try to recover filename from last connection settings
                    var filename = LiteDB.Studio.Wpf.Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings?.Filename ?? "";
                    BuildDatabaseTree(db, filename);
                }
            }
            catch
            {
                // ignore
            }
        }

        private void BuildDatabaseTree(LiteDB.LiteDatabase db, string filename)
        {
            var root = new DbTreeNode { Header = Path.GetFileName(filename), Icon = "pack://application:,,,/LiteDB.Studio.Wpf;component/Resources/database.png" };
            var system = new DbTreeNode { Header = "System", Icon = "pack://application:,,,/LiteDB.Studio.Wpf;component/Resources/folder_page.png" };
            root.Children.Add(system);

            var sc = db.GetCollection("$cols")
                .Query()
                .Where("type = 'system'")
                .OrderBy("name")
                .ToDocuments();

            foreach (var doc in sc)
            {
                var name = doc["name"].AsString;
                system.Children.Add(new DbTreeNode { Header = name, Tag = $"SELECT $ FROM {name}", Icon = "pack://application:,,,/LiteDB.Studio.Wpf;component/Resources/page_white_gear.png" });
            }

            foreach (var key in db.GetCollectionNames().OrderBy(x => x))
            {
                root.Children.Add(new DbTreeNode { Header = key, Tag = $"SELECT $ FROM {key};", Icon = "pack://application:,,,/LiteDB.Studio.Wpf;component/Resources/table.png" });
            }

            DatabaseTree.Add(root);
        }

        public long? GetCollectionCount(string name)
        {
            try
            {
                if (_dbService.Database is LiteDB.LiteDatabase db)
                {
                    var col = db.GetCollection(name);
                    return col?.Count();
                }
            }
            catch { }
            return null;
        }

        public string[] GetCollectionIndexes(string name)
        {
            try
            {
                if (_dbService.Database is LiteDB.LiteDatabase db)
                {
                    // try system collection $indexes
                    try
                    {
                        var coll = db.GetCollection("$indexes");
                        var docs = coll.Query().Where($"collection = '{name}'").ToDocuments();
                        return docs.Select(d => d.ToString()).ToArray();
                    }
                    catch
                    {
                        return Array.Empty<string>();
                    }
                }
            }
            catch { }
            return Array.Empty<string>();
        }

        public void DropCollection(string name)
        {
            try
            {
                if (_dbService.Database is LiteDB.LiteDatabase db)
                {
                    db.DropCollection(name);
                    RefreshDatabaseTree();
                }
            }
            catch { }
        }

        public bool RenameCollection(string oldName, string newName)
        {
            try
            {
                if (_dbService.Database is LiteDB.LiteDatabase db)
                {
                    db.RenameCollection(oldName, newName);
                    RefreshDatabaseTree();
                    return true;
                }
            }
            catch { }
            return false;
        }

        public void ExportCollectionToJson(string name, string path)
        {
            try
            {
                if (_dbService.Database is LiteDB.LiteDatabase db)
                {
                    var col = db.GetCollection(name);
                    using var sw = new System.IO.StreamWriter(path);
                    foreach (var doc in col.FindAll())
                    {
                        sw.WriteLine(doc.ToString());
                    }
                }
            }
            catch { }
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
                await _dbService.ConnectAsync(cs);

                LiteDB.Studio.Wpf.Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings = cs;
                LiteDB.Studio.Wpf.Util.AppSettingsManager.AddToRecentList(cs);

                IsConnected = true;
            }
            catch (Exception ex)
            {
                CursorText = "Error: " + ex.Message;
                IsConnected = false;
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
    }
}
