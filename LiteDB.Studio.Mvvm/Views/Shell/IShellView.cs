using LiteDB.Studio.Mvvm.ViewModels.Shell;

namespace LiteDB.Studio.Mvvm.Views.Shell;

/// <summary>
/// Interface IShellView
/// </summary>
/// <remarks>This is used internally by the framework and should not be directly implemented.</remarks>
public interface IShellView {
    /// <summary>
    /// Gets the main shell view model.
    /// </summary>
    /// <value>The view model.</value>
    IShellViewModel ViewModel { get; }

}
