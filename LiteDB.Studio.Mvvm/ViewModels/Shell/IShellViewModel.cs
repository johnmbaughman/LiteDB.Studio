using LiteDB.Studio.Mvvm.Views.Shell;

namespace LiteDB.Studio.Mvvm.ViewModels.Shell;

/// <summary>
/// Represents the contract for the main shell view model in the WPF application.
/// Provides properties and methods for managing application configuration, printer selection,
/// production date, user authentication, dialogs, timers, and UI state.
/// </summary>
public interface IShellViewModel : IViewModel {
    ///// <summary>
    ///// Gets or sets a value indicating whether the configuration panel is visible.
    ///// </summary>
    ///// <value><c>true</c> if the configuration panel is visible; otherwise, <c>false</c>.</value>
    //bool AreConfigurationsVisible { get; set; }

    ///// <summary>
    ///// Gets or sets a value indicating whether the settings panel is visible.
    ///// </summary>
    ///// <value><c>true</c> if the settings panel is visible; otherwise, <c>false</c>.</value>
    //bool AreSettingsVisible { get; set; }

    ///// <summary>
    ///// Gets or sets a value indicating whether the application is running in test mode.
    ///// </summary>
    ///// <value><c>true</c> if in test mode; otherwise, <c>false</c>.</value>
    //bool InTestMode { get; set; }

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

    ///// <summary>
    ///// Gets or sets the progress message dialog view.
    ///// </summary>
    ///// <value>The progress message dialog view instance.</value>
    //SimpleMessageView ProgressMessageDialog { get; set; }

    ///// <summary>
    ///// Gets or sets the simple message dialog view.
    ///// </summary>
    ///// <value>The simple message dialog view instance.</value>
    //SimpleMessageView SimpleMessageDialog { get; set; }

    ///// <summary>
    ///// Gets or sets the clock for updating the displayed date and time.
    ///// </summary>
    ///// <value>The updating date/time clock instance.</value>
    //ClockTimer Clock { get; set; }

    ///// <summary>
    ///// Gets or sets the yes/no message dialog view.
    ///// </summary>
    ///// <value>The yes/no message dialog view instance.</value>
    //YesNoMessageView YesNoMessageDialog { get; set; }

    ///// <summary>
    ///// Gets the notification service for displaying toast notifications.
    ///// </summary>
    ///// <value>The notification service instance.</value>
    //INotificationService NotificationService { get; }

    ///// <summary>
    ///// Performs asynchronous cleanup operations for the shell view model.
    ///// </summary>
    ///// <returns>A <see cref="Task"/> representing the asynchronous cleanup operation.</returns>
    //Task CleanupAsync();

    ///// <summary>
    ///// Initializes the progress message dialog view.
    ///// </summary>
    //void InitializeProgressMessageDialog();

    ///// <summary>
    ///// Initializes the simple message dialog view.
    ///// </summary>
    //void InitializeSimpleMessageDialog();

    ///// <summary>
    ///// Initializes the yes/no message dialog view.
    ///// </summary>
    //void InitializeYesNoMessageDialog();
}
