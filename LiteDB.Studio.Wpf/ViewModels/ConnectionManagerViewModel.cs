using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;

namespace LiteDB.Studio.Wpf.ViewModels
{
    public enum ConnectionMode
    {
        Direct,
        Shared
    }

    public class ConnectionManagerViewModel : ObservableObject
    {
        private ConnectionMode _mode = ConnectionMode.Direct;
        private string _filename = string.Empty;
        private string _password = string.Empty;
        private int _initialSize = 0;
        private string _collationLeft = string.Empty;
        private string _collationRight = string.Empty;
        private bool _readOnly = false;
        private bool _upgradeFromV4 = false;

        public event EventHandler<bool?>? RequestClose;

        public ConnectionMode Mode { get => _mode; set => SetProperty(ref _mode, value); }
        public string Filename { get => _filename; set => SetProperty(ref _filename, value); }
        public string Password { get => _password; set => SetProperty(ref _password, value); }
        public int InitialSize { get => _initialSize; set => SetProperty(ref _initialSize, value); }
        public string CollationLeft { get => _collationLeft; set => SetProperty(ref _collationLeft, value); }
        public string CollationRight { get => _collationRight; set => SetProperty(ref _collationRight, value); }
        public bool ReadOnly { get => _readOnly; set => SetProperty(ref _readOnly, value); }
        public bool UpgradeFromV4 { get => _upgradeFromV4; set => SetProperty(ref _upgradeFromV4, value); }

        public ICommand ConnectCommand { get; }
        public ICommand CancelCommand { get; }

        public ConnectionManagerViewModel()
        {
            ConnectCommand = new RelayCommand(OnConnect);
            CancelCommand = new RelayCommand(OnCancel);
        }

        private void OnConnect()
        {
            RequestClose?.Invoke(this, true);
        }

        private void OnCancel()
        {
            RequestClose?.Invoke(this, false);
        }
    }
}
