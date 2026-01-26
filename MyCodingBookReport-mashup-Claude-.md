# LiteDB.Studio.Wpf C# Coding Style Guide - Comprehensive Reference

> **Purpose**: This document consolidates coding style, architecture patterns, and conventions for the LiteDB.Studio.Wpf project to guide GitHub Copilot in generating consistent, high-quality C# code.

---

## Table of Contents
1. [Project Architecture](#project-architecture)
2. [Code Formatting Rules](#code-formatting-rules)
3. [Naming Conventions](#naming-conventions)
4. [Language Features & Preferences](#language-features--preferences)
5. [MVVM Architecture with CommunityToolkit.Mvvm](#mvvm-architecture-with-communitytoolkitmvvm)
6. [Async/Await Patterns](#asyncawait-patterns)
7. [Service Layer & Dependency Injection](#service-layer--dependency-injection)
8. [Error Handling & Validation](#error-handling--validation)
9. [Logging with Serilog](#logging-with-serilog)
10. [WPF/XAML Conventions](#wpfxaml-conventions)
11. [Common Implementation Patterns](#common-implementation-patterns)
12. [Quick Reference Checklist](#quick-reference-checklist)

---

## Project Architecture

### Technology Stack
- **Target Framework**: .NET 9.0 Windows (`net9.0-windows`)
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Architecture**: MVVM (Model-View-ViewModel)
- **MVVM Toolkit**: CommunityToolkit.Mvvm v8.2.0
- **DI Container**: Microsoft.Extensions.DependencyInjection v8.0.0
- **Logging**: Serilog (Console, File, Debug sinks)
- **Editor**: AvalonEdit v6.3.1.120
- **Database**: LiteDB (referenced project)

### Project Settings
```xml
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
```

---

## Code Formatting Rules

### Indentation & Spacing
- **Indentation**: 4 spaces (never tabs)
- **Tab width**: 4
- **Line endings**: CRLF (Windows style)
- **Final newline**: Not required
- **Single-line preservation**: Yes (preserve existing single-line blocks/statements)

### Brace Style (Allman)
```csharp
// Correct - braces on new line
if (condition)
{
    DoSomething();
}

public void Method()
{
    // implementation
}

// New lines before catch, else, finally
try
{
    // code
}
catch (Exception ex)
{
    // handle
}
finally
{
    // cleanup
}
```

### Spacing Rules
- Space after keywords: `if (`, `for (`, `while (`, `switch (`
- Space around binary operators: `a + b`, `x == y`, `result && flag`
- No space after cast: `(int)value`
- No space before/after dot: `obj.Method()`
- No space inside parentheses: `Method(arg)` not `Method( arg )`
- No space inside brackets: `array[0]` not `array[ 0 ]`
- Space after comma: `Method(a, b, c)`
- No space before semicolon: `statement;` not `statement ;`

### Using Directives
- Place `using` directives **outside** namespace (before namespace declaration)
- No enforced grouping (`dotnet_separate_import_directive_groups = false`)
- System directives not forced first (`dotnet_sort_system_directives_first = false`)
- `ImplicitUsings` enabled, so common namespaces auto-included

---

## Naming Conventions

### General Rules
| Element | Convention | Example |
|---------|-----------|---------|
| Classes | PascalCase | `MainViewModel`, `LiteDbService` |
| Interfaces | `I` + PascalCase | `IDatabaseService`, `ICommand` |
| Methods | PascalCase | `ConnectAsync`, `LoadData` |
| Properties | PascalCase | `IsConnected`, `CurrentDatabase` |
| Events | PascalCase | `ConnectionStateChanged` |
| Private fields | `_camelCase` | `_dbService`, `_isConnected` |
| Local variables | camelCase | `result`, `connectionString` |
| Constants | PascalCase | `MaxRetries`, `DefaultTimeout` |
| Parameters | camelCase | `connectionString`, `cancellationToken` |

### Special Patterns
```csharp
// Async methods must end with 'Async'
public async Task<QueryResult> ExecuteAsync(string query, CancellationToken cancellationToken)

// Event handlers
private void OnConnectionStateChanged(object? sender, EventArgs e)
private void Button_Click(object sender, RoutedEventArgs e)  // XAML events

// Command properties end with 'Command'
public IAsyncRelayCommand ConnectCommand { get; }
public IRelayCommand DisconnectCommand { get; }
```

### Code Example
```csharp
namespace LiteDB.Studio.Wpf.ViewModels;

public partial class DatabaseViewModel : ObservableObject
{
    private readonly IDatabaseService _databaseService;
    private string _connectionString = string.Empty;
    private bool _isConnected;
    
    [ObservableProperty]
    private string _statusText = string.Empty;
    
    public IAsyncRelayCommand ConnectCommand { get; }
    
    public DatabaseViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
    }
    
    private async Task ConnectAsync()
    {
        await _databaseService.ConnectAsync(_connectionString, CancellationToken.None);
    }
}
```

---

## Language Features & Preferences

### `var` Usage
**Use `var`**:
- With built-in types when assignment is obvious
- When type is apparent from right-hand side
- With LINQ queries

**Avoid `var`**:
- When type is not obvious

```csharp
// Correct usage of var
var count = 0;
var name = "LiteDB";
var result = new QueryResult();
var items = collection.Where(x => x.IsActive).ToList();
var db = _serviceProvider.GetRequiredService<IDatabaseService>();

// Avoid var here (not obvious)
var connection = GetConnection();  // What type is returned?
IDatabaseService connection = GetConnection();  // Better
```

### Type References
- **Always** use built-in keywords: `string`, `int`, `bool`, `object`
- **Never** use BCL names: `String`, `Int32`, `Boolean`, `Object`

### `this.` Qualification
**Never use `this.` qualifier** for any member access:
```csharp
// Correct
_dbService.Connect();
IsConnected = true;
LoadData();

// Incorrect - never do this
this._dbService.Connect();
this.IsConnected = true;
this.LoadData();
```

### String Literals
- Prefer `string.Empty` over `""`
- Use string interpolation for formatting: `$"Connected to {filename}"`
- Use verbatim strings for paths: `@"C:\Data\file.db"`

### Expression-Bodied Members
```csharp
// Properties - prefer expression bodies when simple
public string StatusText => $"Collections: {CollectionsCount}";
public bool IsReady => IsConnected && !TransactionActive;

// Accessors - expression bodies OK
private string _name;
public string Name
{
    get => _name;
    set => SetProperty(ref _name, value);
}

// Methods - prefer block bodies
public void Disconnect()
{
    _dbService.Disconnect();
    IsConnected = false;
}

// Lambdas - expression bodies encouraged
items.Select(x => x.Name)
```

### Null Handling
```csharp
// Null-coalescing
var value = input ?? defaultValue;
var name = user?.Name ?? "Unknown";

// Null-propagation
var count = collection?.Count;
var result = service?.ExecuteQuery()?.Results;

// Null checks with pattern matching
if (value is null) return;
if (obj is not null) Process(obj);

// Pattern matching for type checks
if (sender is Button button)
{
    button.IsEnabled = false;
}
```

### Modern C# Features (Encouraged)
```csharp
// Object initializers
var config = new ConnectionConfig
{
    Filename = path,
    ReadOnly = true,
    Password = pwd
};

// Collection initializers
var items = new List<string> { "a", "b", "c" };

// Collection expressions (C# 12+)
var array = [item1, item2, item3];

// Pattern matching
var result = status switch
{
    Status.Connected => "Active",
    Status.Disconnected => "Idle",
    _ => "Unknown"
};

// Property patterns
if (result is { Success: true, Count: > 0 })
{
    ProcessResults(result);
}

// Target-typed new
List<string> names = new();
QueryResult result = new();
```

### Access Modifiers
- Always explicit on non-interface members
- Required by .editorconfig: `dotnet_style_require_accessibility_modifiers = for_non_interface_members`

### Readonly Fields
- Use `readonly` whenever possible for immutability

---

## MVVM Architecture with CommunityToolkit.Mvvm

### ViewModel Base Structure
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB.Studio.Wpf.Services;

namespace LiteDB.Studio.Wpf.ViewModels;

public partial class ExampleViewModel : ObservableObject
{
    private readonly IExampleService _service;
    
    // Observable properties via source generator
    [ObservableProperty]
    private string _title = string.Empty;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private ObservableCollection<ItemViewModel> _items = [];
    
    // Commands
    public IAsyncRelayCommand LoadCommand { get; }
    public IRelayCommand ClearCommand { get; }
    public IRelayCommand<string> OpenCommand { get; }
    
    // Constructor with DI
    public ExampleViewModel(IExampleService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        ClearCommand = new RelayCommand(Clear);
        OpenCommand = new RelayCommand<string>(Open);
    }
    
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var data = await _service.GetDataAsync(CancellationToken.None);
            Items = new ObservableCollection<ItemViewModel>(data.Select(d => new ItemViewModel(d)));
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private void Clear()
    {
        Items.Clear();
    }
    
    private void Open(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        // Implementation
    }
}
```

### Key MVVM Rules

#### 1. ViewModels Must Be Partial
Classes using `[ObservableProperty]` or `[RelayCommand]` **must** be declared `partial`:
```csharp
public partial class MyViewModel : ObservableObject  // 'partial' required
```

#### 2. Observable Properties
```csharp
// Simple property - use [ObservableProperty]
[ObservableProperty]
private string _name = string.Empty;
// Generates: public string Name { get; set; } with INotifyPropertyChanged

// Property with custom logic - use SetProperty manually
private string _title;
public string Title
{
    get => _title;
    set
    {
        if (!SetProperty(ref _title, value)) return;
        
        // Custom logic after value changes
        OnPropertyChanged(nameof(DisplayTitle));
    }
}

public string DisplayTitle => $"Title: {Title}";

// Property change callback - partial method
[ObservableProperty]
private QueryResult? _lastResult;

partial void OnLastResultChanged(QueryResult? value)
{
    ResultGridViewModel.QueryResult = value;
    OnPropertyChanged(nameof(HasResults));
}
```

#### 3. Commands
```csharp
// Declare command properties (no backing field needed)
public IAsyncRelayCommand SaveCommand { get; }
public IRelayCommand CancelCommand { get; }
public IRelayCommand<int> SelectItemCommand { get; }

// Initialize in constructor
public MyViewModel()
{
    SaveCommand = new AsyncRelayCommand(SaveAsync);
    CancelCommand = new RelayCommand(Cancel);
    SelectItemCommand = new RelayCommand<int>(SelectItem);
}

// Implementation methods
private async Task SaveAsync()
{
    // Async work
}

private void Cancel()
{
    // Synchronous work
}

private void SelectItem(int index)
{
    // Work with parameter
}
```

#### 4. ObservableCollection for Bindable Lists
```csharp
[ObservableProperty]
private ObservableCollection<TabViewModel> _tabs = [];

// Add/remove items - UI updates automatically
Tabs.Add(newTab);
Tabs.Remove(oldTab);
Tabs.Clear();
```

#### 5. Derived/Computed Properties
```csharp
[ObservableProperty]
private int _collectionsCount;

[ObservableProperty]
private int _systemCount;

// Computed property - raise change when dependencies change
public string StatusText => $"Collections: {CollectionsCount} / System: {SystemCount}";

// After changing CollectionsCount or SystemCount:
OnPropertyChanged(nameof(StatusText));
```

---

## Async/Await Patterns

### Method Signatures
```csharp
// Standard async method
public async Task<QueryResult> ExecuteAsync(string query, CancellationToken cancellationToken)
{
    // Async implementation
}

// Synchronous work returning completed task
public Task ConnectAsync(string connectionString, CancellationToken cancellationToken)
{
    // Sync work
    _connection = new Connection(connectionString);
    return Task.CompletedTask;
}

// Void async (avoid except for event handlers)
private async void Button_Click(object sender, RoutedEventArgs e)
{
    await LoadDataAsync();
}
```

### CancellationToken Usage
- **Always** include `CancellationToken` as last parameter
- **Always** pass token to downstream async calls
- Check cancellation in loops

```csharp
public async Task ProcessItemsAsync(IEnumerable<Item> items, CancellationToken cancellationToken)
{
    foreach (var item in items)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        await ProcessItemAsync(item, cancellationToken);
    }
}

// Caller
await service.ProcessItemsAsync(items, CancellationToken.None);
// Or with real cancellation
await service.ProcessItemsAsync(items, cancellationTokenSource.Token);
```

### Async Error Handling
```csharp
private async Task LoadDataAsync()
{
    IsLoading = true;
    ErrorMessage = null;
    
    try
    {
        var data = await _service.GetDataAsync(CancellationToken.None);
        Items = new ObservableCollection<ItemViewModel>(data.Select(ToViewModel));
    }
    catch (OperationCanceledException)
    {
        // User cancelled - typically silent
        Log.Debug("Operation cancelled by user");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to load data");
        ErrorMessage = $"Failed to load data: {ex.Message}";
    }
    finally
    {
        IsLoading = false;
    }
}
```

### Sync Wrapper for Async (when necessary)
```csharp
// Dispose pattern with async cleanup
public void Dispose()
{
    DisconnectAsync().GetAwaiter().GetResult();
}

// Synchronous API that calls async implementation
public void Disconnect()
{
    DisconnectAsync().GetAwaiter().GetResult();
}
```

---

## Service Layer & Dependency Injection

### Interface Definition
```csharp
namespace LiteDB.Studio.Wpf.Services;

/// <summary>
/// Provides database connection and query execution services.
/// </summary>
public interface IDatabaseService : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether a database connection is active.
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Gets a value indicating whether a transaction is currently active.
    /// </summary>
    bool TransactionActive { get; }
    
    /// <summary>
    /// Connects to a database using the specified connection string.
    /// </summary>
    Task ConnectAsync(string connectionString, CancellationToken cancellationToken);
    
    /// <summary>
    /// Disconnects from the current database.
    /// </summary>
    Task DisconnectAsync();
    
    /// <summary>
    /// Executes a query and returns the results.
    /// </summary>
    Task<QueryResult> ExecuteAsync(string? query, CancellationToken cancellationToken);
    
    /// <summary>
    /// Raised when the connection state changes.
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
}
```

### Implementation Pattern
```csharp
using Serilog;

namespace LiteDB.Studio.Wpf.Services;

public class LiteDbService : IDatabaseService
{
    private LiteDatabase? _db;
    private readonly Lock _sync = new();
    
    public bool IsConnected => _db != null;
    public bool TransactionActive { get; private set; }
    
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    
    public Task ConnectAsync(string connectionString, CancellationToken cancellationToken)
    {
        // Guard clause
        if (IsConnected) throw new InvalidOperationException("Already connected");
        
        try
        {
            _db = new LiteDatabase(connectionString);
            Log.Information("Connected to database: {ConnectionString}", connectionString);
            
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(true));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to connect to database");
            throw;
        }
        
        return Task.CompletedTask;
    }
    
    public Task DisconnectAsync()
    {
        if (!IsConnected) return Task.CompletedTask;
        
        if (TransactionActive)
        {
            throw new InvalidOperationException("Cannot disconnect with active transaction");
        }
        
        lock (_sync)
        {
            try
            {
                _db?.Dispose();
            }
            finally
            {
                _db = null;
                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(false));
            }
        }
        
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
    }
}
```

### Service Registration (App.xaml.cs or Program.cs)
```csharp
var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        // Register services
        services.AddSingleton<IDatabaseService, LiteDbService>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<ConnectionManagerViewModel>();
    })
    .Build();
```

### Constructor Injection in ViewModels
```csharp
public partial class MainViewModel : ObservableObject
{
    private readonly IDatabaseService _dbService;
    
    public MainViewModel(IDatabaseService dbService)
    {
        _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        
        // Subscribe to events
        _dbService.ConnectionStateChanged += OnConnectionStateChanged;
        _dbService.TransactionStateChanged += OnTransactionStateChanged;
    }
    
    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        IsConnected = e.IsConnected;
    }
}
```

---

## Error Handling & Validation

### Guard Clauses
```csharp
// Null argument checks
public MyClass(IService service, string name)
{
    _service = service ?? throw new ArgumentNullException(nameof(service));
    _name = !string.IsNullOrEmpty(name) 
        ? name 
        : throw new ArgumentException("Name cannot be empty", nameof(name));
}

// State validation
public async Task ExecuteAsync(string query, CancellationToken cancellationToken)
{
    if (!IsConnected) throw new InvalidOperationException("Not connected");
    if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Query cannot be empty", nameof(query));
    
    // Implementation
}

// Early returns
if (collection.Count == 0) return;
if (!File.Exists(path)) return;
```

### Exception Handling Pattern
```csharp
public async Task<QueryResult> ExecuteQueryAsync(string query, CancellationToken cancellationToken)
{
    // 1. Guard clauses
    if (!IsConnected) throw new InvalidOperationException("Not connected");
    
    // 2. Wrap risky operations
    try
    {
        var result = await _db.ExecuteAsync(query);
        return result;
    }
    catch (LiteException ex)
    {
        // 3. Log with context
        Log.Error(ex, "Query execution failed: {Query}", query);
        
        // 4. Wrap or rethrow with context
        throw new InvalidOperationException($"Query execution failed: {ex.Message}", ex);
    }
}
```

### User-Facing Error Messages
```csharp
// ViewModel pattern for user errors
[ObservableProperty]
private string? _errorMessage;

[ObservableProperty]
private string _statusText = "Ready";

private async Task ConnectAsync()
{
    ErrorMessage = null;
    StatusText = "Connecting...";
    
    try
    {
        await _dbService.ConnectAsync(ConnectionString, CancellationToken.None);
        StatusText = "Connected";
    }
    catch (Exception ex)
    {
        ErrorMessage = $"Connection failed: {ex.Message}";
        StatusText = "Connection error";
    }
}
```

---

## Logging with Serilog

### Basic Usage
```csharp
using Serilog;

// Information - normal flow
Log.Information("Connected to database: {Database}", databasePath);

// Warning - recoverable issues
Log.Warning("Collection {Collection} not found, using default", collectionName);

// Error - exceptions
Log.Error(ex, "Failed to execute query: {Query}", query);

// Debug - detailed diagnostics
Log.Debug("Loading {Count} collections", collections.Count);
```

### Structured Logging
```csharp
// Use property placeholders, not string interpolation
// Good
Log.Information("User {UserId} executed query: {Query}", userId, query);

// Bad - loses structure
Log.Information($"User {userId} executed query: {query}");

// Complex objects
Log.Information("Query completed. Result: {@Result}", result);
```

### Context and Exceptions
```csharp
try
{
    await ProcessAsync();
}
catch (Exception ex)
{
    // Exception as first parameter for proper formatting
    Log.Error(ex, "Processing failed for {Item} with {Count} records", itemName, recordCount);
    throw;
}
```

### Guidelines
- **Don't log**: Passwords, connection strings with credentials, PII
- **Do log**: Operation results, errors with context, timing for slow operations
- **Use levels appropriately**:
  - `Debug`: Detailed diagnostics, verbose state
  - `Information`: Normal application flow, key operations
  - `Warning`: Unexpected but handled situations
  - `Error`: Failures, exceptions

---

## WPF/XAML Conventions

### File-Scoped Namespaces
```csharp
// Use file-scoped namespaces in all new files
namespace LiteDB.Studio.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // ...
}
```

### Pack URIs for Resources
```csharp
// Icon in Resources folder
IconUri = "pack://application:,,,/LiteDB.Studio.Wpf;component/Resources/Icons/database.png";

// XAML
<Image Source="pack://application:,,,/LiteDB.Studio.Wpf;component/Resources/app_icon.png" />
```

### Minimal Code-Behind
```csharp
// MainWindow.xaml.cs - keep minimal
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Get ViewModel from DI container
        var host = (Application.Current as App)?.HostInstance;
        if (host != null)
        {
            var vm = host.Services.GetRequiredService<MainViewModel>();
            DataContext = vm;
            vm.Initialize();
        }
    }
    
    // Only UI-specific event handlers that can't be bound
    private void LoadLastDb_Click(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as MainViewModel;
        _ = vm?.OpenRecentAsync(GetLastDbPath());
    }
}
```

### Attached Behaviors for Non-Bindable Properties
```csharp
// Example: AvalonEdit doesn't have bindable Text property
// Use attached property behavior

// XAML
<avalon:TextEditor 
    controls:AvalonEditBehaviors.EditorText="{Binding EditorText, Mode=TwoWay}"
    controls:AvalonEditBehaviors.CaretOffset="{Binding CaretOffset, Mode=TwoWay}" />

// AvalonEditBehaviors.cs
public static class AvalonEditBehaviors
{
    public static readonly DependencyProperty EditorTextProperty =
        DependencyProperty.RegisterAttached(
            "EditorText",
            typeof(string),
            typeof(AvalonEditBehaviors),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnEditorTextChanged));
    
    public static string GetEditorText(DependencyObject obj)
    {
        return (string)obj.GetValue(EditorTextProperty);
    }
    
    public static void SetEditorText(DependencyObject obj, string value)
    {
        obj.SetValue(EditorTextProperty, value);
    }
    
    private static void OnEditorTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextEditor editor)
        {
            editor.Text = e.NewValue as string ?? string.Empty;
        }
    }
}
```

### XAML Binding Patterns
```xml
<!-- Command binding -->
<Button Content="Connect" Command="{Binding ConnectCommand}" />

<!-- Two-way binding -->
<TextBox Text="{Binding Username, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />

<!-- Visibility binding with converter -->
<Grid Visibility="{Binding IsConnected, Converter={StaticResource BoolToVis}}" />

<!-- Collection binding -->
<ListBox ItemsSource="{Binding Items}" SelectedItem="{Binding SelectedItem}" />

<!-- Data template -->
<TabControl ItemsSource="{Binding Tabs}">
    <TabControl.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Title}" />
        </DataTemplate>
    </TabControl.ItemTemplate>
</TabControl>
```

---

## Common Implementation Patterns

### Settings Persistence
```csharp
// Save connection to recent list
Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings = cs;
Util.AppSettingsManager.AddToRecentList(cs);
Util.AppSettingsManager.PersistData();

// Load recent list
foreach (var cs in Util.AppSettingsManager.ApplicationSettings.RecentConnectionStrings)
{
    RecentDatabases.Add(cs.Filename);
}
```

### Tree Node Construction
```csharp
// Pass action delegate for callbacks
Action<string> insertSnippet = snippet => InsertSnippetRequested?.Invoke(this, snippet);

var node = new DbTreeNode(_databaseService, insertSnippet)
{
    Header = collectionName,
    Tag = "collection",
    IconUri = "pack://application:,,,/Resources/Icons/collection.png"
};

Children.Add(node);
```

### Connection String Mapping
```csharp
var cs = new ConnectionString(filename)
{
    Connection = mode == ConnectionMode.Direct ? ConnectionType.Direct : ConnectionType.Shared,
    Filename = filename,
    ReadOnly = readOnly,
    Upgrade = upgradeFromV4,
    Password = !string.IsNullOrWhiteSpace(password) ? password.Trim() : null
};

if (initialSizeMb > 0)
{
    cs.InitialSize = initialSizeMb * 1024 * 1024;
}
```

### Dialog Patterns
```csharp
// Show dialog and check result
private async Task ConnectAsync()
{
    var vm = new ConnectionManagerViewModel();
    var dialog = new ConnectionManagerWindow
    {
        Owner = Application.Current?.MainWindow,
        DataContext = vm
    };
    
    var result = dialog.ShowDialog();
    if (result != true) return;
    
    // Use dialog data
    await ProcessConnectionAsync(vm.Filename);
}
```

### File Dialogs
```csharp
private void BrowseForFile()
{
    var dialog = new OpenFileDialog
    {
        Filter = "LiteDB files (*.db)|*.db|All files (*.*)|*.*",
        Title = "Select Database File"
    };
    
    if (dialog.ShowDialog() == true)
    {
        Filename = dialog.FileName;
    }
}

private void SaveResults()
{
    var dialog = new SaveFileDialog
    {
        Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
        FileName = $"{CollectionName}.json"
    };
    
    if (dialog.ShowDialog() == true)
    {
        File.WriteAllText(dialog.FileName, jsonData);
    }
}
```

---

## Quick Reference Checklist

### When Creating a New ViewModel
- [ ] Inherit from `ObservableObject`
- [ ] Declare class as `partial`
- [ ] Use `[ObservableProperty]` for simple properties
- [ ] Initialize commands in constructor
- [ ] Inject services via constructor with null checks
- [ ] Use file-scoped namespace
- [ ] Place in `ViewModels` folder

### When Creating a New Service
- [ ] Define interface in `Services` namespace
- [ ] Interface inherits `IDisposable` if managing resources
- [ ] Use async methods with `CancellationToken`
- [ ] Implement guard clauses
- [ ] Add structured logging
- [ ] Register in DI container
- [ ] Use file-scoped namespace

### Formatting Checklist
- [ ] 4-space indentation
- [ ] Allman-style braces (new line)
- [ ] No `this.` qualification
- [ ] Private fields with `_` prefix
- [ ] `string.Empty` instead of `""`
- [ ] Async methods end with `Async`
- [ ] Commands end with `Command`

### Async Method Checklist
- [ ] Method name ends with `Async`
- [ ] Returns `Task` or `Task<T>`
- [ ] Includes `CancellationToken` parameter (last)
- [ ] Passes token to async calls
- [ ] Has try/catch with logging
- [ ] Uses `await` (not `.Result` or `.Wait()`)

### XAML Binding Checklist
- [ ] Commands bound to `ICommand` properties
- [ ] Two-way bindings where needed
- [ ] Use pack URIs for resources
- [ ] Minimal or no code-behind
- [ ] Converters in resources
- [ ] `ObservableCollection` for dynamic lists

---

## Examples from Codebase

### Complete ViewModel Example
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB.Studio.Wpf.Services;
using System.Collections.ObjectModel;

namespace LiteDB.Studio.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDatabaseService _dbService;

    [ObservableProperty]
    private string _cursorText = string.Empty;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _currentDatabase = string.Empty;

    public ObservableCollection<TabViewModel> Tabs { get; } = [];
    public ObservableCollection<string> RecentDatabases { get; } = [];

    public MainViewModel(IDatabaseService dbService)
    {
        _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));

        _dbService.ConnectionStateChanged += OnConnectionStateChanged;

        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
        DisconnectCommand = new RelayCommand(Disconnect);
        RunCommand = new RelayCommand(Run);
    }

    public IAsyncRelayCommand ConnectCommand { get; }
    public IRelayCommand DisconnectCommand { get; }
    public IRelayCommand RunCommand { get; }

    private async Task ConnectAsync()
    {
        try
        {
            CursorText = "Connecting...";
            await _dbService.ConnectAsync(CurrentDatabase, CancellationToken.None);
            IsConnected = true;
            CursorText = "Connected";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Connection failed");
            CursorText = $"Error: {ex.Message}";
        }
    }

    private void Disconnect()
    {
        _dbService.Disconnect();
        IsConnected = false;
        CursorText = "Disconnected";
    }

    private void Run()
    {
        // Implementation
    }

    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        IsConnected = e.IsConnected;
    }
}
```

### Complete Service Example
```csharp
using Serilog;

namespace LiteDB.Studio.Wpf.Services;

public class LiteDbService : IDatabaseService
{
    private LiteDatabase? _db;
    private readonly Lock _sync = new();

    public bool IsConnected => _db != null;

    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    public Task ConnectAsync(string connectionString, CancellationToken cancellationToken)
    {
        if (IsConnected) throw new InvalidOperationException("Already connected");

        try
        {
            _db = new LiteDatabase(connectionString);
            Log.Information("Connected to database: {ConnectionString}", connectionString);
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(true));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to connect");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        if (!IsConnected) return Task.CompletedTask;

        lock (_sync)
        {
            try
            {
                _db?.Dispose();
            }
            finally
            {
                _db = null;
                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(false));
            }
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
    }
}
```

---

## Summary

This coding style guide ensures consistency across the LiteDB.Studio.Wpf project by:

1. **Formatting**: 4 spaces, Allman braces, CRLF, no `this.` qualification
2. **Naming**: PascalCase public members, `_camelCase` private fields
3. **MVVM**: `ObservableObject` + `[ObservableProperty]` + source generators
4. **Async**: All async methods take `CancellationToken`, use `await`
5. **DI**: Constructor injection, interface-based services
6. **Logging**: Structured Serilog with context
7. **Error Handling**: Guard clauses, try/catch with logging, user-friendly messages
8. **XAML**: Minimal code-behind, binding-driven UI

Use this guide to generate code that seamlessly integrates with the existing codebase.
