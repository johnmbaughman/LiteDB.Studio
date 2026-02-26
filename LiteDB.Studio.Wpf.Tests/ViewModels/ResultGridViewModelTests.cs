using System.Threading;
using System.Threading.Tasks;
using LiteDB.Studio.Wpf.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.ViewModels;

/// <summary>Unit tests for <see cref="ResultGridViewModel"/>.</summary>
public class ResultGridViewModelTests
{
    [Fact]
    public async Task UpdateCellValueAsync_CallsUpdateDocumentFieldAsync_WithCorrectParameters()
    {
        // Arrange
        IDatabaseService? mockDatabaseService = Substitute.For<IDatabaseService>();
        var viewModel = new LiteDB.Studio.Wpf.ViewModels.ResultGridViewModel(
            mockDatabaseService,
            NullLogger<LiteDB.Studio.Wpf.ViewModels.ResultGridViewModel>.Instance)
        {
            CollectionName = "test_collection"
        };

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
