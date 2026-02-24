# Ground Truth

## Project Goal
Port LiteDB.Studio (WinForms) to WPF using MVVM + DI, without modifying the `LiteDB` library.

## Architecture
WPF (`LiteDB.Studio.Wpf`) + MVVM library (`LiteDB.Studio.Mvvm`) · DI via `Microsoft.Extensions.Hosting` · Constructor injection throughout; no static lookups or manual `new` for ViewModels/Views.

## Key Decisions
- All ViewModels/Views registered in DI (singleton for main views, transient for dialogs)
- `DatabaseTreeView` and `ConnectionManagerWindow` injected via constructor, not XAML-instantiated
- XAML `DataContext` set in code-behind; `ContentControl` pattern used to host DI-created views
- ViewFactory infrastructure removed (was unused)
- `.github/copilot-instructions.md` consolidates operating guidelines + C# style

## Constraints
- **Do not touch** `LiteDB/` or `LiteDB.Studio/` project trees
- No functional UI redesigns; minimal, scoped refactors only

## Current Status
MVVM/DI refactor complete. All 37 tests passing. Manual integration testing pending.
Serilog removed from MVVM base types; `ILogger<T>` constructor-injected throughout; `ILoggerFactory` threaded via `MainViewModel → TabManager → TabViewModel → ResultGridViewModel`.
`LiteDbOptions.MaxRows` (default 1000) read from `appsettings.json` `LiteDb` section via `IOptions<LiteDbOptions>`; `new LiteDbService()` in tests still works via optional parameter.
All refactor items complete. Only manual integration testing remains.
