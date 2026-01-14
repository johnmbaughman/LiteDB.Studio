using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB.Studio.Wpf.Services;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Data;
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
            var dlg = new OpenFileDialog()
            {
                Filter = "LiteDB files (*.db)|*.db|All files (*.*)|*.*",
                Title = "Open LiteDB file"
            };

            var result = dlg.ShowDialog();
            if (result != true) return;

            var filename = dlg.FileName;

            var cs = new LiteDB.ConnectionString(filename);

            try
            {
                CursorText = "Opening " + filename;
                ElapsedText = "Reading...";

                await _dbService.ConnectAsync(cs);

                // persist last connection and recent list
                LiteDB.Studio.Wpf.Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings = cs;
                LiteDB.Studio.Wpf.Util.AppSettingsManager.AddToRecentList(cs);

                IsConnected = true;

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
