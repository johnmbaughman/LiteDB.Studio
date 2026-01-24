namespace LiteDB.Studio.Wpf.Services;

public class ConnectionStateChangedEventArgs(bool isConnected) : EventArgs
{
    public bool IsConnected { get; } = isConnected;
}