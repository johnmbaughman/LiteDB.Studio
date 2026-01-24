using System.IO;
using System.Threading.Tasks;
using Xunit;
using LiteDB.Studio.Wpf.Services;
using System.Linq;

namespace LiteDB.Studio.Wpf.Tests.Services
{
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

            var completions = await SqlCompletionProvider.GetKeywordCompletionsFromFileAsync(candidate);

            Assert.Contains(completions, c => c.Text == "COUNT");
            Assert.Contains(completions, c => c.Text == "NOW");
            Assert.Contains(completions, c => c.Text == "JSON");
            Assert.Contains(completions, c => c.Text == "LOWER");
        }
    }
}
