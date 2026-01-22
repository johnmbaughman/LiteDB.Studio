using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using NSubstitute;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.ViewModels
{
    public class ResultGridViewModelTests
    {
        [Fact]
        public async Task UpdateCellValueAsync_CallsUpdateDocumentFieldAsync_WithCorrectParameters()
        {
            // Arrange
            var mockDatabaseService = Substitute.For<LiteDB.Studio.Wpf.Services.IDatabaseService>();
            var viewModel = new LiteDB.Studio.Wpf.ViewModels.ResultGridViewModel(mockDatabaseService);
            viewModel.CollectionName = "test_collection";

            var document = new BsonDocument
            {
                ["_id"] = new BsonValue(1),
                ["name"] = new BsonValue("test")
            };

            // Act
            await viewModel.UpdateCellValueAsync(document, "name", "updated_name", CancellationToken.None);

            // Assert
            await mockDatabaseService.Received(1).UpdateDocumentFieldAsync(
                Arg.Is("test_collection"),
                Arg.Is(1),
                Arg.Is("name"),
                Arg.Is("updated_name"),
                Arg.Any<CancellationToken>());
        }
    }
}