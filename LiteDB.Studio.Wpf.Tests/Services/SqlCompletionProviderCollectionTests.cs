using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using LiteDB.Studio.Wpf.Services;
using System.Collections.Generic;

namespace LiteDB.Studio.Wpf.Tests.Services
{
    public class SqlCompletionProviderCollectionTests
    {
        [Fact]
        public async Task GetCollectionCompletionsAsync_ReturnsCollections()
        {
            var db = Substitute.For<IDatabaseService>();
            db.GetCollectionNamesAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IEnumerable<string>>(new[] { "users", "orders" }));

            var completions = await SqlCompletionProvider.GetCollectionCompletionsAsync(db);

            Assert.Contains(completions, c => c.Text == "users" && (string?)c.Tag == "collection");
            Assert.Contains(completions, c => c.Text == "orders" && (string?)c.Tag == "collection");
        }

        [Fact]
        public async Task GetCollectionCompletionsAsync_HandlesEmptyReturns()
        {
            var db = Substitute.For<IDatabaseService>();
            db.GetCollectionNamesAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IEnumerable<string>>(new string[0]));

            var completions = await SqlCompletionProvider.GetCollectionCompletionsAsync(db);

            Assert.Empty(completions);
        }
    }
}
