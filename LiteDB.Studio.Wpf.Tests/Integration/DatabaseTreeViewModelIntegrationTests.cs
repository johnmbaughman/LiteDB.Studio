using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiteDB.Studio.Wpf.Services;
using LiteDB.Studio.Wpf.ViewModels;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.Integration;

public class DatabaseTreeViewModelIntegrationTests
{
    public DatabaseTreeViewModelIntegrationTests()
    {
        TestAppHostInitializer.EnsureInitialized();
    }

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
        DbTreeNode root = vm.RootNodes.First();
        Assert.Equal("Database", root.Header);
        // root should contain a System folder and our collection
        Assert.Contains("System", root.Children.Select(c => c.Header));
        Assert.Contains("test_collection", root.Children.Select(c => c.Header));
    }
}
