using System.Threading;
using System.Threading.Tasks;
using LiteDB.Studio.Mvvm.ViewModels.Shell;
using LiteDB.Studio.Mvvm.Views.Shell;
using LiteDB.Studio.Wpf.Services;
using LiteDB.Studio.Wpf.ViewModels;
using NSubstitute;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.ViewModels;

public class DatabaseTreeViewModelTests
{
    public DatabaseTreeViewModelTests()
    {
        TestAppHostInitializer.EnsureInitialized();
    }

    [Fact]
    public async Task LoadRootNodesAsync_PopulatesRootNodes()
    {
        // Arrange
        IDatabaseService? mockService = Substitute.For<IDatabaseService>();
        mockService.GetCollectionNamesAsync(CancellationToken.None).ReturnsForAnyArgs(["collection1", "collection2"]);
        mockService.GetSystemCollectionNamesAsync(CancellationToken.None).ReturnsForAnyArgs(["system1", "system2"]);

        var viewModel = new DatabaseTreeViewModel(mockService, CreateShellContentView());

        // Act
        await viewModel.LoadRootNodesAsync();

        // Assert
        Assert.Single(viewModel.RootNodes);
        DbTreeNode root = viewModel.RootNodes[0];
        Assert.Equal("Database", root.Header);
        Assert.Equal("database", root.Tag);
        Assert.Equal("pack://application:,,,/Resources/Icons/database.png", root.IconUri);

        // Check system node
        Assert.Equal(3, root.Children.Count); // system folder + 2 collections
        DbTreeNode systemNode = root.Children[0];
        Assert.Equal("System", systemNode.Header);
        Assert.Equal("systemfolder", systemNode.Tag);
        Assert.Equal("pack://application:,,,/Resources/Icons/system.png", systemNode.IconUri);
        Assert.Equal(2, systemNode.Children.Count);
        Assert.Equal("system1", systemNode.Children[0].Header);
        Assert.Equal("system", systemNode.Children[0].Tag);
        Assert.Equal("pack://application:,,,/Resources/Icons/system.png", systemNode.Children[0].IconUri);
        Assert.Equal("system2", systemNode.Children[1].Header);

        // Check collections
        DbTreeNode col1 = root.Children[1];
        Assert.Equal("collection1", col1.Header);
        Assert.Equal("collection", col1.Tag);
        Assert.Equal("pack://application:,,,/Resources/Icons/collection.png", col1.IconUri);

        DbTreeNode col2 = root.Children[2];
        Assert.Equal("collection2", col2.Header);
        Assert.Equal("collection", col2.Tag);
        Assert.Equal("pack://application:,,,/Resources/Icons/collection.png", col2.IconUri);

        // Counts and status
        Assert.Equal(2, viewModel.CollectionsCount);
        Assert.Equal(2, viewModel.SystemCount);
        Assert.Equal("Collections: 2 / System: 2", viewModel.StatusText);
    }

    [Fact]
    public async Task LoadRootNodesAsync_FiltersOutSystemCollectionsFromUserList()
    {
        // Arrange
        IDatabaseService? mockService = Substitute.For<IDatabaseService>();
        mockService.GetCollectionNamesAsync(CancellationToken.None).ReturnsForAnyArgs(["collection1", "system1"]);
        mockService.GetSystemCollectionNamesAsync(CancellationToken.None).ReturnsForAnyArgs(["system1", "system2"]);

        var viewModel = new DatabaseTreeViewModel(mockService, CreateShellContentView());

        // Act
        await viewModel.LoadRootNodesAsync();

        // Assert
        Assert.Single(viewModel.RootNodes);
        DbTreeNode root = viewModel.RootNodes[0];

        // system folder + single user collection
        Assert.Equal(2, root.Children.Count);
        DbTreeNode systemNode = root.Children[0];
        Assert.Equal(2, systemNode.Children.Count);

        DbTreeNode col = root.Children[1];
        Assert.Equal("collection1", col.Header);
        Assert.Equal("collection", col.Tag);

        // Counts and status
        Assert.Equal(1, viewModel.CollectionsCount);
        Assert.Equal(2, viewModel.SystemCount);
        Assert.Equal("Collections: 1 / System: 2", viewModel.StatusText);
    }
    private static IShellContentView CreateShellContentView()
    {
        IShellContentView? shellContentView = Substitute.For<IShellContentView>();
        IShellContentViewModel? shellContentViewModel = Substitute.For<IShellContentViewModel>();
        shellContentView.ShellContentViewModel.Returns(shellContentViewModel);
        return shellContentView;
    }
}
