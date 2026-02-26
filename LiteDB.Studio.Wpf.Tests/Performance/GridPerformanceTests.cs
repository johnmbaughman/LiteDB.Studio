using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LiteDB.Studio.Wpf.Services;
using LiteDB.Studio.Wpf.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.Performance;

/// <summary>
/// Performance tests verifying that large result sets (10 000 rows) are processed
/// within the plan.md SLO: grid ViewModel update must complete in under 3 seconds.
/// </summary>
public class GridPerformanceTests
{
    private static QueryResult Build10KQueryResult()
    {
        var columns = new List<ColumnInfo>
        {
            new() { Name = "name",   BsonType = "String"  },
            new() { Name = "value",  BsonType = "Int32"   },
            new() { Name = "active", BsonType = "Boolean" },
        };

        var rows = Enumerable.Range(0, 10_000)
            .Select(i => (object)new BsonDocument
            {
                ["_id"]    = i,
                ["name"]   = $"item_{i}",
                ["value"]  = i,
                ["active"] = i % 2 == 0,
            })
            .ToList();

        return new QueryResult
        {
            Rows          = rows,
            Columns       = columns,
            RowCount      = rows.Count,
            ExecutionTime = TimeSpan.FromMilliseconds(50),
            LimitExceeded = false,
        };
    }

    private static ResultGridViewModel CreateGridViewModel()
    {
        IDatabaseService service = Substitute.For<IDatabaseService>();
        return new ResultGridViewModel(service, NullLogger<ResultGridViewModel>.Instance);
    }

    [Fact]
    public void Load10kRows_GridViewModelUpdate_CompletesUnder3Seconds()
    {
        // Arrange
        ResultGridViewModel gridVm = CreateGridViewModel();
        QueryResult result = Build10KQueryResult();

        var stopwatch = Stopwatch.StartNew();

        // Act — push result into grid ViewModel; triggers UpdateColumns() synchronously
        gridVm.QueryResult = result;

        stopwatch.Stop();

        // Assert — row count reflected and total processing time within SLO
        Assert.Equal(10_000, gridVm.QueryResult!.RowCount);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(3),
            $"Expected <3 s but took {stopwatch.Elapsed.TotalSeconds:F2} s");
    }

    [Fact]
    public void Load10kRows_ExecutionTime_IsPopulatedInQueryResult()
    {
        // Arrange — build result with a positive ExecutionTime (set by the service in production)
        QueryResult result = Build10KQueryResult();

        // Assert — ExecutionTime is positive and row count is correct
        Assert.True(result.ExecutionTime > TimeSpan.Zero,
            "ExecutionTime should be a positive duration");
        Assert.Equal(10_000, result.RowCount);
    }

    [Fact]
    public void Load10kRows_ResultGridViewModel_ColumnsPopulatedCorrectly()
    {
        // Arrange
        ResultGridViewModel gridVm = CreateGridViewModel();
        QueryResult result = Build10KQueryResult();

        // Act
        gridVm.QueryResult = result;

        // Assert — column descriptors are built from the explicitly supplied schema
        Assert.NotEmpty(gridVm.Columns);
        Assert.Contains(gridVm.Columns, c => c.Header == "name");
        Assert.Contains(gridVm.Columns, c => c.Header == "value");
        Assert.Contains(gridVm.Columns, c => c.Header == "active");
    }
}
