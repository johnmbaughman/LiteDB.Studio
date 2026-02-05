using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB.Studio.Wpf.Services;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using LiteDB.Studio.Mvvm.ViewModels.Shell;

namespace LiteDB.Studio.Wpf.ViewModels;

public partial class MainViewModel : ShellContentViewModel
{
    private readonly IDatabaseService _dbService;
    private readonly IConnectionManagerDialogService _connectionDialogService;
    private readonly IDialogService _dialogService;
    private readonly IAppSettingsService _appSettingsService;

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

    public MainViewModel(
        IDatabaseService dbService,
        DatabaseTreeViewModel tree,
        IConnectionManagerDialogService connectionDialogService,
        IDialogService dialogService,
        IAppSettingsService appSettingsService)
    {
        _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        _connectionDialogService = connectionDialogService ?? throw new ArgumentNullException(nameof(connectionDialogService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
        // TODO: Pick up moving things around here. Need to find a way to connect TreeView events to MainViewModel without tight coupling in MVVM framework.
        Tree = tree ?? throw new ArgumentNullException(nameof(tree));
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
        foreach (ConnectionString cs in _appSettingsService.ApplicationSettings.RecentConnectionStrings)
        {
            RecentDatabases.Add(cs.Filename);
        }

        // auto-open last DB if requested
        if (!LoadLastDatabaseOnStartup || !_appSettingsService.IsLastDbExist()) { return; }

        var last = _appSettingsService.ApplicationSettings.LastConnectionStrings?.Filename;
        if (!string.IsNullOrEmpty(last))
        {
            _ = OpenRecentAsync(last);
        }
    }

    // Update the shell window title when the current database changes
    partial void OnCurrentDatabaseChanged(string value)
    {
        try
        {
            var appName = Mvvm.LiteDbStudioApplication.ApplicationName;
            var titleBase = string.IsNullOrEmpty(appName) ? "LiteDB Studio" : appName;
            var dbName = string.IsNullOrEmpty(value) ? null : Path.GetFileName(value);

            ShellViewModel.Title = string.IsNullOrEmpty(dbName) ? titleBase : $"{titleBase} - {dbName}";
        }
        catch
        {
            // ignore errors updating title
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

            _appSettingsService.ApplicationSettings.LoadLastDbOnStartup = value;
            _appSettingsService.PersistData();
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

        var dialogResult = _connectionDialogService.ShowDialog();
        if (dialogResult == null)
        {
            return;
        }

        var filename = dialogResult.Filename;
        if (string.IsNullOrEmpty(filename)) {
            return;
        }

        var cs = new ConnectionString(filename);

        // map ConnectionManagerViewModel -> ConnectionString (same logic as WinForms ConnectionForm)
        try
        {
            cs.Connection = dialogResult.Mode == ConnectionMode.Direct ? ConnectionType.Direct : ConnectionType.Shared;

            cs.Filename = dialogResult.Filename;
            cs.ReadOnly = dialogResult.ReadOnly;
            cs.Upgrade = dialogResult.UpgradeFromV4;

            cs.Password = !string.IsNullOrWhiteSpace(dialogResult.Password) ? dialogResult.Password.Trim() : null;

            const long mb = 1024 * 1024;
            if (dialogResult.InitialSize > 0)
            {
                cs.InitialSize = dialogResult.InitialSize * mb;
            }

            if (!string.IsNullOrWhiteSpace(dialogResult.CollationLeft))
            {
                var collation = dialogResult.CollationLeft;
                if (!string.IsNullOrWhiteSpace(dialogResult.CollationRight))
                {
                    collation += "/" + dialogResult.CollationRight;
                }

                cs.Collation = new Collation(collation);
            }

            CursorText = "Opening " + filename;
            ElapsedText = "Reading...";

            var connectionString = BuildConnectionString(cs);
            await _dbService.ConnectAsync(connectionString, CancellationToken.None);

            // persist last connection and recent list using same AppSettingsManager calls
            _appSettingsService.ApplicationSettings.LastConnectionStrings = cs;
            _appSettingsService.AddToRecentList(cs);

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
            var confirmed = _dialogService.Confirm(
                "You have unsaved changes in some tabs. Do you want to disconnect anyway?",
                "Unsaved Changes",
                DialogIcon.Warning);
            if (!confirmed) {
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
            Tabs.RemoveAt(idx);
        }

        // If there are no non-plus (real) tabs, ensure we create one
        if (Tabs.All(t => t.IsPlus))
        {
            AddNewTab();
        }

        // Select a reasonable tab: prefer the item that occupies the previous index, then fallback
        if (Tabs.Count <= 0) { return; }

        {
            var selectIndex = Math.Min(idx, Tabs.Count - 1);
            SelectedTab = Tabs[selectIndex];

            if (!SelectedTab.IsPlus) { return; }

            TabViewModel? nonPlus = Tabs.FirstOrDefault(t => !t.IsPlus);
            if (nonPlus != null)
            {
                SelectedTab = nonPlus;
            }
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

            _appSettingsService.ApplicationSettings.LastConnectionStrings = cs;
            _appSettingsService.AddToRecentList(cs);

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
        _appSettingsService.ClearRecentList();
        RecentDatabases.Clear();
    }

    private void ValidateRecentList()
    {
        _appSettingsService.ValidateRecentList();
        RecentDatabases.Clear();
        foreach (ConnectionString cs in _appSettingsService.ApplicationSettings.RecentConnectionStrings)
        {
            RecentDatabases.Add(cs.Filename);
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
