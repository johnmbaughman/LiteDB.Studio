using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Studio.Wpf.Services;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.Services;

/// <summary>Tests that verify the bundled default SQL keywords file is present and complete.</summary>
public class SqlKeywordDefaultFileTests
{
    [Fact]
    public async Task DefaultKeywordsFile_ContainsFunctionKeywords()
    {
        // Try to locate the default keywords file by walking up from the test bin directory
        var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
        string? candidate = null;

        while (dir != null)
        {
            var path = Path.Combine(dir.FullName, "LiteDB.Studio.Wpf", "Resources", "sql_keywords.json");
            if (File.Exists(path))
            {
                candidate = path;
                break;
            }

            dir = dir.Parent;
        }

        Assert.False(string.IsNullOrEmpty(candidate), "Could not find LiteDB.Studio.Wpf/Resources/sql_keywords.json in repo tree relative to test runner base dir.");

        IEnumerable<SqlCompletionProvider> completions = await SqlCompletionProvider.GetKeywordCompletionsFromFileAsync(candidate);

        var sqlCompletionProviders = completions.ToList();
        Assert.Contains(sqlCompletionProviders, c => c.Text == "COUNT");
        Assert.Contains(sqlCompletionProviders, c => c.Text == "NOW");
        Assert.Contains(sqlCompletionProviders, c => c.Text == "JSON");
        Assert.Contains(sqlCompletionProviders, c => c.Text == "LOWER");
    }
}
