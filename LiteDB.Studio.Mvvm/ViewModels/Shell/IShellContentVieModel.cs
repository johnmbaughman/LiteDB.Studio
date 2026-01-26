//using System.Timers;
using LiteDB.Studio.Mvvm.Views.Shell;
//using Timer = System.Timers.Timer;

namespace LiteDB.Studio.Mvvm.ViewModels.Shell;

/// <summary>
/// Represents the contract for a shell content view model in the WPF application.
/// Extends <see cref="IViewModel"/>.
/// Provides dialog, timer, error, and progress management for shell content.
/// </summary>
public interface IShellContentViewModel : IViewModel {
    ///// <summary>
    ///// Gets or sets the context object that owns the dialog.
    ///// </summary>
    //object? DialogOwnerContext { get; set; }

    ///// <summary>
    ///// Gets or sets a value indicating whether the error message is visible.
    ///// </summary>
    //bool IsErrorMessageVisible { get; set; }

    /// <summary>
    /// Gets or sets the shell view model associated with this content.
    /// </summary>
    IShellViewModel ShellViewModel { get; set; }

    ///// <summary>
    ///// Gets the timer instance used for periodic operations.
    ///// </summary>
    //Timer Timer { get; }

    ///// <summary>
    ///// Gets or sets the validation error messages.
    ///// </summary>
    //string? ValidationErrors { get; set; }

    /// <summary>
    /// Gets or sets the shell content view associated with this view model.
    /// </summary>
    IShellContentView View { get; set; }

    /// <summary>
    /// Assigns the shell view model to this content view model.
    /// </summary>
    /// <param name="shellViewModel">The shell view model to assign.</param>
    void AssignShellViewModel(IShellViewModel shellViewModel);

    ///// <summary>
    ///// Performs asynchronous cleanup operations for the view model.
    ///// </summary>
    ///// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    //Task CleanupAsync();

    ///// <summary>
    ///// Hides the currently visible validation error message.
    ///// </summary>
    //void HideValidationError();

    ///// <summary>
    ///// Initializes dialog components required by the view model.
    ///// </summary>
    //void InitializeDialogs();

    ///// <summary>
    ///// Initializes the timer with the specified interval and event handler.
    ///// </summary>
    ///// <param name="interval">The timer interval in milliseconds.</param>
    ///// <param name="elapsedEventHandler">The event handler for timer elapsed events.</param>
    //void InitializeTimer(double interval, ElapsedEventHandler elapsedEventHandler);

    ///// <summary>
    ///// Sets the message displayed in the progress dialog.
    ///// </summary>
    ///// <param name="message">The message to display.</param>
    //void SetProgressDialogMessage(string message);

    ///// <summary>
    ///// Shows a message dialog with the specified title, message, and accept option.
    ///// </summary>
    ///// <param name="title">The dialog title.</param>
    ///// <param name="message">The message to display. Default is empty.</param>
    ///// <param name="canAccept">Indicates if the dialog can be accepted. Default is true.</param>
    ///// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    //Task ShowMessageDialog(string title, string message = "", bool canAccept = true);

    ///// <summary>
    ///// Shows a yes/no message dialog and returns the user's choice.
    ///// </summary>
    ///// <param name="title">The dialog title.</param>
    ///// <param name="message">The message to display. Default is empty.</param>
    ///// <returns>A <see cref="Task{Boolean}"/> representing the asynchronous operation, with the result indicating the user's choice.</returns>
    //Task<bool> ShowYesNoMessageDialog(string title, string message = "");

    ///// <summary>
    ///// Shows a progress dialog with the specified title and message.
    ///// </summary>
    ///// <param name="title">The dialog title.</param>
    ///// <param name="message">The message to display. Default is empty.</param>
    ///// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    //Task ShowProgressDialog(string title, string message = "");

    ///// <summary>
    ///// Waits for the progress dialog to close, optionally updating the message.
    ///// </summary>
    ///// <param name="message">The final message to display. Default is empty.</param>
    ///// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    //Task WaitForProgressDialogClose(string message = "");
}
