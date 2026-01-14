using System;
using System.Threading.Tasks;

namespace LiteDB.Studio.Wpf.Services
{
    public interface IDatabaseService : IDisposable
    {
        bool IsConnected { get; }
        object? Database { get; }
        Task<object> ConnectAsync(LiteDB.ConnectionString connectionString);
        void Disconnect();
    }
}
