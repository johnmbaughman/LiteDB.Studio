using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiteDB.Studio.Wpf.Services;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.Integration;

/// <summary>Integration tests verifying system-collection discovery in <see cref="LiteDbService"/>.</summary>
public class LiteDbServiceSystemCollectionsTests : IDisposable
{
    private readonly LiteDbService _service = new();

    /// <summary>Disposes the shared <see cref="LiteDbService"/> after each test.</summary>
    public void Dispose()
    {
        _service.Dispose();
    }

    [Fact]
    public async Task GetSystemCollectionNamesAsync_ReturnsRegisteredSystemCollections()
    {
        var cts = new CancellationTokenSource();
        await _service.ConnectAsync(":memory:", cts.Token);

        var systems = (await _service.GetSystemCollectionNamesAsync(cts.Token)).ToArray();

        // Registered system collections like `$indexes` should be present
        Assert.Contains("$indexes", systems);
        Assert.Contains("$cols", systems);
    }
}
