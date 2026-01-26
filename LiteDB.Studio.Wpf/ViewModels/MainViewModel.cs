using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB.Studio.Wpf.Services;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Windows;
using LiteDB.Studio.Mvvm.ViewModels.Shell;

namespace LiteDB.Studio.Wpf.ViewModels;

public partial class MainViewModel : ShellContentViewModel
{
    private readonly IDatabaseService _dbService;

    [ObservableProperty]
    private string _cursorText = string.Empty;

    [ObservableProperty]
    private string _elapsedText = string.Empty;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _currentDatabase = string.Empty;

    [ObservableProperty]
    private bool _transactionActive;

    private bool _loadLastDatabaseOnStartup;
    private TabViewModel? _selectedTab;

    public ObservableCollection<TabViewModel> Tabs { get; } = [];
    public ObservableCollection<string> RecentDatabases { get; } = [];

    public DatabaseTreeViewModel Tree { get; }
    public DataTable CurrentResults { get; } = new();

    public MainViewModel(IDatabaseService dbService)
    {
        _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        Tree = new DatabaseTreeViewModel(dbService);
        Tree.InsertSnippetRequested += (_, snippet) => InsertSnippet(snippet);

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
            if (p is string s && !string.IsNullOrEmpty(s)) {
                _ = OpenRecentAsync(s);
            }
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
        foreach (ConnectionString cs in Util.AppSettingsManager.ApplicationSettings.RecentConnectionStrings)
        {
            RecentDatabases.Add(cs.Filename);
        }

        // auto-open last DB if requested
        if (!LoadLastDatabaseOnStartup || !Util.AppSettingsManager.IsLastDbExist()) { return; }

        var last = Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings?.Filename;
        if (!string.IsNullOrEmpty(last))
        {
            _ = OpenRecentAsync(last);
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
            if (!SetProperty(ref _selectedTab, value))
            {
                return;
            }

            if (_selectedTab is { Title: "+" })
            {
                AddNewTab();
            }
        }
    }

    public bool LoadLastDatabaseOnStartup
    {
        get => _loadLastDatabaseOnStartup;
        set
        {
            if (!SetProperty(ref _loadLastDatabaseOnStartup, value)) { return; }

            Util.AppSettingsManager.ApplicationSettings.LoadLastDbOnStartup = value;
            Util.AppSettingsManager.PersistData();
        }
    }

    private async Task ConnectAsync()
    {
        // If already connected, perform disconnect instead (toggle behavior)
        if (_dbService.IsConnected)
        {
            await _dbService.DisconnectAsync();
            IsConnected = false;
            CursorText = "Disconnected";
            return;
        }
        // show connection manager dialog
        var vm = new ConnectionManagerViewModel();
        var win = new Views.ConnectionManagerWindow
        {
            Owner = Application.Current?.MainWindow,
            DataContext = vm
        };

        var shown = win.ShowDialog();
        if (shown != true) {
            return;
        }

        var filename = vm.Filename;
        if (string.IsNullOrEmpty(filename)) {
            return;
        }

        var cs = new ConnectionString(filename);

        // map ConnectionManagerViewModel -> ConnectionString (same logic as WinForms ConnectionForm)
        try
        {
            cs.Connection = vm.Mode == ConnectionMode.Direct ? ConnectionType.Direct : ConnectionType.Shared;

            cs.Filename = vm.Filename;
            cs.ReadOnly = vm.ReadOnly;
            cs.Upgrade = vm.UpgradeFromV4;

            cs.Password = !string.IsNullOrWhiteSpace(vm.Password) ? vm.Password.Trim() : null;

            const long mb = 1024 * 1024;
            if (vm.InitialSize > 0)
            {
                cs.InitialSize = vm.InitialSize * mb;
            }

            if (!string.IsNullOrWhiteSpace(vm.CollationLeft))
            {
                var collation = vm.CollationLeft;
                if (!string.IsNullOrWhiteSpace(vm.CollationRight))
                {
                    collation += "/" + vm.CollationRight;
                }

                cs.Collation = new Collation(collation);
            }

            CursorText = "Opening " + filename;
            ElapsedText = "Reading...";

            var connectionString = BuildConnectionString(cs);
            await _dbService.ConnectAsync(connectionString, CancellationToken.None);

            // persist last connection and recent list using same AppSettingsManager calls
            Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings = cs;
            Util.AppSettingsManager.AddToRecentList(cs);

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

    private void InsertSnippet(string? snippet)
    {
        if (SelectedTab == null || string.IsNullOrEmpty(snippet)) {
            return;
        }

        var text = SelectedTab.EditorText;
        var offset = SelectedTab.CaretOffset;

        // Ensure offset is within bounds
        if (offset < 0) {
            offset = 0;
        }

        if (offset > text.Length) {
            offset = text.Length;
        }

        SelectedTab.EditorText = text.Insert(offset, snippet);
        SelectedTab.IsModified = true;
    }

    private void Disconnect()
    {
        var unsavedTabs = Tabs.Where(t => t is { IsModified: true, IsPlus: false }).ToList();
        if (unsavedTabs.Any())
        {
            MessageBoxResult result = MessageBox.Show("You have unsaved changes in some tabs. Do you want to disconnect anyway?", "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) {
                return;
            }
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
        // Delegate run to selected tab's async RunCommand (executes selection or entire buffer)
        if (SelectedTab == null) { return; }

        try
        {
            _ = SelectedTab.RunCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            CursorText = "Error executing query: " + ex.Message;
        }
    }

    private void AddNewTab()
    {
        var newTab = new TabViewModel(_dbService) { Title = $"Query {Tabs.Count}" };

        // insert before plus tab
        TabViewModel? plus = Tabs.FirstOrDefault(t => t.Title == "+");
        if (plus != null)
        {
            var idx = Tabs.IndexOf(plus);
            Tabs.Insert(idx, newTab);
        }
        else
        {
            Tabs.Add(newTab);
        }

        SelectedTab = newTab;
    }

    public void AddSqlSnippet(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) {
            return;
        }

        // if there's no selected tab or selected tab is the plus tab, or current content is empty -> set into current
        if (SelectedTab == null || SelectedTab.Title == "+" || string.IsNullOrWhiteSpace(SelectedTab.EditorText))
        {
            // ensure there's a non-plus tab to place content
            if (SelectedTab == null || SelectedTab.Title == "+")
            {
                AddNewTab();
            }

            SelectedTab = SelectedTab ?? throw new InvalidOperationException("SelectedTab is null after AddNewTab");
            SelectedTab.EditorText = sql.Replace("\\n", "\n");
        }
        else
        {
            // insert new tab before plus
            TabViewModel? plus = Tabs.FirstOrDefault(t => t.Title == "+");
            var newTab = new TabViewModel(_dbService) { Title = $"Query {Tabs.Count}", EditorText = sql.Replace("\\n", "\n") };
            if (plus != null)
            {
                var idx = Tabs.IndexOf(plus);
                Tabs.Insert(idx, newTab);
            }
            else
            {
                Tabs.Add(newTab);
            }

            SelectedTab = newTab;
        }
    }

    private void CloseTab(TabViewModel? tab)
    {
        if (tab == null || tab.IsPlus) {
            return;
        }

        var idx = Tabs.IndexOf(tab);
        if (idx >= 0) {
            Tabs.Remove(tab);
        }

        if (Tabs.Count > 0)
        {
            SelectedTab = Tabs[Math.Max(0, idx - 1)];
        }
    }

    public async Task OpenRecentAsync(object? filename)
    {
        var fName = filename as string;
        if (string.IsNullOrEmpty(fName)) {
            return;
        }

        CursorText = "Opening: " + fName;

        try
        {
            var cs = new ConnectionString(fName);
            var connectionString = BuildConnectionString(cs);
            await _dbService.ConnectAsync(connectionString, CancellationToken.None);

            Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings = cs;
            Util.AppSettingsManager.AddToRecentList(cs);

            IsConnected = true;
            CurrentDatabase = fName;
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
        Util.AppSettingsManager.ClearRecentList();
        RecentDatabases.Clear();
    }

    private void ValidateRecentList()
    {
        Util.AppSettingsManager.ValidateRecentList();
        RecentDatabases.Clear();
        foreach (ConnectionString cs in Util.AppSettingsManager.ApplicationSettings.RecentConnectionStrings)
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

        for (var i = 0; i < 10; i++)
        {
            DataRow row = CurrentResults.NewRow();
            row[0] = i;
            row[1] = sql + " - row " + i;
            CurrentResults.Rows.Add(row);
        }
    }

    private static string BuildConnectionString(ConnectionString cs)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(cs.Filename))
        {
            parts.Add($"Filename={cs.Filename}");
        }

        parts.Add($"Connection={(cs.Connection == ConnectionType.Shared ? "shared" : "direct")}");

        if (!string.IsNullOrWhiteSpace(cs.Password)) {
            parts.Add($"Password={cs.Password}");
        }

        if (cs.ReadOnly) {
            parts.Add("ReadOnly=true");
        }

        if (cs.Upgrade) {
            parts.Add("Upgrade=true");
        }

        if (cs.AutoRebuild) {
            parts.Add("Auto-Rebuild=true");
        }

        if (cs.InitialSize > 0) {
            parts.Add($"Initial Size={cs.InitialSize.ToString(CultureInfo.InvariantCulture)}");
        }

        if (cs.Collation != null) {
            parts.Add($"Collation={cs.Collation}");
        }

        return string.Join(";", parts);
    }

    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        // Open a fresh query tab when the database connects (if no user tabs exist)
        try
        {
            if (!e.IsConnected) { return; }

            Serilog.Log.Information("Database connected - ensuring a query tab is available");
            var hasUserTabs = Tabs.Any(t => !t.IsPlus);
            if (hasUserTabs) { return; }

            AddNewTab();
            Serilog.Log.Information("Added new query tab on connect");
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "OnConnectionStateChanged handler failed");
        }
    }

    private void OnTransactionStateChanged(object? sender, TransactionStateChangedEventArgs e)
    {
        TransactionActive = e.TransactionActive;
    }

    public override void RegisterMessengerReceivers()
    {

    }
}
