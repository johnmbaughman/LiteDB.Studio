namespace LiteDB.Studio.Wpf.Services;

/// <summary>Provides data for the <see cref="IDatabaseService.ConnectionStateChanged"/> event.</summary>
public class ConnectionStateChangedEventArgs(bool isConnected) : EventArgs
{
    /// <summary>Gets a value indicating whether the database is now connected.</summary>
    public bool IsConnected { get; } = isConnected;
}
