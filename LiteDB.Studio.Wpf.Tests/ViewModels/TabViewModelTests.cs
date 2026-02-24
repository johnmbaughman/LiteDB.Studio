using System.Threading;
using System.Threading.Tasks;
using LiteDB.Studio.Wpf.Services;
using LiteDB.Studio.Wpf.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.ViewModels;

public class TabViewModelTests
{
    [Fact]
    public async Task RunCommand_SetsLastResult_WhenQuerySucceeds()
    {
        IDatabaseService? mock = Substitute.For<IDatabaseService>();
        var expectedResult = new QueryResult { RowCount = 1 };
        mock.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var vm = new Wpf.ViewModels.TabViewModel(mock, NullLoggerFactory.Instance);

        await vm.RunCommand.ExecuteAsync(null);

        Assert.NotNull(vm.LastResult);
        Assert.Equal(1, vm.LastResult.RowCount);
    }

    [Fact]
    public async Task RunCommand_SetsLastError_WhenQueryFails()
    {
        IDatabaseService? mock = Substitute.For<IDatabaseService>();
        mock.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<QueryResult>(new System.Exception("Query failed")));

        var vm = new Wpf.ViewModels.TabViewModel(mock, NullLoggerFactory.Instance);

        await vm.RunCommand.ExecuteAsync(null);

        Assert.NotNull(vm.LastError);
        Assert.Contains("Query failed", vm.LastError);
    }

    [Fact]
    public async Task RunCommand_ExecutesSelection_WhenSelectionExists()
    {
        IDatabaseService? mock = Substitute.For<IDatabaseService>();
        var expectedResult = new QueryResult { RowCount = 1 };
        mock.ExecuteAsync(Arg.Is<string>(s => s == "SELECT * FROM users"), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var vm = new Wpf.ViewModels.TabViewModel(mock, NullLoggerFactory.Instance)
        {
            EditorText = "SELECT * FROM users; SELECT * FROM products;", SelectionStart = 0, SelectionLength = 19 // "SELECT * FROM users;"
        };

        await vm.RunCommand.ExecuteAsync(null);

        Assert.NotNull(vm.LastResult);
        Assert.Equal(1, vm.LastResult.RowCount);
    }

    [Fact]
    public async Task CloseCommand_PromptsIfModified_AndDoesNotPromptIfNotModified()
    {
        IDatabaseService? mock = Substitute.For<IDatabaseService>();
        IDialogService mockDialog = Substitute.For<IDialogService>();
        mockDialog.Confirm(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogIcon>()).Returns(false);

        var vm = new Wpf.ViewModels.TabViewModel(mock, NullLoggerFactory.Instance, mockDialog)
        {
            Title = "Query 1",
            Filename = "test.sql",
            IsModified = true
        };

        await vm.CloseCommand.ExecuteAsync(null);

        mockDialog.Received(1).Confirm(
            Arg.Is<string>(s => s.Contains("test.sql")),
            Arg.Any<string>(),
            Arg.Any<DialogIcon>());
    }

    [Fact]
    public async Task CloseCommand_DoesNotPrompt_WhenNotModified()
    {
        IDatabaseService? mock = Substitute.For<IDatabaseService>();
        IDialogService mockDialog = Substitute.For<IDialogService>();

        var vm = new Wpf.ViewModels.TabViewModel(mock, NullLoggerFactory.Instance, mockDialog)
        {
            IsModified = false
        };

        await vm.CloseCommand.ExecuteAsync(null);

        mockDialog.DidNotReceive().Confirm(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogIcon>());
    }

    [Fact]
    public void EditorText_SetsIsModified_WhenNotPlusTab()
    {
        IDatabaseService? mock = Substitute.For<IDatabaseService>();
        var vm = new Wpf.ViewModels.TabViewModel(mock, NullLoggerFactory.Instance);

        vm.EditorText = "SELECT 1;";

        Assert.True(vm.IsModified);
    }

    [Fact]
    public void EditorText_DoesNotSetIsModified_WhenPlusTab()
    {
        IDatabaseService? mock = Substitute.For<IDatabaseService>();
        var vm = new Wpf.ViewModels.TabViewModel(mock, NullLoggerFactory.Instance) { IsPlus = true };

        vm.EditorText = "SELECT 1;";

        Assert.False(vm.IsModified);
    }

    [Fact]
    public async Task ShowCompletionCommand_PopulatesLastCompletions()
    {
        IDatabaseService? mock = Substitute.For<IDatabaseService>();
        mock.GetCollectionNamesAsync(Arg.Any<CancellationToken>()).Returns(["users", "products"]);

        var vm = new Wpf.ViewModels.TabViewModel(mock, NullLoggerFactory.Instance);

        await vm.ShowCompletionCommand.ExecuteAsync(null);

        Assert.NotNull(vm.LastCompletions);
        Assert.Contains(vm.LastCompletions, c => c.Text == "users");
    }
}
