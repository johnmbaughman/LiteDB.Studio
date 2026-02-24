// unset:none

using System.Diagnostics;
using Microsoft.Extensions.Options;
using Serilog;

namespace LiteDB.Studio.Wpf.Services;

public class LiteDbService : IDatabaseService, IAsyncDisposable
{
    private readonly int _maxRows;
    private LiteDatabase? _db;
    private readonly Lock _sync = new();

    public LiteDbService(IOptions<LiteDbOptions>? options = null)
    {
        _maxRows = options?.Value.MaxRows ?? LiteDbOptions.DefaultMaxRows;
    }

    public bool IsConnected => _db != null;

    public bool TransactionActive { get; private set; }

    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    public event EventHandler<TransactionStateChangedEventArgs>? TransactionStateChanged;

    public void Dispose()
    {
        // Perform synchronous disconnect without blocking on async calls
        DoDisconnect();
    }

    public async ValueTask DisposeAsync()
    {
        // Support async disposal for callers who want to await it
        await DisconnectAsync().ConfigureAwait(false);
    }

    public object? Database => _db;

    public void Disconnect()
    {
        // Synchronous disconnect
        DoDisconnect();
    }

    private void DoDisconnect()
    {
        if (!IsConnected)
        {
            return;
        }

        if (TransactionActive)
        {
            throw new InvalidOperationException("Transaction in progress (must commit or rollback before disconnecting).");
        }

        lock (_sync)
        {
            try
            {
                _db?.Dispose();
            }
            finally
            {
                _db = null;
                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(false));
            }
        }
    }

    public Task ConnectAsync(string connectionString, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsConnected)
        {
            throw new InvalidOperationException("Already connected");
        }

        try
        {
            // Lightweight connect: accept a file path or a LiteDB connection string
            _db = new LiteDatabase(connectionString);

            // Log discovered collections immediately for diagnostics
            try
            {
                var names = _db.GetCollectionNames().ToArray();
                Log.Information("Connected to LiteDB. Connection string: {Conn}. Collections found: {Count}", connectionString, names.Length);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Connected to LiteDB but failed to enumerate collections");
            }

            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(true));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to connect to database with connection string {Conn}", connectionString);
            throw;
        }

        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        // Reuse synchronous disconnect implementation for now. This keeps the method fast and avoids sync-over-async.
        DoDisconnect();
        return Task.CompletedTask;
    }

    public Task<QueryResult> ExecuteAsync(string? query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using IBsonDataReader reader = _db!.Execute(query);
            var columns = new List<ColumnInfo>();
            var rows = new List<object>();
            var rowCount = 0;
            var limitExceeded = false;

            // Read first row to infer columns
            if (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var firstDoc = reader.Current as BsonDocument;
                if (firstDoc != null)
                {
                    columns.AddRange(
                        firstDoc.Keys.Select(key => new ColumnInfo
                        {
                            Name = key,
                            BsonType = firstDoc[key]?.Type.ToString() ?? "Null",
                            DisplayFormat = null
                        }));
                    rows.Add(firstDoc);
                    rowCount++;
                }
            }

            // Read remaining rows up to limit
            while (reader.Read() && rowCount < _maxRows)
            {
                var doc = reader.Current as BsonDocument;
                if (doc != null)
                {
                    rows.Add(doc);
                    rowCount++;
                }
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (reader.Read())
            {
                limitExceeded = true;
            }

            stopwatch.Stop();

            return Task.FromResult(new QueryResult
            {
                Rows = rows,
                Columns = columns,
                LimitExceeded = limitExceeded,
                RowCount = rowCount,
                ExecutionTime = stopwatch.Elapsed,
                Warnings = [],
                Metadata = new Dictionary<string, object?>()
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            throw new InvalidOperationException($"Query execution failed: {ex.Message}", ex);
        }
    }

    public Task<IEnumerable<string>> GetCollectionNamesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected");
        }

        var names = _db!.GetCollectionNames().ToArray();
        Log.Information("GetCollectionNamesAsync returning {Count} collections", names.Length);
        return Task.FromResult<IEnumerable<string>>(names);
    }

    public Task<IEnumerable<string>> GetSystemCollectionNamesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected");
        }

        try
        {
            // Try reading $cols system collection which records system collections with type='system'
            ILiteCollection<BsonDocument> sysCol = _db!.GetCollection("$cols");
            if (sysCol != null)
            {
                try
                {
                    IEnumerable<BsonDocument> docs = sysCol.Query().Where("type = 'system'").OrderBy("name").ToDocuments();
                    var names = docs.Select(d => d["name"].AsString).ToArray();
                    if (names.Length > 0)
                    {
                        Log.Debug("GetSystemCollectionNamesAsync returning {Count} system collections from $cols: {Names}", names.Length, string.Join(", ", names));
                        return Task.FromResult<IEnumerable<string>>(names);
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Failed to read $cols for system collections, falling back to collection name prefix detection");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Error while trying to determine system collection names from $cols");
        }

        // Fallback: Return system collections (those starting with "$" or "_"). Some system collections in LiteDB start with '$' (e.g. $cols, $indexes)
        var namesFallback = _db!.GetCollectionNames().Where(name => name.StartsWith("$") || name.StartsWith("_")).ToArray();
        Log.Debug("GetSystemCollectionNamesAsync returning {Count} system collections (fallback): {Names}", namesFallback.Length, string.Join(", ", namesFallback));
        return Task.FromResult<IEnumerable<string>>(namesFallback);
    }

    public Task<IEnumerable<ColumnInfo>> GetCollectionSchemaAsync(string collectionName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected");
        }

        ILiteCollection<BsonDocument> collection = _db!.GetCollection(collectionName);
        var schema = new Dictionary<string, string>();

        // Sample first 100 documents to infer schema
        IEnumerable<BsonDocument> documents = collection.Find(Query.All(), 0, 100);
        foreach (BsonDocument doc in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var key in doc.Keys)
            {
                if (schema.ContainsKey(key)) { continue; }

                BsonValue value = doc[key];
                schema[key] = value?.Type.ToString() ?? "Null";
            }
            cancellationToken.ThrowIfCancellationRequested();
        }

        IOrderedEnumerable<ColumnInfo> columns = schema.Select(kvp => new ColumnInfo
        {
            Name = kvp.Key,
            BsonType = kvp.Value,
            DisplayFormat = null
        }).OrderBy(c => c.Name);

        return Task.FromResult<IEnumerable<ColumnInfo>>(columns);
    }

    public Task UpdateDocumentFieldAsync(string collectionName, object documentId, string fieldPath, object? newValue, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected");
        }

        ILiteCollection<BsonDocument> collection = _db!.GetCollection(collectionName);
        var id = new BsonValue(documentId);

        // Find the document
        BsonDocument doc = collection.FindById(id);
        if (doc == null)
        {
            throw new InvalidOperationException($"Document with id {documentId} not found in collection {collectionName}");
        }

        // Convert newValue to BsonValue
        BsonValue bsonValue = newValue != null ? new BsonValue(newValue) : BsonValue.Null;

        // For now, assume top-level field (fieldPath without dots)
        if (fieldPath.Contains('.'))
        {
            throw new NotImplementedException("Nested field paths are not yet supported");
        }

        doc[fieldPath] = bsonValue;

        // Update the document
        var updated = collection.Update(doc);
        return !updated
            ? throw new InvalidOperationException("Failed to update document")
            : Task.CompletedTask;
    }

    public Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected");
        }

        if (TransactionActive)
        {
            throw new InvalidOperationException("Transaction already active");
        }

        _db!.BeginTrans();
        TransactionActive = true;
        TransactionStateChanged?.Invoke(this, new TransactionStateChangedEventArgs(true));
        return Task.CompletedTask;
    }

    public Task CommitTransactionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected");
        }

        if (!TransactionActive)
        {
            throw new InvalidOperationException("No active transaction");
        }

        _db!.Commit();
        TransactionActive = false;
        TransactionStateChanged?.Invoke(this, new TransactionStateChangedEventArgs(false));
        return Task.CompletedTask;
    }

    public Task RollbackTransactionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected");
        }

        if (!TransactionActive)
        {
            throw new InvalidOperationException("No active transaction");
        }

        _db!.Rollback();
        TransactionActive = false;
        TransactionStateChanged?.Invoke(this, new TransactionStateChangedEventArgs(false));
        return Task.CompletedTask;
    }

    public Task CheckpointAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected");
        }

        _db!.Checkpoint();
        return Task.CompletedTask;
    }
}
