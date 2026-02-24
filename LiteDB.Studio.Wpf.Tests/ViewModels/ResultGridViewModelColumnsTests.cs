using System.Linq;
using LiteDB.Studio.Wpf.Services;
using LiteDB.Studio.Wpf.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.ViewModels;

public class ResultGridViewModelColumnsTests
{
    [Fact]
    public void SettingQueryResult_PopulatesColumnDescriptors()
    {
        // Arrange
        var mockDb = NSubstitute.Substitute.For<LiteDB.Studio.Wpf.Services.IDatabaseService>();
        var vm = new ResultGridViewModel(mockDb, NullLogger<ResultGridViewModel>.Instance);

        var result = new QueryResult
        {
            Columns = new[] { new ColumnInfo { Name = "name" }, new ColumnInfo { Name = "value" } },
            Rows = new object[] { }
        };

        // Act
        vm.QueryResult = result;

        // Assert
        Assert.Equal(2, vm.Columns.Count);
        Assert.Equal("name", vm.Columns[0].Name);
        Assert.Equal("value", vm.Columns[1].Name);
    }
}
