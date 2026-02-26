namespace LiteDB.Studio.Wpf.Services;

/// <summary>Provides data for the <see cref="IDatabaseService.TransactionStateChanged"/> event.</summary>
public class TransactionStateChangedEventArgs(bool transactionActive) : EventArgs
{
    /// <summary>Gets a value indicating whether a transaction is currently active.</summary>
    public bool TransactionActive { get; } = transactionActive;
}
