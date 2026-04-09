using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB.Studio.Wpf.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
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
    private bool _isReadOnly;

    [ObservableProperty]
    private string _currentDatabase = string.Empty;

    [ObservableProperty]
    private bool _transactionActive;

    [ObservableProperty]
    private string _lastConnectedPath;

    private bool _loadLastDatabaseOnStartup;
    private readonly TabManager _tabManager;

    /// <summary>Gets the observable collection of editor tabs (includes the plus-tab).</summary>
    public ObservableCollection<TabViewModel> Tabs => _tabManager.Tabs;
    /// <summary>Gets the list of recently opened database file paths.</summary>
    public ObservableCollection<string> RecentDatabases { get; } = [];

    /// <summary>Gets the database tree view model.</summary>
    public DatabaseTreeViewModel Tree { get; }

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

        LastConnectedPath = _appSettingsService.ApplicationSettings.LastConnectionStrings?.Filename ?? string.Empty;

        _dbService.ConnectionStateChanged += OnConnectionStateChanged;
        _dbService.TransactionStateChanged += OnTransactionStateChanged;

        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync);
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
        BeginTransactionCommand = new AsyncRelayCommand(BeginTransactionAsync, () => !TransactionActive);
        CommitTransactionCommand = new AsyncRelayCommand(CommitTransactionAsync, () => TransactionActive);
        RollbackTransactionCommand = new AsyncRelayCommand(RollbackTransactionAsync, () => TransactionActive);
    }

    /// <summary>Loads persisted recent databases and optionally auto-opens the last-used database.</summary>
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

    partial void OnTransactionActiveChanged(bool value)
    {
        BeginTransactionCommand.NotifyCanExecuteChanged();
        CommitTransactionCommand.NotifyCanExecuteChanged();
        RollbackTransactionCommand.NotifyCanExecuteChanged();
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

    /// <summary>Command that shows the connection dialog and connects to the selected database.</summary>
    public IAsyncRelayCommand ConnectCommand { get; }
    /// <summary>Command that disconnects from the current database.</summary>
    public IAsyncRelayCommand DisconnectCommand { get; }
    /// <summary>Command that executes the query in the selected tab.</summary>
    public IRelayCommand RunCommand { get; }
    /// <summary>Command that opens a new empty query tab.</summary>
    public IRelayCommand NewTabCommand { get; }
    /// <summary>Command that closes the specified tab.</summary>
    public IAsyncRelayCommand<TabViewModel> CloseTabCommand { get; }
    /// <summary>Command that connects to a recently-used database by file path.</summary>
    public IAsyncRelayCommand<object> OpenRecentCommand { get; }
    /// <summary>Non-async wrapper around <see cref="OpenRecentCommand"/> suitable for XAML <c>Command</c> bindings.</summary>
    public IRelayCommand<object> OpenRecentWrapperCommand { get; }
    /// <summary>Command that removes all entries from the recent-databases list.</summary>
    public IRelayCommand ClearRecentCommand { get; }
    /// <summary>Command that removes non-existent files from the recent-databases list.</summary>
    public IRelayCommand ValidateRecentCommand { get; }
    /// <summary>Command that reloads the database tree from the service.</summary>
    public IAsyncRelayCommand RefreshTreeCommand { get; }
    /// <summary>Command that inserts a snippet string at the caret of the selected tab.</summary>
    public IRelayCommand<string> InsertSnippetCommand { get; }
    /// <summary>Command that re-opens the last-used database.</summary>
    public IRelayCommand LoadLastDatabaseCommand { get; }
    /// <summary>Command that shows an open-file dialog and loads a SQL file into a new tab.</summary>
    public IAsyncRelayCommand OpenFileCommand { get; }
    /// <summary>Command that saves the selected tab's content to its associated file.</summary>
    public IAsyncRelayCommand SaveFileCommand { get; }
    /// <summary>Command that saves all modified tabs to their associated files.</summary>
    public IAsyncRelayCommand SaveAllCommand { get; }
    /// <summary>Command that begins a database transaction. Enabled only when no transaction is active.</summary>
    public IAsyncRelayCommand BeginTransactionCommand { get; }
    /// <summary>Command that commits the active database transaction.</summary>
    public IAsyncRelayCommand CommitTransactionCommand { get; }
    /// <summary>Command that rolls back the active database transaction.</summary>
    public IAsyncRelayCommand RollbackTransactionCommand { get; }

    /// <summary>Gets or sets the currently selected editor tab.</summary>
    public TabViewModel? SelectedTab
    {
        get => _tabManager.SelectedTab;
        set => _tabManager.SelectedTab = value;
    }

    /// <summary>Gets or sets whether the last-used database is opened automatically on startup.</summary>
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
        // If already connected, run the full disconnect flow (T178); abort if user cancels
        if (_dbService.IsConnected)
        {
            await DisconnectCommand.ExecuteAsync(null);
            if (_dbService.IsConnected) { return; }
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

    private async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        // Step 1: if a transaction is active, confirm rollback first
        if (TransactionActive)
        {
            var rollbackConfirmed = _dialogService.Confirm(
                "An active transaction will be rolled back. Proceed with disconnect?",
                "Active Transaction",
                DialogIcon.Warning);
            if (!rollbackConfirmed) { return; }

            try
            {
                await _dbService.RollbackTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CursorText = "Error rolling back transaction: " + ex.Message;
                return;
            }
        }

        // Step 2: prompt save for each modified tab (tabs remain open after disconnect)
        foreach (TabViewModel tab in Tabs.Where(t => !t.IsPlus && t.IsModified).ToList())
        {
            var save = _dialogService.Confirm(
                $"Save changes to {(string.IsNullOrEmpty(tab.Filename) ? tab.Title : tab.Filename)}?",
                "Unsaved Changes",
                DialogIcon.Question);
            if (save)
            {
                await SaveTabAsync(tab, cancellationToken);
            }
        }

        // Step 3: disconnect — tabs remain with content preserved, actions disabled until reconnected
        try
        {
            await _dbService.DisconnectAsync();
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

    /// <summary>Places <paramref name="sql"/> into the current or a new editor tab.</summary>
    /// <param name="sql">SQL text to insert.</param>
    public void AddSqlSnippet(string sql)
    {
        _tabManager.AddSqlSnippet(sql);
    }

    /// <summary>Always opens a new editor tab and sets its content to <paramref name="sql"/>.</summary>
    /// <param name="sql">SQL text to insert.</param>
    public void AddSqlSnippetInNewTab(string sql)
    {
        _tabManager.AddSqlSnippetInNewTab(sql);
    }

    /// <summary>Connects to the database identified by <paramref name="filename"/>.</summary>
    /// <param name="filename">File path boxed as <see cref="object"/>, or a plain <see cref="string"/>.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
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
            await _dbService.ConnectAsync(connectionString, cs.ReadOnly, cs.Password, cancellationToken);

            _appSettingsService.ApplicationSettings.LastConnectionStrings = cs;
            _appSettingsService.AddToRecentList(cs);

            IsConnected = true;
            CurrentDatabase = filename;
            LastConnectedPath = filename;

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
        IsReadOnly = e.IsConnected && _dbService.IsReadOnly;

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

    private async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbService.BeginTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error beginning transaction");
            CursorText = "Error: " + ex.Message;
        }
    }

    private async Task CommitTransactionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbService.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error committing transaction");
            CursorText = "Error: " + ex.Message;
        }
    }

    private async Task RollbackTransactionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbService.RollbackTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error rolling back transaction");
            CursorText = "Error: " + ex.Message;
        }
    }

    /// <inheritdoc />
    public override void RegisterMessengerReceivers()
    {

    }
}
