# LiteDB.Studio.Wpf — Comprehensive Coding Guidelines for GitHub Copilot

This document consolidates coding style, architecture patterns, and conventions for the `LiteDB.Studio.Wpf` project. Use this guide to generate code that matches the existing codebase.

## 1. Project Overview & Architecture

- **Framework**: .NET 9.0 (`net9.0-windows`).
- **UI Framework**: WPF (Windows Presentation Foundation).
- **Architecture**: MVVM (Model-View-ViewModel).
- **Key Libraries**:
  - **MVVM**: `CommunityToolkit.Mvvm` (v8.2.0).
  - **Dependency Injection**: `Microsoft.Extensions.DependencyInjection`.
  - **Logging**: `Serilog` (Console, File, Debug sinks).
  - **Editor**: `AvalonEdit`.
  - **Database**: `LiteDB`.
- **Project Settings**: `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`.

## 2. Code Formatting & Style (Strict)

Follow the `.editorconfig` settings and observed patterns:

- **Indentation**: 4 spaces (NO tabs).
- **Line Endings**: CRLF.
- **Braces**: **Allman style** (Open brace on a new line).
  ```csharp
  // correct
  if (condition)
  {
      DoSomething();
  }
  ```
- **New Lines**: Before `catch`, `else`, `finally`.
- **Namespaces**: Use **File-scoped namespaces** in all new files.
  ```csharp
  namespace LiteDB.Studio.Wpf.ViewModels; // no braces
  ```
- **Using Directives**: Place **outside** the namespace. No forced sorting, but usually System first.
- **This Qualification**: **DO NOT** use `this.` to access members (e.g., use `_property` or `Property`, not `this.Property`).

## 3. Naming Conventions

- **PascalCase**: Classes, Methods, Properties, Constants, Events, Interfaces (prefix with `I`).
- **_camelCase**: Private fields (e.g., `_dbService`, `_isConnected`).
- **camelCase**: Local variables, parameters.
- **Suffixes**:
  - Async methods: `...Async` (e.g., `ConnectAsync`).
  - Commands: `...Command` (e.g., `SaveCommand`).
- **String Literals**: Prefer `string.Empty` over `""`.

## 4. MVVM Patterns (CommunityToolkit.Mvvm)

### ViewModels
- Inherit from `ObservableObject`.
- Use `partial` class to enable source generators.
- Use `[ObservableProperty]` for simple fields. This generates the public property and `INotifyPropertyChanged` code.
- **Constructor Injection**: Inject services via constructor. Guard against nulls.

```csharp
public partial class MainViewModel : ObservableObject
{
    private readonly IDatabaseService _dbService;

    [ObservableProperty]
    private string _title = string.Empty;

    public MainViewModel(IDatabaseService dbService)
    {
        _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
    }

    public IAsyncRelayCommand ConnectCommand { get; }
    
    private async Task ConnectAsync() { /* ... */ }
}
```

### Commands
- Use `IAsyncRelayCommand` / `AsyncRelayCommand` for async operations.
- Use `IRelayCommand` / `RelayCommand` for synchronous operations.
- Initialize commands in the constructor or use `[RelayCommand]` attribute if appropriate (though explicit initialization is common in this repo).

## 5. Service Layer & Dependency Injection

- **Interface-First**: All services must implement an interface (e.g., `IDatabaseService`).
- **Implementation**:
  - Use `Microsoft.Extensions.DependencyInjection`.
  - Register services in `App.xaml.cs` or the composition root.
- **Service Pattern**:
  - Guard clauses at the start of methods.
  - Use `CancellationToken` in async methods.
  - Wrap external library calls (like LiteDB) in try/catch blocks with logging.

## 6. Asynchronous Programming

- **Return Types**: `Task` or `Task<T>`. Avoid `async void`.
- **Cancellation**: Always accept a `CancellationToken` parameter (default to `default` or `None` if optional) and pass it down.
- **Task.CompletedTask**: Use for synchronous implementations of async interfaces.
- **Await**: Always await tasks; do not use `.Result` or `.Wait()`.

```csharp
public async Task<QueryResult> ExecuteAsync(string query, CancellationToken cancellationToken)
{
    // ...
}
```

## 7. Logging (Serilog)

- **Injection**: Use `ILogger` or static `Log` class (be consistent with context).
- **Structured Logging**: Use message templates with named properties.
- **Exceptions**: Pass the exception object as the first argument.

```csharp
Log.Information("Connecting to database: {ConnectionString}", connectionString);
Log.Error(ex, "Failed to execute query");
```

## 8. Error Handling

- **Guard Clauses**: Validate arguments and state early.
  ```csharp
  if (_db == null) throw new InvalidOperationException("Not connected");
  ```
- **User Feedback**: Capture exceptions in ViewModels and expose them via properties (e.g., `ErrorMessage`, `StatusText`) rather than throwing to the UI.

## 9. WPF & XAML Conventions

- **Bindings**: Prefer binding commands and properties over code-behind events.
- **Attached Behaviors**: Use for controls that don't support binding natively (e.g., `AvalonEdit`).
- **Resources**: Use **Pack URIs** for images/resources.
  `pack://application:,,,/LiteDB.Studio.Wpf;component/Resources/Icons/database.png`
- **Code-Behind**: Keep minimal. Only use for strictly view-centric logic that cannot be handled by VM (e.g., window closing dialogs).
- **Controls**: Use `Grid` and `DockPanel` for layout.

## 10. Checklist for New Code

1.  **File Header**: No specific header required.
2.  **Namespace**: File-scoped.
3.  **Class**: Partial (if VM).
4.  **Formatting**: 4 spaces, Allman braces.
5.  **Logic**: Async/Await with CancellationTokens.
6.  **Style**: No `this.`, `_camelCase` fields.
