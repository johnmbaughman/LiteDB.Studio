using LiteDB.Studio.Wpf.Services;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.Services;

public class SqlCompletionProviderTests
{
    [Fact]
    public void FromKeyword_SetsTextAndDescription()
    {
        var item = SqlCompletionProvider.FromKeyword("select");

        Assert.Equal("SELECT", item.Text);
        Assert.Contains("SQL keyword", item.Description?.ToString());
    }

    [Fact]
    public void FromCollection_SetsTextAndTag()
    {
        var item = SqlCompletionProvider.FromCollection("users");

        Assert.Equal("users", item.Text);
        Assert.Equal("collection", item.Tag);
        Assert.Contains("Collection", item.Description?.ToString());
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var item = new SqlCompletionProvider("foo", "desc", tag: 123);

        Assert.Equal("foo", item.Text);
        Assert.Equal("desc", item.Description);
        Assert.Equal(123, item.Tag);
    }
}
