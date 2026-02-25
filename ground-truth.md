# Ground Truth

## Project Goal

Port LiteDB.Studio (WinForms) to WPF using MVVM + DI, without modifying the `LiteDB` library.

## Architecture

WPF (`LiteDB.Studio.Wpf`) + MVVM library (`LiteDB.Studio.Mvvm`) · DI via `Microsoft.Extensions.Hosting` · Constructor injection throughout.

## Key Decisions

- All ViewModels/Views registered in DI (singleton for main views, transient for dialogs)
- XAML `DataContext` set in code-behind; `ContentControl` pattern used to host DI-created views
- Transaction commands (Begin/Commit/Rollback) use `AsyncRelayCommand` with `CanExecute` predicates; `OnTransactionActiveChanged` triggers `NotifyCanExecuteChanged`
- Toolbar buttons are sole entry point for transactions (no Transaction menu)
- `DatabaseDebuggerService` + `DebuggerViewModel` + `DebuggerView` added as singletons; hosted in left-panel below tree view
- `LiteDatabase.Execute()` auto-commits each SQL script, bypassing `BeginTrans()`; rollback data-persistence tests not viable via `ExecuteAsync`

## Constraints

- **Do not touch** `LiteDB/` or `LiteDB.Studio/` project trees
- No functional UI redesigns; minimal, scoped refactors only

## Current Status

Phase 8 (US6 — Transactions & Debugger) complete. 46 tests passing. T109–T121 all marked [X]. Manual integration testing pending.
