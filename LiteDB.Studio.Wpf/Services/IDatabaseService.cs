namespace LiteDB.Studio.Wpf.Services;

/// <summary>
/// Defines the contract for a LiteDB database service, exposing connection management,
/// query execution, schema discovery, transaction control, and state-change events.
/// </summary>
public interface IDatabaseService : IDisposable, IAsyncDisposable
{
    /// <summary>Gets a value indicating whether a database connection is currently open.</summary>
    bool IsConnected { get; }

    /// <summary>Gets the underlying database object, or <c>null</c> when not connected.</summary>
    object? Database { get; }

    /// <summary>Gets a value indicating whether a transaction is currently active.</summary>
    bool TransactionActive { get; }

    /// <summary>Opens a connection to the database identified by <paramref name="connectionString"/>.</summary>
    /// <param name="connectionString">LiteDB connection string or file path.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task ConnectAsync(string connectionString, CancellationToken cancellationToken);

    /// <summary>Closes the active database connection asynchronously.</summary>
    Task DisconnectAsync();

    /// <summary>Closes the active database connection synchronously.</summary>
    void Disconnect();

    /// <summary>Executes a SQL query and returns the result set.</summary>
    /// <param name="query">The SQL statement to execute.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A <see cref="QueryResult"/> containing rows, columns, and metadata.</returns>
    Task<QueryResult> ExecuteAsync(string? query, CancellationToken cancellationToken);

    /// <summary>Returns the names of all user collections in the database.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<IEnumerable<string>> GetCollectionNamesAsync(CancellationToken cancellationToken);

    /// <summary>Returns the names of all system collections in the database.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<IEnumerable<string>> GetSystemCollectionNamesAsync(CancellationToken cancellationToken);

    /// <summary>Samples the collection to infer a schema (field names and BSON types).</summary>
    /// <param name="collectionName">Name of the collection to inspect.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<IEnumerable<ColumnInfo>> GetCollectionSchemaAsync(string collectionName, CancellationToken cancellationToken);

    /// <summary>Updates a single field of a document identified by <paramref name="documentId"/>.</summary>
    /// <param name="collectionName">Name of the collection containing the document.</param>
    /// <param name="documentId">The document's <c>_id</c> value.</param>
    /// <param name="fieldPath">Dot-separated path to the field to update.</param>
    /// <param name="newValue">The new value to assign.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task UpdateDocumentFieldAsync(string collectionName, object documentId, string fieldPath, object? newValue, CancellationToken cancellationToken);

    /// <summary>Begins a new database transaction.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task BeginTransactionAsync(CancellationToken cancellationToken);

    /// <summary>Commits the active database transaction.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task CommitTransactionAsync(CancellationToken cancellationToken);

    /// <summary>Rolls back the active database transaction.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task RollbackTransactionAsync(CancellationToken cancellationToken);

    /// <summary>Flushes pending write-ahead log pages to the data file.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task CheckpointAsync(CancellationToken cancellationToken);

    /// <summary>Raised when the database connection state changes (connected or disconnected).</summary>
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <summary>Raised when the transaction state changes (begun or ended).</summary>
    event EventHandler<TransactionStateChangedEventArgs>? TransactionStateChanged;
}
