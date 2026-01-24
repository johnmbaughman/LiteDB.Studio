using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using LiteDB.Studio.Wpf.Services;

namespace LiteDB.Studio.Wpf.Tests.Services
{
    public class SqlCompletionProviderCompletionTests
    {
        [Fact]
        public async Task CompletionList_IncludesKeywordsAndCollectionNames()
        {
            var db = Substitute.For<IDatabaseService>();
            db.GetCollectionNamesAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult((System.Collections.Generic.IEnumerable<string>)new[] { "users", "orders" }));

            var kws = await SqlCompletionProvider.GetKeywordCompletionsFromFileAsync(null);
            var cols = await SqlCompletionProvider.GetCollectionCompletionsAsync(db);

            var combined = kws.Select(k => k.Text).Concat(cols.Select(c => c.Text)).ToArray();

            Assert.Contains("SELECT", combined);
            Assert.Contains("COUNT", combined);
            Assert.Contains("users", combined);
            Assert.Contains("orders", combined);
        }
    }
}
