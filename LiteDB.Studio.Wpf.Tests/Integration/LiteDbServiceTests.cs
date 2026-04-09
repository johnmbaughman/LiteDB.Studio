using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiteDB.Studio.Wpf.Services;
using LiteDB.Studio.Wpf.ViewModels;
using NSubstitute;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.Integration;

/// <summary>Integration tests for <see cref="LiteDbService"/> using an in-memory LiteDB database.</summary>
public class LiteDbServiceTests : IDisposable
{
    private readonly LiteDbService _service = new();

    /// <summary>Disposes the shared <see cref="LiteDbService"/> after each test.</summary>
    public void Dispose()
    {
        _service.Dispose();
    }

    [Fact]
    public async Task ConnectAsync_EstablishesConnection()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        // Act
        await _service.ConnectAsync(":memory:", false, null, cts.Token);

        // Assert
        Assert.True(_service.IsConnected);
        Assert.NotNull(_service.Database);
    }

    [Fact]
    public async Task DisconnectAsync_ReleasesResources()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await _service.ConnectAsync(":memory:", false, null, cts.Token);
        Assert.True(_service.IsConnected);

        // Act
        await _service.DisconnectAsync();

        // Assert
        Assert.False(_service.IsConnected);
        Assert.Null(_service.Database);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsQueryResult()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await _service.ConnectAsync(":memory:", false, null, cts.Token);

        // Insert some test data
        const string insertQuery = "INSERT INTO test_collection VALUES { name: 'test1', value: 42 }";
        await _service.ExecuteAsync(insertQuery, cts.Token);

        // Act
        QueryResult result = await _service.ExecuteAsync("SELECT * FROM test_collection", cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.RowCount);
        Assert.True(result.ExecutionTime > TimeSpan.Zero);
        Assert.NotEmpty(result.Columns);
    }

    [Fact]
    public async Task UpdateDocumentFieldAsync_UpdatesField()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await _service.ConnectAsync(":memory:", false, null, cts.Token);

        // Insert a test document
        const string insertQuery = "INSERT INTO test_collection VALUES { name: 'test1', value: 42 }";
        await _service.ExecuteAsync(insertQuery, cts.Token);

        // Get the document ID (assuming it's auto-generated, we need to query it)
        QueryResult queryResult = await _service.ExecuteAsync("SELECT _id FROM test_collection", cts.Token);
        Assert.NotNull(queryResult);
        Assert.Equal(1, queryResult.RowCount);
        var documentId = ((BsonDocument)queryResult.Rows.First())["_id"].RawValue;

        // Act
        await _service.UpdateDocumentFieldAsync("test_collection", documentId, "value", 100, cts.Token);

        // Assert
        QueryResult updatedResult = await _service.ExecuteAsync("SELECT value FROM test_collection", cts.Token);
        Assert.NotNull(updatedResult);
        Assert.Equal(1, updatedResult.RowCount);
        Assert.Equal(100, (int)((BsonDocument)updatedResult.Rows.First())["value"].RawValue);
    }

    [Fact]
    public async Task DropCollectionWorkflow_RemovesCollection()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await _service.ConnectAsync(":memory:", false, null, cts.Token);

        // Create a test collection
        const string insertQuery = "INSERT INTO test_drop_collection VALUES { name: 'test' }";
        await _service.ExecuteAsync(insertQuery, cts.Token);

        // Verify collection exists
        IEnumerable<string> collectionsBefore = await _service.GetCollectionNamesAsync(cts.Token);
        Assert.Contains("test_drop_collection", collectionsBefore);

        IDialogService dialogService = Substitute.For<IDialogService>();
        dialogService.Confirm(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogIcon>()).Returns(true);
        IFileDialogService fileDialogService = Substitute.For<IFileDialogService>();
        IFileService fileService = Substitute.For<IFileService>();

        // Create a DbTreeNode for the collection with a confirmer that always returns true
        var node = new DbTreeNode(_service, dialogService, fileDialogService, fileService)
        {
            Header = "test_drop_collection",
            Tag = "collection"
        };

        // Act - Invoke Drop command
        await node.DropCommand.ExecuteAsync(null);

        // Assert - Collection should be removed
        IEnumerable<string> collectionsAfter = await _service.GetCollectionNamesAsync(cts.Token);
        Assert.DoesNotContain("test_drop_collection", collectionsAfter);
    }

    [Fact]
    public async Task TransactionWorkflow_BeginInsertCommit_DataPersisted()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await _service.ConnectAsync(":memory:", false, null, cts.Token);

        // Act
        await _service.BeginTransactionAsync(cts.Token);
        Assert.True(_service.TransactionActive);

        await _service.ExecuteAsync("INSERT INTO tx_test VALUES { name: 'committed' }", cts.Token);
        await _service.CommitTransactionAsync(cts.Token);

        // Assert: transaction closed and data persisted
        Assert.False(_service.TransactionActive);
        QueryResult result = await _service.ExecuteAsync("SELECT * FROM tx_test", cts.Token);
        Assert.Equal(1, result.RowCount);
    }

    [Fact]
    public async Task TransactionWorkflow_BeginRollback_TransactionStateClosed()
    {
        // Note: LiteDatabase.Execute() auto-commits each SQL script independently of BeginTrans(),
        // so verifying that inserted data is absent after rollback is not achievable via ExecuteAsync.
        // This test verifies the transaction lifecycle state only.
        var cts = new CancellationTokenSource();
        await _service.ConnectAsync(":memory:", false, null, cts.Token);

        await _service.BeginTransactionAsync(cts.Token);
        Assert.True(_service.TransactionActive);

        await _service.RollbackTransactionAsync(cts.Token);

        Assert.False(_service.TransactionActive);
    }

    // ── T125: Schema discovery with nested documents and arrays ──────────────────

    [Fact]
    public async Task GetCollectionSchemaAsync_WithNestedDocument_IncludesFieldAsDocumentType()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await _service.ConnectAsync(":memory:", false, null, cts.Token);
        await _service.ExecuteAsync(
            "INSERT INTO nested_test VALUES { name: 'Alice', address: { street: '123 Main', city: 'Springfield' } }",
            cts.Token);

        // Act
        IEnumerable<ColumnInfo> schema = await _service.GetCollectionSchemaAsync("nested_test", cts.Token);
        var columns = schema.ToList();

        // Assert
        Assert.NotEmpty(columns);
        Assert.Contains(columns, c => c is { Name: "name", BsonType: "String" });
        Assert.Contains(columns, c => c is { Name: "address", BsonType: "Document" });
    }

    [Fact]
    public async Task GetCollectionSchemaAsync_WithArrayField_IncludesFieldAsArrayType()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await _service.ConnectAsync(":memory:", false, null, cts.Token);
        await _service.ExecuteAsync(
            "INSERT INTO array_test VALUES { title: 'Example', tags: ['one', 'two', 'three'] }",
            cts.Token);

        // Act
        IEnumerable<ColumnInfo> schema = await _service.GetCollectionSchemaAsync("array_test", cts.Token);
        var columns = schema.ToList();

        // Assert
        Assert.Contains(columns, c => c is { Name: "title", BsonType: "String" });
        Assert.Contains(columns, c => c is { Name: "tags", BsonType: "Array" });
    }

    [Fact]
    public async Task GetCollectionSchemaAsync_WithDocumentsHavingDifferentFields_ReturnsCombinedSchema()
    {
        // Arrange — two documents with disjoint field sets
        var cts = new CancellationTokenSource();
        await _service.ConnectAsync(":memory:", false, null, cts.Token);
        await _service.ExecuteAsync("INSERT INTO mixed_test VALUES { fieldA: 'value1' }", cts.Token);
        await _service.ExecuteAsync("INSERT INTO mixed_test VALUES { fieldB: 42 }", cts.Token);

        // Act
        IEnumerable<ColumnInfo> schema = await _service.GetCollectionSchemaAsync("mixed_test", cts.Token);
        var columns = schema.ToList();

        // Assert — schema is the union of all observed fields across sampled documents
        Assert.Contains(columns, c => c.Name == "fieldA");
        Assert.Contains(columns, c => c.Name == "fieldB");
    }

    // ── T126: Edge cases — locked file, already-connected, not-connected ─────────

    [Fact]
    public async Task ConnectAsync_WhenAlreadyConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await _service.ConnectAsync(":memory:", false, null, cts.Token);
        Assert.True(_service.IsConnected);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ConnectAsync(":memory:", false, null, cts.Token));
    }

    [Fact]
    public async Task ConnectAsync_WithLockedFile_ThrowsException()
    {
        // Arrange — open a direct-mode (exclusive) connection to a temp file
        var tempFile = Path.Combine(Path.GetTempPath(), $"litedb_lock_{Guid.NewGuid():N}.litedb");
        var service1 = new LiteDbService();
        try
        {
            await service1.ConnectAsync(tempFile, false, null, CancellationToken.None);
            Assert.True(service1.IsConnected);

            // Act — second instance on the same file should fail (direct/exclusive mode)
            var service2 = new LiteDbService();
            await Assert.ThrowsAnyAsync<Exception>(
                () => service2.ConnectAsync(tempFile, false, null, CancellationToken.None));
        }
        finally
        {
            await service1.DisposeAsync();
            if (File.Exists(tempFile)) { File.Delete(tempFile); }
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // _service starts disconnected — no ConnectAsync called
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExecuteAsync("SELECT * FROM test", CancellationToken.None));
    }
}
