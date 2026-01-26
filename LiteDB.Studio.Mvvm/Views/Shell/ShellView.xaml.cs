using System.ComponentModel;
using System.Windows;
using LiteDB.Studio.Mvvm.ViewModels.Shell;

namespace LiteDB.Studio.Mvvm.Views.Shell;

/// <summary>
/// Represents the main shell view for the WPF application.
/// </summary>
/// <remarks>
/// <para>This class defines the base application window (shell) and is used internally by the framework.
/// It should not be directly used or inherited from.</para>
/// <note type="important">
/// This class is managed by the framework and should not be directly instantiated or extended.
/// </note>
/// </remarks>
/// <seealso cref="IShellView" />
/// <seealso cref="System.Windows.Markup.IComponentConnector" />
internal partial class ShellView : IShellView
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShellView"/> class.
    /// Sets up the view and attaches the provided shell view model as the data context.
    /// </summary>
    /// <param name="shellViewModel">The shell view model to attach to this view.</param>
    public ShellView(IShellViewModel shellViewModel)
    {
        DataContext = shellViewModel;
        Loaded += (_, _) => ViewModel.ViewLoaded();
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model attached to this view.
    /// </summary>
    /// <value>The <see cref="IShellViewModel"/> instance associated with this view.</value>
    public IShellViewModel ViewModel => (IShellViewModel)DataContext;

    /// <summary>
    /// Handles the window closing event and shuts down the application.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
    private void ShellView_OnClosing(object? sender, CancelEventArgs e)
    {
        // TODO: Evaluate the use of this here.
        Application.Current.Shutdown();
    }
}
