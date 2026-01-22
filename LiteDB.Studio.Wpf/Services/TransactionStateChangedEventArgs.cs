using System;

namespace LiteDB.Studio.Wpf.Services
{
    public class TransactionStateChangedEventArgs : EventArgs
    {
        public bool TransactionActive { get; }

        public TransactionStateChangedEventArgs(bool transactionActive)
        {
            TransactionActive = transactionActive;
        }
    }
}
