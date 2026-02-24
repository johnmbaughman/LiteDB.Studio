using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB.Studio.Wpf.Services;
using Microsoft.Extensions.Logging;
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
    private readonly IFileDialogService _fileDialogService;
    private readonly IFileService _fileService;
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
    private readonly TabManager _tabManager;

    public ObservableCollection<TabViewModel> Tabs => _tabManager.Tabs;
    public ObservableCollection<string> RecentDatabases { get; } = [];

    public DatabaseTreeViewModel Tree { get; }
    public DataTable CurrentResults { get; } = new();

    public MainViewModel(
        IDatabaseService dbService,
        DatabaseTreeViewModel tree,
        IConnectionManagerDialogService connectionDialogService,
        IDialogService dialogService,
        IFileDialogService fileDialogService,
        IFileService fileService,
        IAppSettingsService appSettingsService,
        ILogger<MainViewModel> logger,
        ILoggerFactory loggerFactory) : base(logger)
    {
        _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        _connectionDialogService = connectionDialogService ?? throw new ArgumentNullException(nameof(connectionDialogService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
        // TODO: Pick up moving things around here. Need to find a way to connect TreeView events to MainViewModel without tight coupling in MVVM framework.
        Tree = tree ?? throw new ArgumentNullException(nameof(tree));

        _tabManager = new TabManager(_dbService, loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory)), _dialogService, SaveTabAsync);
        _tabManager.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TabManager.SelectedTab))
            {
                OnPropertyChanged(nameof(SelectedTab));
            }
        };

        Tree.InsertSnippetRequested += (_, snippet) => _tabManager.InsertSnippet(snippet);
        Tree.AddSqlSnippetRequested += (_, snippet) => _tabManager.AddSqlSnippet(snippet);

        _dbService.ConnectionStateChanged += OnConnectionStateChanged;
        _dbService.TransactionStateChanged += OnTransactionStateChanged;

        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
        DisconnectCommand = new RelayCommand(Disconnect);
        RunCommand = new RelayCommand(Run);
        NewTabCommand = new RelayCommand(() => _tabManager.AddNewTab());
        CloseTabCommand = new AsyncRelayCommand<TabViewModel>(async (tab, ct) =>
        {
            if (tab == null) { return; }
            await tab.CloseCommand.ExecuteAsync(ct);
            _tabManager.CloseTab(tab);
        });
        OpenRecentCommand = new AsyncRelayCommand<object>(OpenRecentAsync);
        OpenRecentWrapperCommand = new RelayCommand<object>(p =>
        {
            // ensure we only forward string parameters to the async handler
            if (p is string s && !string.IsNullOrEmpty(s)) {
                _ = OpenRecentAsync(s, CancellationToken.None);
            }
        });
        ClearRecentCommand = new RelayCommand(ClearRecentList);
        ValidateRecentCommand = new RelayCommand(ValidateRecentList);
        RefreshTreeCommand = new AsyncRelayCommand(RefreshTreeAsync);
        InsertSnippetCommand = new RelayCommand<string>(snippet => _tabManager.InsertSnippet(snippet));
        LoadLastDatabaseCommand = new RelayCommand(LoadLastDatabase);
        OpenFileCommand = new AsyncRelayCommand(OpenFileAsync);
        SaveFileCommand = new AsyncRelayCommand(SaveFileAsync);
        SaveAllCommand = new AsyncRelayCommand(SaveAllAsync);
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
            _ = OpenRecentAsync(last, CancellationToken.None);
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
    public IAsyncRelayCommand<TabViewModel> CloseTabCommand { get; }
    public IAsyncRelayCommand<object> OpenRecentCommand { get; }
    public IRelayCommand<object> OpenRecentWrapperCommand { get; }
    public IRelayCommand ClearRecentCommand { get; }
    public IRelayCommand ValidateRecentCommand { get; }
    public IAsyncRelayCommand RefreshTreeCommand { get; }
    public IRelayCommand<string> InsertSnippetCommand { get; }
    public IRelayCommand LoadLastDatabaseCommand { get; }
    public IAsyncRelayCommand OpenFileCommand { get; }
    public IAsyncRelayCommand SaveFileCommand { get; }
    public IAsyncRelayCommand SaveAllCommand { get; }

    public TabViewModel? SelectedTab
    {
        get => _tabManager.SelectedTab;
        set => _tabManager.SelectedTab = value;
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

    private Task SaveFileAsync(CancellationToken cancellationToken)
        => SaveTabAsync(SelectedTab, cancellationToken);

    private async Task SaveAllAsync(CancellationToken cancellationToken)
    {
        foreach (TabViewModel tab in Tabs)
        {
            if (tab.IsPlus || !tab.IsModified) { continue; }
            await SaveTabAsync(tab, cancellationToken);
        }
    }

    private async Task SaveTabAsync(TabViewModel? tab, CancellationToken cancellationToken)
    {
        if (tab == null || tab.IsPlus) { return; }

        var path = tab.Filename;
        if (string.IsNullOrEmpty(path))
        {
            path = _fileDialogService.SaveFile(new SaveFileDialogOptions
            {
                Title = "Save SQL File",
                Filter = "SQL files (*.sql)|*.sql|All files (*.*)|*.*",
                FileName = string.IsNullOrEmpty(tab.Title) ? "query.sql" : tab.Title
            });
            if (string.IsNullOrEmpty(path)) { return; }
        }

        try
        {
            await _fileService.WriteAllTextAsync(path, tab.EditorText, cancellationToken);
            tab.Filename = path;
            tab.Title = Path.GetFileName(path);
            tab.IsModified = false;
        }
        catch (Exception ex)
        {
            CursorText = $"Error saving file: {ex.Message}";
        }
    }

    private async Task OpenFileAsync(CancellationToken cancellationToken)
    {
        var path = _fileDialogService.OpenFile(new OpenFileDialogOptions
        {
            Title = "Open SQL File",
            Filter = "SQL files (*.sql)|*.sql|All files (*.*)|*.*"
        });
        if (string.IsNullOrEmpty(path)) { return; }

        string content;
        try
        {
            content = await _fileService.ReadAllTextAsync(path, cancellationToken);
        }
        catch (Exception ex)
        {
            CursorText = $"Error opening file: {ex.Message}";
            return;
        }

        // Use the current tab if it is empty and unmodified; otherwise open a new tab
        TabViewModel? target = _tabManager.SelectedTab;
        if (target == null || target.IsPlus || !string.IsNullOrWhiteSpace(target.EditorText))
        {
            _tabManager.AddNewTab();
            target = _tabManager.SelectedTab;
        }

        if (target == null) { return; }

        target.EditorText = content;
        target.Filename = path;
        target.IsModified = false;
        target.Title = Path.GetFileName(path);
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        // If already connected, perform disconnect instead (toggle behavior)
        if (_dbService.IsConnected)
        {
            await _dbService.DisconnectAsync();
            IsConnected = false;
            CursorText = "Disconnected";
            return;
        }

        ConnectionManagerDialogResult? dialogResult = _connectionDialogService.ShowDialog();
        if (dialogResult == null)
        {
            return;
        }

        var filename = dialogResult.Filename;
        if (string.IsNullOrEmpty(filename)) {
            return;
        }

        var cs = new ConnectionString(filename)
        {
            // map ConnectionManagerViewModel -> ConnectionString (same logic as WinForms ConnectionForm)
            Connection = dialogResult.Mode == ConnectionMode.Direct ? ConnectionType.Direct : ConnectionType.Shared,
            Filename = dialogResult.Filename,
            ReadOnly = dialogResult.ReadOnly,
            Upgrade = dialogResult.UpgradeFromV4,
            Password = !string.IsNullOrWhiteSpace(dialogResult.Password) ? dialogResult.Password.Trim() : null
        };

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

        await ConnectWithConnectionStringAsync(cs, filename, true, cancellationToken);
    }

    private async Task RefreshTreeAsync(CancellationToken cancellationToken)
    {
        Tree.RootNodes.Clear();
        await Tree.LoadRootNodesAsync(cancellationToken);
    }

    private void Disconnect()
    {
        if (_tabManager.HasUnsavedTabs)
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

    private void LoadLastDatabase()
    {
        var last = _appSettingsService.ApplicationSettings.LastConnectionStrings?.Filename;
        if (!string.IsNullOrEmpty(last))
        {
            _ = OpenRecentAsync(last, CancellationToken.None);
        }
    }

    public void AddSqlSnippet(string sql)
    {
        _tabManager.AddSqlSnippet(sql);
    }

    public void AddSqlSnippetInNewTab(string sql)
    {
        _tabManager.AddSqlSnippetInNewTab(sql);
    }

    public async Task OpenRecentAsync(object? filename, CancellationToken cancellationToken)
    {
        var fName = filename as string;
        if (string.IsNullOrEmpty(fName)) {
            return;
        }

        CursorText = "Opening: " + fName;

        var cs = new ConnectionString(fName);
        await ConnectWithConnectionStringAsync(cs, fName, false, cancellationToken);
    }

    private async Task ConnectWithConnectionStringAsync(ConnectionString cs, string filename, bool populateTree, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var connectionString = BuildConnectionString(cs);
            await _dbService.ConnectAsync(connectionString, cancellationToken);

            _appSettingsService.ApplicationSettings.LastConnectionStrings = cs;
            _appSettingsService.AddToRecentList(cs);

            IsConnected = true;
            CurrentDatabase = filename;

            if (populateTree)
            {
                try
                {
                    await Tree.LoadRootNodesAsync(cancellationToken);
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
            if (_tabManager.HasUserTabs) { return; }

            _tabManager.AddNewTab();
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
