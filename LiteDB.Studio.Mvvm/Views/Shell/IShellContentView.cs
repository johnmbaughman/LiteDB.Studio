using LiteDB.Studio.Mvvm.ViewModels.Shell;

namespace LiteDB.Studio.Mvvm.Views.Shell;

/// <summary>
/// Defines the contract for the main shell content view in the WPF application.
/// This interface should be implemented by the main user control representing the shell's content area.
/// </summary>
/// <remarks>
/// <para>
/// The framework currently supports only a single main shell content view.
/// </para>
/// <para>
/// Implementations of this interface are expected to provide access to the associated shell content view model.
/// </para>
/// </remarks>
public interface IShellContentView {
    /// <summary>
    /// Gets the shell content view model associated with this view.
    /// </summary>
    /// <remarks>
    /// This property is typically set in the view's constructor and provides access to
    /// the logic and state management for the shell content.
    /// </remarks>
    /// <value>
    /// The <see cref="IShellContentViewModel"/> instance representing the view model for this shell content view.
    /// </value>
    IShellContentViewModel ShellContentViewModel { get; }
}
