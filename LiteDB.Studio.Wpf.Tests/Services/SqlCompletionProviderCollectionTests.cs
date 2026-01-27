using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiteDB.Studio.Wpf.Services;
using NSubstitute;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.Services;

public class SqlCompletionProviderCollectionTests
{
    [Fact]
    public async Task GetCollectionCompletionsAsync_ReturnsCollections()
    {
        IDatabaseService? db = Substitute.For<IDatabaseService>();
        db.GetCollectionNamesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(["users", "orders"]));

        IEnumerable<SqlCompletionProvider> completions = await SqlCompletionProvider.GetCollectionCompletionsAsync(db);

        var sqlCompletionProviders = completions.ToList();
        Assert.Contains(sqlCompletionProviders, c => c.Text == "users" && (string?)c.Tag == "collection");
        Assert.Contains(sqlCompletionProviders, c => c.Text == "orders" && (string?)c.Tag == "collection");
    }

    [Fact]
    public async Task GetCollectionCompletionsAsync_HandlesEmptyReturns()
    {
        IDatabaseService? db = Substitute.For<IDatabaseService>();
        db.GetCollectionNamesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>([]));

        IEnumerable<SqlCompletionProvider> completions = await SqlCompletionProvider.GetCollectionCompletionsAsync(db);

        Assert.Empty(completions);
    }
}
