# Ground Truth

## Project Goal

Port LiteDB.Studio (WinForms) to WPF using MVVM + DI, without modifying the `LiteDB` library.

## Architecture

WPF (`LiteDB.Studio.Wpf`) + MVVM library (`LiteDB.Studio.Mvvm`) · DI via `Microsoft.Extensions.Hosting` · Constructor injection throughout.

## Key Decisions

- All ViewModels/Views registered in DI (singleton for main views, transient for dialogs)
- Transaction commands use `AsyncRelayCommand` with `CanExecute` predicates; `OnTransactionActiveChanged` triggers `NotifyCanExecuteChanged`
- `LiteDatabase.Execute()` auto-commits each SQL script — rollback data-persistence tests not viable via `ExecuteAsync`
- Performance tests use direct `QueryResult` construction; memory-leak tests use `WeakReference` + static helper methods
- xUnit: use predicate overloads (`Assert.Contains`, `Assert.DoesNotContain`) — never `Assert.True(x.Any(...))` or `Assert.NotEmpty(Where(...))`
- CS0067 on `ICommand.CanExecuteChanged`: suppress with empty `add { } remove { }` accessors
- All `public`/`protected` members carry XML doc comments; interface implementations use `/// <inheritdoc />`
- `LiteDbService.DisposeAsync` calls `DisconnectAsync`; tests use `await service.DisposeAsync()` in `finally` blocks

## Constraints

- **Do not touch** `LiteDB/` or `LiteDB.Studio/` project trees
- No functional UI redesigns; minimal, scoped refactors only

## Current Status

US7 in progress. 66 tests passing. T129–T131 (CI pipeline, PR template) remaining.