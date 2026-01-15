# LiteDB.Studio WPF Migration Plan

**Date:** January 14, 2026  
**Project:** Migrating LiteDB.Studio (WinForms) to LiteDB.Studio.Wpf

---

## Executive Summary

This document outlines the migration from the existing WinForms-based **LiteDB.Studio** to a modern WPF application using MVVM architecture, dependency injection, and contemporary C# patterns. The WPF version leverages CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection, and AvalonEdit for a maintainable, testable codebase.

---

## Project Rules

When making any changes to the WPF project, you **must** reference the original WinForms project and use code already produced. **Do not** recreate code if the code already exists.

## Current State Analysis

### WinForms Application (LiteDB.Studio)

**Technology Stack:**
- .NET 9.0 Windows Forms
- ICSharpCode.TextEditor for SQL editing
- Code-behind architecture (tight coupling)
- Manual thread management with `SynchronizationContext`

**Key Components:**
- `MainForm.cs` (984 lines) - monolithic form with all logic
- `TaskData.cs` - query execution state management
- `SqlCodeCompletion.cs` - code completion provider
- `DatabaseDebugger.cs` - HTTP server for page inspection
- `UIExtensions.cs` - helper methods for UI binding

**Core Features:**
1. Database connection management with connection strings
2. Multi-tabbed SQL editor with syntax highlighting
3. SQL execution with async threading
4. Result display in three modes: Grid, Text (JSON), Parameters
5. Database tree view with collections and system tables
6. Code completion (Ctrl+Space)
7. Editable result grid with direct document updates
8. Transaction support (BEGIN/COMMIT/ROLLBACK/CHECKPOINT)
9. Load/Save SQL files per tab
10. Recent database list with validation
11. Database debugger (HTTP listener on random port)
12. Custom tab rendering with close buttons
13. Keyboard shortcuts (F5 to run, Ctrl+Space for completion)

### WPF Application (LiteDB.Studio.Wpf)

**Technology Stack:**
- .NET 9.0 WPF
- AvalonEdit for SQL editing
- MVVM architecture with CommunityToolkit.Mvvm
- Microsoft.Extensions.DependencyInjection + Hosting
- Modern async/await patterns

**Current Implementation:**
- ✅ DI container with Host pattern
- ✅ MVVM foundation (MainViewModel, TabViewModel)
- ✅ Service layer (IDatabaseService, LiteDbService)
- ✅ Settings persistence (AppSettingsManager with JSON)
- ✅ Basic UI layout with toolbar and tabs
- ✅ Connect/Disconnect functionality
- ✅ Recent database list
- ✅ Tab management with "+" tab
- ✅ Content preservation per tab
- ⚠️ SQL execution returns fake data only
- ❌ No result grid rendering
- ❌ No database tree view
- ❌ No code completion
- ❌ No transaction commands
- ❌ No file operations
- ❌ No debugger integration

---

## Architecture Comparison

### Data Flow: WinForms vs WPF

**WinForms (Current):**
```
User Action → Event Handler → Direct DB Call → Thread Pool → 
SynchronizationContext.Post → Update UI Controls
```

**WPF (Target):**
```
User Action → Command (IRelayCommand) → Service Layer → 
Async Operation → ObservableCollection Update → 
Data Binding → UI Automatic Update
```

### Key Architectural Improvements

1. **Separation of Concerns**
   - Business logic extracted to services
   - UI logic in ViewModels
   - Zero code-behind logic (only view setup)

2. **Testability**
   - Services can be unit tested
   - ViewModels can be tested without UI
   - Dependency injection enables mocking

3. **Maintainability**
   - MVVM pattern reduces coupling
   - Commands replace event handlers
   - Data binding eliminates manual UI updates

4. **Modern Patterns**
   - Async/await instead of manual threading
   - CancellationToken support
   - IAsyncRelayCommand for long operations

---

## Feature Gap Analysis

### ❌ Critical Missing Features

#### 1. SQL Execution Engine
**WinForms Implementation:**
- Uses `LiteDatabase.Execute(StringReader, BsonDocument)` with `IBsonDataReader`
- Executes on background thread with `ManualResetEventSlim`
- Supports parameter binding via `BsonDocument`
- Implements 1000-row limit with `LimitExceeded` flag

**WPF Status:** Stub implementation with fake data

**Migration Tasks:**
- Implement `ExecuteAsync` in `IDatabaseService`
- Add `IBsonDataReader` processing
- Support cancellation tokens
- Handle parameters
- Implement result limiting

#### 2. Result Display System
**WinForms Implementation:**
- Grid view with custom BsonValue rendering
- Cell editing with optimistic concurrency (WHERE clause checks)
- Text view with JSON serialization
- Parameters view showing bound values
- Lazy loading per tab (IsGridLoaded, IsTextLoaded, IsParametersLoaded)

**WPF Status:** Basic DataGrid with no BsonValue handling

**Migration Tasks:**
- Create BsonValueToStringConverter for data binding
- Implement tab switching for Grid/Text/Parameters
- Add cell editing support
- Implement UPDATE query on cell commit
- Add proper error handling for edit conflicts

#### 3. Database Explorer (TreeView)
**WinForms Implementation:**
- Root node: database filename
- System folder: system collections ($cols filtered by type='system')
- Collection nodes: all user collections
- Context menus with SQL snippet templates
- Double-click inserts SQL into editor

**WPF Status:** Empty left panel

**Migration Tasks:**
- Create `DatabaseTreeViewModel` with hierarchical structure
- Implement `LoadTreeAsync` on connection
- Add context menu with commands
- Wire double-click to insert SQL snippet

#### 4. Code Completion
**WinForms Implementation:**
- ICSharpCode.TextEditor completion window
- Triggers on Ctrl+Space
- Shows collection names, keywords, fields
- Updates on database connection

**WPF Status:** Not implemented

**Migration Tasks:**
- Research AvalonEdit completion API
- Port completion data provider to AvalonEdit
- Implement completion window trigger
- Add collection/field suggestions from database schema

#### 5. Transaction Commands
**WinForms Implementation:**
- Dedicated toolbar buttons
- Executes simple SQL: "BEGIN", "COMMIT", "ROLLBACK", "CHECKPOINT"

**WPF Status:** Buttons exist but not wired

**Migration Tasks:**
- Add commands to MainViewModel
- Wire to toolbar buttons
- Execute via service layer

#### 6. File Operations
**WinForms Implementation:**
- Load SQL: Opens file dialog, creates new tab with content, tracks filename
- Save SQL: Saves to tracked filename or prompts for new file
- Tab title shows filename when file is associated

**WPF Status:** Not implemented

**Migration Tasks:**
- Add LoadSqlCommand and SaveSqlCommand
- Track Filename property on TabViewModel
- Update tab title binding
- Add OpenFileDialog and SaveFileDialog

#### 7. Database Debugger
**WinForms Implementation:**
- Creates HttpListener on random port (8000-9000)
- Serves HTML pages for disk page inspection
- GET /{pageID}: Dump page structure
- POST: Accept page JSON for inspection
- /list/{pageID}: List pages

**WPF Status:** Not implemented

**Migration Tasks:**
- Port DatabaseDebugger class (should work as-is)
- Add DebugCommand to MainViewModel
- Launch browser on button click
- Consider making port configurable

### ⚠️ Partially Implemented Features

#### Tab Management
**Implemented:**
- Add/remove tabs
- "+" tab for new query
- Content preservation

**Missing:**
- Close button in tab header
- Tab reordering
- Middle-click to close
- Modified state indicator
- Confirmation on close with unsaved changes

#### Connection Management
**Implemented:**
- File-based connection (OpenFileDialog)
- Recent list persistence
- Auto-load last database

**Missing:**
- Full ConnectionForm dialog (with advanced options)
- Connection string builder UI
- Connection validation before opening

---

## Service Layer Design

### Current IDatabaseService
```csharp
public interface IDatabaseService : IDisposable
{
    bool IsConnected { get; }
    object? Database { get; }
    Task<object> ConnectAsync(ConnectionString connectionString);
    void Disconnect();
}
```

### Proposed Enhanced IDatabaseService
```csharp
public interface IDatabaseService : IDisposable
{
    bool IsConnected { get; }
    LiteDatabase? Database { get; }
    ConnectionString? CurrentConnection { get; }
    
    // Connection
    Task ConnectAsync(ConnectionString connectionString, CancellationToken ct = default);
    void Disconnect();
    
    // Execution
    Task<QueryResult> ExecuteAsync(string sql, BsonDocument? parameters = null, CancellationToken ct = default);
    
    // Schema
    IEnumerable<string> GetCollectionNames();
    Task<BsonDocument> GetCollectionSchemaAsync(string collection);
    Task<List<BsonDocument>> GetSystemCollectionsAsync();
    
    // Updates
    Task<int> UpdateDocumentFieldAsync(string collection, BsonValue id, string field, BsonValue oldValue, BsonValue newValue);
    
    // Transactions
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    Task CheckpointAsync();
}

public class QueryResult
{
    public List<BsonValue> Documents { get; set; }
    public string Collection { get; set; }
    public TimeSpan Elapsed { get; set; }
    public bool LimitExceeded { get; set; }
    public int Count => Documents.Count;
}
```

---

## ViewModel Design

### Enhanced MainViewModel
```csharp
public class MainViewModel : ObservableObject
{
    // Properties
    public ObservableCollection<TabViewModel> Tabs { get; }
    public ObservableCollection<string> RecentDatabases { get; }
    public DatabaseTreeViewModel TreeView { get; }
    public TabViewModel? SelectedTab { get; set; }
    public bool IsConnected { get; set; }
    public bool IsExecuting { get; set; }
    public string StatusText { get; set; }
    public string ElapsedText { get; set; }
    public string ResultCountText { get; set; }
    
    // Connection Commands
    public IAsyncRelayCommand ConnectCommand { get; }
    public IRelayCommand DisconnectCommand { get; }
    public IAsyncRelayCommand<string> OpenRecentCommand { get; }
    
    // Execution Commands
    public IAsyncRelayCommand RunCommand { get; }
    public IAsyncRelayCommand RunSelectionCommand { get; }
    public IRelayCommand CancelCommand { get; }
    
    // Transaction Commands
    public IAsyncRelayCommand BeginCommand { get; }
    public IAsyncRelayCommand CommitCommand { get; }
    public IAsyncRelayCommand RollbackCommand { get; }
    public IAsyncRelayCommand CheckpointCommand { get; }
    
    // File Commands
    public IAsyncRelayCommand LoadSqlCommand { get; }
    public IAsyncRelayCommand SaveSqlCommand { get; }
    
    // Tab Commands
    public IRelayCommand AddTabCommand { get; }
    public IRelayCommand<TabViewModel> CloseTabCommand { get; }
    
    // Other Commands
    public IRelayCommand RefreshTreeCommand { get; }
    public IAsyncRelayCommand DebugCommand { get; }
    public IRelayCommand<string> InsertSnippetCommand { get; }
}
```

### Enhanced TabViewModel
```csharp
public class TabViewModel : ObservableObject
{
    public string Title { get; set; }
    public string Content { get; set; }
    public bool IsPlus { get; set; }
    public bool IsModified { get; set; }
    public string? Filename { get; set; }
    
    // Execution state
    public QueryResult? LastResult { get; set; }
    public Exception? LastError { get; set; }
    public BsonDocument Parameters { get; set; }
    public TimeSpan Elapsed { get; set; }
    
    // UI state
    public string SelectedResultTab { get; set; } // "Grid", "Text", "Parameters"
    public int CaretLine { get; set; }
    public int CaretColumn { get; set; }
    public string? SelectedText { get; set; }
    
    // Lazy loading flags
    public bool IsGridLoaded { get; set; }
    public bool IsTextLoaded { get; set; }
    public bool IsParametersLoaded { get; set; }
}
```

### New DatabaseTreeViewModel
```csharp
public class DatabaseTreeViewModel : ObservableObject
{
    public ObservableCollection<TreeNodeViewModel> Nodes { get; }
    
    public IAsyncRelayCommand LoadTreeCommand { get; }
    public IRelayCommand<TreeNodeViewModel> ExecuteNodeQueryCommand { get; }
    public IRelayCommand RefreshCommand { get; }
}

public class TreeNodeViewModel : ObservableObject
{
    public string Name { get; set; }
    public string? SqlSnippet { get; set; }
    public string IconKey { get; set; }
    public ObservableCollection<TreeNodeViewModel> Children { get; }
    public bool IsExpanded { get; set; }
}
```

---

## UI Components

### Value Converters Needed

```csharp
// Convert BsonValue to display string
public class BsonValueToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not BsonValue bson) return string.Empty;
        
        return bson.Type switch
        {
            BsonType.MinValue => "-∞",
            BsonType.MaxValue => "+∞",
            BsonType.Null => "(null)",
            BsonType.Boolean => bson.AsBoolean.ToString().ToLower(),
            BsonType.DateTime => bson.AsDateTime.ToString(),
            BsonType.Binary => Convert.ToBase64String(bson.AsBinary),
            BsonType.Int32 or BsonType.Int64 or BsonType.Double or BsonType.Decimal => bson.RawValue.ToString(),
            BsonType.String or BsonType.ObjectId or BsonType.Guid => bson.ToString(),
            _ => JsonSerializer.Serialize(bson)
        };
    }
}

// Convert BsonValue type to color (for null styling)
public class BsonTypeToColorConverter : IValueConverter { }

// Path trimming for recent list (already exists)
public class PathTrimmerConverter : IValueConverter { }
```

### Custom Controls Needed

1. **BsonDataGrid** (UserControl)
   - Extends DataGrid with BsonValue support
   - Custom cell editing
   - Column generation from heterogeneous documents
   - Sort comparison for BsonValues

2. **SqlEditor** (UserControl wrapping AvalonEdit)
   - Syntax highlighting
   - Code completion trigger
   - Line number display
   - Caret position tracking

3. **DatabaseTreeView** (UserControl)
   - Custom TreeView styling
   - Context menu per node type
   - Icons for database/folder/table

---

## Migration Phases

### Phase 1: Core Execution (Week 1)
**Goal:** Get SQL execution working end-to-end

**Tasks:**
1. Enhance `IDatabaseService` with `ExecuteAsync`
2. Implement `QueryResult` class
3. Wire `RunCommand` to execute SQL
4. Display results in DataGrid (basic rendering)
5. Add progress indication (IsExecuting property)
6. Implement cancellation support

**Acceptance Criteria:**
- Can execute simple SELECT queries
- Results display in grid
- Elapsed time shows
- Result count displays
- Progress indicator works

### Phase 2: Result Display (Week 2)
**Goal:** Properly render and interact with BsonValue data

**Tasks:**
1. Create `BsonValueToStringConverter`
2. Implement Grid/Text/Parameters tab switching
3. Add JSON formatting for Text view
4. Implement lazy loading per tab
5. Add cell editing support
6. Implement UPDATE on cell commit
7. Add error handling for concurrency conflicts

**Acceptance Criteria:**
- All BsonValue types render correctly
- Can switch between Grid/Text/Parameters
- Can edit cells in grid
- Updates persist to database
- Conflicts show proper error messages

### Phase 3: Database Explorer (Week 3)
**Goal:** Navigate database structure visually

**Tasks:**
1. Create `DatabaseTreeViewModel` and `TreeNodeViewModel`
2. Implement `LoadTreeAsync`
3. Add TreeView to left panel
4. Create context menu with SQL snippets
5. Wire double-click to insert snippet
6. Add icons for different node types
7. Implement refresh command

**Acceptance Criteria:**
- TreeView loads on connection
- Shows database/system/collections hierarchy
- Double-click inserts SELECT statement
- Context menu has INSERT/UPDATE/DELETE templates
- Refresh updates tree

### Phase 4: Enhanced Editor (Week 4)
**Goal:** Rich SQL editing experience

**Tasks:**
1. Research AvalonEdit completion window API
2. Port `SqlCodeCompletion` logic
3. Implement completion data provider
4. Add Ctrl+Space trigger
5. Add F5 keyboard shortcut for run
6. Implement "run selection" (Ctrl+Enter)
7. Track caret position per tab
8. Add syntax highlighting for SQL

**Acceptance Criteria:**
- Ctrl+Space shows completion window
- Completion suggests collections and keywords
- F5 executes query
- Can execute selected text only
- Caret position persists across tab switches

### Phase 5: File Operations (Week 5)
**Goal:** Load/save SQL files

**Tasks:**
1. Add `Filename` property to `TabViewModel`
2. Implement `LoadSqlCommand`
3. Implement `SaveSqlCommand`
4. Add OpenFileDialog integration
5. Add SaveFileDialog integration
6. Update tab title to show filename
7. Track modified state
8. Add "save before close" confirmation

**Acceptance Criteria:**
- Can load .sql files into tabs
- Can save tabs to .sql files
- Tab title shows filename
- Modified indicator shows when changed
- Prompts before closing unsaved tabs

### Phase 6: Transaction & Advanced (Week 6)
**Goal:** Complete feature parity

**Tasks:**
1. Wire transaction commands (BEGIN/COMMIT/ROLLBACK/CHECKPOINT)
2. Port `DatabaseDebugger` class
3. Add `DebugCommand` to start debugger
4. Implement ConnectionForm dialog (optional)
5. Add keyboard shortcuts
6. Polish UI (icons, styling, animations)
7. Add about dialog with version info

**Acceptance Criteria:**
- All toolbar buttons functional
- Transactions work correctly
- Debugger launches and serves pages
- All keyboard shortcuts work
- UI matches WinForms feature set

### Phase 7: Testing & Polish (Week 7)
**Goal:** Production-ready quality

**Tasks:**
1. Write unit tests for ViewModels
2. Write integration tests for services
3. Test with large databases
4. Performance profiling
5. Memory leak testing
6. Error handling review
7. User acceptance testing

---

## Technical Challenges & Solutions

### Challenge 1: ICSharpCode.TextEditor → AvalonEdit
**Problem:** Completely different APIs for code completion

**Solution:**
- Study AvalonEdit documentation and samples
- Implement `ICompletionData` for AvalonEdit
- Use `CompletionWindow` class
- Trigger from `TextArea.TextEntering` event

**Resources:**
- AvalonEdit documentation: https://github.com/icsharpcode/AvalonEdit
- Sample: https://github.com/icsharpcode/AvalonEdit/wiki/Code-Completion

### Challenge 2: Thread Synchronization
**Problem:** WinForms used `SynchronizationContext.Post`

**Solution:**
- WPF data binding handles UI updates automatically
- Use `ObservableCollection<T>` for collections
- Use `INotifyPropertyChanged` for properties
- For explicit UI updates: `Application.Current.Dispatcher.InvokeAsync`

### Challenge 3: Custom Tab Rendering
**Problem:** WinForms used `DrawItem` event for close buttons

**Solution:**
- Use WPF `TabControl.ItemTemplate`
- Add close button in DataTemplate
- Bind to `CloseTabCommand`
- Use style triggers for "+" tab

**Example:**
```xaml
<TabControl.ItemTemplate>
    <DataTemplate>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="{Binding Title}" />
            <Button Content="×" 
                    Command="{Binding DataContext.CloseTabCommand, RelativeSource={...}}"
                    CommandParameter="{Binding}"
                    Visibility="{Binding IsPlus, Converter={StaticResource InverseBoolToVis}}" />
        </StackPanel>
    </DataTemplate>
</TabControl.ItemTemplate>
```

### Challenge 4: BsonValue in DataGrid
**Problem:** DataGrid expects strongly-typed objects, BsonValue is dynamic

**Solution:**
- Create wrapper class: `BsonDocumentRow`
- Dynamically generate columns from first document
- Use `DataGridTemplateColumn` with custom CellTemplate
- Implement `IValueConverter` for display
- Handle cell editing with BsonValue serialization/deserialization

### Challenge 5: Async SQL Execution
**Problem:** Long-running queries block UI

**Solution:**
- Already using `IAsyncRelayCommand` ✅
- Add `CancellationTokenSource` per query
- Bind `IsExecuting` to progress bar
- Disable Run button during execution
- Implement Cancel command

---

## Data Binding Strategy

### Connection State
```xaml
<!-- Enable/disable based on connection -->
<Button IsEnabled="{Binding IsConnected}" />
<TreeView Visibility="{Binding IsConnected, Converter={StaticResource BoolToVis}}" />

<!-- Toggle button text -->
<Button Content="{Binding IsConnected, Converter={StaticResource ConnectButtonTextConverter}}" />
```

### Execution State
```xaml
<!-- Progress indication -->
<ProgressBar IsIndeterminate="{Binding IsExecuting}" />
<Button Command="{Binding RunCommand}" IsEnabled="{Binding IsExecuting, Converter={StaticResource InverseBoolConverter}}" />
<Button Command="{Binding CancelCommand}" IsEnabled="{Binding IsExecuting}" />
```

### Result Display
```xaml
<!-- Tab content binding -->
<DataGrid ItemsSource="{Binding SelectedTab.LastResult.Documents}" />
<TextBox Text="{Binding SelectedTab.LastResult, Converter={StaticResource QueryResultToJsonConverter}}" />
```

---

## Testing Strategy

### Unit Tests (ViewModels)
```csharp
[Fact]
public async Task ConnectAsync_ValidFile_SetsIsConnected()
{
    // Arrange
    var mockService = new Mock<IDatabaseService>();
    mockService.Setup(x => x.ConnectAsync(It.IsAny<ConnectionString>(), default))
               .Returns(Task.CompletedTask);
    var vm = new MainViewModel(mockService.Object);
    
    // Act
    // (trigger connect via dialog mock)
    
    // Assert
    Assert.True(vm.IsConnected);
}

[Fact]
public async Task RunCommand_ValidSql_PopulatesResults()
{
    // Arrange
    var mockService = new Mock<IDatabaseService>();
    mockService.Setup(x => x.ExecuteAsync(It.IsAny<string>(), null, default))
               .ReturnsAsync(new QueryResult { Documents = new List<BsonValue>() });
    var vm = new MainViewModel(mockService.Object);
    vm.SelectedTab = new TabViewModel { Content = "SELECT * FROM users" };
    
    // Act
    await vm.RunCommand.ExecuteAsync(null);
    
    // Assert
    Assert.NotNull(vm.SelectedTab.LastResult);
}
```

### Integration Tests (Services)
```csharp
[Fact]
public async Task LiteDbService_ExecuteAsync_ReturnsDocuments()
{
    // Arrange
    using var tempDb = new TempDatabase();
    var service = new LiteDbService();
    await service.ConnectAsync(new ConnectionString(tempDb.Path));
    
    // Act
    var result = await service.ExecuteAsync("SELECT * FROM test");
    
    // Assert
    Assert.NotNull(result);
    Assert.NotEmpty(result.Documents);
}
```

---

## Performance Considerations

### Lazy Loading
- Don't render Grid/Text/Parameters until tab is selected
- Use `IsGridLoaded`, `IsTextLoaded`, `IsParametersLoaded` flags
- Only load tree nodes on expansion (for large databases)

### Virtualization
```xaml
<!-- Enable virtualization for large result sets -->
<DataGrid VirtualizingPanel.IsVirtualizing="True"
          VirtualizingPanel.VirtualizationMode="Recycling" />

<TreeView VirtualizingPanel.IsVirtualizing="True" />
```

### Result Limiting
- Maintain 1000-row limit (configurable?)
- Show "LimitExceeded" indicator
- Suggest adding LIMIT clause to query

### Memory Management
- Dispose `LiteDatabase` properly
- Clear large result sets when switching tabs
- Use weak references for cached completion data

---

## Code Style & Patterns

### MVVM Best Practices
1. **No code-behind logic** - only view initialization
2. **Commands over events** - use `IRelayCommand`
3. **Services for I/O** - never in ViewModels
4. **Async by default** - use `IAsyncRelayCommand`
5. **Observable properties** - use `SetProperty()`

### Naming Conventions
- Commands: `{Verb}Command` (e.g., `RunCommand`, `ConnectCommand`)
- Async methods: `{Verb}Async` (e.g., `ExecuteAsync`, `LoadTreeAsync`)
- Properties: PascalCase (e.g., `IsConnected`, `SelectedTab`)
- Private fields: `_camelCase` (e.g., `_dbService`, `_currentTab`)

### Error Handling
```csharp
public async Task ExecuteAsync()
{
    try
    {
        StatusText = "Executing...";
        var result = await _dbService.ExecuteAsync(sql);
        SelectedTab.LastResult = result;
        SelectedTab.LastError = null;
    }
    catch (Exception ex)
    {
        SelectedTab.LastError = ex;
        StatusText = $"Error: {ex.Message}";
    }
}
```

---

## Migration Checklist

### Infrastructure
- [x] DI container setup
- [x] MVVM foundation
- [x] Service layer interface
- [x] Settings persistence
- [ ] Logging infrastructure (optional)

### Connection
- [x] Connect/Disconnect
- [x] Recent list
- [x] Auto-load last database
- [ ] Connection dialog
- [ ] Connection string validation

### Execution
- [ ] Execute SQL async
- [ ] Parameter binding
- [ ] Result limiting
- [ ] Cancellation support
- [ ] Execute selection only

### Results
- [ ] Grid view with BsonValue rendering
- [ ] Text view with JSON
- [ ] Parameters view
- [ ] Tab switching
- [ ] Cell editing
- [ ] Sort comparison

### Editor
- [ ] Syntax highlighting
- [ ] Code completion
- [ ] Keyboard shortcuts
- [ ] Caret position tracking
- [ ] Line numbers

### Database Explorer
- [ ] TreeView structure
- [ ] Load collections
- [ ] Context menus
- [ ] Double-click to insert
- [ ] Refresh command

### File Operations
- [ ] Load SQL file
- [ ] Save SQL file
- [ ] Filename tracking
- [ ] Modified state
- [ ] Save confirmation

### Transactions
- [ ] BEGIN command
- [ ] COMMIT command
- [ ] ROLLBACK command
- [ ] CHECKPOINT command

### Advanced
- [ ] Database debugger
- [ ] Custom tab close buttons
- [ ] Tab reordering
- [ ] About dialog

### Testing
- [ ] ViewModel unit tests
- [ ] Service integration tests
- [ ] UI automation tests (optional)
- [ ] Performance testing

---

## Resources

### Documentation
- **CommunityToolkit.Mvvm:** https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/
- **AvalonEdit:** https://github.com/icsharpcode/AvalonEdit
- **LiteDB:** https://www.litedb.org/

### Sample Code
- WinForms source: `LiteDB.Studio\Forms\MainForm.cs`
- Current WPF: `LiteDB.Studio.Wpf\ViewModels\MainViewModel.cs`

### Tools
- Visual Studio 2022
- LiteDB.Studio (for reference)
- .NET 9.0 SDK

---

## Conclusion

The WPF migration is well-positioned with solid architectural foundations. The DI container, MVVM pattern, and service layer provide a clean separation of concerns. The primary work ahead is implementing the execution engine, result display system, and database explorer.

**Estimated Completion:** 7 weeks (assuming 1 developer, part-time)

**Next Steps:**
1. Review and approve this plan
2. Begin Phase 1: Core Execution
3. Establish CI/CD pipeline (optional)
4. Set up unit test project
5. Create GitHub issues for tracking

**Success Criteria:**
- Feature parity with WinForms version
- All tests passing
- Performance equal or better than WinForms
- Clean, maintainable MVVM architecture
- Comprehensive documentation

---

*Document Version: 1.0*  
*Last Updated: January 14, 2026*
