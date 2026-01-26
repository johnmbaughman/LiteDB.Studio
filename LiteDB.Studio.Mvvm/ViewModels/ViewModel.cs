using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace LiteDB.Studio.Mvvm.ViewModels;

/// <summary>
/// Abstract base class for all view models.
/// Provides common properties and behaviors such as dirty state tracking, busy state, logging, and dialog view references.
/// Implements <see cref="ObservableObject"/> for property change notification and <see cref="IViewModel"/> for standard view model contract.
/// </summary>
public abstract partial class ViewModel : ObservableObject, IViewModel {
    private bool _canAccept;
    private bool _canCancel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModel"/> class.
    /// Sets up the logger for the specific view model type.
    /// </summary>
    /// <exception cref="System.NullReferenceException">Thrown if logger is not available.</exception>
    protected ViewModel() {
        Logger = LiteDbStudioApplication.Logger?.ForContext(GetType())
                 ?? throw new NullReferenceException("Logger must not be null. Ensure MaiOtherMvvmApplication.Logger is initialized.");
    }

    /// <summary>
    /// Gets or sets a value indicating whether the view model is busy performing an operation.
    /// </summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// Gets or sets a value indicating whether the view model has unsaved changes.
    /// </summary>
    [ObservableProperty]
    private bool _isDirty;

    /// <summary>
    /// Gets or sets a value indicating whether this instance can accept changes.
    /// Setting this property marks the view model as dirty.
    /// </summary>
    public virtual bool CanAccept {
        get => _canAccept;
        set {
            SetProperty(ref _canAccept, value);
            IsDirty = true;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this instance can cancel changes.
    /// Setting this property marks the view model as dirty.
    /// </summary>
    public virtual bool CanCancel {
        get => _canCancel;
        set {
            SetProperty(ref _canCancel, value);
            IsDirty = true;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the application is running in design mode (e.g., in Visual Studio designer).
    /// </summary>
    public bool InDesignMode => Debugger.IsAttached;

    /// <summary>
    /// Gets the logger instance for this view model.
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Registers messenger receivers for inter-view model communication.
    /// Called by <see cref="ShellViewModel"/> during construction.
    /// <b>IMPORTANT:</b> Handlers should process minimal data; major data processing should be completed elsewhere.
    /// </summary>
    public abstract void RegisterMessengerReceivers();

    /// <summary>
    /// Handles the view loaded event.
    /// Override in derived classes to perform actions when the view is loaded.
    /// </summary>
    /// <returns>A completed <see cref="Task"/> by default.</returns>
    public virtual Task ViewLoaded() {
        return Task.CompletedTask;
    }
}
