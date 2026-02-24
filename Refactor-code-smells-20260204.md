# Refactor Analysis (LiteDB.Studio.Wpf + LiteDB.Studio.Mvvm)

Date: 2026-02-04
Scope: LiteDB.Studio.Wpf + LiteDB.Studio.Mvvm (analysis only, no code changes)

## Summary
The WPF project is functional but contains MVVM boundary violations, synchronous blocking inside services, and view-specific logic embedded in ViewModels. The MVVM support library also mixes concerns (logging, view references, async startup), which creates hidden dependencies and makes testing harder. The refactor opportunities below are grouped by priority, with affected files and rationale.

## High-Priority Refactor Opportunities

### 1) Remove UI dependencies from ViewModels (Done)
**Why:** ViewModels call UI APIs directly (MessageBox, dialogs, Application.Current), which couples them to WPF and makes them hard to unit test.

**Examples:**
- MainViewModel uses MessageBox and opens ConnectionManagerWindow directly.
- DbTreeNode uses MessageBox and SaveFileDialog.
- ConnectionManagerViewModel uses OpenFileDialog.

**Refactor:** Introduce abstractions (e.g., IDialogService, IFileDialogService, IFileService) and inject them into ViewModels. Views or services implement WPF-specific dialogs.

**Files:**
- LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs
- LiteDB.Studio.Wpf/ViewModels/DbTreeNode.cs
- LiteDB.Studio.Wpf/ViewModels/ConnectionManagerViewModel.cs

---

### 2) Remove WPF UI types from ViewModels (Done)
**Why:** ResultGridViewModel creates DataGridColumn, Binding, Style. That is view-specific logic and should live in the View or a UI layer.

**Refactor:** Replace DataGridColumn creation with a view-model-friendly descriptor (e.g., ColumnDescriptor with name, type, formatting hints). Build WPF DataGridColumns in ResultGrid control or a view-only helper.

**Files:**
- LiteDB.Studio.Wpf/ViewModels/ResultGridViewModel.cs
- LiteDB.Studio.Wpf/Controls/ResultGrid.xaml.cs

---

### 3) Replace static AppSettingsManager with injectable service (Done)
**Why:** Static utility with file I/O is hard to test and hides dependencies; it also makes persistence logic implicit and scattered.

**Refactor:** Create an IAppSettingsService with file system dependency injected. Use it from ViewModels and views. This improves testability and allows mocking.

**Files:**
- LiteDB.Studio.Wpf/Util/AppSettingsManager.cs
- LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs
- LiteDB.Studio.Wpf/Views/MainWindow.xaml.cs

---

### 4) Avoid blocking calls in services and app startup (Done)
**Why:** LiteDbService uses .GetAwaiter().GetResult() in Dispose/Disconnect. LiteDbStudioApplication uses Task.Run for AppHost.StartAsync without proper lifetime handling. Both can cause deadlocks and obscure failure paths.

**Refactor:** Implement IAsyncDisposable for LiteDbService and manage shutdown explicitly. Make AppHost.StartAsync awaited on startup (or a robust background start with error handling and readiness flag). Avoid sync-over-async.

**Files:**
- LiteDB.Studio.Wpf/Services/LiteDbService.cs
- LiteDB.Studio.Wpf/App.xaml.cs
- LiteDB.Studio.Mvvm/LiteDbStudioApplication.cs

---

## Medium-Priority Refactor Opportunities

### 5) Move code-behind logic into ViewModels or behaviors (Done)
**Why:** Views include behavior (double-click handling, syntax highlighting lifecycle) that could be separated for testability and consistency with MVVM.

**Refactor:**
- Use behaviors or commands for TreeView double-click.
- Encapsulate highlighting registration and application in a service or attached behavior.

**Files:**
- LiteDB.Studio.Wpf/Views/DatabaseTreeView.xaml.cs
- LiteDB.Studio.Wpf/Views/MainWindow.xaml.cs

---

### 6) Remove direct Serilog usage from core MVVM abstractions ✅ Done
**Why:** IViewModel and ViewModel require Serilog directly, which bakes in a logging framework. This also hides the dependency and makes unit tests harder.

**Refactor:** Replace Serilog dependency with Microsoft.Extensions.Logging abstractions (ILogger<T>) and inject loggers at the ViewModel level. Keep Serilog only in composition root.

**Files:**
- LiteDB.Studio.Mvvm/ViewModels/IViewModel.cs
- LiteDB.Studio.Mvvm/ViewModels/ViewModel.cs
- LiteDB.Studio.Mvvm/LiteDbStudioApplication.cs
- LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs
- LiteDB.Studio.Wpf/ViewModels/TabViewModel.cs
- LiteDB.Studio.Wpf/ViewModels/ResultGridViewModel.cs

---

### 7) Cancellation token usage is inconsistent (Done)
**Why:** Many async operations use CancellationToken.None, which prevents cancellation and affects responsiveness.

**Refactor:** Add CancellationToken parameters to commands and pass tokens through to services. Introduce cancellation support for long queries and schema sampling.

**Files:**
- LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs
- LiteDB.Studio.Wpf/ViewModels/TabViewModel.cs
- LiteDB.Studio.Wpf/ViewModels/DbTreeNode.cs
- LiteDB.Studio.Wpf/Services/LiteDbService.cs

---

### 8) Reduce tight coupling between ViewModels and Views in MVVM layer (Done)
**Why:** ShellContentViewModel keeps a hard reference to IShellContentView (View). This creates a back-reference from VM to View, violating MVVM separation and making testing difficult.

**Refactor:** Remove the View property or replace it with an interface that only exposes necessary services (e.g., IWindowService). Prefer messaging or mediator patterns rather than direct View references.

**Files:**
- LiteDB.Studio.Mvvm/ViewModels/Shell/ShellContentViewModel.cs

---

## Low-Priority Refactor Opportunities

### 9) Configuration for row limits and timeouts ✅ Done
**Why:** Max rows (1000) and other behavior are hard-coded in LiteDbService.

**Refactor:** Move to configuration options (appsettings.json or options pattern). This provides user control and easier testing.

**Files:**
- LiteDB.Studio.Wpf/Services/LiteDbService.cs
- LiteDB.Studio.Wpf/appsettings.json

---

### 10) Improve cohesion of tab management logic (Done)
**Why:** MainViewModel handles multiple concerns (tabs, recent files, connection flow, UI title updates).

**Refactor:** Extract tab management into a dedicated service or child ViewModel. Keep MainViewModel as orchestrator.

**Files:**
- LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs

---

### 11) Reduce duplication in connection flows (Done)
**Why:** Connection logic is duplicated between ConnectAsync and OpenRecentAsync.

**Refactor:** Extract common connection logic to a single method to ensure consistent behavior and state updates.

**Files:**
- LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs

---

### 12) Improve MVVM abstractions for design mode detection (Done)
**Why:** ViewModel.InDesignMode uses Debugger.IsAttached, which is not an accurate design-mode indicator in WPF.

**Refactor:** Use DesignerProperties.GetIsInDesignMode or a dedicated IDesignModeService.

**Files:**
- LiteDB.Studio.Mvvm/ViewModels/ViewModel.cs

---

## Suggested Incremental Refactor Plan
1) Add IDialogService + IFileDialogService + IFileService and refactor DbTreeNode or ConnectionManagerViewModel (small, high impact).
2) Add IAppSettingsService and replace static AppSettingsManager usage.
3) Refactor ResultGridViewModel to use UI-agnostic column descriptors.
4) Clean up LiteDbService disposal and blocking calls; update app shutdown and LiteDbStudioApplication startup.
5) Migrate code-behind behavior into commands/behaviors.
6) ✅ Replace Serilog in MVVM base types with ILogger<T> and update DI.
7) Remove View references from ViewModels and replace with services or messaging.

## Test Impact / Opportunities
- Unit tests for settings persistence via IAppSettingsService.
- Unit tests for DbTreeNode drop/export flows using mock dialog/file services.
- Unit tests for MainViewModel connect/disconnect without real dialogs.
- UI integration tests for ResultGrid column generation (after descriptor refactor).
- MVVM-level tests that validate ShellViewModel startup and main content wiring without WPF views.

## Notes
- This analysis does not modify any files.
- The LiteDB project itself should not be modified per repository constraints.
