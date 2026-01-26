# LiteDB.Studio.Wpf — Copilot Instruction Summary (Raptor)

💡 **Purpose:** Short, actionable instructions for GitHub Copilot to generate C# code consistent with the LiteDB.Studio.Wpf codebase.

## Project at a glance
- Target: .NET 9.0 (net9.0-windows)
- UI: WPF (MVVM)
- Toolkit: CommunityToolkit.Mvvm (source generators)
- DI: Microsoft.Extensions.DependencyInjection
- Logging: Serilog (structured)
- Editor component: AvalonEdit
- Nullable: enabled; ImplicitUsings: enabled

---

## Formatting & style (must follow)
- Indent with **4 spaces** (no tabs). CRLF line endings. ✅
- **Allman brace style** (open brace on new line) for methods/blocks.
- Use **file-scoped namespaces** for new files (e.g., `namespace LiteDB.Studio.Wpf.ViewModels;`).
- Place `using` directives **outside** namespace. No `this.` qualifier on members.
- Follow spacing rules: space after commas & around binary ops; no extra spaces inside parentheses/dots.

---

## Naming & structure
- Public types/members: **PascalCase**. Interfaces: start with `I`.
- Private fields: **_camelCase** (leading underscore).
- Local variables/parameters: camelCase.
- Async methods end with `Async`; commands end with `Command`.
- Prefer `string.Empty` over `""`.

---

## MVVM & CommunityToolkit guidelines
- ViewModels: inherit `ObservableObject` and be declared `partial` when using source generators.
- Use `[ObservableProperty]` for simple properties; use manual `SetProperty` for custom setter logic.
- Use `IAsyncRelayCommand` / `AsyncRelayCommand` for async actions and `IRelayCommand` / `RelayCommand` for sync.
- Initialize commands in constructor and inject dependencies via constructor with null-guard checks.
- Keep code-behind minimal (wiring and view-specific actions only).

Example ViewModel snippet:

```csharp
public partial class MainViewModel : ObservableObject
{
    private readonly IDatabaseService _dbService;

    [ObservableProperty]
    private string _title = string.Empty;

    public IAsyncRelayCommand ConnectCommand { get; }

    public MainViewModel(IDatabaseService dbService)
    {
        _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
    }

    private async Task ConnectAsync(CancellationToken ct = default) { /* ... */ }
}
```

---

## Async, cancellation & error handling
- Async methods: return `Task`/`Task<T>` and accept `CancellationToken` (last parameter).
- Use `await` (avoid `.Result` / `.Wait()`) and check `cancellationToken.ThrowIfCancellationRequested()` in loops.
- Use guard clauses (`ArgumentNullException`, `InvalidOperationException`) at entry.
- Catch exceptions, log with Serilog (exception as first parameter), and expose user-facing messages via ViewModel properties.

---

## Services & DI patterns
- Define service interfaces (e.g., `IDatabaseService`) and implement with a concrete class (e.g., `LiteDbService`).
- Register services in host (`Host.CreateDefaultBuilder().ConfigureServices(...)`).
- Use constructor injection and subscribe to events for state changes. Keep resource cleanup via `Dispose()`/async wrappers.

---

## Logging (Serilog)
- Use structured logging: `Log.Information("Message {Prop}", value)`; exception first in error calls: `Log.Error(ex, "...")`.
- Avoid logging secrets/PII.

---

## WPF & XAML conventions
- Use bindings, DataTemplates, converters and `ObservableCollection<T>` for dynamic lists.
- Use pack URIs for resources: `pack://application:,,,/LiteDB.Studio.Wpf;component/Resources/<file>`.
- Use attached behaviors for non-bindable controls (e.g., AvalonEdit editor text/caret attachments).

---

## Quick checklist for Copilot-generated snippets ✅
- 4-space indent, Allman braces
- File-scoped namespace & `using` outside namespace
- No `this.` qualifier
- `_camelCase` private fields, PascalCase public API
- `ObservableObject` + `[ObservableProperty]` for VMs
- Commands with `RelayCommand`/`AsyncRelayCommand`
- Async methods include `CancellationToken`
- Guard clauses + structured Serilog logging
- Minimal code-behind, binding-driven XAML