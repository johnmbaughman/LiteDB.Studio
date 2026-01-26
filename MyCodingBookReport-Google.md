# LiteDB.Studio.Wpf Coding Style & Architecture Report

This report analyzes the coding style, patterns, and conventions used in the `LiteDB.Studio.Wpf` project. It is intended to guide GitHub Copilot in generating code that is consistent with the existing codebase.

## 1. Project Overview & Architecture

- **Framework**: .NET 9.0 (`net9.0-windows`).
- **Pattern**: MVVM (Model-View-ViewModel).
- **MVVM Library**: `CommunityToolkit.Mvvm` (v8.2.0).
- **Dependency Injection**: `Microsoft.Extensions.DependencyInjection`.
- **Logging**: `Serilog`.
- **UI Framework**: WPF (Windows Presentation Foundation).
- **Editor Component**: `AvalonEdit`.

## 2. C# Coding Conventions

### 2.1. Formatting
- **Indentation**: 4 spaces (no tabs).
- **Line Endings**: Windows style (CRLF).
- **Brace Style**: Allman (K&R style with new line before open brace).
  ```csharp
  // Correct
  if (condition)
  {
      // ...
  }
  ```
- **Namespaces**: usage of **File-scoped namespaces** is preferred in new files (`file-header_template = unset`).
  ```csharp
  namespace LiteDB.Studio.Wpf.ViewModels;
  
  public class MyClass ...
  ```
- **Usings**:
  - `ImplicitUsings` are enabled (`.csproj`).
  - Explicit `using` directives placed **outside** the namespace declaration.
  - No sorting enforcement observed, but typically System first.

### 2.2. Naming Conventions
- **Classes/Methods/Properties**: PascalCase.
- **Interfaces**: PascalCase with `I` prefix (e.g., `IDatabaseService`).
- **Private Fields**: `_camelCase` with underscore prefix.
  ```csharp
  private readonly IDatabaseService _dbService;
  private string _title;
  ```
- **Constants**: PascalCase (typically).
- **Async Methods**: Suffix with `Async` (e.g., `ConnectAsync`).

### 2.3. Language Features
- **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`).
- **Var**: Prefer `var` when type is apparent (constructors, casts) or for built-in types (e.g., `int`, `string` per .editorconfig: `csharp_style_var_for_built_in_types = true:warning`).
- **File-Scoped Namespaces**: Used consistently in ViewModels and Services.
- **Async/Await**: extensively used for I/O bound operations. Avoid `async void` except for event handlers or `RelayCommand` delegates (though `AsyncRelayCommand` is preferred).

## 3. MVVM Patterns (CommunityToolkit.Mvvm)

### 3.1. ViewModels
- Inherit from `ObservableObject`.
- Use `partial` classes to enable source generators.
- Use `[ObservableProperty]` attribute for bindable properties.
  ```csharp
  public partial class MyViewModel : ObservableObject
  {
      [ObservableProperty]
      private string _name; // Generates public string Name { get; set; }
  }
  ```
- **Property Change Logic**: For custom logic in setters, use `SetProperty(ref _field, value)` manually instead of the attribute, or partial methods `OnNameChanged`.

### 3.2. Commands
- Use `[RelayCommand]` attribute on methods OR explicit `RelayCommand` / `AsyncRelayCommand` in constructors.
- **Naming**: Command fields/properties should end with `Command` (e.g., `ConnectCommand`).
- **Async Commands**: Use `AsyncRelayCommand` for awaitable operations.
  ```csharp
  ConnectCommand = new AsyncRelayCommand(ConnectAsync);
  ```

## 4. Services & Dependency Injection

- **Abstraction**: Services are defined by interfaces (e.g., `IDatabaseService`).
- **Injection**: Constructor injection.
- **Implementation**:
  - `LiteDbService` implements `IDatabaseService`.
  - Services are registered in `App.xaml.cs` / `Program.cs` (via `Host`).
- **Logging**: Inject `ILogger` or use static `Serilog.Log` (mixed usage observed, favor DI pattern if possible, but distinct static `Log.Information` usage seen in implementations).

## 5. LiteDB Specifics

- **Connection**: `LiteDatabase` instance is managed within `LiteDbService`.
- **Queries**:
  - Use `Execute` for raw SQL-like queries.
  - Use `GetCollectionNames`, `GetCollection` for schema exploration.
- **System Collections**: Special handling for system collections (starting with `$`).

## 6. WPF / XAML Style

- **Resources**: Centralized in `App.xaml` or typically local `Window.Resources` for converters.
- **Images/Icons**: Use Pack URIs.
  `pack://application:,,,/LiteDB.Studio.Wpf;component/Resources/icon.png`
- **Control Layout**: extensive use of `Grid` and `DockPanel`.
- **Behaviors**: Use attached properties/behaviors for non-bindable events (e.g., `AvalonEditBehaviors`).

## 7. .EditorConfig Highlights

- `dotnet_style_qualification_for_field = false`: Do not use `this._field`.
- `csharp_style_expression_bodied_methods = false`: Prefer block bodies for methods.
- `csharp_style_expression_bodied_properties = true`: Prefer expression bodies for simple properties.
- `csharp_new_line_before_open_brace = all`: Allman style braces.

## 8. Development Guidelines Summary

1.  **Create ViewModels** inheriting `ObservableObject`.
2.  **Use Source Generators** for properties (`[ObservableProperty]`) and commands (`[RelayCommand]`) where simple.
3.  **Inject Dependencies** via constructor.
4.  **Use Async/Await** for all file/database operations.
5.  **Follow .editorconfig** for formatting (4 spaces, Allman braces).
6.  **Separate Logic**: Keep code-behind empty; move logic to ViewModel or Services.
