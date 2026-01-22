using LiteDB.Studio.Wpf.Services;
using LiteDB.Studio.Wpf.ViewModels;
using NSubstitute;
using Xunit;
using System.Threading.Tasks;

namespace LiteDB.Studio.Wpf.Tests.ViewModels
{
    public class DatabaseTreeViewModelTests
    {
        [Fact]
        public async Task LoadRootNodesAsync_PopulatesRootNodes()
        {
            // Arrange
            var mockService = Substitute.For<IDatabaseService>();
            mockService.GetCollectionNamesAsync(default).ReturnsForAnyArgs(["collection1", "collection2"]);
            mockService.GetSystemCollectionNamesAsync(default).ReturnsForAnyArgs(["system1", "system2"]);

            var viewModel = new DatabaseTreeViewModel(mockService);

            // Act
            await viewModel.LoadRootNodesAsync();

            // Assert
            Assert.Single(viewModel.RootNodes);
            var root = viewModel.RootNodes[0];
            Assert.Equal("Database", root.Header);
            Assert.Equal("database", root.Tag);
            Assert.Equal("pack://application:,,,/Resources/Icons/database.png", root.IconUri);

            // Check system node
            Assert.Equal(3, root.Children.Count); // system folder + 2 collections
            var systemNode = root.Children[0];
            Assert.Equal("System", systemNode.Header);
            Assert.Equal("systemfolder", systemNode.Tag);
            Assert.Equal("pack://application:,,,/Resources/Icons/system.png", systemNode.IconUri);
            Assert.Equal(2, systemNode.Children.Count);
            Assert.Equal("system1", systemNode.Children[0].Header);
            Assert.Equal("system", systemNode.Children[0].Tag);
            Assert.Equal("pack://application:,,,/Resources/Icons/system.png", systemNode.Children[0].IconUri);
            Assert.Equal("system2", systemNode.Children[1].Header);

            // Check collections
            var col1 = root.Children[1];
            Assert.Equal("collection1", col1.Header);
            Assert.Equal("collection", col1.Tag);
            Assert.Equal("pack://application:,,,/Resources/Icons/collection.png", col1.IconUri);

            var col2 = root.Children[2];
            Assert.Equal("collection2", col2.Header);
            Assert.Equal("collection", col2.Tag);
            Assert.Equal("pack://application:,,,/Resources/Icons/collection.png", col2.IconUri);

            // Counts and status
            Assert.Equal(2, viewModel.CollectionsCount);
            Assert.Equal(2, viewModel.SystemCount);
            Assert.Equal("Collections: 2 / System: 2", viewModel.StatusText);
        }
    }
}