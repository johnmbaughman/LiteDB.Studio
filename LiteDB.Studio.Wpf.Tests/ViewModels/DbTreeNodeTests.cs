using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LiteDB.Studio.Wpf.Services;
using LiteDB.Studio.Wpf.ViewModels;
using NSubstitute;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.ViewModels;

/// <summary>Unit tests for <see cref="DbTreeNode"/>.</summary>
public class DbTreeNodeTests
{
    [Fact]
    public async Task LoadChildrenCommand_LoadsSchema()
    {
        // Arrange
        IDatabaseService? mockService = Substitute.For<IDatabaseService>();
        var schema = new List<ColumnInfo>
        {
            new() { Name = "field1" },
            new() { Name = "field2" }
        };
        mockService.GetCollectionSchemaAsync("testCollection", CancellationToken.None).ReturnsForAnyArgs(schema);

        IDialogService dialogService = Substitute.For<IDialogService>();
        IFileDialogService fileDialogService = Substitute.For<IFileDialogService>();
        IFileService fileService = Substitute.For<IFileService>();

        var node = new DbTreeNode(mockService, dialogService, fileDialogService, fileService)
        {
            Header = "testCollection",
            Tag = "collection"
        };

        // Act
        await node.LoadChildrenCommand.ExecuteAsync(null);

        // Assert
        Assert.True(node.IsLoaded);
        Assert.Equal(2, node.Children.Count);
        Assert.Equal("field1", node.Children[0].Header);
        Assert.Equal("field", node.Children[0].Tag);
        Assert.Equal("pack://application:,,,/Resources/Icons/field.png", node.Children[0].IconUri);
        Assert.Equal("field2", node.Children[1].Header);
        Assert.Equal("field", node.Children[1].Tag);
        Assert.Equal("pack://application:,,,/Resources/Icons/field.png", node.Children[1].IconUri);
    }

    [Fact]
    public void InsertSnippet_InsertsCollectionSnippet_ForSystemTag()
    {
        // Arrange
        string? inserted = null;
        IDatabaseService? mockService = Substitute.For<IDatabaseService>();
        IDialogService dialogService = Substitute.For<IDialogService>();
        IFileDialogService fileDialogService = Substitute.For<IFileDialogService>();
        IFileService fileService = Substitute.For<IFileService>();

        var node = new DbTreeNode(mockService, dialogService, fileDialogService, fileService, snippet => inserted = snippet)
        {
            Header = "system.$cols",
            Tag = "system"
        };

        // Act
        node.InsertSnippetCommand.Execute(null);

        // Assert
        Assert.Equal("SELECT $ FROM system.$cols;", inserted);
    }
}
