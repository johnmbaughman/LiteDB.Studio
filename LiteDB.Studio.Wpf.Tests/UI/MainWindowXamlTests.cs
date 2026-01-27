using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.UI;

public class MainWindowXamlTests
{
    [Fact]
    public async Task MainWindow_Xaml_ShouldContain_F5KeyBinding()
    {
        // Walk up from the test runner base directory to locate the project source
        var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
        string? candidate = null;

        while (dir != null)
        {
            var path = Path.Combine(dir.FullName, "..", "..", "..", "LiteDB.Studio.Wpf", "Views", "MainWindow.xaml");
            path = Path.GetFullPath(path);
            if (File.Exists(path))
            {
                candidate = path;
                break;
            }

            dir = dir.Parent;
        }

        Assert.False(string.IsNullOrEmpty(candidate), "Could not find LiteDB.Studio.Wpf/MainWindow.xaml relative to test runner base dir.");

        var text = await File.ReadAllTextAsync(candidate);
        Assert.Contains("<KeyBinding Key=\"F5\" Command=\"{Binding RunCommand}\"", text);
    }

    [Fact]
    public async Task MainWindow_Xaml_ShouldEnable_AvalonEditCompletion()
    {
        // Walk up from the test runner base directory to locate the project source
        var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
        string? candidate = null;

        while (dir != null)
        {
            var path = Path.Combine(dir.FullName, "..", "..", "..", "LiteDB.Studio.Wpf", "Views", "MainWindow.xaml");
            path = Path.GetFullPath(path);
            if (File.Exists(path))
            {
                candidate = path;
                break;
            }

            dir = dir.Parent;
        }

        Assert.False(string.IsNullOrEmpty(candidate), "Could not find LiteDB.Studio.Wpf/MainWindow.xaml relative to test runner base dir.");

        var text = await File.ReadAllTextAsync(candidate);
        // Should use the new MVVM behavior bindings (EditorText or ShowCompletionCommand)
        Assert.True(text.Contains("behaviors:AvalonEditBehaviors.EditorText") || text.Contains("behaviors:AvalonEditBehaviors.ShowCompletionCommand"), "MainWindow.xaml should bind AvalonEdit to the MVVM behaviors (EditorText or ShowCompletionCommand)");
    }
}
