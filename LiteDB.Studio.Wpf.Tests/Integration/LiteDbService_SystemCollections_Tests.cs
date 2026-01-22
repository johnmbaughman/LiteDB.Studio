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
        public async Task GetSystemCollectionNamesAsync_UsesColsWhenAvailable()
        {
            var cts = new CancellationTokenSource();
            await _service.ConnectAsync(":memory:", cts.Token);

            // Insert a fake system collection entry into $cols
            var db = _service.Database as LiteDatabase;
            Assert.NotNull(db);

            var cols = db.GetCollection("$cols");
            cols.Insert(new BsonDocument { ["name"] = "$sys_test", ["type"] = "system" });

            var systems = (await _service.GetSystemCollectionNamesAsync(cts.Token)).ToArray();
            Assert.Contains("$sys_test", systems);
        }
    }
}