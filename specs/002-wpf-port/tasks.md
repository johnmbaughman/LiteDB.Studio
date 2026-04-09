# Implementation Tasks: WPF Port — LiteDB.Studio Migration

**Feature**: 002-wpf-port | **Branch**: `002-wpf-port` | **Generated**: 2026-01-22  
**Source**: [spec.md](spec.md), [plan.md](plan.md), [data-model.md](data-model.md), [contracts/](contracts/)

## Overview

This document breaks down the WPF port into prioritized, executable tasks organized by user story. Each user story represents an independently testable increment. Tasks follow the checklist format for tracking and validation.

**Task Format**: `- [ ] [TaskID] [P?] [Story?] Description with file path`
- `[P]` = Parallelizable (no dependencies on incomplete tasks in same phase)
- `[Story]` = User story label (US1, US2, etc.) for story-specific tasks

**Implementation Strategy**: Logging implementation first (higher priority per constitution), then MVP first (User Story 1), then incremental delivery in priority order.

---

## Phase 1: Setup & Project Infrastructure (including Logging)

**Goal**: Initialize project structure, dependencies, foundational types, and implement mandatory Serilog file logging

**Story Goal**: N/A (infrastructure setup and cross-cutting logging)  
**Independent Test Criteria**: Project builds successfully; core types compile; log files created in %APPDATA%\Temp\LiteDB.Studio\ with structured entries

### Tasks

- [X] T001 Add NuGet package Serilog to LiteDB.Studio.Wpf.csproj
- [X] T002 [P] Add NuGet package Serilog.Sinks.File to LiteDB.Studio.Wpf.csproj
- [X] T003 Create Logging.cs in LiteDB.Studio.Wpf/Util/Logging.cs with Serilog configuration (file sink to Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Temp", "LiteDB.Studio"), rolling file naming, structured logging) — NOTE resolved: spec.md and plan.md both specify `%APPDATA%` (SpecialFolder.ApplicationData / Roaming); implementation correctly uses `ApplicationData`; no change to LocalApplicationData is needed or planned
- [X] T004 Initialize logging in App.xaml.cs OnStartup method (call Logging.Configure())
- [X] T005 Add logging statements to key application events: startup, shutdown, database connections, query executions, errors
- [X] T006 Verify log files are created and accessible in the specified location
- [X] T007 Add global exception handler in App.xaml.cs to catch unhandled exceptions and log them with full message and stack trace.
- [X] T008 Ensure all handled exceptions in services and ViewModels are logged with message and stack trace.
- [X] T009 Verify LiteDB.Studio.Wpf.csproj targets .NET 9.0 or compatible version
- [X] T010 [P] Add NuGet package CommunityToolkit.Mvvm to LiteDB.Studio.Wpf.csproj
- [X] T011 [P] Add NuGet package AvalonEdit to LiteDB.Studio.Wpf.csproj (if not already present)
- [X] T012 [P] Create LiteDB.Studio.Wpf/Services directory
- [X] T013 [P] Create LiteDB.Studio.Wpf/ViewModels directory
- [X] T014 [P] Create LiteDB.Studio.Wpf/Views directory
- [X] T015 [P] Create LiteDB.Studio.Wpf/Controls directory
- [X] T016 [P] Create LiteDB.Studio.Wpf/Converters directory
- [X] T017 [P] Create LiteDB.Studio.Wpf/Resources/Icons directory
- [X] T018 Create LiteDB.Studio.Wpf.Tests test project (xUnit) and add to solution
- [X] T019 Add NuGet package NSubstitute to LiteDB.Studio.Wpf.Tests.csproj
- [X] T020 Add project reference from LiteDB.Studio.Wpf.Tests to LiteDB.Studio.Wpf

---

## Phase 2: Foundational Types & Services

**Goal**: Implement core domain types and database service abstraction (blocking prerequisites for all user stories)

**Story Goal**: N/A (foundational layer)  
**Independent Test Criteria**: QueryResult, ColumnInfo, and IDatabaseService compile; LiteDbService passes integration tests for connect/disconnect

### Tasks

- [X] T021 Create QueryResult class in LiteDB.Studio.Wpf/Services/QueryResult.cs with properties: Rows, Columns, LimitExceeded, RowCount, ExecutionTime, Warnings, Metadata
- [X] T022 [P] Create ColumnInfo class in LiteDB.Studio.Wpf/Services/ColumnInfo.cs with properties: Name, BsonType, DisplayFormat
- [X] T023 Create IDatabaseService interface in LiteDB.Studio.Wpf/Services/IDatabaseService.cs with methods: ConnectAsync, DisconnectAsync, IsConnected, ExecuteAsync
- [X] T024 Add schema discovery methods to IDatabaseService: GetCollectionNamesAsync, GetSystemCollectionNamesAsync, GetCollectionSchemaAsync
- [X] T025 [P] Add UpdateDocumentFieldAsync method to IDatabaseService
- [X] T026 [P] Add transaction methods to IDatabaseService: BeginTransactionAsync, CommitTransactionAsync, RollbackTransactionAsync, CheckpointAsync, TransactionActive property
- [X] T027 [P] Add ConnectionStateChanged and TransactionStateChanged events to IDatabaseService
- [X] T028 Create LiteDbService class in LiteDB.Studio.Wpf/Services/LiteDbService.cs implementing IDatabaseService
- [X] T029 Implement LiteDbService.ConnectAsync in LiteDB.Studio.Wpf/Services/LiteDbService.cs (connection string parsing, file handle acquisition, error handling for locked files)
- [X] T030 Implement LiteDbService.DisconnectAsync in LiteDB.Studio.Wpf/Services/LiteDbService.cs (release handles, check for active transaction)
- [X] T031 Implement LiteDbService.ExecuteAsync in LiteDB.Studio.Wpf/Services/LiteDbService.cs (execute query, enforce row limit default 1000, measure ExecutionTime, populate QueryResult)
- [X] T032 [P] Implement LiteDbService.GetCollectionNamesAsync in LiteDB.Studio.Wpf/Services/LiteDbService.cs
- [X] T033 [P] Implement LiteDbService.GetSystemCollectionNamesAsync in LiteDB.Studio.Wpf/Services/LiteDbService.cs
- [X] T034 Implement LiteDbService.GetCollectionSchemaAsync in LiteDB.Studio.Wpf/Services/LiteDbService.cs (sample first 100 docs, infer schema, return ColumnInfo collection)
- [X] T035 [P] Implement LiteDbService.UpdateDocumentFieldAsync in LiteDB.Studio.Wpf/Services/LiteDbService.cs (locate document by _id, update field, handle type conversion)
- [X] T036 [P] Implement transaction methods in LiteDB.Studio.Wpf/Services/LiteDbService.cs: BeginTransactionAsync, CommitTransactionAsync, RollbackTransactionAsync, CheckpointAsync
- [X] T037 Add integration test LiteDbServiceTests.ConnectAsync_EstablishesConnection in LiteDB.Studio.Wpf.Tests/Integration/LiteDbServiceTests.cs (use in-memory database)
- [X] T038 [P] Add integration test LiteDbServiceTests.DisconnectAsync_ReleasesResources in LiteDB.Studio.Wpf.Tests/Integration/LiteDbServiceTests.cs
- [X] T039 [P] Add integration test LiteDbServiceTests.ExecuteAsync_ReturnsQueryResult in LiteDB.Studio.Wpf.Tests/Integration/LiteDbServiceTests.cs (insert and select, verify RowCount and ExecutionTime)
- [X] T174 Update IDatabaseService.ConnectAsync signature in LiteDB.Studio.Wpf/Services/IDatabaseService.cs: change to `ConnectAsync(string connectionString, bool readOnly, string? password, CancellationToken ct)`; add `bool IsReadOnly { get; }` property; update ExecuteAsync Metadata documentation to note `IsDdl` boolean key set for DDL queries (CREATE/DROP collection or index)
- [X] T175 Update LiteDbService.ConnectAsync in LiteDB.Studio.Wpf/Services/LiteDbService.cs to accept `readOnly` and `password` params; set `ConnectionString.ReadOnly = readOnly`; set `ConnectionString.Password = password` when non-null; set internal `_isReadOnly` field; implement `IsReadOnly` property; ensure password is never written to any log output, settings, or field beyond this method's scope

---

## Phase 3: User Story 1 — Core Execution (MVP)

**Story**: As a user, I can run SQL against a connected database and see results in a grid.

**Story Goal**: Users can connect to a LiteDB file, execute SQL queries, and view results in a virtualized grid with execution time and row limit indicators.

**Independent Test Criteria**:
- TabViewModel.RunCommand executes query and populates LastResult or LastError
- ResultGrid displays rows with virtualization
- UI shows "Limit exceeded" indicator when 1000-row limit hit
- Long-running queries are cancellable via CancellationToken

### Tasks

- [X] T040 [US1] Create TabViewModel class in LiteDB.Studio.Wpf/ViewModels/TabViewModel.cs inheriting from ObservableObject
- [X] T041 [US1] Add properties to TabViewModel: Title, Filename, IsModified, EditorText, CaretOffset, SelectionStart, SelectionLength, LastResult, LastError, IsResultLoaded
- [X] T042 [US1] Implement TabViewModel.RunCommand in LiteDB.Studio.Wpf/ViewModels/TabViewModel.cs (IAsyncRelayCommand): execute selection if present else entire buffer, call IDatabaseService.ExecuteAsync, set LastResult or LastError
- [X] T043 [US1] Add TabViewModel.CloseCommand in LiteDB.Studio.Wpf/ViewModels/TabViewModel.cs (IRelayCommand): prompt save if IsModified
- [X] T044 [US1] Create MainViewModel class in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs inheriting from ObservableObject
- [X] T045 [US1] Add properties to MainViewModel: Tabs (ObservableCollection<TabViewModel>), SelectedTab, IsConnected, CurrentDatabase, TransactionActive
- [X] T046 [US1] Implement MainViewModel.RunCommand in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (delegate to SelectedTab.RunCommand)
- [X] T047 [US1] Implement MainViewModel.ConnectCommand in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (show file picker, call IDatabaseService.ConnectAsync, set CurrentDatabase, create initial tab only if `Tabs` collection is empty — do NOT create a tab if tabs already exist from a prior session; see T177 for the full single-connection flow update)
- [X] T048 [US1] Implement MainViewModel.DisconnectCommand in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (confirm if unsaved tabs, call IDatabaseService.DisconnectAsync, clear CurrentDatabase)
- [X] T049 [US1] Implement MainViewModel.NewTabCommand in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (create new TabViewModel, add to Tabs, set as SelectedTab)
- [X] T050 [US1] Create ResultGrid control in LiteDB.Studio.Wpf/Controls/ResultGrid.xaml and ResultGrid.xaml.cs
- [X] T051 [US1] Implement ResultGrid with DataGrid in LiteDB.Studio.Wpf/Controls/ResultGrid.xaml (VirtualizingStackPanel.IsVirtualizing="True", bind ItemsSource to QueryResult.Rows)
- [X] T052 [US1] Add dynamic column generation in ResultGrid.xaml.cs based on QueryResult.Columns metadata
- [X] T053 [US1] Create BsonValueToStringConverter in LiteDB.Studio.Wpf/Converters/BsonValueToStringConverter.cs implementing IValueConverter (handle ObjectId, DateTime, Binary, nested Documents/Arrays)
- [X] T054 [US1] Use BsonValueToStringConverter in ResultGrid column templates in LiteDB.Studio.Wpf/Controls/ResultGrid.xaml
- [X] T055 [US1] Add LimitExceeded indicator to ResultGrid UI in LiteDB.Studio.Wpf/Controls/ResultGrid.xaml (show warning banner when QueryResult.LimitExceeded = true)
- [X] T056 [US1] Update MainWindow.xaml in LiteDB.Studio.Wpf/Views/MainWindow.xaml with TabControl bound to MainViewModel.Tabs
- [X] T057 [US1] Add AvalonEdit `TextEditor` to tab content template in MainWindow.xaml bound to `TabViewModel.EditorText` via attached properties/behaviors (preferred MVVM pattern)
- [X] T058 [US1] Add ResultGrid to tab content template in MainWindow.xaml bound to TabViewModel.LastResult
- [X] T059 [US1] Wire MainViewModel to MainWindow.xaml.cs in LiteDB.Studio.Wpf/Views/MainWindow.xaml.cs (set DataContext, inject IDatabaseService)
- [X] T060 [US1] Add unit test TabViewModelTests.RunCommand_SetsLastResult_WhenQuerySucceeds in LiteDB.Studio.Wpf.Tests/ViewModels/TabViewModelTests.cs (mock IDatabaseService, verify LastResult populated)
- [X] T061 [P] [US1] Add unit test TabViewModelTests.RunCommand_SetsLastError_WhenQueryFails in LiteDB.Studio.Wpf.Tests/ViewModels/TabViewModelTests.cs (mock exception, verify LastError set)
- [X] T062 [P] [US1] Add unit test TabViewModelTests.RunCommand_ExecutesSelection_WhenSelectionExists in LiteDB.Studio.Wpf.Tests/ViewModels/TabViewModelTests.cs (set SelectionLength > 0, verify extracted query)
- [X] T063 [P] [US1] Add unit test MainViewModelTests.ConnectCommand_SetsIsConnected in LiteDB.Studio.Wpf.Tests/ViewModels/MainViewModelTests.cs (mock ConnectAsync, verify IsConnected = true)
- [X] T064 [P] [US1] Add unit test MainViewModelTests.RunCommand_DelegatesToSelectedTab in LiteDB.Studio.Wpf.Tests/ViewModels/MainViewModelTests.cs (verify SelectedTab.RunCommand invoked)
- [X] T176 [US1] Add `IsReadOnly` and `LastConnectedPath` properties to MainViewModel in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs: mirror `IDatabaseService.IsReadOnly`; load `LastConnectedPath` from `AppSettings` on construction; save updated path to `AppSettings` on each successful `ConnectAsync`; subscribe to `ConnectionStateChanged` to update `IsReadOnly`
- [X] T177 [US1] Update `ConnectCommand` in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs to implement single-connection flow: if `IsConnected`, invoke `DisconnectCommand` (implemented in T178) rather than duplicating the disconnect sequence inline — do NOT create a new initial tab if `Tabs` is non-empty (preserve existing tabs); update connection dialog to include a read-only checkbox and masked optional password field (blank = no encryption); call `IDatabaseService.ConnectAsync(path, readOnly, password, ct)`; password not stored or logged — ⚠️ **Depends on T178** (disconnect flow) and **T146** (DI bootstrap)
- [X] T178 [US1] Update `DisconnectCommand` in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs to implement full disconnect flow: (1) if `TransactionActive`, show "Active transaction will be rolled back. Proceed?" — on confirm call `RollbackTransactionAsync`, on cancel abort; (2) prompt save for each `IsModified` tab; (3) call `DisconnectAsync`; after disconnect, tabs remain in `Tabs` collection with content preserved but `RunCommand`/edit commit/tree actions disabled until reconnected

---

## Phase 4: User Story 2 — Result Display & Editing

**Story**: As a user, I can inspect results (Grid/Text/Parameters), edit cell values, and persist edits back to the DB.

**Story Goal**: Users can switch between Grid/Text/Parameters result views, edit cells in the grid, and persist changes to the database.

**Independent Test Criteria**:
- Grid cell editing invokes UpdateDocumentFieldAsync
- BsonValueToStringConverter renders all BSON types correctly
- Text view displays JSON representation of QueryResult
- Cell edits refresh UI or mark tab dirty

### Tasks

- [X] T065 [US2] Handle DataGrid.CellEditEnding event in ResultGrid.xaml.cs to invoke IDatabaseService.UpdateDocumentFieldAsync — ⚠️ **Superseded by T149** which moves this service call to `TabViewModel.CommitCellEditCommand`; this code-behind implementation will be removed/replaced when T149 runs
- [X] T066 [US2] Add error handling for UpdateDocumentFieldAsync failures in ResultGrid.xaml.cs (show error message, revert cell value) — ⚠️ **Superseded by T149** which moves error surfacing to `TabViewModel.LastError`; this code-behind implementation will be removed/replaced when T149 runs
- [ ] T149 [US2] Rework ResultGrid cell-edit workflow to be constitution-compliant (Principle III): move `UpdateDocumentFieldAsync` invocation out of `ResultGrid.xaml.cs` code-behind into a `TabViewModel.CommitCellEditCommand` (IAsyncRelayCommand); `ResultGrid.xaml.cs` handles only the `CellEditEnding` event wiring — it invokes the ViewModel command and passes the changed value, but does NOT call the service directly; failures revert the cell value and surface errors via `TabViewModel.LastError`; add tests in LiteDB.Studio.Wpf.Tests/Controls/ResultGridTests.cs (verify event triggers command) and LiteDB.Studio.Wpf.Tests/ViewModels/TabViewModelTests.cs (verify command calls service, handles failure, sets LastError) — this resolves the code-behind service-call constitution concern from T065/T066
- [X] T067 [US2] Create ResultTextView control in LiteDB.Studio.Wpf/Controls/ResultTextView.xaml and ResultTextView.xaml.cs (display QueryResult as JSON)
- [X] T068 [P] [US2] Create ParametersView control in LiteDB.Studio.Wpf/Controls/ParametersView.xaml and ParametersView.xaml.cs (display query parameters if supported)
- [X] T069 [US2] Add tab selector (Grid/Text/Parameters) to result view in MainWindow.xaml tab content template
- [X] T070 [US2] Update BsonValueToStringConverter in LiteDB.Studio.Wpf/Converters/BsonValueToStringConverter.cs to handle edge cases: truncate large strings with ellipsis, show tooltip for full value
- [X] T071 [US2] Add integration test LiteDbServiceTests.UpdateDocumentFieldAsync_UpdatesField in LiteDB.Studio.Wpf.Tests/Integration/LiteDbServiceTests.cs (insert doc, update field, verify change)
- [ ] T072 [P] [US2] Add unit test for cell edit workflow: mock `IDatabaseService.UpdateDocumentFieldAsync`; invoke `TabViewModel.CommitCellEditCommand` with a document ID and changed field value; verify service called with correct parameters; verify failure path reverts cell value and sets `TabViewModel.LastError` (LiteDB.Studio.Wpf.Tests/ViewModels/TabViewModelTests.cs) — **re-opened**: original [X] tested the old code-behind-calls-service pattern; T149 redesigns this to `CommitCellEditCommand`; test must be updated to match new architecture
- [ ] T179 [US2] Add `IsReadOnly` guard to cell-edit workflow using a ViewModel-first push-model (constitution Principle III): add a `bool CanEdit` property to `TabViewModel` in LiteDB.Studio.Wpf/ViewModels/TabViewModel.cs; `MainViewModel` sets `tab.CanEdit = !_dbService.IsReadOnly` for all tabs whenever `ConnectionStateChanged` fires — no back-reference from `TabViewModel` to `MainViewModel` (avoids circular ownership and simplifies ViewModel unit tests); bind `DataGrid.IsReadOnly` in ResultGrid.xaml to `TabViewModel.CanEdit` (inverted) declaratively; add a status-bar binding for "database opened read-only" state; unit test: set `tabViewModel.CanEdit = false` directly and verify grid becomes read-only without requiring a `MainViewModel` instance

---

## Phase 5: User Story 3 — Database Explorer

**Story**: As a user, I can browse collections, indexes, system collections, and insert snippets by double-click.

**Story Goal**: Users can explore database schema via tree view with lazy loading, invoke context actions (Drop, Export), and insert snippets into the active editor.

**Independent Test Criteria**:
- Tree lazy-loads collections on expand
- Context menu shows Drop/Export/InsertSnippet actions
- Double-click inserts snippet at caret
- Drop command shows confirmation dialog and executes DROP statement

### Tasks

- [X] T073 [US3] Create DatabaseTreeViewModel class in LiteDB.Studio.Wpf/ViewModels/DatabaseTreeViewModel.cs with RootNodes property (ObservableCollection<DbTreeNode>)
- [X] T074 [US3] Implement DatabaseTreeViewModel.LoadRootNodesAsync in LiteDB.Studio.Wpf/ViewModels/DatabaseTreeViewModel.cs (call GetCollectionNamesAsync and GetSystemCollectionNamesAsync, create DbTreeNode instances)
- [X] T075 [US3] Create DbTreeNode class in LiteDB.Studio.Wpf/ViewModels/DbTreeNode.cs with properties: Header, Tag, IconUri, Children, IsLoaded, IsExpanded
- [X] T076 [US3] Implement DbTreeNode.LoadChildrenCommand in LiteDB.Studio.Wpf/ViewModels/DbTreeNode.cs (IAsyncRelayCommand): call GetCollectionSchemaAsync, create child nodes for fields, set IsLoaded = true
- [X] T077 [US3] Add DbTreeNode.DropCommand in LiteDB.Studio.Wpf/ViewModels/DbTreeNode.cs (IAsyncRelayCommand): show confirmation dialog with typed confirmation for user collections, execute DROP statement via ExecuteAsync
- [X] T078 [P] [US3] Add DbTreeNode.ExportCommand in LiteDB.Studio.Wpf/ViewModels/DbTreeNode.cs (IAsyncRelayCommand): show SaveFileDialog, export collection to JSON; surface per-document export failures (report which documents failed and why), and allow retry for the failed subset per spec.md Edge Cases EC-8
- [X] T079 [P] [US3] Add DbTreeNode.InsertSnippetCommand in LiteDB.Studio.Wpf/ViewModels/DbTreeNode.cs (IRelayCommand): generate snippet text (e.g., "db.collectionName.find()"), notify MainViewModel to insert at caret
- [X] T080 [US3] Add Tree property to MainViewModel in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (DatabaseTreeViewModel instance)
- [X] T081 [US3] Implement MainViewModel.RefreshTreeCommand in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (clear Tree.RootNodes, call LoadRootNodesAsync)
- [X] T082 [US3] Implement MainViewModel.InsertSnippetCommand in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (insert snippet text at SelectedTab.CaretOffset, set IsModified = true)
- [X] T083 [US3] Create DatabaseTreeView control in LiteDB.Studio.Wpf/Views/DatabaseTreeView.xaml and DatabaseTreeView.xaml.cs (TreeView bound to DatabaseTreeViewModel.RootNodes)
- [X] T084 [US3] Add context menu to TreeViewItem in DatabaseTreeView.xaml with Drop/Export/InsertSnippet commands
- [X] T085 [US3] Handle TreeViewItem double-click in DatabaseTreeView.xaml.cs to invoke InsertSnippetCommand
- [X] T086 [US3] Add DatabaseTreeView to MainWindow.xaml (left panel or sidebar)
- [X] T087 [US3] Add icons for collections, fields, system collections to LiteDB.Studio.Wpf/Resources/Icons/ (e.g., database.png, collection.png, field.png) with Build Action = Resource
- [X] T088 [US3] Set DbTreeNode.IconUri to pack URIs referencing icons in LiteDB.Studio.Wpf/ViewModels/DbTreeNode.cs
- [X] T089 [US3] Add unit test DatabaseTreeViewModelTests.LoadRootNodesAsync_PopulatesRootNodes in LiteDB.Studio.Wpf.Tests/ViewModels/DatabaseTreeViewModelTests.cs (mock GetCollectionNamesAsync, verify nodes created)
- [X] T090 [P] [US3] Add unit test DbTreeNodeTests.LoadChildrenCommand_LoadsSchema in LiteDB.Studio.Wpf.Tests/ViewModels/DbTreeNodeTests.cs (mock GetCollectionSchemaAsync, verify children populated)
- [X] T091 [P] [US3] Add integration test for Drop workflow (create collection, invoke Drop, verify collection removed)
- [ ] T180 [US3] Add `IsSystemCollection` property to DbTreeNode in LiteDB.Studio.Wpf/ViewModels/DbTreeNode.cs (bool, immutable after construction); update `DatabaseTreeViewModel.LoadRootNodesAsync` to set `IsSystemCollection = true` for system collections; update `DatabaseTreeView.xaml` context menu: when `IsSystemCollection = true` show Open + InsertSnippet only (Drop and Export items hidden via `Visibility` binding); when `false` show all four items
- [ ] T181 [US3] Add DDL auto-refresh in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs: after each `RunCommand` completes, inspect `SelectedTab.LastResult?.Metadata["IsDdl"]`; if `true`, call `Tree.LoadRootNodesAsync` automatically without requiring manual `RefreshTreeCommand` invocation

---

## Phase 6: User Story 4 — Editor Enhancements

**Story**: As a user, I get code completion (Ctrl+Space), run selection (F5), and caret-aware run behavior.

**Story Goal**: Users have IntelliSense-style completion for collection names and SQL keywords, can run selected text with F5, and commands respect caret position.

**Independent Test Criteria**:
- Ctrl+Space shows completion window with collection names and keywords
- F5 executes selected text if selection exists, else entire buffer
- Completion provider queries IDatabaseService for collection names

### Tasks

- [X] T092 [US4] Create SqlCompletionProvider class in LiteDB.Studio.Wpf/Services/SqlCompletionProvider.cs implementing AvalonEdit ICompletionData
- [X] T093 [US4] Implement SqlCompletionProvider to return collection names from IDatabaseService.GetCollectionNamesAsync in LiteDB.Studio.Wpf/Services/SqlCompletionProvider.cs
- [X] T094 [US4] Add SQL keywords (SELECT, INSERT, UPDATE, DELETE, etc.) to SqlCompletionProvider in LiteDB.Studio.Wpf/Services/SqlCompletionProvider.cs (functions added from LiteDB docs)
- [X] T095 [US4] Wire Ctrl+Space to show AvalonEdit completion window in MainWindow.xaml.cs (hook TextArea.TextEntering event)
- [X] T096 [US4] Bind F5 to MainViewModel.RunCommand in MainWindow.xaml (InputBindings or KeyGesture)
- [X] T097 [US4] Update TabViewModel.RunCommand in LiteDB.Studio.Wpf/ViewModels/TabViewModel.cs to check SelectionLength and execute selected text vs entire buffer (already implemented in T042, verify logic)
- [X] T098 [US4] Add unit test for completion provider (mock GetCollectionNamesAsync, verify completion list contains collection names and keywords)

---

## Phase 7: User Story 5 — File Operations & UX

**Story**: As a user, I can open/save SQL files, track modified state, and are prompted to save before closing.

**Story Goal**: Users can load SQL files into tabs, save edits, see modified indicator, and are prompted before closing unsaved tabs.

**Independent Test Criteria**:
- OpenFileCommand loads file into tab, sets Filename and IsModified = false
- SaveFileCommand writes EditorText to disk, clears IsModified
- CloseTabCommand prompts save if IsModified
- EditorText changes set IsModified = true

### Tasks

- [X] T099 [US5] Implement MainViewModel.OpenFileCommand in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (IRelayCommand): show OpenFileDialog filtered to .sql, load file into new or selected tab, set Filename and IsModified = false
- [X] T100 [US5] Implement MainViewModel.SaveFileCommand in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (IRelayCommand): if Filename null show SaveFileDialog, write EditorText to file, set IsModified = false, handle IO errors
- [X] T101 [P] [US5] Implement MainViewModel.SaveAllCommand in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (IRelayCommand): iterate Tabs, save all where IsModified = true
- [X] T102 [US5] Update TabViewModel to set IsModified = true on EditorText PropertyChanged in LiteDB.Studio.Wpf/ViewModels/TabViewModel.cs (add PropertyChanged handler)
- [X] T103 [US5] Update TabViewModel.CloseCommand in LiteDB.Studio.Wpf/ViewModels/TabViewModel.cs to show confirmation dialog if IsModified ("Save changes to {Filename}?"), invoke SaveFileCommand if yes
- [X] T104 [US5] Implement MainViewModel.CloseTabCommand in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (IRelayCommand): invoke TabViewModel.CloseCommand, remove from Tabs collection
- [X] T105 [US5] Add File menu to MainWindow.xaml with Open, Save, Save All, Close Tab commands
- [X] T106 [US5] Add unit test MainViewModelTests.OpenFileCommand_LoadsFile in LiteDB.Studio.Wpf.Tests/ViewModels/MainViewModelTests.cs (mock file dialog, verify tab created with file content)
- [X] T107 [P] [US5] Add unit test MainViewModelTests.SaveFileCommand_WritesFile in LiteDB.Studio.Wpf.Tests/ViewModels/MainViewModelTests.cs (verify EditorText written, IsModified cleared)
- [X] T108 [P] [US5] Add unit test TabViewModelTests.CloseCommand_PromptsIfModified in LiteDB.Studio.Wpf.Tests/ViewModels/TabViewModelTests.cs (set IsModified, verify dialog shown)
- [ ] T183 [US5] Add `RowLimit` property (int?) to TabViewModel in LiteDB.Studio.Wpf/ViewModels/TabViewModel.cs; update `RunCommand` to use `RowLimit ?? _appSettings.RowLimit` (defaulting to global AppSettings value, typically 1000) as the row cap passed to `IDatabaseService.ExecuteAsync`
- [ ] T184 [US5] Add RowLimit numeric input to the tab toolbar in tab content template (MainWindow.xaml or tab DataTemplate): bind to `TabViewModel.RowLimit`; show placeholder text = global default from AppSettings; changes take effect on next query run; value is transient (not persisted across sessions)

---

## Phase 8: User Story 6 — Transactions & Debugger

**Story**: As a user, I can begin/commit/rollback transactions and use the Database Debugger tools.

**Story Goal**: Users can manage transaction lifecycle via commands and port WinForms debugger features (breakpoints, step actions) with behavioral parity.

**Independent Test Criteria**:
- BeginTransactionCommand sets TransactionActive = true
- CommitTransactionCommand commits and sets TransactionActive = false
- RollbackTransactionCommand reverts changes and sets TransactionActive = false
- Transaction commands enable/disable based on state
- Debugger features operate with parity to WinForms implementation

### Tasks

- [X] T109 [US6] Implement MainViewModel.BeginTransactionCommand in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (IAsyncRelayCommand): call IDatabaseService.BeginTransactionAsync
- [X] T110 [US6] Implement MainViewModel.CommitTransactionCommand in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (IAsyncRelayCommand): call IDatabaseService.CommitTransactionAsync
- [X] T111 [US6] Implement MainViewModel.RollbackTransactionCommand in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (IAsyncRelayCommand): call IDatabaseService.RollbackTransactionAsync
- [X] T112 [US6] Subscribe to IDatabaseService.TransactionStateChanged event in MainViewModel constructor in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs to update TransactionActive property
- [X] T113 [US6] Add CanExecute logic to transaction commands in MainViewModel: BeginTransactionCommand requires !TransactionActive, CommitTransactionCommand and RollbackTransactionCommand require TransactionActive
- [X] T114 [US6] Add Transaction menu to MainWindow.xaml with Begin, Commit, Rollback commands
- [X] T115 [US6] Review LiteDB.Studio/Classes/Debugger for breakpoint and step logic (WinForms implementation)
- [X] T116 [US6] Port debugger ViewModel in LiteDB.Studio.Wpf/ViewModels/DebuggerViewModel.cs with breakpoint management and step commands; acceptance criteria: breakpoint set/clear per-line, step-over/step-into/step-out commands, and current-line indicator all operate with parity to LiteDB.Studio/Classes/Debugger WinForms behavior (verified against T115 review output)
- [X] T117 [US6] Port debugger View in LiteDB.Studio.Wpf/Views/DebuggerView.xaml with breakpoint list and step controls
- [X] T118 [US6] Integrate DebuggerView into MainWindow.xaml (panel or tool window)
- [X] T119 [US6] Add integration test for transaction workflow: begin, insert, commit, verify data persisted
- [X] T120 [P] [US6] Add integration test for rollback workflow: begin, insert, rollback, verify data not persisted
- [X] T121 [P] [US6] Add unit test for transaction command state management (verify CanExecute logic)
- [ ] T182 [US6] Update all three transaction command `CanExecute` guards in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs per spec.md contract: `BeginTransactionCommand` requires `!TransactionActive AND !IsReadOnly`; `CommitTransactionCommand` requires `TransactionActive AND !IsReadOnly`; `RollbackTransactionCommand` requires `TransactionActive AND !IsReadOnly`; update `CanExecute` notifications to fire when both `TransactionActive` and `IsReadOnly` change — NOTE: spec.md:L83 and L110 explicitly require `!IsReadOnly` on all three primitives; if a safety exemption for Rollback is desired in the future, file a spec clarification before diverging

---

## Phase 9: User Story 7 — Testing & Polish

**Story**: Before merge, tests exist and CI gates run green.

**Story Goal**: Comprehensive unit and integration tests cover all ViewModels and LiteDbService; performance targets met; CI configured to gate on test failures.

**Independent Test Criteria**:
- Unit tests achieve >80% coverage for ViewModel logic
- Integration tests cover connect/execute/update/transactions
- Performance: 95% of queries <1000 rows render in <1s
- No memory leaks in open/close cycles
- CI runs dotnet test and fails build on failures

### Tasks

- [ ] T122 [US7] Add unit test coverage for MainViewModel commands not yet tested: NewTabCommand, CloseTabCommand, RefreshTreeCommand, InsertSnippetCommand in LiteDB.Studio.Wpf.Tests/ViewModels/MainViewModelTests.cs — **MUST** include: after `InsertSnippetCommand` executes, assert `SelectedTab.IsModified == true` (spec.md:L166 explicit MUST requirement) — **re-opened**: this assertion was added after the original `[X]` check; verify it exists in the implemented test before re-checking
- [X] T123 [P] [US7] Add unit test coverage for TabViewModel edge cases: empty EditorText, null LastResult, cancellation in LiteDB.Studio.Wpf.Tests/ViewModels/TabViewModelTests.cs
- [X] T124 [P] [US7] Add unit test coverage for DatabaseTreeViewModel: Clear, LoadRootNodesAsync with empty collections in LiteDB.Studio.Wpf.Tests/ViewModels/DatabaseTreeViewModelTests.cs
- [X] T125 [P] [US7] Add integration test for schema discovery: GetCollectionSchemaAsync with nested documents, arrays in LiteDB.Studio.Wpf.Tests/Integration/LiteDbServiceTests.cs
- [X] T126 [P] [US7] Add integration test for edge cases: locked file, malformed connection string, schema mismatch in LiteDB.Studio.Wpf.Tests/Integration/LiteDbServiceTests.cs
- [X] T127 [US7] Add performance test: load 10k rows, measure grid render time, verify <3s in LiteDB.Studio.Wpf.Tests/Performance/GridPerformanceTests.cs
- [X] T128 [P] [US7] Add memory leak test: open/close 100 tabs in loop, verify memory returns to baseline in LiteDB.Studio.Wpf.Tests/Performance/MemoryLeakTests.cs
- [ ] T129 [US7] **[MERGE-GATE]** Configure CI pipeline to run dotnet test for LiteDB.Studio.Wpf.Tests project — required by spec.md Testing & CI; no PR may merge without this
- [ ] T130 [US7] **[MERGE-GATE]** Update CI pipeline to fail build on test failures — required by spec.md Testing & CI; no PR may merge without this
- [ ] T131 [US7] **[MERGE-GATE]** Create PR template in .github/PULL_REQUEST_TEMPLATE.md with sections: Problem, Approach, Risks, Tests, Rollout/Rollback — required by constitution §Additional Constraints; no PR may merge without this
- [ ] T132 [US7] Run all tests locally, verify green, fix any failures before opening PR
- [ ] T185 [P] [US7] Add unit test: `IsReadOnly` enforcement — mock `IDatabaseService.IsReadOnly = true`; fire `ConnectionStateChanged`; verify `BeginTransactionCommand.CanExecute = false`; verify `RunCommand.CanExecute = true`; verify all `TabViewModel` instances in `Tabs` have `CanEdit = false` (T179 push-model: `MainViewModel` sets `tab.CanEdit` on `ConnectionStateChanged`); verify write `ExecuteAsync` returns `LastError` containing "read-only" (LiteDB.Studio.Wpf.Tests/ViewModels/MainViewModelTests.cs)
- [ ] T186 [P] [US7] Add unit test: DDL auto-refresh — mock `ExecuteAsync` returning `Metadata["IsDdl"] = true`; invoke `RunCommand`; verify `DatabaseTreeViewModel.LoadRootNodesAsync` called automatically without user action (LiteDB.Studio.Wpf.Tests/ViewModels/MainViewModelTests.cs)
- [ ] T187 [P] [US7] Add unit test: `RowLimit` override — set `TabViewModel.RowLimit = 50`; mock `ExecuteAsync`; verify limit 50 passed to service; clear `RowLimit` (null); verify `AppSettings.RowLimit` (1000) used instead (LiteDB.Studio.Wpf.Tests/ViewModels/TabViewModelTests.cs)
- [ ] T188 [P] [US7] Add unit test: disconnect flow — mock `TransactionActive = true`; invoke `DisconnectCommand`; verify warn dialog shown, `RollbackTransactionAsync` called, then `DisconnectAsync` called; verify all tabs remain in `Tabs` collection after disconnect (LiteDB.Studio.Wpf.Tests/ViewModels/MainViewModelTests.cs)
- [ ] T196 [P] [US7] Add unit test: save-dialog cancel path during disconnect — set one tab `IsModified = true` with no active transaction; invoke `DisconnectCommand`; when the save-changes dialog fires, simulate user clicking **Cancel**; verify `DisconnectAsync` is NOT called, connection remains active, and tab remains in `Tabs` with `IsModified = true` (LiteDB.Studio.Wpf.Tests/ViewModels/MainViewModelTests.cs)
- [ ] T189 [P] [US7] Add unit test: single-connection flow — mock `IsConnected = true`; invoke `ConnectCommand`; verify full disconnect sequence (rollback if needed, save prompts, `DisconnectAsync`) completes before `ConnectAsync` is called (LiteDB.Studio.Wpf.Tests/ViewModels/MainViewModelTests.cs)
- [ ] T190 [P] [US7] Add unit test: `IsSystemCollection` context menu restriction — create `DbTreeNode` with `IsSystemCollection = true`; verify `DropCommand`/`ExportCommand` have no visible bindings (or `CanExecute = false`); verify `InsertSnippetCommand` remains available; repeat with `IsSystemCollection = false` to verify all four actions present (LiteDB.Studio.Wpf.Tests/ViewModels/DbTreeNodeTests.cs)
- [ ] T191 [P] [US7] Add integration test: read-only connection — `ConnectAsync(path, readOnly: true, password: null, ct)`; execute SELECT, verify succeeds and returns rows; execute INSERT, verify `LastError` contains "read-only" message and no data was written (LiteDB.Studio.Wpf.Tests/Integration/LiteDbServiceTests.cs)
- [ ] T194 [P] [US7] Add performance test for SC-2 (95th-percentile <1s for queries returning <1000 rows): execute a parametric result set of 500 rows 100 times against an in-memory LiteDB, measure render-cycle time including grid column generation, assert p95 latency <= 1000ms (LiteDB.Studio.Wpf.Tests/Performance/GridPerformanceTests.cs) — closes coverage gap for spec.md SC-2
- [ ] T195 [P] [US7] Add logging overhead performance test: run 1000 representative log statements (Info, Warning, Error with stack trace) using the configured Serilog file sink and assert total elapsed time represents <=5% overhead relative to a baseline run without logging (LiteDB.Studio.Wpf.Tests/Performance/LoggingOverheadTests.cs); cite spec.md:L38 in test comments

---

## Phase 10: Polish & Cross-Cutting Concerns

**Goal**: Address edge cases, error handling, UX polish, and ensure all constitutional requirements met

**Story Goal**: N/A (cross-cutting improvements)  
**Independent Test Criteria**: All edge cases handled gracefully; error messages actionable; resource cleanup verified

### Tasks

- [ ] T133 [P] Add error handling for connection failures: locked files, insufficient permissions, malformed connection strings in LiteDB.Studio.Wpf/Services/LiteDbService.cs; error messages MUST name the specific cause and include a remediation suggestion (e.g., for locked files: suggest closing competing processes or opening read-only; for malformed paths: show the invalid path and expected format) — see spec.md Edge Cases section (Locked DB files, ~L216–L217) for locked-file 3-option recovery requirements
- [ ] T134 [P] Add query cancellation edge case handling: ensure cancellation releases DB resources, surface "Cancelled by user" message in TabViewModel as a distinct outcome from error
- [ ] T135 [P] Add schema mismatch handling: gracefully display placeholder for missing fields in result grid, avoid crashes
- [ ] T136 [P] Verify and harden destructive-action typed confirmation in DbTreeNode.DropCommand: T077 implemented the initial confirmation dialog; this task confirms the typed-phrase requirement (user must type the collection name exactly) is exercised by a unit test, and extends the typed-confirmation pattern to DELETE-many and other bulk destructive actions beyond DROP
- [ ] T137 [P] Add Shift-modifier behavior for destructive actions: detect Shift key, bypass/alter confirmation per spec in DbTreeNode.DropCommand
- [ ] T138 [P] Add concurrent update detection: when `UpdateDocumentFieldAsync` detects a version mismatch, expose the conflict via a dedicated `IDialogService.ShowConflictResolutionAsync` (Reload/Overwrite/Merge options) called from `TabViewModel` or `MainViewModel` — do NOT place dialog invocation in `ResultGrid.xaml.cs` code-behind (constitution Principle III); `ResultGrid` only raises a routed event or invokes a ViewModel command; add unit test mocking `UpdateDocumentFieldAsync` to return version-mismatch error and verify dialog service is called with all three options (LiteDB.Studio.Wpf.Tests/Controls/ResultGridTests.cs)
- [ ] T160 Add tooltip to ResultGrid cells for truncated BSON values (show full value on hover) — NOTE: T070 claimed to implement truncation + tooltip; verify T070's implementation actually added tooltip binding in ResultGrid.xaml templates. If confirmed, close T160 as already covered; if tooltip was deferred, implement the ResultGrid.xaml DataTemplate `ToolTip` binding here and mark T070's tooltip claim as partially delivered
- [ ] T161 [P] Add status bar to MainWindow.xaml showing: connection state, execution time, row count, transaction state, and a startup reconnect link bound to `MainViewModel.LastConnectedPath` (displays "Reconnect to [filename]" when path is set and app is disconnected; invoking it calls `ConnectCommand`; disappears after successful connect or explicit dismiss)
- [ ] T162 Verify all icons use pack URIs and Build Action = Resource in LiteDB.Studio.Wpf.csproj
- [ ] T163 Verify all resources under LiteDB.Studio.Wpf/Resources per constitution requirement
- [ ] T164 Run code review checklist: apply_patch used for all edits, tests included, MVVM-first adhered to, minimal diffs
- [ ] T165 [P] Add unit test for connection failure handling (T133): mock LiteDbService to throw for locked file, permission denied, and malformed connection string; verify each produces a descriptive error message containing cause and remediation hint (LiteDB.Studio.Wpf.Tests/Integration/LiteDbServiceTests.cs)
- [ ] T166 [P] Add unit test for query cancellation edge case (T134): verify CancellationToken is observed, DB resources are released, and TabViewModel surfaces "Cancelled by user" as a distinct outcome from error (LiteDB.Studio.Wpf.Tests/ViewModels/TabViewModelTests.cs)
- [ ] T167 [P] Add unit test for schema mismatch handling (T135): execute query returning documents with unexpected/missing fields; verify ResultGrid shows placeholder values without crashing (LiteDB.Studio.Wpf.Tests/Integration/LiteDbServiceTests.cs)
- [ ] T168 [P] Add unit test for typed DROP confirmation (T136): invoke DropCommand with correct and incorrect typed phrases; verify operation proceeds only when phrase matches collection name exactly (LiteDB.Studio.Wpf.Tests/ViewModels/DbTreeNodeTests.cs)
- [ ] T169 [P] Add unit test for Shift-modifier behavior (T137): simulate Shift key modifier on DropCommand invocation; verify confirmation bypass/alteration per spec.md:L197–L199 (LiteDB.Studio.Wpf.Tests/ViewModels/DbTreeNodeTests.cs)
- [ ] T170 [P] Add unit test for concurrent update conflict dialog (T138): mock `UpdateDocumentFieldAsync` to return a version-mismatch error; mock `IDialogService`; invoke `TabViewModel.CommitCellEditCommand`; verify `IDialogService.ShowConflictResolutionAsync` is called with Reload/Overwrite/Merge options (LiteDB.Studio.Wpf.Tests/ViewModels/TabViewModelTests.cs) — test relocated from `ResultGridTests.cs` to match T138's redesigned `IDialogService`-through-ViewModel architecture
- [ ] T192 [P] Update locked-file connection handling in LiteDB.Studio.Wpf/Services/LiteDbService.cs to present a 3-option recovery dialog when a file lock is detected: (1) "Retry" — wait and retry with backoff after user closes competing process; (2) "Open Read-Only" — call `ConnectAsync(readOnly: true)`; (3) "Cancel" — abort; surface as a UI dialog via a service/dialog abstraction (not a raw exception) so ViewModels can test recovery paths
- [ ] T193 [P] Verify `retainedFileCountLimit: 30` in LiteDB.Studio.Wpf/Util/Logging.cs Serilog configuration; add a comment citing spec.md requirement; add a unit or snapshot test in LiteDB.Studio.Wpf.Tests that reads the configured Serilog `LoggerConfiguration` and asserts `retainedFileCountLimit == 30` per spec.md:L31

---

## Task Summary

| Phase | Task Count | Parallelizable | Story |
|-------|-----------|----------------|-------|
| Phase 1: Setup (including Logging) | 20 | 10 | N/A |
| Phase 2: Foundational | 21 (+T174–T175) | 6 | N/A |
| Phase 3: User Story 1 (MVP) | 28 (+T176–T178) | 4 | Core Execution |
| Phase 4: User Story 2 | 10 (+T179) | 4 | Result Display & Editing |
| Phase 5: User Story 3 | 21 (+T180–T181) | 3 | Database Explorer |
| Phase 6: User Story 4 | 7 | 0 | Editor Enhancements |
| Phase 7: User Story 5 | 12 (+T183–T184) | 3 | File Operations & UX |
| Phase 8: User Story 6 | 14 (+T182) | 3 | Transactions & Debugger |
| Phase 9: User Story 7 | 21 (+T185–T191, T194–T196) | 17 | Testing & Polish |
| Phase 10: Polish & Cross-Cutting Concerns | 19 (+T192–T193) | 15 | N/A |
| Editor Adjustments (T139–T143, T150–T155b, T156–T159) | 16 | 1 | N/A (T155 split; T155b is [P]) |
| Cross-cutting (T144–T148, T171–T173) | 8 | 8 | N/A |
| **Total** | **196** | **73** | **7 stories** |

---

## Editor Porting Adjustments (from spec update)

These tasks were added to reflect the spec and plan updates that mandate porting `ICSharpCode.TextEditor` usages to AvalonEdit and preferring attached properties/behaviors for MVVM bindings.

- [X] T139 ~~Implement AvalonEdit attached properties/behaviors in LiteDB.Studio.Wpf/Controls/AvalonEditBehaviors.cs~~ **Superseded by T150** which is the authoritative implementation; T139 was an early pass and is complete only in the sense that T150 covers its intent.
- [X] T140 ~~Add unit tests for AvalonEdit attached properties in AvalonEditBehaviorsTests.cs~~ **Superseded by T151** which is the authoritative test suite.
- [X] T141 Update MainWindow.xaml tab template and any EditorTab.xaml to use attached properties/behaviors rather than direct code-behind wiring (confirm binding paths and command hooks).
- [X] T142 Update `specs/002-wpf-port/checklists/requirements.md` to include PR verification step: repository search for `ICSharpCode.TextEditor` within `LiteDB.Studio.Wpf` must return zero results before merge.
- [X] T143 Add PR checklist item and a CI verification script (or pipeline step) that fails the PR if `ICSharpCode.TextEditor` references remain in `LiteDB.Studio.Wpf` sources (implemented `.github/scripts/verify-no-icsharpcodetexteditor.ps1`).
- [X] T150 Implement `AvalonEditBehaviors` core in `LiteDB.Studio.Wpf/Controls/AvalonEditBehaviors.cs` (small, testable implementation). Expose these attached properties (two-way where applicable): `EditorText`, `CaretOffset`, `SelectionStart`, `SelectionLength`, `IsModified`, and `ShowCompletionCommand` (ICommand). Preserve caret/selection on programmatic updates, avoid event loops, and reference MVVM samples (https://github.com/Dirkster99/AvalonEdit-Samples) for patterns.
- [X] T151 Add unit tests `LiteDB.Studio.Wpf.Tests/Controls/AvalonEditBehaviorsTests.cs` to verify two-way `EditorText` binding, `CaretOffset`/selection propagation, `IsModified` toggling, and that `ShowCompletionCommand` is executed when appropriate (e.g., Ctrl+Space). Ensure tests run on STA.  

*Status: Implemented — tests added verifying EditorText two-way, CaretOffset and selection propagation, IsModified toggling, and keybinding/command execution.*
- [X] T152 Wire ViewModel properties/commands in `TabViewModel`: add `EditorText`, `CaretOffset`, `SelectionStart`, `SelectionLength`, `IsModified`, and `ShowCompletionCommand` and update `RunCommand` to prefer selection execution. Add unit tests for selection-run behavior.  

*Status: Implemented — properties and commands added; `RunCommand` prefers selection and unit tests added for selection-run and completion population.*
- [X] T153 Update `MainWindow.xaml` and tab templates to bind AvalonEdit via `AvalonEditBehaviors` attached properties (remove direct code-behind wiring and adapters where behavior covers binding). Validate in manual QA and add small integration test that the ViewModel->View bindings work end-to-end.  

*Status: Implemented — `MainWindow.xaml` binds `TextEditor` using `controls:AvalonEditBehaviors` and an integration test `EditorBindingsIntegrationTests` verifies two-way `EditorText` and `ShowCompletionCommand` execution.*
- [X] T154 Refactor `LiteDB.Studio.Wpf/Util/AvalonEditBehavior.cs` to remove duplicated responsibilities (focus it on lightweight compatibility shims or deprecate in favor of the new `AvalonEditBehaviors`). Consolidate completion triggers so `ShowCompletionCommand` is the single entry point and use `IEditorAdapter` for completion UI.  

*Status: Implemented — legacy helper marked obsolete and refactored into a compatibility shim delegating to `AvalonEditBehaviors`. Completion triggers now prefer the ViewModel-provided `ShowCompletionCommand`; a fallback command is installed only when `EnableCompletion` is used and no VM command exists.*
- [ ] T155a Update `specs/002-wpf-port/checklists/requirements.md` with manual QA acceptance criteria and QA steps for: Ctrl+Space completion trigger, F5 run-selection semantics, theme switching, font scaling, and large-document performance (document load / typing latency) — QA checklist only, no automated tests here
- [ ] T155b [P] Add integration tests for AvalonEdit editor behaviors: verify Ctrl+Space completion window opens and is populated via ViewModel/service, F5 run-selection executes correct subset, theme and font changes apply immediately to open editor instances (LiteDB.Studio.Wpf.Tests/ — choose appropriate test class per behavior)
- [ ] T156 Define completion caching policy & add tests (LiteDB.Studio.Wpf/Services/SqlCompletionProvider.cs and LiteDB.Studio.Wpf.Tests/Performance/CompletionCachingTests.cs): specify TTL, invalidation on schema change, cache size limits, and expected behavior under concurrent updates or stale schema views; also add assertion that automatic trigger debounce interval is ≤150ms per spec.md:L182.
- [ ] T157 Port and verify Find/Replace functionality to AvalonEdit: implement Find/Replace control and commands (`LiteDB.Studio.Wpf/Controls/FindReplaceControl.xaml`), integrate with `TabViewModel` (commands/properties), and add tests (`LiteDB.Studio.Wpf.Tests/Controls/FindReplaceTests.cs`) covering replace-all, case-sensitivity, whole-word, and regex modes.
- [ ] T158 Port and test Undo/Redo behavior: ensure editor undo/redo stacks match WinForms behavior, cover grouped edits, selection-based replacements, and programmatic changes that should/shouldn't be undoable; add tests `LiteDB.Studio.Wpf.Tests/Controls/UndoRedoTests.cs`.
- [ ] T159 Bind App-level theme and font settings to AvalonEdit behaviors and add integration tests (implement in `LiteDB.Studio.Wpf/Settings/AppSettings.cs` and tests in `LiteDB.Studio.Wpf.Tests/Integration/EditorThemeFontTests.cs`): ensure theme switching and font scaling apply immediately to open editors and persist across sessions.

## Cross-cutting Tasks: Localization, DI, Performance

- [ ] T144 [P] Localization: Extract UI strings to `LiteDB.Studio.Wpf/Resources/Strings.resx` and update Views/XAML to use resource bindings; include culture-neutral keys and comment usage.
- [ ] T145 [P] Localization Tests: Add unit/integration tests in `LiteDB.Studio.Wpf.Tests/Localization/` to verify resource lookup and a sample culture switch (e.g., `fr-FR`) shows translated strings for a sample view.
- [ ] T146 [P] DI/bootstrap: Add `AppBootstrapper.cs` (or `ServiceRegistration.cs`) in `LiteDB.Studio.Wpf/` that registers services with `IServiceCollection` (register `IDatabaseService`, `EditorCompletionService`, ViewModels) and document the chosen DI approach in `quickstart.md`.
- [ ] T147 [P] DI bootstrap test: Add `BootstrapperTests.cs` in `LiteDB.Studio.Wpf.Tests/` to assert that `ServiceProvider` resolves `IDatabaseService` and `MainViewModel` without exceptions (use a test service collection builder).
- [ ] T148 [P] Performance SLOs: Add performance-check tests for completion provider latency (e.g., `LiteDB.Studio.Wpf.Tests/Performance/CompletionLatencyTests.cs`) asserting median latency <200ms for local schema mocks (SLOs are already documented in spec.md:L178–L182; no spec update needed).
- [ ] T171 [P] Add performance test for editor typing latency (SC-5): simulate 100 rapid keystrokes in AvalonEdit TextEditor and assert p95 render latency ≤50ms per spec.md:L179 (LiteDB.Studio.Wpf.Tests/Performance/EditorTypingLatencyTests.cs)
- [ ] T172 [P] Surface log file path to users: add "View Logs" item to Help menu (or About dialog) that displays the log directory path (%APPDATA%\Temp\LiteDB.Studio\) and optionally opens it in Windows Explorer (LiteDB.Studio.Wpf/Views/MainWindow.xaml + LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs); satisfies spec.md:L36 user accessibility requirement
- [ ] T173 [P] Create AppSettings.cs in LiteDB.Studio.Wpf/Settings/AppSettings.cs: define persisted settings model (TabSize, FontFamily, FontSize, RowLimit, ThemeName, LastConnectedPath); implement load/save via JSON in %APPDATA%\LiteDB.Studio\settings.json; register with DI (T146 prerequisite); required by T159 (theme/font binding) and T176 (LastConnectedPath in MainViewModel) — treat as Phase 2 infrastructure per dependency item 4 in Critical Path


## Dependencies & Execution Order

### Critical Path (Sequential Dependencies)

1. **Phase 1 → Phase 2**: Setup must complete before foundational types
2. **Phase 2 → Phase 3**: IDatabaseService and LiteDbService must exist before ViewModels
3. **Phase 3 → Phase 4+**: MVP (User Story 1) must complete before subsequent stories (establishes core execution and grid rendering)
4. **T173 (AppSettings) → T176, T183, T184, T161, T159**: AppSettings must exist before any task that reads or writes `RowLimit`, `LastConnectedPath`, `FontFamily`/`FontSize`/`TabSize`/`ThemeName`; treat T173 as Phase 2 infrastructure even though it appears in Cross-cutting Tasks (finding C1)
5. **T178 (DisconnectCommand full flow) → T177 (ConnectCommand single-connection flow)**: T177 reuses T178's disconnect sequence; T178 must be complete before T177 is implemented (finding F7)
6. **T146 (DI bootstrap) → T177 (ConnectCommand update)**: T177 calls `IDatabaseService.ConnectAsync` via injected service; DI registration must exist (finding C2)
7. **T146 (DI bootstrap) → T173 (AppSettings registration)**: T173 registers `AppSettings` with the DI container; DI bootstrap must exist before AppSettings can be wired up (finding N5)
8. **T149 (CommitCellEditCommand) → T170 (conflict-dialog test)**: T170 invokes `TabViewModel.CommitCellEditCommand` to trigger the conflict path; T149 must be implemented before T170 can be written or run (finding R2)

### User Story Dependencies

- **User Story 1 (Core Execution)**: No dependencies (MVP)
- **User Story 2 (Result Display & Editing)**: Depends on User Story 1 (requires ResultGrid and TabViewModel.RunCommand)
- **User Story 3 (Database Explorer)**: Independent of User Story 2 (can run in parallel after User Story 1)
- **User Story 4 (Editor Enhancements)**: Depends on User Story 1 (requires TabViewModel and editor integration)
- **User Story 5 (File Operations)**: Independent of User Stories 2-4 (can run in parallel after User Story 1)
- **User Story 6 (Transactions)**: Independent of User Stories 2-5 (can run in parallel after Phase 2)
- **User Story 7 (Testing & Polish)**: Depends on all prior stories (integration point)

### Parallel Execution Examples

**After Phase 2 completes**, the following can run in parallel:
- User Story 1 (Core Execution) — MVP implementation
- User Story 6 (Transactions) — transaction commands (independent of UI rendering)

**After User Story 1 completes**, the following can run in parallel:
- User Story 2 (Result Display & Editing)
- User Story 3 (Database Explorer)
- User Story 4 (Editor Enhancements)
- User Story 5 (File Operations)

**Within each phase**, tasks marked `[P]` are parallelizable (e.g., T002-T009 in Phase 1, T024-T028 in Phase 2).

---

## Suggested MVP Scope

**MVP = User Story 1 (Core Execution)**

**Included Tasks**: T001-T058 (Phase 1 + Phase 2 + Phase 3)

**MVP Deliverables**:
- Users can connect to LiteDB file
- Users can execute SQL queries and see results in virtualized grid
- Results show execution time and row limit indicator
- Queries are cancellable
- Unit tests for TabViewModel and MainViewModel
- Integration tests for LiteDbService

**MVP Success Criteria**:
- `dotnet test` passes for all MVP tests
- Grid renders 1000+ rows without freezing
- Query execution <1s for typical queries

**Next Increment After MVP**: User Story 3 (Database Explorer) recommended (high user value, independent of Story 2).

---

## Implementation Strategy

1. **Start with Setup (Phase 1)**: Establish project structure and dependencies
2. **Build Foundation (Phase 2)**: Implement core types and services with integration tests
3. **Deliver MVP (Phase 3 / User Story 1)**: Core execution with grid rendering
4. **Iterate Stories in Priority Order**: Phase 4-8 (User Stories 2-6)
5. **Test & Polish (Phase 9-10)**: Comprehensive testing, performance validation, edge case handling
6. **Human Commits Only**: Agent prepares patches; human reviews, commits, and pushes per constitution

---

## Validation Checklist

- [ ] All tasks follow checklist format: `- [ ] [TaskID] [P?] [Story?] Description with file path`
- [ ] Each user story has independent test criteria
- [ ] Parallel tasks marked with `[P]`
- [ ] Story tasks labeled with `[US#]`
- [ ] Dependencies clearly documented
- [ ] MVP scope defined
- [ ] Task count: 193 tasks across 10 phases + editor adjustments + cross-cutting sections

**Ready for implementation**: ✅ Tasks are specific, file paths included, parallelization opportunities identified, and test tasks embedded per story.
