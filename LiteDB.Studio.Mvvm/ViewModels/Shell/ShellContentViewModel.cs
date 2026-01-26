using System.Timers;
using LiteDB.Studio.Mvvm.Views.Shell;
using Timer = System.Timers.Timer;

#pragma warning disable CA2007 // ConfigureAwait

namespace LiteDB.Studio.Mvvm.ViewModels.Shell;

/// <summary>
/// Class ContentViewModelBase.
/// Implements the <see cref="ViewModel" />
/// </summary>
/// <seealso cref="ViewModel" />
public abstract class ShellContentViewModel : ViewModel, IShellContentViewModel {
    //private bool _isErrorMessageVisible;
    //private SimpleMessageViewModel _errorDialog = null!;
    private IShellViewModel _shellViewModel = null!;
    //private string? _validationErrors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellContentViewModel"/> class.
    /// </summary>
    protected ShellContentViewModel() { }

    ///// <summary>
    ///// Gets or sets a value indicating whether this instance can accept dialog.
    ///// </summary>
    ///// <value><c>true</c> if this instance can accept dialog; otherwise, <c>false</c>.</value>
    //public bool ProgressCanAcceptDialog {
    //    get => ShellViewModel.ProgressMessageDialog.CanAccept;
    //    set => ShellViewModel.ProgressMessageDialog.CanAccept = value;
    //}

    ///// <summary>
    ///// Initializes the timer.
    ///// </summary>
    ///// <param name="interval">The interval.</param>
    ///// <param name="elapsedEventHandler">The elapsed event handler.</param>
    //public void InitializeTimer(double interval, ElapsedEventHandler elapsedEventHandler) {
    //    Timer = new Timer(interval);
    //    Timer.Elapsed += elapsedEventHandler;
    //}

    ///// <summary>
    ///// Gets or sets the dialog message.
    ///// </summary>
    ///// <value>The dialog message.</value>

    //public void SetProgressDialogMessage(string message) {
    //    ShellViewModel.ProgressMessageDialog.SetDialogMessage(message, true);
    //}

    ///// <summary>
    ///// Gets or sets the owner context.
    ///// </summary>
    ///// <value>The owner context.</value>
    //public object? DialogOwnerContext { get; set; }

    ///// <summary>
    ///// Gets or sets a value indicating whether this instance is error message visible.
    ///// </summary>
    ///// <value><c>true</c> if this instance is error message visible; otherwise, <c>false</c>.</value>
    //public bool IsErrorMessageVisible {
    //    get => _isErrorMessageVisible;
    //    set {
    //        SetProperty(ref _isErrorMessageVisible, value);
    //        IsDirty = true;
    //    }
    //}

    /// <summary>
    /// Gets or sets the shell view model.
    /// </summary>
    /// <value>The shell view model.</value>
    public IShellViewModel ShellViewModel {
        get => _shellViewModel;
        set => SetProperty(ref _shellViewModel, value);
    }

    ///// <summary>
    ///// Gets the timer.
    ///// </summary>
    ///// <value>The timer.</value>
    //public Timer Timer { get; private set; } = null!;

    ///// <summary>
    ///// Gets or sets the validation errors.
    ///// </summary>
    ///// <value>The validation errors.</value>
    //public string? ValidationErrors {
    //    get => _validationErrors;
    //    set {
    //        SetProperty(ref _validationErrors, value);
    //        IsDirty = true;
    //    }
    //}

    /// <summary>
    /// The view
    /// </summary>
    /// <value>The view.</value>
    public IShellContentView View { get; set; } = null!;

    ///// <summary>
    ///// Gets the notification service from the shell view model.
    ///// </summary>
    //protected INotificationService? NotificationService => ShellViewModel?.NotificationService;

    /// <summary>
    /// Assigns the shell.
    /// </summary>
    /// <param name="shellViewModel">The shell view model.</param>
    public virtual void AssignShellViewModel(IShellViewModel shellViewModel) {
        ShellViewModel = shellViewModel;
        //InitializeDialogs();
        //InitializeNotifications();
    }

    ///// <summary>
    ///// Initializes notifications for this view model. Override to configure notifications.
    ///// </summary>
    //protected virtual void InitializeNotifications() {
    //    // Default implementation - can be overridden in derived classes
    //    NotificationService?.Configure(config => {
    //        config.ShowApplicationNameInTitle = true;
    //        config.ApplicationName = "Application"; // This should be set from the actual application name
    //        config.EnableLogging = true;
    //    });
    //}

    ///// <summary>
    ///// Cleanups the asynchronous.
    ///// </summary>
    ///// <returns>Task.</returns>
    //public abstract Task CleanupAsync();

    ///// <summary>
    ///// Hides the progress dialog.
    ///// </summary>
    //public async Task HideProgressDialog() {
    //    await ShellViewModel.ProgressMessageDialog.HideMessageDialog();
    //    ShellViewModel.InitializeProgressMessageDialog();
    //}

    ///// <summary>
    ///// Hides the validation error.
    ///// </summary>
    //public virtual void HideValidationError() {
    //    IsErrorMessageVisible = false;
    //    ValidationErrors = null;
    //}

    ///// <summary>
    ///// Initializes the dialogs.
    ///// </summary>
    //public abstract void InitializeDialogs();

    ///// <summary>
    ///// Shows the message dialog.
    ///// </summary>
    ///// <param name="title">The title.</param>
    ///// <param name="message">The message.</param>
    ///// <param name="canAccept">if set to <c>true</c> [can accept].</param>
    ///// <returns>Task.</returns>
    //public async Task ShowMessageDialog(string title, string message = "", bool canAccept = true) {
    //    ShellViewModel.InitializeSimpleMessageDialog();
    //    CanAccept = canAccept;
    //    ShellViewModel.SimpleMessageDialog.SetDialogMessage(message);
    //    ShellViewModel.SimpleMessageDialog.DialogTitle = title;
    //    _ = await ShellViewModel.SimpleMessageDialog.ShowDialogAsync(canAccept, false);
    //}

    ///// <summary>
    ///// Shows the yes/no message dialog.
    ///// </summary>
    ///// <param name="title">The title.</param>
    ///// <param name="message">The message.</param>
    ///// <returns>Task.</returns>
    //public async Task<bool> ShowYesNoMessageDialog(string title, string message = "") {
    //    ShellViewModel.InitializeYesNoMessageDialog();
    //    ShellViewModel.YesNoMessageDialog.DialogMessage = message;
    //    ShellViewModel.YesNoMessageDialog.DialogTitle = title;
    //    return ((YesNoResult)await ShellViewModel.YesNoMessageDialog.ShowDialogAsync()).YesNoResponse;
    //}

    ///// <summary>
    ///// Shows the progress dialog.
    ///// </summary>
    ///// <param name="title">The title.</param>
    ///// <param name="initialMessage">The message.</param>
    ///// <exception cref="System.NullReferenceException">DialogOwnerContext is null</exception>
    //public async Task ShowProgressDialog(string title, string initialMessage = "") {
    //    ShellViewModel.InitializeProgressMessageDialog();
    //    await ShellViewModel.ProgressMessageDialog.ShowProgressDialog(title, initialMessage);
    //}

    ///// <summary>
    ///// Waits for progress dialog close.
    ///// </summary>
    ///// <param name="message">The message.</param>
    ///// <returns>Task.</returns>
    //public async Task WaitForProgressDialogClose(string message = "") {
    //    await ShellViewModel.ProgressMessageDialog.WaitForProgressDialogClose(message);
    //}

    //#region Convenient Notification Methods

    ///// <summary>
    ///// Shows a success notification.
    ///// </summary>
    ///// <param name="message">The success message.</param>
    ///// <param name="title">Optional title.</param>
    //protected void NotifySuccess(string message, string? title = null) {
    //    NotificationService?.ShowSuccess(title ?? "", message);
    //}

    ///// <summary>
    ///// Shows an error notification.
    ///// </summary>
    ///// <param name="message">The error message.</param>
    ///// <param name="title">Optional title.</param>
    //protected void NotifyError(string message, string? title = null) {
    //    NotificationService?.ShowError(title ?? "", message);
    //}

    ///// <summary>
    ///// Shows an information notification.
    ///// </summary>
    ///// <param name="message">The information message.</param>
    ///// <param name="title">Optional title.</param>
    //protected void NotifyInfo(string message, string? title = null) {
    //    NotificationService?.ShowInformation(title ?? "", message);
    //}

    ///// <summary>
    ///// Shows a warning notification.
    ///// </summary>
    ///// <param name="message">The warning message.</param>
    ///// <param name="title">Optional title.</param>
    //protected void NotifyWarning(string message, string? title = null) {
    //    NotificationService?.ShowWarning(title ?? "", message);
    //}

    ///// <summary>
    ///// Shows a notification for completed operations.
    ///// </summary>
    ///// <param name="operationName">The name of the completed operation.</param>
    //protected void NotifyOperationCompleted(string operationName) {
    //    NotifySuccess($"{operationName} completed successfully", "Operation Complete");
    //}

    ///// <summary>
    ///// Shows a notification for failed operations.
    ///// </summary>
    ///// <param name="operationName">The name of the failed operation.</param>
    ///// <param name="error">Optional error details.</param>
    //protected void NotifyOperationFailed(string operationName, string? error = null) {
    //    var message = string.IsNullOrEmpty(error)
    //        ? $"{operationName} failed"
    //        : $"{operationName} failed: {error}";
    //    NotifyError(message, "Operation Failed");
    //}

    ///// <summary>
    ///// Shows a validation error notification.
    ///// </summary>
    ///// <param name="message">The validation error message.</param>
    //protected void NotifyValidationError(string message) {
    //    NotifyWarning(message, "Validation Error");
    //}

    ///// <summary>
    ///// Shows a notification for data save operations.
    ///// </summary>
    ///// <param name="dataType">The type of data that was saved.</param>
    //protected void NotifyDataSaved(string dataType) {
    //    NotifySuccess($"{dataType} saved successfully", "Save Complete");
    //}

    ///// <summary>
    ///// Shows a notification for data loading operations.
    ///// </summary>
    ///// <param name="dataType">The type of data being loaded.</param>
    //protected void NotifyDataLoading(string dataType) {
    //    NotifyInfo($"Loading {dataType}...", "Loading");
    //}

    //#endregion
}
