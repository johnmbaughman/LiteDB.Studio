using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.ViewModels
{
    public class TabViewModelTests
    {
        [Fact]
        public async Task RunCommand_SetsLastResult_WhenQuerySucceeds()
        {
            var mock = Substitute.For<Wpf.Services.IDatabaseService>();
            var expectedResult = new Wpf.Services.QueryResult { RowCount = 1 };
            mock.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            var vm = new Wpf.ViewModels.TabViewModel(mock);

            await vm.RunCommand.ExecuteAsync(null);

            Assert.NotNull(vm.LastResult);
            Assert.Equal(1, vm.LastResult.RowCount);
        }

        [Fact]
        public async Task RunCommand_SetsLastError_WhenQueryFails()
        {
            var mock = Substitute.For<Wpf.Services.IDatabaseService>();
            mock.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<Wpf.Services.QueryResult>(new System.Exception("Query failed")));

            var vm = new Wpf.ViewModels.TabViewModel(mock);

            await vm.RunCommand.ExecuteAsync(null);

            Assert.NotNull(vm.LastError);
            Assert.Contains("Query failed", vm.LastError);
        }

        [Fact]
        public async Task RunCommand_ExecutesSelection_WhenSelectionExists()
        {
            var mock = Substitute.For<Wpf.Services.IDatabaseService>();
            var expectedResult = new Wpf.Services.QueryResult { RowCount = 1 };
            mock.ExecuteAsync(Arg.Is<string>(s => s == "SELECT * FROM users"), Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            var vm = new Wpf.ViewModels.TabViewModel(mock)
            {
                EditorText = "SELECT * FROM users; SELECT * FROM products;", SelectionStart = 0, SelectionLength = 19 // "SELECT * FROM users;"
            };

            await vm.RunCommand.ExecuteAsync(null);

            Assert.NotNull(vm.LastResult);
            Assert.Equal(1, vm.LastResult.RowCount);
        }

        [Fact]
        public async Task ShowCompletionCommand_PopulatesLastCompletions()
        {
            var mock = Substitute.For<Wpf.Services.IDatabaseService>();
            mock.GetCollectionNamesAsync(Arg.Any<CancellationToken>()).Returns(["users", "products"]);

            var vm = new Wpf.ViewModels.TabViewModel(mock);

            await vm.ShowCompletionCommand.ExecuteAsync(null);

            Assert.NotNull(vm.LastCompletions);
            Assert.Contains(vm.LastCompletions, c => c.Text == "users");
        }
    }
}
