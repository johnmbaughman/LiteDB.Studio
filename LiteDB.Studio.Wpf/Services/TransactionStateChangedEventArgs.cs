namespace LiteDB.Studio.Wpf.Services;

public class TransactionStateChangedEventArgs(bool transactionActive) : EventArgs
{
    public bool TransactionActive { get; } = transactionActive;
}