# LiteDB.Studio.Wpf C# Coding Style Guide for GitHub Copilot

## Project Context
- **Framework**: .NET 9.0 Windows Desktop (WPF)
- **Pattern**: MVVM using CommunityToolkit.Mvvm v8.2.0
- **DI Container**: Microsoft.Extensions.DependencyInjection
- **Logging**: Serilog (File, Console, Debug sinks)
- **Editor**: AvalonEdit v6.3.1.120
- **Nullable**: Enabled
- **ImplicitUsings**: Enabled

---

## Code Formatting (from .editorconfig)

### Basic Formatting
- **Indentation**: 4 spaces (never tabs)
- **Line endings**: CRLF (Windows)
- **Tab width**: 4
- **Final newline**: Not required

### Braces and New Lines
- **Open braces**: Always on new line (Allman style)
  ```csharp
  if (condition)
  {
      // code
  }
  ```
- **New line before**: `catch`, `else`, `finally`
- **Preserve single-line blocks**: Yes (when already present)

### Spacing
- No space after cast: `(int)value`
- Space after keywords: `if (`, `for (`, `while (`
- Space around binary operators: `a + b`, `x == y`
- No space before/after dots: `obj.Method()`
- No space inside parentheses or brackets

---

## Naming Conventions

### Classes and Members
- **Classes/Structs/Enums**: PascalCase
- **Interfaces**: PascalCase with `I` prefix (`IDatabaseService`)
- **Methods**: PascalCase
- **Properties**: PascalCase
- **Events**: PascalCase
- **Private fields**: `_camelCase` with underscore prefix
- **Constants**: PascalCase
- **Local variables**: camelCase

### Special Patterns
- **Async methods**: Suffix with `Async` (`ConnectAsync`, `ExecuteAsync`)
- **Event handlers**: `On{Event}` or `{Control}_{Event}` for XAML

### Examples from Codebase
```csharp
private readonly IDatabaseService _dbService;
private string _currentDatabase = string.Empty;
private bool _isConnected;

public string CurrentDatabase { get; set; }
public bool IsConnected { get; set; }

public async Task ConnectAsync() { }
private void OnConnectionStateChanged() { }
```

---

## Language Style Preferences

### `var` Usage
- **Use `var`**: For built-in types and when type is apparent
  ```csharp
  var count = 0;
  var result = new QueryResult();
  var items = list.Where(x => x.IsActive).ToList();
  ```
- **Avoid `var`**: When type is not obvious from right-hand side

### Type References
- Prefer built-in keywords: `string`, `int`, `bool` (not `String`, `Int32`, `Boolean`)

### `this.` Qualification
- **Never use** `this.` for fields, properties, methods, or events
  ```csharp
  // Correct
  _dbService.Connect();
  IsConnected = true;
  
  // Incorrect
  this._dbService.Connect();
  this.IsConnected = true;
  ```

### Expression-Bodied Members
- **Properties/Accessors**: Expression bodies preferred when simple
  ```csharp
  public string StatusText => $"Collections: {CollectionsCount}";
  ```
- **Methods**: Block bodies preferred (not expression-bodied)
  ```csharp
  // Preferred
  public void Disconnect()
  {
      _dbService.Disconnect();
  }
  ```

### Null Handling
- Use null-coalescing: `value ?? defaultValue`
- Use null-propagation: `obj?.Property`
- Use `is null` checks: `if (value is null)`
- Pattern matching: `if (obj is MyType typed)`

### Modern C# Features
- Collection initializers: `new List<string> { "a", "b" }`
- Object initializers: `new Person { Name = "John" }`
- Pattern matching: `if (result is { Success: true })`
- Switch expressions: Encouraged
- Tuple names: Explicit names preferred

---

## MVVM Architecture (CommunityToolkit.Mvvm)

### ViewModel Structure
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LiteDB.Studio.Wpf.ViewModels;

public partial class MyViewModel : ObservableObject
{
    private readonly IMyService _service;
    
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private bool _isActive;
    
    public MyViewModel(IMyService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);
    }
    
    public IAsyncRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }
    
    private async Task SaveAsync()
    {
        await _service.SaveAsync(Name);
    }
    
    private void Cancel()
    {
        Name = string.Empty;
    }
}
```

### Key Points
1. **Inheritance**: All ViewModels inherit from `ObservableObject`
2. **Partial**: Classes using `[ObservableProperty]` must be `partial`
3. **ObservableProperty**: Use for simple properties; source generator creates public property
4. **Manual Properties**: When custom logic needed in setter, use `SetProperty(ref _field, value)`
5. **Commands**: Use `RelayCommand` (sync) or `AsyncRelayCommand` (async)
6. **Constructor Injection**: All dependencies injected via constructor
7. **Null Guards**: Check injected dependencies with `?? throw new ArgumentNullException`

### Command Patterns
```csharp
// Property-style (exposed to XAML)
public IAsyncRelayCommand ConnectCommand { get; }
public IRelayCommand DisconnectCommand { get; }
public IRelayCommand<string> OpenFileCommand { get; }

// Constructor initialization
ConnectCommand = new AsyncRelayCommand(ConnectAsync);
DisconnectCommand = new RelayCommand(Disconnect);
OpenFileCommand = new RelayCommand<string>(OpenFile);
```

### Property Change Notifications
```csharp
// Using SetProperty for manual control
public string Title
{
    get => _title;
    set
    {
        if (!SetProperty(ref _title, value)) return;
        
        // Custom logic after change
        OnPropertyChanged(nameof(DisplayTitle));
    }
}

// Partial method for generated property
[ObservableProperty]
private QueryResult? _lastResult;

partial void OnLastResultChanged(QueryResult? value)
{
    ResultGridViewModel.QueryResult = value;
}
```

---

## Async/Await Patterns

### Standard Async Method Signature
```csharp
public async Task<QueryResult> ExecuteAsync(string query, CancellationToken cancellationToken)
{
    // Implementation
}

public Task ConnectAsync(string connectionString, CancellationToken cancellationToken)
{
    // Synchronous work that returns completed task
    return Task.CompletedTask;
}
```

### Cancellation Support
- All async methods take `CancellationToken` parameter (last parameter)
- Call `cancellationToken.ThrowIfCancellationRequested()` in loops
- Pass token to downstream async calls

### Error Handling
```csharp
try
{
    var result = await _service.ExecuteAsync(query, cancellationToken);
    LastResult = result;
}
catch (Exception ex)
{
    Log.Warning(ex, "Operation failed");
    LastError = ex.Message;
}
```

---

## Service Layer Patterns

### Interface Definition
```csharp
namespace LiteDB.Studio.Wpf.Services;

public interface IDatabaseService : IDisposable
{
    bool IsConnected { get; }
    bool TransactionActive { get; }
    
    Task ConnectAsync(string connectionString, CancellationToken cancellationToken);
    Task DisconnectAsync();
    Task<QueryResult> ExecuteAsync(string? query, CancellationToken cancellationToken);
    
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
}
```

### Implementation
```csharp
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
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(true));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to connect");
            throw;
        }
        
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
    }
}
```

### Key Service Patterns
1. Interfaces define contracts
2. Guard clauses for invalid state
3. Events for state changes (nullable event handlers)
4. Structured logging with Serilog
5. Dispose pattern when managing resources

---

## Logging with Serilog

### Usage Patterns
```csharp
using Serilog;

// Informational
Log.Information("Connected to database. Collections: {Count}", collectionCount);

// Warnings with context
Log.Warning(ex, "Failed to load collections");

// Errors
Log.Error(ex, "Database connection failed: {Message}", ex.Message);

// Debug
Log.Debug("GetSystemCollectionNamesAsync returning {Count} collections", names.Length);
```

### Guidelines
- Use structured logging (property syntax `{PropertyName}`)
- Include exception as first parameter when logging errors
- Don't log sensitive data (passwords, PII)
- Use appropriate log levels

---

## WPF/XAML Conventions

### File-Scoped Namespaces
```csharp
namespace LiteDB.Studio.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // ...
}
```

### Resource Pack URIs
```csharp
IconUri = "pack://application:,,,/LiteDB.Studio.Wpf;component/Resources/Icons/database.png";
```

### Code-Behind (Minimal)
```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // DI from host
        var host = (Application.Current as App)?.HostInstance;
        var vm = host?.Services.GetRequiredService<MainViewModel>();
        DataContext = vm;
        
        // Initialize
        vm?.Initialize();
    }
}
```

### Attached Behaviors
- Use attached properties for non-bindable properties
- Example: `AvalonEditBehaviors.EditorText`

---

## Error Handling Philosophy

1. **Guard clauses** at method entry
   ```csharp
   if (!IsConnected) throw new InvalidOperationException("Not connected");
   ```

2. **Null checks** for injected dependencies
   ```csharp
   _service = service ?? throw new ArgumentNullException(nameof(service));
   ```

3. **Try-catch** with specific error messages
   ```csharp
   catch (Exception ex)
   {
       throw new InvalidOperationException($"Query failed: {ex.Message}", ex);
   }
   ```

4. **User-facing errors** set properties for binding
   ```csharp
   catch (Exception ex)
   {
       LastError = ex.Message;
       CursorText = "Error: " + ex.Message;
   }
   ```

---

## Collections and LINQ

### Prefer
- `ObservableCollection<T>` for bindable collections
- `List<T>` for internal collections
- LINQ for queries: `items.Where(x => x.IsActive).OrderBy(x => x.Name)`
- Collection expressions (modern C#): `[item1, item2]` for initialization

### Avoid
- Manual loops where LINQ suffices
- `ArrayList` or non-generic collections

---

## Common Patterns Observed

### Connection String Mapping
```csharp
var cs = new ConnectionString(filename)
{
    Connection = mode == Direct ? ConnectionType.Direct : ConnectionType.Shared,
    Filename = filename,
    ReadOnly = readOnly,
    Password = password
};
```

### Tree Node Construction
```csharp
Action<string> insertSnippet = snippet => InsertSnippetRequested?.Invoke(this, snippet);

var node = new DbTreeNode(_databaseService, insertSnippet)
{
    Header = collectionName,
    Tag = "collection",
    IconUri = "pack://application:,,,/Resources/Icons/collection.png"
};
```

### Settings Persistence
```csharp
Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings = cs;
Util.AppSettingsManager.AddToRecentList(cs);
Util.AppSettingsManager.PersistData();
```

---

## Summary: Quick Reference for Code Generation

1. **Indentation**: 4 spaces, Allman braces
2. **Naming**: PascalCase public, `_camelCase` private fields
3. **ViewModels**: `ObservableObject`, `[ObservableProperty]`, commands in constructor
4. **Async**: Always include `CancellationToken`, suffix methods with `Async`
5. **Services**: Interface-based, constructor injection, guard clauses
6. **Logging**: Serilog with structured properties
7. **XAML**: Minimal code-behind, binding-driven
8. **Null safety**: Use `??`, `?.`, and `is null` checks
9. **No `this.`**: Never qualify members with `this.`
10. **File-scoped namespaces**: Use in all new files
