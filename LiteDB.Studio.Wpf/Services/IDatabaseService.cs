using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Studio.Wpf.Services
{
    public interface IDatabaseService : IDisposable
    {
        bool IsConnected { get; }

        object? Database { get; }

        bool TransactionActive { get; }

        Task ConnectAsync(string connectionString, CancellationToken cancellationToken);

        Task DisconnectAsync();

        void Disconnect();

        Task<QueryResult> ExecuteAsync(string query, CancellationToken cancellationToken);

        Task<IEnumerable<string>> GetCollectionNamesAsync(CancellationToken cancellationToken);

        Task<IEnumerable<string>> GetSystemCollectionNamesAsync(CancellationToken cancellationToken);

        Task<IEnumerable<ColumnInfo>> GetCollectionSchemaAsync(string collectionName, CancellationToken cancellationToken);

        Task UpdateDocumentFieldAsync(string collectionName, object documentId, string fieldPath, object? newValue, CancellationToken cancellationToken);

        Task BeginTransactionAsync(CancellationToken cancellationToken);

        Task CommitTransactionAsync(CancellationToken cancellationToken);

        Task RollbackTransactionAsync(CancellationToken cancellationToken);

        Task CheckpointAsync(CancellationToken cancellationToken);

        event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

        event EventHandler<TransactionStateChangedEventArgs>? TransactionStateChanged;
    }
}
