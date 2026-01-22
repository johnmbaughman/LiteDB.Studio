using System;

namespace LiteDB.Studio.Wpf.Services
{
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; }

        public ConnectionStateChangedEventArgs(bool isConnected)
        {
            IsConnected = isConnected;
        }
    }
}
