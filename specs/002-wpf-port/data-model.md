# Phase 1: Data Model

**Feature**: WPF Port — LiteDB.Studio Migration  
**Date**: 2026-01-19  
**Phase**: 1 — Design & Contracts

## Overview

This document defines the domain entities, data structures, and state management for the WPF port. The application manages editor tabs, query results, database connections, and tree node state.

## Core Entities

### 1. QueryResult

**Purpose**: Structured container for query execution results and metadata

**Fields**:
| Field | Type | Description | Validation Rules |
|-------|------|-------------|-----------------|
| Rows | Collection of result documents | Ordered result set | Non-null; may be empty |
| Columns | Collection of column metadata | Column names, types, display hints | Non-null; derived from result schema |
| LimitExceeded | Boolean | Result truncated due to row limit | Read-only; set by service |
| RowCount | Integer | Number of rows returned | >= 0; <= configured limit |
| ExecutionTime | TimeSpan | Query execution duration | >= 0 |
| Warnings | Collection of strings | Service/engine warnings | May be empty |
| Metadata | Key-value dictionary | Extensible metadata | Optional; may be null |

**State Transitions**: Immutable result; created once per query execution

**Relationships**:
- Referenced by `TabViewModel.LastResult`
- Rows contain BSON documents (LiteDB `BsonDocument` type)
- Columns contain `ColumnInfo` descriptors

### 2. ColumnInfo

**Purpose**: Metadata for result grid column rendering

**Fields**:
| Field | Type | Description | Validation Rules |
|-------|------|-------------|-----------------|
| Name | String | Column/field name | Non-empty; unique within result |
| BsonType | Enum | BSON type family (String, Int, Document, etc.) | Valid BsonType enum value |
| DisplayFormat | String (optional) | Formatting hint (e.g., "ISO8601", "hex") | May be null |

**Relationships**:
- Collection contained in `QueryResult.Columns`
- Used by grid column generator and `BsonValueToStringConverter`

### 3. TabViewModel

**Purpose**: State for an editor/result tab (query editor, last result, modified tracking)

**Fields**:
| Field | Type | Description | Validation Rules |
|-------|------|-------------|-----------------|
| Title | String | Display title for tab | Non-empty |
| Filename | String (optional) | Path to saved SQL file | Valid file path or null |
| IsModified | Boolean | Unsaved changes indicator | Set to true on EditorText change |
| EditorText | String | SQL editor content | May be empty |
| CaretOffset | Integer | Caret position in editor | >= 0; <= EditorText.Length |
| SelectionStart | Integer | Selection start offset | >= 0 |
| SelectionLength | Integer | Selection length | >= 0 |
| LastResult | QueryResult (optional) | Most recent query result | May be null |
| LastError | String (optional) | Last execution error message | May be null |
| IsResultLoaded | Boolean | Lazy-load flag for result view | Initial: false |

**State Transitions**:
- New tab: `IsModified = false`, `EditorText = ""`, `LastResult = null`
- After edit: `IsModified = true`
- After save: `IsModified = false`, `Filename` set
- After query: `LastResult` populated or `LastError` set

**Relationships**:
- Owned by `MainViewModel.Tabs` collection
- References `QueryResult` via `LastResult`

### 4. MainViewModel

**Purpose**: Root application state (tabs, connection, database tree, global commands)

**Fields**:
| Field | Type | Description | Validation Rules |
|-------|------|-------------|-----------------|
| Tabs | Observable collection of TabViewModel | Open tabs | Non-null; may be empty |
| SelectedTab | TabViewModel (optional) | Currently active tab | Must exist in Tabs or be null |
| IsConnected | Boolean | Database connection state | Read-only; derived from service |
| CurrentDatabase | String (optional) | Connected DB path | Valid file path or null |
| Tree | DatabaseTreeViewModel | DB explorer root | Non-null |
| TransactionActive | Boolean | Transaction in progress | Read-only; derived from service |

**State Transitions**:
- Startup: `IsConnected = false`, `Tabs = []`, `Tree = empty`
- After connect: `IsConnected = true`, `CurrentDatabase` set, `Tree` populated
- After disconnect: `IsConnected = false`, `Tree` cleared

**Relationships**:
- Owns collection of `TabViewModel`
- Owns `DatabaseTreeViewModel`
- Delegates DB operations to `IDatabaseService`

### 5. DatabaseTreeViewModel / DbTreeNode

**Purpose**: Hierarchical database schema representation (collections, indexes, system collections)

**Fields (DbTreeNode)**:
| Field | Type | Description | Validation Rules |
|-------|------|-------------|-----------------|
| Header | String | Display text | Non-empty |
| Tag | Object | Identity/metadata (collection name, node type) | May be null |
| IconUri | String | Pack URI to icon resource | Valid pack URI or null |
| Children | Observable collection of DbTreeNode | Child nodes | Non-null; may be empty |
| IsLoaded | Boolean | Lazy-load state | Initial: false |
| IsExpanded | Boolean | UI expansion state | Bindable |

**State Transitions**:
- Initial: `IsLoaded = false`, `Children = []` or placeholder
- After expand: `IsLoaded = true`, `Children` populated via service
- After refresh: `IsLoaded = false`, `Children` cleared

**Relationships**:
- Tree structure: parent → children
- Root owned by `MainViewModel.Tree`
- Node context actions reference `MainViewModel` commands (Drop, Export, etc.)

## Validation Rules Summary

| Entity | Key Validation |
|--------|---------------|
| QueryResult | RowCount >= 0; Rows non-null; ExecutionTime >= 0 |
| ColumnInfo | Name non-empty; unique within result |
| TabViewModel | Title non-empty; CaretOffset/SelectionStart/Length >= 0 |
| MainViewModel | SelectedTab exists in Tabs or is null |
| DbTreeNode | Header non-empty; Children non-null |

## State Management Patterns

### Observable Properties
- Use `[ObservableProperty]` source generator for simple properties
- Use `ObservableCollection<T>` for collections bound to UI (Tabs, Children)
- Notify property changes for computed properties (e.g., `IsConnected` when service connection state changes)

### Command State Management
- Commands update ViewModel state atomically (set LastResult OR LastError, never both)
- Async commands use `IAsyncRelayCommand` with automatic busy state tracking
- Commands validate preconditions (e.g., `ConnectCommand.CanExecute` checks if already connected)

### Error State
- Errors stored in `TabViewModel.LastError` as user-friendly strings
- Clearing state: set `LastError = null` before new operation
- Multiple error sources: connection errors in `MainViewModel`, query errors in `TabViewModel`

### Lazy Loading
- Tree nodes: `IsLoaded` flag prevents redundant service calls
- Result views: `IsResultLoaded` flag enables deferred rendering for large grids

## Relationships Diagram

```
MainViewModel
├── Tabs: ObservableCollection<TabViewModel>
│   └── TabViewModel
│       ├── LastResult: QueryResult?
│       │   ├── Rows: IEnumerable<BsonDocument>
│       │   └── Columns: ColumnInfo[]
│       └── LastError: string?
├── SelectedTab: TabViewModel?
├── Tree: DatabaseTreeViewModel
│   └── RootNodes: ObservableCollection<DbTreeNode>
│       └── DbTreeNode (recursive)
│           └── Children: ObservableCollection<DbTreeNode>
└── [IDatabaseService] (injected dependency)
```

## Persistence & Serialization

- **Editor state**: Not persisted; users explicitly save SQL files via `SaveFileCommand`
- **Application preferences**: Row limit, window size, recent files (stored in user settings; out of scope for this spec)
- **Database files**: User-provided LiteDB files; read/write via `IDatabaseService`

## Concurrency & Thread Safety

- **UI thread**: All ViewModel property updates occur on UI thread (SynchronizationContext)
- **Background operations**: Service calls (`ExecuteAsync`, `LoadChildrenAsync`) run on thread pool
- **ConfigureAwait**: Service layer uses `ConfigureAwait(false)` to avoid SynchronizationContext overhead; ViewModels use default (true) to marshal updates to UI thread

## Edge Cases & Constraints

- **Empty results**: `Rows = []`, `RowCount = 0`, `Columns = []` (valid state)
- **Null results**: `LastResult = null` indicates no execution yet or cleared state
- **Large result sets**: `LimitExceeded = true`; UI shows indicator; rows truncated at configured limit
- **Schema mismatch**: Result documents may have varying fields; `Columns` derived from first N rows or explicit schema; missing fields render as null/empty
- **Concurrent tab operations**: Each `TabViewModel` operates independently; no shared query state between tabs
- **Transaction scope**: `TransactionActive` flag prevents conflicting operations (e.g., disable Connect during transaction)

## Summary

Core entities defined: `QueryResult`, `ColumnInfo`, `TabViewModel`, `MainViewModel`, `DbTreeNode`. All entities support MVVM observable patterns, validation rules enforce data integrity, and state transitions align with user workflows. Ready to generate service contracts.
