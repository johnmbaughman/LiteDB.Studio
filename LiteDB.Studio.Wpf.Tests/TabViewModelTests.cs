using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests
{
    public class TabViewModelTests
    {
        [Fact]
        public async Task RunCommand_SetsLastResult_WhenQuerySucceeds()
        {
            var mock = Substitute.For<LiteDB.Studio.Wpf.Services.IDatabaseService>();
            var expectedResult = new LiteDB.Studio.Wpf.Services.QueryResult { RowCount = 1 };
            mock.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            var vm = new LiteDB.Studio.Wpf.ViewModels.TabViewModel(mock);

            await vm.RunCommand.ExecuteAsync(null);

            Assert.NotNull(vm.LastResult);
            Assert.Equal(1, vm.LastResult.RowCount);
        }

        [Fact]
        public async Task RunCommand_SetsLastError_WhenQueryFails()
        {
            var mock = Substitute.For<LiteDB.Studio.Wpf.Services.IDatabaseService>();
            mock.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<LiteDB.Studio.Wpf.Services.QueryResult>(new System.Exception("Query failed")));

            var vm = new LiteDB.Studio.Wpf.ViewModels.TabViewModel(mock);

            await vm.RunCommand.ExecuteAsync(null);

            Assert.NotNull(vm.LastError);
            Assert.Contains("Query failed", vm.LastError);
        }

        [Fact]
        public async Task RunCommand_ExecutesSelection_WhenSelectionExists()
        {
            var mock = Substitute.For<LiteDB.Studio.Wpf.Services.IDatabaseService>();
            var expectedResult = new LiteDB.Studio.Wpf.Services.QueryResult { RowCount = 1 };
            mock.ExecuteAsync(Arg.Is<string>(s => s == "SELECT * FROM users"), Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            var vm = new LiteDB.Studio.Wpf.ViewModels.TabViewModel(mock);
            vm.EditorText = "SELECT * FROM users; SELECT * FROM products;";
            vm.SelectionStart = 0;
            vm.SelectionLength = 19; // "SELECT * FROM users;"

            await vm.RunCommand.ExecuteAsync(null);

            Assert.NotNull(vm.LastResult);
            Assert.Equal(1, vm.LastResult.RowCount);
        }
    }
}
