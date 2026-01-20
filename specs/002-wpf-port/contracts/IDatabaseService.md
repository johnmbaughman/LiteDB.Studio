# IDatabaseService Contract

**Purpose**: Abstraction for all database lifecycle and operations; enables testability and decouples UI from LiteDB engine.

**Implementation**: `LiteDbService` (production); mocked for ViewModel unit tests

## Methods

### Connection Lifecycle

#### ConnectAsync
```
Signature: Task ConnectAsync(string connectionString, CancellationToken cancellationToken)
Purpose: Establish connection to database file
Preconditions: Not currently connected
Postconditions: IsConnected = true; connection handle acquired
Parameters:
  - connectionString: LiteDB connection string or file path
  - cancellationToken: Cancellation support
Returns: Task (void)
Errors:
  - FileNotFoundException: Database file not found
  - UnauthorizedAccessException: Insufficient permissions
  - InvalidOperationException: Already connected
  - LockedException: File locked by another process
```

#### DisconnectAsync
```
Signature: Task DisconnectAsync()
Purpose: Close connection and release database handles
Preconditions: Currently connected
Postconditions: IsConnected = false; resources released
Returns: Task (void)
Errors:
  - InvalidOperationException: Not connected
  - TransactionActiveException: Transaction in progress (must commit/rollback first)
```

#### IsConnected (Property)
```
Signature: bool IsConnected { get; }
Purpose: Query current connection state
Returns: true if connected; false otherwise
```

### Query Execution

#### ExecuteAsync
```
Signature: Task<QueryResult> ExecuteAsync(string query, CancellationToken cancellationToken)
Purpose: Execute SQL query and return structured result with metadata
Preconditions: IsConnected = true
Postconditions: QueryResult populated; transaction state may change if query contains BEGIN/COMMIT/ROLLBACK
Parameters:
  - query: LiteDB SQL query (SELECT, INSERT, UPDATE, DELETE, etc.)
  - cancellationToken: Cancellation support (abort query mid-execution)
Returns: QueryResult
  - Rows: result documents
  - Columns: column metadata derived from result schema
  - LimitExceeded: true if result truncated at configured row limit
  - RowCount: number of rows returned
  - ExecutionTime: elapsed query time
  - Warnings: any engine warnings
  - Metadata: extensible metadata (e.g., affected rows for UPDATE/DELETE)
Errors:
  - InvalidOperationException: Not connected
  - LiteDbSyntaxException: Invalid SQL syntax
  - OperationCanceledException: Cancelled via cancellationToken
  - LiteDbException: Engine error (e.g., constraint violation, disk full)
```

### Schema Discovery

#### GetCollectionNamesAsync
```
Signature: Task<IEnumerable<string>> GetCollectionNamesAsync(CancellationToken cancellationToken)
Purpose: Retrieve list of user collections (excludes system collections)
Preconditions: IsConnected = true
Returns: Collection names (may be empty)
Errors:
  - InvalidOperationException: Not connected
  - OperationCanceledException: Cancelled
```

#### GetSystemCollectionNamesAsync
```
Signature: Task<IEnumerable<string>> GetSystemCollectionNamesAsync(CancellationToken cancellationToken)
Purpose: Retrieve list of system collections (_chunks, _files, etc.)
Preconditions: IsConnected = true
Returns: System collection names (may be empty)
Errors:
  - InvalidOperationException: Not connected
  - OperationCanceledException: Cancelled
```

#### GetCollectionSchemaAsync
```
Signature: Task<IEnumerable<ColumnInfo>> GetCollectionSchemaAsync(string collectionName, CancellationToken cancellationToken)
Purpose: Infer schema (field names and types) from collection documents
Preconditions: IsConnected = true; collection exists
Parameters:
  - collectionName: Target collection
  - cancellationToken: Cancellation support
Returns: Collection of ColumnInfo (field names, BSON types, optional display hints)
  - May sample first N documents to infer schema
  - Returns empty if collection is empty
Errors:
  - InvalidOperationException: Not connected
  - CollectionNotFoundException: Collection does not exist
  - OperationCanceledException: Cancelled
```

### Document Updates

#### UpdateDocumentFieldAsync
```
Signature: Task UpdateDocumentFieldAsync(string collectionName, object documentId, string fieldPath, object newValue, CancellationToken cancellationToken)
Purpose: Update a single field in a document (triggered by grid cell edit)
Preconditions: IsConnected = true; document exists
Parameters:
  - collectionName: Target collection
  - documentId: Document _id (typically ObjectId or int)
  - fieldPath: Dot-notation field path (e.g., "name" or "address.city")
  - newValue: New field value (will be converted to appropriate BSON type)
  - cancellationToken: Cancellation support
Postconditions: Document updated in database
Returns: Task (void)
Errors:
  - InvalidOperationException: Not connected
  - DocumentNotFoundException: Document with specified _id not found
  - FieldPathException: Invalid field path syntax
  - TypeConversionException: newValue cannot be converted to target BSON type
  - OperationCanceledException: Cancelled
```

### Transactions

#### BeginTransactionAsync
```
Signature: Task BeginTransactionAsync(CancellationToken cancellationToken)
Purpose: Start a new transaction
Preconditions: IsConnected = true; no active transaction
Postconditions: TransactionActive = true
Returns: Task (void)
Errors:
  - InvalidOperationException: Not connected or transaction already active
```

#### CommitTransactionAsync
```
Signature: Task CommitTransactionAsync(CancellationToken cancellationToken)
Purpose: Commit active transaction
Preconditions: TransactionActive = true
Postconditions: TransactionActive = false; changes persisted
Returns: Task (void)
Errors:
  - InvalidOperationException: No active transaction
  - LiteDbException: Commit failure (e.g., constraint violation)
```

#### RollbackTransactionAsync
```
Signature: Task RollbackTransactionAsync(CancellationToken cancellationToken)
Purpose: Rollback active transaction
Preconditions: TransactionActive = true
Postconditions: TransactionActive = false; changes reverted
Returns: Task (void)
Errors:
  - InvalidOperationException: No active transaction
```

#### CheckpointAsync
```
Signature: Task CheckpointAsync(CancellationToken cancellationToken)
Purpose: Write transaction log to disk (flush WAL)
Preconditions: IsConnected = true
Returns: Task (void)
Errors:
  - InvalidOperationException: Not connected
  - IOException: Disk write failure
```

#### TransactionActive (Property)
```
Signature: bool TransactionActive { get; }
Purpose: Query current transaction state
Returns: true if transaction in progress; false otherwise
```

## Events

### ConnectionStateChanged
```
Signature: event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged
Purpose: Notify subscribers when connection state changes (connected/disconnected)
Event Args:
  - IsConnected: new connection state
Usage: MainViewModel subscribes to update IsConnected property and refresh Tree
```

### TransactionStateChanged
```
Signature: event EventHandler<TransactionStateChangedEventArgs> TransactionStateChanged
Purpose: Notify subscribers when transaction state changes
Event Args:
  - TransactionActive: new transaction state
Usage: MainViewModel subscribes to update TransactionActive property and enable/disable commands
```

## Implementation Notes

### LiteDbService (Production)
- Wraps LiteDB `LiteDatabase` instance
- Connection string supports file paths and connection options (read-only, upgrade, etc.)
- ExecuteAsync uses LiteDB `BsonDataReader` to enumerate results and build QueryResult
- Row limit enforced during enumeration (configurable; default 1000)
- Schema inference samples first 100 documents and merges field sets
- UpdateDocumentFieldAsync uses LiteDB `Update` with field setter
- Transactions map to LiteDB `BeginTrans`/`Commit`/`Rollback`
- Dispose releases LiteDatabase handle

### Mock (Unit Tests)
- Use NSubstitute to create mock IDatabaseService
- Mock setup returns predefined QueryResult instances for testing ViewModel logic
- Mock verifies that commands invoke service methods with correct parameters

## Contract Validation

- All async methods accept `CancellationToken` for cancellation support
- All methods throw descriptive exceptions with actionable messages
- Connection state enforced via `IsConnected` property checks
- Transaction state enforced via `TransactionActive` property checks
- QueryResult shape matches data-model.md definition

## Acceptance Criteria

- [ ] ConnectAsync establishes connection and sets IsConnected = true
- [ ] DisconnectAsync releases resources and sets IsConnected = false
- [ ] ExecuteAsync returns QueryResult with Rows, Columns, ExecutionTime, LimitExceeded
- [ ] Cancellation via CancellationToken aborts query and releases resources
- [ ] GetCollectionNamesAsync returns user collections; GetSystemCollectionNamesAsync returns system collections
- [ ] GetCollectionSchemaAsync infers schema from collection documents
- [ ] UpdateDocumentFieldAsync updates document field in database
- [ ] BeginTransactionAsync/CommitTransactionAsync/RollbackTransactionAsync manage transaction lifecycle
- [ ] CheckpointAsync flushes WAL to disk
- [ ] Events fire when connection or transaction state changes
