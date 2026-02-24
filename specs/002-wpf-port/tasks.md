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
- [X] T003 Create Logging.cs in LiteDB.Studio.Wpf/Util/Logging.cs with Serilog configuration (file sink to Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "LiteDB.Studio"), rolling file naming, structured logging)
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
- [X] T047 [US1] Implement MainViewModel.ConnectCommand in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (show file picker, call IDatabaseService.ConnectAsync, set CurrentDatabase, create initial tab)
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

- [X] T065 [US2] Handle DataGrid.CellEditEnding event in ResultGrid.xaml.cs to invoke IDatabaseService.UpdateDocumentFieldAsync
- [X] T066 [US2] Add error handling for UpdateDocumentFieldAsync failures in ResultGrid.xaml.cs (show error message, revert cell value)
- [ ] T149 [US2] Rework and fix ResultGrid cell-edit workflow in LiteDB.Studio.Wpf/Controls/ResultGrid.xaml.cs and add tests in LiteDB.Studio.Wpf.Tests/Controls/ResultGridTests.cs to ensure updates are reliably committed, failures revert values, and errors surface to the UI
- [X] T067 [US2] Create ResultTextView control in LiteDB.Studio.Wpf/Controls/ResultTextView.xaml and ResultTextView.xaml.cs (display QueryResult as JSON)
- [X] T068 [P] [US2] Create ParametersView control in LiteDB.Studio.Wpf/Controls/ParametersView.xaml and ParametersView.xaml.cs (display query parameters if supported)
- [X] T069 [US2] Add tab selector (Grid/Text/Parameters) to result view in MainWindow.xaml tab content template
- [X] T070 [US2] Update BsonValueToStringConverter in LiteDB.Studio.Wpf/Converters/BsonValueToStringConverter.cs to handle edge cases: truncate large strings with ellipsis, show tooltip for full value
- [X] T071 [US2] Add integration test LiteDbServiceTests.UpdateDocumentFieldAsync_UpdatesField in LiteDB.Studio.Wpf.Tests/Integration/LiteDbServiceTests.cs (insert doc, update field, verify change)
- [X] T072 [P] [US2] Add unit test for cell edit workflow (mock UpdateDocumentFieldAsync, verify invocation with correct parameters)

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
- [X] T078 [P] [US3] Add DbTreeNode.ExportCommand in LiteDB.Studio.Wpf/ViewModels/DbTreeNode.cs (IAsyncRelayCommand): show SaveFileDialog, export collection to JSON
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
- [X] T097 [US4] Update TabViewModel.RunCommand in LiteDB.Studio.Wpf/ViewModels/TabViewModel.cs to check SelectionLength and execute selected text vs entire buffer (already implemented in T034, verify logic)
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

- [ ] T109 [US6] Implement MainViewModel.BeginTransactionCommand in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (IAsyncRelayCommand): call IDatabaseService.BeginTransactionAsync
- [ ] T110 [US6] Implement MainViewModel.CommitTransactionCommand in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (IAsyncRelayCommand): call IDatabaseService.CommitTransactionAsync
- [ ] T111 [US6] Implement MainViewModel.RollbackTransactionCommand in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs (IAsyncRelayCommand): call IDatabaseService.RollbackTransactionAsync
- [ ] T112 [US6] Subscribe to IDatabaseService.TransactionStateChanged event in MainViewModel constructor in LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs to update TransactionActive property
- [ ] T113 [US6] Add CanExecute logic to transaction commands in MainViewModel: BeginTransactionCommand requires !TransactionActive, CommitTransactionCommand and RollbackTransactionCommand require TransactionActive
- [ ] T114 [US6] Add Transaction menu to MainWindow.xaml with Begin, Commit, Rollback commands
- [ ] T115 [US6] Review LiteDB.Studio/Classes/Debugger for breakpoint and step logic (WinForms implementation)
- [ ] T116 [US6] Port debugger ViewModel in LiteDB.Studio.Wpf/ViewModels/DebuggerViewModel.cs with breakpoint management and step commands
- [ ] T117 [US6] Port debugger View in LiteDB.Studio.Wpf/Views/DebuggerView.xaml with breakpoint list and step controls
- [ ] T118 [US6] Integrate DebuggerView into MainWindow.xaml (panel or tool window)
- [ ] T119 [US6] Add integration test for transaction workflow: begin, insert, commit, verify data persisted
- [ ] T120 [P] [US6] Add integration test for rollback workflow: begin, insert, rollback, verify data not persisted
- [ ] T121 [P] [US6] Add unit test for transaction command state management (verify CanExecute logic)

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

- [ ] T122 [US7] Add unit test coverage for MainViewModel commands not yet tested: NewTabCommand, CloseTabCommand, RefreshTreeCommand, InsertSnippetCommand in LiteDB.Studio.Wpf.Tests/ViewModels/MainViewModelTests.cs
- [ ] T123 [P] [US7] Add unit test coverage for TabViewModel edge cases: empty EditorText, null LastResult, cancellation in LiteDB.Studio.Wpf.Tests/ViewModels/TabViewModelTests.cs
- [ ] T124 [P] [US7] Add unit test coverage for DatabaseTreeViewModel: Clear, LoadRootNodesAsync with empty collections in LiteDB.Studio.Wpf.Tests/ViewModels/DatabaseTreeViewModelTests.cs
- [ ] T125 [P] [US7] Add integration test for schema discovery: GetCollectionSchemaAsync with nested documents, arrays in LiteDB.Studio.Wpf.Tests/Integration/LiteDbServiceTests.cs
- [ ] T126 [P] [US7] Add integration test for edge cases: locked file, malformed connection string, schema mismatch in LiteDB.Studio.Wpf.Tests/Integration/LiteDbServiceTests.cs
- [ ] T127 [US7] Add performance test: load 10k rows, measure grid render time, verify <3s in LiteDB.Studio.Wpf.Tests/Performance/GridPerformanceTests.cs
- [ ] T128 [P] [US7] Add memory leak test: open/close 100 tabs in loop, verify memory returns to baseline in LiteDB.Studio.Wpf.Tests/Performance/MemoryLeakTests.cs
- [ ] T129 [US7] Configure CI pipeline to run dotnet test for LiteDB.Studio.Wpf.Tests project
- [ ] T130 [US7] Update CI pipeline to fail build on test failures
- [ ] T131 [US7] Create PR template in .github/PULL_REQUEST_TEMPLATE.md with sections: Problem, Approach, Risks, Tests, Rollout/Rollback
- [ ] T132 [US7] Run all tests locally, verify green, fix any failures before opening PR

---

## Phase 10: Polish & Cross-Cutting Concerns

**Goal**: Address edge cases, error handling, UX polish, and ensure all constitutional requirements met

**Story Goal**: N/A (cross-cutting improvements)  
**Independent Test Criteria**: All edge cases handled gracefully; error messages actionable; resource cleanup verified

### Tasks

- [ ] T133 [P] Add error handling for connection failures: locked files, insufficient permissions, malformed connection strings in LiteDB.Studio.Wpf/Services/LiteDbService.cs (surface actionable error messages)
- [ ] T134 [P] Add query cancellation edge case handling: ensure cancellation releases DB resources, surface "Cancelled by user" message in TabViewModel
- [ ] T135 [P] Add schema mismatch handling: gracefully display placeholder for missing fields in result grid, avoid crashes
- [ ] T136 [P] Add confirmation dialog for destructive actions: implement typed confirmation for DROP commands in DbTreeNode.DropCommand
- [ ] T137 [P] Add Shift-modifier behavior for destructive actions: detect Shift key, bypass/alter confirmation per spec in DbTreeNode.DropCommand
- [ ] T138 [P] Add concurrent update detection: when UpdateDocumentFieldAsync detects version mismatch, show conflict resolution dialog (reload/overwrite/merge options) in ResultGrid.xaml.cs
- [ ] T139 Add tooltip to ResultGrid cells for truncated BSON values (show full value on hover)
- [ ] T140 [P] Add status bar to MainWindow.xaml showing: connection state, execution time, row count, transaction state
- [ ] T141 Verify all icons use pack URIs and Build Action = Resource in LiteDB.Studio.Wpf.csproj
- [ ] T142 Verify all resources under LiteDB.Studio.Wpf/Resources per constitution requirement
- [ ] T143 Run code review checklist: apply_patch used for all edits, tests included, MVVM-first adhered to, minimal diffs

---

## Task Summary

| Phase | Task Count | Parallelizable | Story |
|-------|-----------|----------------|-------|
| Phase 1: Setup (including Logging) | 20 | 10 | N/A |
| Phase 2: Foundational | 19 | 6 | N/A |
| Phase 3: User Story 1 (MVP) | 25 | 4 | Core Execution |
| Phase 4: User Story 2 | 8 | 3 | Result Display & Editing |
| Phase 5: User Story 3 | 19 | 3 | Database Explorer |
| Phase 6: User Story 4 | 7 | 0 | Editor Enhancements |
| Phase 7: User Story 5 | 10 | 3 | File Operations & UX |
| Phase 8: User Story 6 | 13 | 3 | Transactions & Debugger |
| Phase 9: User Story 7 | 11 | 7 | Testing & Polish |
| Phase 10: Polish & Cross-Cutting Concerns | 11 | 7 | N/A |
| Editor Adjustments | 5 | 0 | N/A |
| Cross-cutting | 4 | 4 | N/A |
| **Total** | **147** | **50** | **7 stories** |

---

## Editor Porting Adjustments (from spec update)

These tasks were added to reflect the spec and plan updates that mandate porting `ICSharpCode.TextEditor` usages to AvalonEdit and preferring attached properties/behaviors for MVVM bindings.

- [X] T139 Implement AvalonEdit attached properties/behaviors in LiteDB.Studio.Wpf/Controls/AvalonEditBehaviors.cs exposing: `EditorText`, `CaretOffset`/`LineColumn`, `SelectionStart`, `SelectionLength`, `IsModified` and routed events to update bound `TabViewModel` properties.
- [X] T140 Add unit tests for AvalonEdit attached properties in LiteDB.Studio.Wpf.Tests/ViewModels/AvalonEditBehaviorsTests.cs (create `TextEditor` instance, apply attached properties, verify `TabViewModel`-observable updates via a test helper).
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
- [ ] T155 Add manual QA checklist and integration tests: verify Ctrl+Space completion via ViewModel/service, F5 run-selection semantics, theme switching, font scaling, and large-document performance (document load / typing latency). Include steps and acceptance criteria in `specs/002-wpf-port/tasks.md` and `specs/002-wpf-port/checklists/requirements.md`.
- [ ] T156 Define completion caching policy & add tests (LiteDB.Studio.Wpf/Services/SqlCompletionProvider.cs and LiteDB.Studio.Wpf.Tests/Performance/CompletionCachingTests.cs): specify TTL, invalidation on schema change, cache size limits, and expected behavior under concurrent updates or stale schema views.
- [ ] T157 Port and verify Find/Replace functionality to AvalonEdit: implement Find/Replace control and commands (`LiteDB.Studio.Wpf/Controls/FindReplaceControl.xaml`), integrate with `TabViewModel` (commands/properties), and add tests (`LiteDB.Studio.Wpf.Tests/Controls/FindReplaceTests.cs`) covering replace-all, case-sensitivity, whole-word, and regex modes.
- [ ] T158 Port and test Undo/Redo behavior: ensure editor undo/redo stacks match WinForms behavior, cover grouped edits, selection-based replacements, and programmatic changes that should/shouldn't be undoable; add tests `LiteDB.Studio.Wpf.Tests/Controls/UndoRedoTests.cs`.
- [ ] T159 Bind App-level theme and font settings to AvalonEdit behaviors and add integration tests (implement in `LiteDB.Studio.Wpf/Settings/AppSettings.cs` and tests in `LiteDB.Studio.Wpf.Tests/Integration/EditorThemeFontTests.cs`): ensure theme switching and font scaling apply immediately to open editors and persist across sessions.

## Cross-cutting Tasks: Localization, DI, Performance

- [ ] T144 [P] Localization: Extract UI strings to `LiteDB.Studio.Wpf/Resources/Strings.resx` and update Views/XAML to use resource bindings; include culture-neutral keys and comment usage.
- [ ] T145 [P] Localization Tests: Add unit/integration tests in `LiteDB.Studio.Wpf.Tests/Localization/` to verify resource lookup and a sample culture switch (e.g., `fr-FR`) shows translated strings for a sample view.
- [ ] T146 [P] DI/bootstrap: Add `AppBootstrapper.cs` (or `ServiceRegistration.cs`) in `LiteDB.Studio.Wpf/` that registers services with `IServiceCollection` (register `IDatabaseService`, `EditorCompletionService`, ViewModels) and document the chosen DI approach in `quickstart.md`.
- [ ] T147 [P] DI bootstrap test: Add `BootstrapperTests.cs` in `LiteDB.Studio.Wpf.Tests/` to assert that `ServiceProvider` resolves `IDatabaseService` and `MainViewModel` without exceptions (use a test service collection builder).
- [ ] T148 [P] Performance SLOs: Add performance-check tests for completion provider latency (e.g., `LiteDB.Studio.Wpf.Tests/Performance/CompletionLatencyTests.cs`) asserting median latency <200ms for local schema mocks and document the SLOs in `specs/002-wpf-port/spec.md`.


## Dependencies & Execution Order

### Critical Path (Sequential Dependencies)

1. **Phase 1 → Phase 2**: Setup must complete before foundational types
2. **Phase 2 → Phase 3**: IDatabaseService and LiteDbService must exist before ViewModels
3. **Phase 3 → Phase 4+**: MVP (User Story 1) must complete before subsequent stories (establishes core execution and grid rendering)

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
- [ ] Task count: 141 tasks across 10 phases

**Ready for implementation**: ✅ Tasks are specific, file paths included, parallelization opportunities identified, and test tasks embedded per story.
