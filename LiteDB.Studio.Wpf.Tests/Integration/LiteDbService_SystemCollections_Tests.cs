using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using LiteDB.Studio.Wpf.Services;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.Integration
{
    public class LiteDbService_SystemCollections_Tests : IDisposable
    {
        private readonly LiteDbService _service;

        public LiteDbService_SystemCollections_Tests()
        {
            _service = new LiteDbService();
        }

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
}