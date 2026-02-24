using CommunityToolkit.Mvvm.ComponentModel;
using LiteDB.Studio.Mvvm.Views.Shell;
using Microsoft.Extensions.Logging;

namespace LiteDB.Studio.Mvvm.ViewModels.Shell;

/// <summary>
/// Screen view model base. Use this class if you need Dirty, busy, error message and notify property changed
/// </summary>
public partial class ShellViewModel : ViewModel, IShellViewModel {
    /// <summary>
    /// Shell window title shown in the Window chrome.
    /// </summary>
    [ObservableProperty]
    private string _title = "LiteDB Studio";

    /// <summary>
    /// Icon URI for the Shell window. Use a pack URI pointing at the project's resources.
    /// </summary>
    [ObservableProperty]
    private string _iconUri = "pack://application:,,,/LiteDB.Studio.Wpf;component/Resources/litedb_icon.ico";

    /// <summary>
    /// Gets or sets the main content view model.
    /// </summary>
    /// <value>The main content view model.</value>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    private IShellContentViewModel _mainContentViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellViewModel" /> class.
    /// </summary>
    /// <param name="mainContentView">The main content view.</param>
    /// <param name="logger">Logger for this view model.</param>
    public ShellViewModel(IShellContentView mainContentView, ILogger<ShellViewModel> logger) : base(logger) {
        MainContentView = mainContentView;
        MainContentViewModel = MainContentView.ShellContentViewModel;
    }

    /// <summary>
    /// Gets the main content view.
    /// </summary>
    /// <value>The main content view.</value>
    public IShellContentView MainContentView { get; }

    /// <summary>
    /// Cleanups the asynchronous.
    /// </summary>
    /// <returns>Task.</returns>
    public Task CleanupAsync() {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Registers the messenger receivers.
    /// </summary>
    /// <remarks>Called by ShellViewModel during constructor execution.
    /// <b>IMPORTANT:</b>Handlers should process minimal data. Major data processing should be completed elsewhere, not directly in this function.</remarks>
    public override void RegisterMessengerReceivers() { }

    /// <summary>
    /// Handles the <see cref="E:ViewLoaded" /> event.
    /// </summary>
    /// <returns>Task.</returns>
    public override Task ViewLoaded() {
        MainContentViewModel.AssignShellViewModel(this);
        MainContentViewModel.RegisterMessengerReceivers();

        return base.ViewLoaded();
    }
}
