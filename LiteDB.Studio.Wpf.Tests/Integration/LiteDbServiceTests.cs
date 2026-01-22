using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiteDB.Studio.Wpf.Services;
using LiteDB.Studio.Wpf.ViewModels;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.Integration
{
    public class LiteDbServiceTests : IDisposable
    {
        private readonly LiteDbService _service;

        public LiteDbServiceTests()
        {
            _service = new LiteDbService();
        }

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
            await _service.ConnectAsync(":memory:", cts.Token);

            // Assert
            Assert.True(_service.IsConnected);
            Assert.NotNull(_service.Database);
        }

        [Fact]
        public async Task DisconnectAsync_ReleasesResources()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            await _service.ConnectAsync(":memory:", cts.Token);
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
            await _service.ConnectAsync(":memory:", cts.Token);

            // Insert some test data
            var insertQuery = "INSERT INTO test_collection VALUES { name: 'test1', value: 42 }";
            await _service.ExecuteAsync(insertQuery, cts.Token);

            // Act
            var result = await _service.ExecuteAsync("SELECT * FROM test_collection", cts.Token);

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
            await _service.ConnectAsync(":memory:", cts.Token);

            // Insert a test document
            var insertQuery = "INSERT INTO test_collection VALUES { name: 'test1', value: 42 }";
            await _service.ExecuteAsync(insertQuery, cts.Token);

            // Get the document ID (assuming it's auto-generated, we need to query it)
            var queryResult = await _service.ExecuteAsync("SELECT _id FROM test_collection", cts.Token);
            Assert.NotNull(queryResult);
            Assert.Equal(1, queryResult.RowCount);
            var documentId = ((BsonDocument)queryResult.Rows.First())["_id"].RawValue;

            // Act
            await _service.UpdateDocumentFieldAsync("test_collection", documentId, "value", 100, cts.Token);

            // Assert
            var updatedResult = await _service.ExecuteAsync("SELECT value FROM test_collection", cts.Token);
            Assert.NotNull(updatedResult);
            Assert.Equal(1, updatedResult.RowCount);
            Assert.Equal(100, (int)((BsonDocument)updatedResult.Rows.First())["value"].RawValue);
        }

        [Fact]
        public async Task DropCollectionWorkflow_RemovesCollection()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            await _service.ConnectAsync(":memory:", cts.Token);

            // Create a test collection
            var insertQuery = "INSERT INTO test_drop_collection VALUES { name: 'test' }";
            await _service.ExecuteAsync(insertQuery, cts.Token);

            // Verify collection exists
            var collectionsBefore = await _service.GetCollectionNamesAsync(cts.Token);
            Assert.Contains("test_drop_collection", collectionsBefore);

            // Create a DbTreeNode for the collection with a confirmer that always returns true
            var node = new DbTreeNode(_service, confirmer: _ => true)
            {
                Header = "test_drop_collection",
                Tag = "collection"
            };

            // Act - Invoke Drop command
            await node.DropCommand.ExecuteAsync(null);

            // Assert - Collection should be removed
            var collectionsAfter = await _service.GetCollectionNamesAsync(cts.Token);
            Assert.DoesNotContain("test_drop_collection", collectionsAfter);
        }
    }
}