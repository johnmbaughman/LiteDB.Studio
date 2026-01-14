using System;
using System.Threading.Tasks;

namespace LiteDB.Studio.Wpf.Services
{
    public class LiteDbService : IDatabaseService
    {
        private LiteDB.LiteDatabase? _db;

        public bool IsConnected => _db != null;

        public object? Database => _db;

        public async Task<object> ConnectAsync(LiteDB.ConnectionString connectionString)
        {
            // create on background thread to avoid UI blocking
            _db = await Task.Run(() => new LiteDB.LiteDatabase(connectionString));

            // force open by reading user version
            try
            {
                var _ = _db.UserVersion;
            }
            catch
            {
                _db?.Dispose();
                _db = null;
                throw;
            }

            return _db;
        }

        public void Disconnect()
        {
            try
            {
                _db?.Dispose();
            }
            finally
            {
                _db = null;
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
