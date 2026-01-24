using System.IO;
using System.Threading.Tasks;
using Xunit;
using LiteDB.Studio.Wpf.Services;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Studio.Wpf.Tests.Services
{
    public class SqlKeywordFileTests
    {
        [Fact]
        public async Task GetKeywordCompletionsFromFileAsync_ReadsFile()
        {
            var tmp = Path.GetTempFileName();
            await File.WriteAllTextAsync(tmp, System.Text.Json.JsonSerializer.Serialize(new[] { "select", "insert", "explain" }));

            var completions = await SqlCompletionProvider.GetKeywordCompletionsFromFileAsync(tmp);

            var sqlCompletionProviders = completions.ToList();
            Assert.Contains(sqlCompletionProviders, c => c.Text == "SELECT");
            Assert.Contains(sqlCompletionProviders, c => c.Text == "INSERT");
            Assert.Contains(sqlCompletionProviders, c => c.Text == "EXPLAIN");

            File.Delete(tmp);
        }

        [Fact]
        public async Task GetKeywordCompletionsFromFileAsync_MissingFile_ReturnsEmpty()
        {
            var tmp = Path.Combine(Path.GetTempPath(), "nonexistent-xyz.json");

            var completions = await SqlCompletionProvider.GetKeywordCompletionsFromFileAsync(tmp);

            Assert.Empty(completions);
        }
    }
}
