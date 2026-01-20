# Quick Start Guide: WPF Port Implementation

**Audience**: Developers implementing the WPF port  
**Date**: 2026-01-19  
**Prerequisites**: Visual Studio 2022, .NET 9.0 SDK, familiarity with WPF and MVVM

## Phase-by-Phase Implementation

### Phase 1: Core Execution (MVP)

**Goal**: Execute SQL queries and display results in a grid

#### Step 1: Implement IDatabaseService and LiteDbService

1. Create `LiteDB.Studio.Wpf/Services/IDatabaseService.cs`:
   - Copy interface from `contracts/IDatabaseService.md`
   - Define `QueryResult`, `ColumnInfo` classes

2. Create `LiteDB.Studio.Wpf/Services/LiteDbService.cs`:
   - Implement connection lifecycle (ConnectAsync, DisconnectAsync)
   - Implement ExecuteAsync with row limit enforcement (default 1000)
   - Use `Stopwatch` to measure ExecutionTime

3. Test with integration test:
   ```csharp
   var service = new LiteDbService();
   await service.ConnectAsync(tempFile);
   var result = await service.ExecuteAsync("db.test.insert({name: 'Alice'})", CancellationToken.None);
   Assert.Equal(1, result.RowCount);
   ```

#### Step 2: Implement TabViewModel and RunCommand

1. Create `LiteDB.Studio.Wpf/ViewModels/TabViewModel.cs`:
   - Inherit from `ObservableObject` (CommunityToolkit.Mvvm)
   - Add properties: `EditorText`, `LastResult`, `LastError`, etc.
   - Implement `RunCommand` as `IAsyncRelayCommand`:
     ```csharp
     [RelayCommand]
     private async Task RunAsync(CancellationToken cancellationToken)
     {
         LastResult = null;
         LastError = null;
         try
         {
             var query = SelectionLength > 0 ? GetSelectedText() : EditorText;
             LastResult = await _databaseService.ExecuteAsync(query, cancellationToken);
         }
         catch (Exception ex)
         {
             LastError = ex.Message;
         }
     }
     ```

2. Unit test TabViewModel:
   ```csharp
   var mockService = Substitute.For<IDatabaseService>();
   mockService.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
       .Returns(new QueryResult { RowCount = 5 });
   var viewModel = new TabViewModel(mockService);
   await viewModel.RunCommand.ExecuteAsync(null);
   Assert.NotNull(viewModel.LastResult);
   ```

#### Step 3: Create ResultGrid Control

1. Create `LiteDB.Studio.Wpf/Controls/ResultGrid.xaml`:
   - Use `DataGrid` with `VirtualizingStackPanel.IsVirtualizing="True"`
   - Bind `ItemsSource` to `QueryResult.Rows`
   - Generate columns dynamically from `QueryResult.Columns` in code-behind

2. Implement `BsonValueToStringConverter`:
   - Implement `IValueConverter`
   - Handle ObjectId, DateTime, Binary, nested Documents/Arrays

3. Test grid rendering with 1000+ rows; verify virtualization prevents freezing

#### Step 4: Wire MainViewModel and Main UI

1. Create `LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs`:
   - Add `Tabs`, `SelectedTab`, `RunCommand`
   - RunCommand delegates to `SelectedTab.RunCommand`

2. Update `MainWindow.xaml`:
   - Add TabControl bound to `MainViewModel.Tabs`
   - Add AvalonEdit editor in tab content
   - Add ResultGrid for `LastResult`

3. Manual test: Run app, create connection, execute query, verify grid displays results

**Phase 1 Acceptance**: Users can connect, run SQL, and see results in grid

---

### Phase 2: Result Display & Editing

**Goal**: Grid/Text/Parameters views; cell editing persists to DB

#### Step 1: Implement Grid Cell Editing

1. In `ResultGrid.xaml`, handle `CellEditEnding` event:
   ```csharp
   private async void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
   {
       var doc = e.Row.Item as BsonDocument;
       var column = e.Column.Header.ToString();
       var newValue = (e.EditingElement as TextBox)?.Text;
       await _databaseService.UpdateDocumentFieldAsync(collectionName, doc["_id"], column, newValue, CancellationToken.None);
   }
   ```

2. Implement `LiteDbService.UpdateDocumentFieldAsync`:
   - Use LiteDB `Update` method with field setter
   - Validate document exists; handle type conversion

3. Test: Edit cell in grid; verify DB updated; refresh result to confirm

#### Step 2: Add Text and Parameters Views

1. Create `ResultTextView` control: display `LastResult` as JSON
2. Create `ParametersView` control: display query parameters (if supported)
3. Add tab selector in UI to switch between Grid/Text/Parameters

**Phase 2 Acceptance**: Users can edit cells and switch result views

---

### Phase 3: Database Explorer

**Goal**: Tree view with collections, lazy loading, context actions

#### Step 1: Implement Schema Discovery

1. Implement `LiteDbService.GetCollectionNamesAsync` and `GetCollectionSchemaAsync`:
   - Use LiteDB `GetCollectionNames()`
   - Sample first 100 documents to infer schema

2. Test: Verify collection names and schema returned

#### Step 2: Implement DatabaseTreeViewModel and DbTreeNode

1. Create `DatabaseTreeViewModel`:
   - `LoadRootNodesAsync` creates nodes for collections

2. Create `DbTreeNode`:
   - Properties: `Header`, `IconUri`, `Children`, `IsLoaded`
   - `LoadChildrenCommand` loads schema on expand

3. Bind `TreeView` to `MainViewModel.Tree.RootNodes` in `MainWindow.xaml`

#### Step 3: Add Context Menu Actions

1. Add `DropCommand`, `ExportCommand`, `InsertSnippetCommand` to `DbTreeNode`
2. Show confirmation dialog for `DropCommand` (require typed confirmation)
3. Implement snippet insertion: double-click inserts snippet at caret

**Phase 3 Acceptance**: Users can browse collections, drop, export, insert snippets

---

### Phase 4: Editor Enhancements

**Goal**: Code completion, run selection, caret tracking

#### Step 1: Implement Code Completion

1. Create `SqlCompletionProvider` implementing AvalonEdit `ICompletionData`:
   - Return collection names from `GetCollectionNamesAsync`
   - Return SQL keywords (SELECT, INSERT, etc.)

2. Wire Ctrl+Space to show completion window

3. Test: Type "db." and verify collection names appear

#### Step 2: Implement Run Selection

1. In `TabViewModel.RunCommand`:
   - Check `SelectionLength > 0`
   - Extract selected text and execute

2. Bind F5 to `RunCommand`

**Phase 4 Acceptance**: Users get completion and run selection with F5

---

### Phase 5: File Operations & UX

**Goal**: Open/save SQL files, track modified state

#### Step 1: Implement File Commands

1. Add `OpenFileCommand` in `MainViewModel`:
   - Show `OpenFileDialog` filtered to .sql
   - Load file into new or selected tab
   - Set `Filename` and `IsModified = false`

2. Add `SaveFileCommand`:
   - If `Filename` is null, show `SaveFileDialog`
   - Write `EditorText` to file
   - Set `IsModified = false`

3. Track `IsModified` on `EditorText` changes

#### Step 2: Prompt Save on Close

1. In `CloseTabCommand`, check `IsModified`
2. Show confirmation: "Save changes to {Filename}?"
3. If yes, call `SaveFileCommand`; if no, discard; if cancel, abort close

**Phase 5 Acceptance**: Users can open/save files and are prompted on close

---

### Phase 6: Transactions & Debugger

**Goal**: Transaction lifecycle and debugger features

#### Step 1: Implement Transaction Commands

1. Implement `LiteDbService.BeginTransactionAsync`, `CommitTransactionAsync`, `RollbackTransactionAsync`
2. Add commands to `MainViewModel`: `BeginTransactionCommand`, `CommitTransactionCommand`, `RollbackTransactionCommand`
3. Enable/disable commands based on `TransactionActive` state

#### Step 2: Port Debugger Features

1. Review `LiteDB.Studio/Classes/Debugger` for breakpoint and step logic
2. Port features to WPF ViewModels and Views
3. Maintain behavioral parity with WinForms implementation

**Phase 6 Acceptance**: Users can manage transactions and use debugger

---

### Phase 7: Testing & Polish

**Goal**: Comprehensive tests and CI gating

#### Step 1: Unit Tests for ViewModels

1. Create `LiteDB.Studio.Wpf.Tests` project (xUnit)
2. Add tests for `MainViewModel`, `TabViewModel`, `DatabaseTreeViewModel`:
   ```csharp
   [Fact]
   public async Task RunCommand_SetsLastResult_WhenQuerySucceeds()
   {
       var mockService = Substitute.For<IDatabaseService>();
       mockService.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Returns(new QueryResult { RowCount = 10 });
       var viewModel = new TabViewModel(mockService);
       await viewModel.RunCommand.ExecuteAsync(null);
       Assert.NotNull(viewModel.LastResult);
       Assert.Equal(10, viewModel.LastResult.RowCount);
   }
   ```

3. Target >80% coverage for ViewModel logic

#### Step 2: Integration Tests for LiteDbService

1. Add integration tests using ephemeral DB files:
   ```csharp
   [Fact]
   public async Task ExecuteAsync_ReturnsResults_ForValidQuery()
   {
       var tempFile = Path.GetTempFileName();
       var service = new LiteDbService();
       await service.ConnectAsync(tempFile);
       var result = await service.ExecuteAsync("db.test.insert({x: 1})", CancellationToken.None);
       Assert.Equal(1, result.RowCount);
       File.Delete(tempFile);
   }
   ```

2. Test transaction lifecycle, schema discovery, updates

#### Step 3: Performance and Leak Checks

1. Load 10k rows and verify grid renders in <3s
2. Open/close 100 tabs in loop; verify no memory leaks (use profiler)

#### Step 4: CI Configuration

1. Update CI pipeline to run `dotnet test` for all test projects
2. Fail build on test failures
3. Add PR template with Problem, Approach, Risks, Tests, Rollout/Rollback

**Phase 7 Acceptance**: Tests pass in CI; performance targets met

---

## Development Workflow

### Daily Workflow

1. **Pull latest**: `git pull origin 002-wpf-port`
2. **Create feature branch**: `git checkout -b feature/phase1-execution`
3. **Implement**: Edit files using `apply_patch` workflow
4. **Test**: Run unit and integration tests: `dotnet test`
5. **Commit**: Human commits only (per constitution)
6. **PR**: Open PR with template fields; run CI; request review

### Key Commands

```powershell
# Build solution
dotnet build LiteDB.Studio.sln

# Run tests
dotnet test LiteDB.Studio.Wpf.Tests/LiteDB.Studio.Wpf.Tests.csproj

# Run app
dotnet run --project LiteDB.Studio.Wpf/LiteDB.Studio.Wpf.csproj
```

### Debugging Tips

- Use Visual Studio debugger with breakpoints in ViewModels
- For XAML binding errors, check Output window (Data Binding category)
- For service errors, add logging to `LiteDbService` methods
- For performance issues, use Visual Studio Profiler

## Common Patterns

### Adding a New Command

1. Add command property to ViewModel:
   ```csharp
   [RelayCommand]
   private async Task MyActionAsync(CancellationToken cancellationToken)
   {
       // Implementation
   }
   ```

2. Bind in XAML:
   ```xaml
   <Button Command="{Binding MyActionCommand}" Content="My Action" />
   ```

### Adding a Resource

1. Add icon to `LiteDB.Studio.Wpf/Resources/Icons/myicon.png`
2. Set Build Action = "Resource" in .csproj
3. Reference in XAML:
   ```xaml
   <Image Source="pack://application:,,,/LiteDB.Studio.Wpf;component/Resources/Icons/myicon.png" />
   ```

### Mocking IDatabaseService

```csharp
var mockService = Substitute.For<IDatabaseService>();
mockService.IsConnected.Returns(true);
mockService.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
    .Returns(new QueryResult { RowCount = 5 });
var viewModel = new MainViewModel(mockService);
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| DataGrid not displaying results | Check `ItemsSource` binding; verify `Rows` is IEnumerable |
| Command not executing | Check `CanExecute` logic; verify service state |
| UI freezing during query | Ensure using `IAsyncRelayCommand`; verify async/await pattern |
| Icons not showing | Check Build Action = "Resource"; verify pack URI syntax |
| Tests failing | Check mock setup; verify async command execution with `.ExecuteAsync(null)` |

## Next Steps

1. Review [spec.md](spec.md) for full requirements
2. Review [contracts/IDatabaseService.md](contracts/IDatabaseService.md) and [contracts/ViewModels.md](contracts/ViewModels.md) for detailed interfaces
3. Start with Phase 1: implement `LiteDbService` and `TabViewModel.RunCommand`
4. Open PR after Phase 1 MVP; iterate phases in priority order

## Resources

- [CommunityToolkit.Mvvm Docs](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [AvalonEdit Docs](http://avalonedit.net/)
- [LiteDB Docs](https://www.litedb.org/)
- [WPF DataGrid Virtualization](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/datagrid)
