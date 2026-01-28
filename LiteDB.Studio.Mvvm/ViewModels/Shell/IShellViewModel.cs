using LiteDB.Studio.Mvvm.Views.Shell;

namespace LiteDB.Studio.Mvvm.ViewModels.Shell;

/// <summary>
/// Represents the contract for the main shell view model in the WPF application.
/// Provides properties and methods for managing application configuration, printer selection,
/// production date, user authentication, dialogs, timers, and UI state.
/// </summary>
public interface IShellViewModel : IViewModel {
    /// <summary>
    /// Gets the main content view for the shell.
    /// </summary>
    /// <value>The main shell content view instance.</value>
    IShellContentView MainContentView { get; }

    /// <summary>
    /// Gets or sets the view model for the main content view.
    /// </summary>
    /// <value>The main shell content view model.</value>
    // TODO: But why? - Ryan Reynolds
    IShellContentViewModel MainContentViewModel { get; }
}
