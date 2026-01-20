# Phase 0: Research & Technology Decisions

**Feature**: WPF Port — LiteDB.Studio Migration  
**Date**: 2026-01-19  
**Phase**: 0 — Outline & Research

## Research Tasks

All NEEDS CLARIFICATION items from Technical Context have been resolved through existing repository analysis and spec requirements. This document records key technology decisions and best practices.

## Decision Log

### 1. MVVM Framework Selection

**Decision**: CommunityToolkit.Mvvm (formerly Microsoft.Toolkit.Mvvm)

**Rationale**:
- Official Microsoft MVVM toolkit with active support
- Source generators reduce boilerplate (`ObservableObject`, `RelayCommand`, `ObservableProperty`)
- Async command support via `IAsyncRelayCommand` for database operations
- Lightweight and well-documented
- Already specified in spec constitution

**Alternatives Considered**:
- Prism: More heavyweight; includes navigation/modularity not needed for single-window app
- MVVMLight: Legacy; no longer actively maintained
- ReactiveUI: Steep learning curve; reactive paradigm not required for this migration

**Implementation Notes**:
- Use `[ObservableProperty]` source generator for simple properties
- Use `IAsyncRelayCommand<T>` for async operations (ExecuteAsync, Connect, etc.)
- Use `ObservableCollection<T>` for collections bound to UI (Tabs, tree nodes)

### 2. SQL Editor Component

**Decision**: AvalonEdit

**Rationale**:
- Mature WPF text editor with syntax highlighting
- Extensible completion/IntelliSense support
- Good performance for large documents
- Already used in existing LiteDB.Studio.Wpf partial implementation

**Alternatives Considered**:
- ICSharpCode.TextEditor: Legacy WinForms component (current implementation uses this); not WPF-native
- Custom WPF TextBox: Insufficient feature set for code editing
- Monaco Editor (web-based): Requires embedding browser; adds complexity

**Implementation Notes**:
- Create custom `ICompletionData` provider for SQL keywords, collection names, and functions
- Bind editor text bidirectionally to `TabViewModel.EditorText`
- Track caret position for "run selection" behavior
- Use syntax highlighting definition for SQL

### 3. Result Grid Virtualization

**Decision**: WPF DataGrid with VirtualizingStackPanel

**Rationale**:
- Built-in WPF DataGrid supports UI virtualization out of the box
- `VirtualizingStackPanel.IsVirtualizing="True"` handles large row counts efficiently
- Supports custom column templates for BSON rendering
- Editable cells map cleanly to update operations

**Alternatives Considered**:
- Custom ItemsControl: More control but reinvents built-in virtualization
- Third-party grids (DevExpress, Telerik): Licensing cost; adds external dependency

**Implementation Notes**:
- Enable virtualization: `VirtualizingStackPanel.IsVirtualizing="True"`
- Use `ItemsSource` binding to `QueryResult.Rows` (IEnumerable<BsonDocument>)
- Generate columns dynamically from `QueryResult.Columns` metadata
- Use `BsonValueToStringConverter` for cell rendering
- Handle `CellEditEnding` event to invoke `UpdateDocumentFieldAsync`

### 4. Async/Await Patterns for DB Operations

**Decision**: Use async/await throughout; expose CancellationToken in service methods

**Rationale**:
- LiteDB engine supports async operations
- Prevents UI freezes during long-running queries
- `IAsyncRelayCommand` integrates cancellation tokens automatically
- Aligns with modern C# async patterns

**Best Practices**:
- All `IDatabaseService` methods return `Task` or `Task<T>`
- Accept `CancellationToken` parameter for cancellable operations
- Use `ConfigureAwait(false)` in service layer to avoid SynchronizationContext overhead
- ViewModels use `IAsyncRelayCommand.ExecuteAsync` with automatic cancellation support
- Surface cancellation as distinct outcome from error (e.g., `LastError = "Cancelled by user"` vs exception message)

### 5. BSON Rendering Strategy

**Decision**: Custom `BsonValueToStringConverter` with type-aware rendering

**Rationale**:
- BSON types (Document, Array, Binary, ObjectId, DateTime, etc.) require specialized display
- WPF converters allow declarative binding in XAML
- Centralized conversion logic ensures consistency across grid, tree, and detail views

**Implementation Notes**:
- Implement `IValueConverter` to convert `BsonValue` → `string`
- Handle special types: ObjectId (hex), DateTime (ISO8601), Binary (base64 preview), nested Documents/Arrays (JSON-like preview)
- Provide formatting hints from `ColumnInfo.DisplayFormat` if present
- Truncate large strings/arrays with ellipsis and tooltip for full value

### 6. Tree Lazy Loading Pattern

**Decision**: Async `LoadChildrenAsync()` on `DbTreeNode` with `IsLoaded` flag

**Rationale**:
- Database schema discovery (collections, indexes) can be slow for large DBs
- Lazy loading improves initial tree rendering performance
- Users expand nodes on-demand

**Best Practices**:
- `DbTreeNode.Children` initialized to empty or placeholder ("Loading...")
- On first expand, call `await LoadChildrenAsync()` which invokes service methods (`GetCollectionNames`, `GetCollectionSchemaAsync`)
- Set `IsLoaded = true` to prevent redundant loads
- Use `IAsyncRelayCommand` for context menu actions (Drop, Export) to support cancellation

### 7. Testing Strategy

**Decision**: Unit tests for ViewModels with mocked IDatabaseService; integration tests for LiteDbService with ephemeral DB files

**Rationale**:
- ViewModels contain UI logic and command orchestration; mock database to isolate ViewModel behavior
- LiteDbService requires real LiteDB engine to validate query execution, schema discovery, and transaction behavior
- Separation ensures fast unit tests and comprehensive integration coverage

**Test Framework**: xUnit (align with existing `LiteDB.Tests` project)  
**Mocking**: NSubstitute for `IDatabaseService` mocks

**Unit Test Pattern** (ViewModels):
```csharp
// Arrange: mock IDatabaseService
var mockService = Substitute.For<IDatabaseService>();
mockService.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
    .Returns(new QueryResult { Rows = ..., RowCount = 5 });
var viewModel = new TabViewModel(mockService);

// Act: execute command
await viewModel.RunCommand.ExecuteAsync(null);

// Assert: verify ViewModel state
Assert.NotNull(viewModel.LastResult);
Assert.Equal(5, viewModel.LastResult.RowCount);
Assert.Null(viewModel.LastError);
```

**Integration Test Pattern** (LiteDbService):
```csharp
// Arrange: create ephemeral DB
var tempFile = Path.GetTempFileName();
var service = new LiteDbService();
await service.ConnectAsync(tempFile);

// Act: execute real query
var result = await service.ExecuteAsync("db.testCollection.insert({name: 'Alice'})", CancellationToken.None);

// Assert: verify result and DB state
Assert.Equal(1, result.RowCount);
var schema = await service.GetCollectionSchemaAsync("testCollection");
Assert.Contains(schema, c => c.Name == "name");

// Cleanup
File.Delete(tempFile);
```

### 8. Error Handling & User Feedback

**Decision**: Surface structured errors in `TabViewModel.LastError`; use confirmation dialogs for destructive actions

**Rationale**:
- Users need actionable error messages (connection failures, syntax errors, locked files)
- Destructive operations (DROP, DELETE) require explicit confirmation to prevent accidental data loss
- Cancellation should be surfaced as informational, not error

**Best Practices**:
- Catch exceptions in ViewModels; set `LastError` with user-friendly message
- For locked files: "Database file is locked. Close other connections and retry."
- For syntax errors: Include LiteDB parser error message
- Confirmation dialogs: Use `MessageBox` or custom dialog; describe operation scope; require typed confirmation for bulk actions
- Modifier behavior (Shift+Delete): bypass confirmation but show clear affordance

### 9. Resource Management & Pack URIs

**Decision**: Store icons/images under `LiteDB.Studio.Wpf/Resources/Icons/` and reference via pack URIs

**Rationale**:
- WPF pack URIs enable compile-time resource embedding and XAML binding
- Centralized resource folder maintains organization
- Constitution requires this pattern

**Pack URI Format**:
```
pack://application:,,,/LiteDB.Studio.Wpf;component/Resources/Icons/database.png
```

**Implementation Notes**:
- Set Build Action = "Resource" for icon files in .csproj
- Bind to `DbTreeNode.IconUri` property (type: `Uri` or `string`)
- Use `BitmapImage` in XAML `Image` controls

### 10. Performance Targets & Monitoring

**Decision**: Target <1s for queries <1000 rows; use Stopwatch to measure `ExecutionTime`

**Rationale**:
- Spec defines measurable success criteria (95% of queries <1s)
- `QueryResult.ExecutionTime` provides user feedback and diagnostic data
- Performance monitoring enables future optimization

**Implementation Notes**:
- Start `Stopwatch` before LiteDB query execution
- Stop after result enumeration complete
- Store in `QueryResult.ExecutionTime`
- Display in status bar or result metadata panel
- Log slow queries (>3s) for telemetry if configured

## Summary

All technical unknowns have been resolved through:
- Repository analysis (existing WPF project structure, constitution requirements)
- Spec requirements (MVVM-first, IDatabaseService abstraction, CommunityToolkit.Mvvm)
- Best practices research (AvalonEdit, DataGrid virtualization, async patterns, testing strategies)

No additional clarification or research required. Ready to proceed to Phase 1 (Design & Contracts).
