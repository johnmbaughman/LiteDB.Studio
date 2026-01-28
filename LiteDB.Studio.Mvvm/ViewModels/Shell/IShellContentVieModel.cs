using LiteDB.Studio.Mvvm.Views.Shell;

namespace LiteDB.Studio.Mvvm.ViewModels.Shell;

/// <summary>
/// Represents the contract for a shell content view model in the WPF application.
/// Extends <see cref="IViewModel"/>.
/// Provides dialog, timer, error, and progress management for shell content.
/// </summary>
public interface IShellContentViewModel : IViewModel {
    /// <summary>
    /// Gets or sets the shell view model associated with this content.
    /// </summary>
    IShellViewModel ShellViewModel { get; set; }

    /// <summary>
    /// Gets or sets the shell content view associated with this view model.
    /// </summary>
    IShellContentView View { get; set; }

    /// <summary>
    /// Assigns the shell view model to this content view model.
    /// </summary>
    /// <param name="shellViewModel">The shell view model to assign.</param>
    void AssignShellViewModel(IShellViewModel shellViewModel);
}
