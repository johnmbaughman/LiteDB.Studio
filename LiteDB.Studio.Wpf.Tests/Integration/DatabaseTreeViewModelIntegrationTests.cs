using LiteDB.Studio.Wpf.Services;
using LiteDB.Studio.Wpf.ViewModels;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.Integration
{
    public class DatabaseTreeViewModelIntegrationTests
    {
        [Fact]
        public async Task LoadRootNodesAsync_WithRealService_PopulatesNodes()
        {
            // Arrange
            var service = new LiteDbService();
            var cts = new CancellationTokenSource();
            await service.ConnectAsync(":memory:", cts.Token);

            // create a test collection
            await service.ExecuteAsync("INSERT INTO test_collection VALUES { name: 'a' }", cts.Token);

            var vm = new DatabaseTreeViewModel(service);

            // Act
            await vm.LoadRootNodesAsync(cts.Token);

            // Assert
            Assert.Single(vm.RootNodes);
            var root = vm.RootNodes.First();
            Assert.Equal("Database", root.Header);
            // root should contain a System folder and our collection
            Assert.True(root.Children.Any(c => c.Header == "System"));
            Assert.True(root.Children.Any(c => c.Header == "test_collection"));
        }
    }
}