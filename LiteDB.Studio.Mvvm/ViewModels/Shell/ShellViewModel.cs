using CommunityToolkit.Mvvm.ComponentModel;
using LiteDB.Studio.Mvvm.Views.Shell;

namespace LiteDB.Studio.Mvvm.ViewModels.Shell;

/// <summary>
/// Screen view model base. Use this class if you need Dirty, busy, error message and notify property changed
/// </summary>
public partial class ShellViewModel : ViewModel, IShellViewModel {
    //private readonly IDialogCoordinator _dialogCoordinator;
    //private bool _isErrorMessageVisible;
    //private string _validationErrors = null!;

    ///// <summary>
    ///// Gets or sets a value indicating whether [are configurations visible].
    ///// </summary>
    ///// <value><c>true</c> if [are configurations visible]; otherwise, <c>false</c>.</value>
    //[ObservableProperty]
    //private bool _areConfigurationsVisible;

    ///// <summary>
    ///// Gets or sets a value indicating whether [are settings visible].
    ///// </summary>
    ///// <value><c>true</c> if [are settings visible]; otherwise, <c>false</c>.</value>
    //[ObservableProperty]
    //private bool _areSettingsVisible;

    ///// <summary>
    ///// Gets or sets a value indicating whether [in test mode].
    ///// </summary>
    ///// <value><c>true</c> if [in test mode]; otherwise, <c>false</c>.</value>
    //[ObservableProperty]
    //private bool _inTestMode;

    /// <summary>
    /// Gets or sets the main content view model.
    /// </summary>
    /// <value>The main content view model.</value>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    private IShellContentViewModel _mainContentViewModel;

    ///// <summary>
    ///// Gets or sets the clock.
    ///// </summary>
    ///// <value>The clock.</value>
    //[ObservableProperty]
    //[NotifyPropertyChangedFor(nameof(IsDirty))]
    //private ClockTimer _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellViewModel" /> class.
    /// </summary>
    /// <param name="mainContentView">The main content view.</param>
    /// <param name="dialogCoordinator">The dialog coordinator.</param>
    /// <param name="notificationService">The notification service.</param>
    // TODO: Move settingsFlyoutContentView to MaiOtherMvvmApplication
    public ShellViewModel(IShellContentView mainContentView) {
        //IDialogCoordinator dialogCoordinator, INotificationService notificationService) {
        //_dialogCoordinator = dialogCoordinator;
        //NotificationService = notificationService;
        //Clock = new ClockTimer();
        MainContentView = mainContentView;
        MainContentViewModel = MainContentView.ShellContentViewModel;
        //InitializeSimpleMessageDialog();
        //InitializeProgressMessageDialog();
        //InitializeYesNoMessageDialog();
    }

    /// <summary>
    /// Gets the main content view.
    /// </summary>
    /// <value>The main content view.</value>
    public IShellContentView MainContentView { get; }

    ///// <summary>
    ///// Gets or sets the simple message dialog.
    ///// </summary>
    ///// <value>The simple message dialog.</value>
    //public SimpleMessageView SimpleMessageDialog { get; set; } = null!;

    ///// <summary>
    ///// Gets or sets the progress message dialog.
    ///// </summary>
    ///// <value>The progress message dialog.</value>
    //public SimpleMessageView ProgressMessageDialog { get; set; } = null!;

    ///// <summary>
    ///// Gets or sets the yes no message dialog.
    ///// </summary>
    ///// <value>The yes no message dialog.</value>
    //public YesNoMessageView YesNoMessageDialog { get; set; } = null!;

    ///// <summary>
    ///// Gets the notification service for displaying toast notifications.
    ///// </summary>
    ///// <value>The notification service instance.</value>
    //public INotificationService NotificationService { get; }

    /// <summary>
    /// Cleanups the asynchronous.
    /// </summary>
    /// <returns>Task.</returns>
    public Task CleanupAsync() {
        //AreSettingsVisible = false;
        //AreConfigurationsVisible = false;
        //Clock.Stop();
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

    ///// <summary>
    ///// Initializes the progress message dialog.
    ///// </summary>
    //public void InitializeProgressMessageDialog() {
    //    ProgressMessageDialog = new SimpleMessageView(new SimpleMessageViewModel(this), MainContentViewModel,
    //        _dialogCoordinator);
    //}

    ///// <summary>
    ///// Initializes the simple message dialog.
    ///// </summary>
    //public void InitializeSimpleMessageDialog() {
    //    SimpleMessageDialog = new SimpleMessageView(new SimpleMessageViewModel(this), MainContentViewModel,
    //        _dialogCoordinator);
    //}

    ///// <summary>
    ///// Initializes the yes no message dialog.
    ///// </summary>
    //public void InitializeYesNoMessageDialog() {
    //    YesNoMessageDialog = new YesNoMessageView(new YesNoMessageViewModel(this), MainContentViewModel,
    //        _dialogCoordinator);
    //}

    ///// <summary>
    ///// Shows the settings flyout.
    ///// </summary>
    ///// <returns>Task.</returns>
    //[RelayCommand]
    //private Task ShowSettingsFlyout() {
    //    Logger.Debug("Showing settings");
    //    AreSettingsVisible = true;
    //    return Task.CompletedTask;
    //}

    ///// <summary>
    ///// Gets or sets the validation errors.
    ///// </summary>
    ///// <value>
    ///// The validation errors.
    ///// </value>
    //public string ValidationErrors
    //{
    //    get => _validationErrors;
    //    set => SetField(ref _validationErrors, value);
    //}
    ///// <summary>
    ///// Hides the validation error.
    ///// </summary>
    //public void HideValidationError()
    //{
    //    IsErrorMessageVisible = false;
    //    ValidationErrors = null!;
    //}
}
