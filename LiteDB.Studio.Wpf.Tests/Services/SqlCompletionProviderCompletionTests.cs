using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiteDB.Studio.Wpf.Services;
using NSubstitute;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.Services;

/// <summary>Integration-style tests that combine keyword and collection completions.</summary>
public class SqlCompletionProviderCompletionTests
{
    [Fact]
    public async Task CompletionList_IncludesKeywordsAndCollectionNames()
    {
        IDatabaseService? db = Substitute.For<IDatabaseService>();
        db.GetCollectionNamesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(["users", "orders"]));

        IEnumerable<SqlCompletionProvider> kws = await SqlCompletionProvider.GetKeywordCompletionsFromFileAsync();
        IEnumerable<SqlCompletionProvider> cols = await SqlCompletionProvider.GetCollectionCompletionsAsync(db);

        var combined = kws.Select(k => k.Text).Concat(cols.Select(c => c.Text)).ToArray();

        Assert.Contains("SELECT", combined);
        Assert.Contains("COUNT", combined);
        Assert.Contains("users", combined);
        Assert.Contains("orders", combined);
    }
}
