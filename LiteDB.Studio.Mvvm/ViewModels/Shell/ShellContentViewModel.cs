namespace LiteDB.Studio.Mvvm.ViewModels.Shell;

/// <summary>
/// Class ContentViewModelBase.
/// Implements the <see cref="ViewModel" />
/// </summary>
/// <seealso cref="ViewModel" />
public abstract class ShellContentViewModel : ViewModel, IShellContentViewModel {
    private IShellViewModel _shellViewModel = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellContentViewModel"/> class.
    /// </summary>
    protected ShellContentViewModel(Microsoft.Extensions.Logging.ILogger logger) : base(logger) { }

    public IShellViewModel ShellViewModel {
        get => _shellViewModel;
        set => SetProperty(ref _shellViewModel, value);
    }

    /// <summary>
    /// Assigns the shell.
    /// </summary>
    /// <param name="shellViewModel">The shell view model.</param>
    public virtual void AssignShellViewModel(IShellViewModel shellViewModel) {
        ShellViewModel = shellViewModel;
    }
}
