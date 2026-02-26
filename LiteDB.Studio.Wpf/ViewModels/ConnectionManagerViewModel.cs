using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB.Studio.Wpf.Services;
using System.Windows.Input;

namespace LiteDB.Studio.Wpf.ViewModels;

/// <summary>Defines the LiteDB connection type.</summary>
public enum ConnectionMode
{
    /// <summary>Exclusive (direct) connection — only one process may access the file at a time.</summary>
    Direct,
    /// <summary>Shared connection — multiple processes may access the file simultaneously.</summary>
    Shared
}

/// <summary>ViewModel for the connection-manager dialog window.</summary>
public class ConnectionManagerViewModel : ObservableObject
{
    private readonly IFileDialogService _fileDialogService;
    private ConnectionMode _mode = ConnectionMode.Direct;
    private string _filename = string.Empty;
    private string _password = string.Empty;
    private int _initialSize;
    private string _collationLeft = string.Empty;
    private string _collationRight = string.Empty;
    private bool _readOnly;
    private bool _upgradeFromV4;

    private bool? _closeTrigger;

    /// <summary>Gets or sets a value that triggers the window to close. <c>true</c> = OK, <c>false</c> = Cancel.</summary>
    public bool? CloseTrigger { get => _closeTrigger; set => SetProperty(ref _closeTrigger, value); }

    /// <summary>Gets or sets the selected connection mode.</summary>
    public ConnectionMode Mode { get => _mode; set => SetProperty(ref _mode, value); }
    /// <summary>Gets or sets the path to the LiteDB database file.</summary>
    public string Filename { get => _filename; set => SetProperty(ref _filename, value); }
    /// <summary>Gets or sets the optional encryption password.</summary>
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    /// <summary>Gets or sets the initial database file size in megabytes (0 = default).</summary>
    public int InitialSize { get => _initialSize; set => SetProperty(ref _initialSize, value); }
    /// <summary>Gets or sets the left part of the collation string (e.g. locale).</summary>
    public string CollationLeft { get => _collationLeft; set => SetProperty(ref _collationLeft, value); }
    /// <summary>Gets or sets the right part of the collation string (e.g. sort options).</summary>
    public string CollationRight { get => _collationRight; set => SetProperty(ref _collationRight, value); }
    /// <summary>Gets or sets a value indicating whether to open in read-only mode.</summary>
    public bool ReadOnly { get => _readOnly; set => SetProperty(ref _readOnly, value); }
    /// <summary>Gets or sets a value indicating whether to upgrade from the LiteDB v4 file format.</summary>
    public bool UpgradeFromV4 { get => _upgradeFromV4; set => SetProperty(ref _upgradeFromV4, value); }

    /// <summary>Command that accepts the dialog (sets <see cref="CloseTrigger"/> to <c>true</c>).</summary>
    public ICommand ConnectCommand { get; }
    /// <summary>Command that cancels the dialog (sets <see cref="CloseTrigger"/> to <c>false</c>).</summary>
    public ICommand CancelCommand { get; }
    /// <summary>Command that opens a file-browse dialog to select the database file.</summary>
    public ICommand BrowseCommand { get; }

    /// <summary>Initializes a new instance of <see cref="ConnectionManagerViewModel"/>.</summary>
    /// <param name="fileDialogService">Service used to show the file-browse dialog.</param>
    public ConnectionManagerViewModel(IFileDialogService fileDialogService)
    {
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        ConnectCommand = new RelayCommand(OnConnect);
        CancelCommand = new RelayCommand(OnCancel);
        BrowseCommand = new RelayCommand(OnBrowse);
    }

    private void OnConnect()
    {
        CloseTrigger = true;
    }

    private void OnCancel()
    {
        CloseTrigger = false;
    }

    private void OnBrowse()
    {
        var filename = _fileDialogService.OpenFile(new OpenFileDialogOptions
        {
            Filter = "LiteDB files (*.db)|*.db|All files (*.*)|*.*",
            Title = "Open LiteDB file"
        });

        if (!string.IsNullOrWhiteSpace(filename))
        {
            Filename = filename;
        }
    }
}
