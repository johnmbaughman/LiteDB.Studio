using Serilog;

namespace LiteDB.Studio.Mvvm.ViewModels;

/// <summary>
/// Represents a base interface for all ViewModel classes.
/// Provides common properties and methods for UI state, OPC control, label printing, and logging.
/// </summary>
public interface IViewModel {
    /// <summary>
    /// Gets or sets a value indicating whether this instance can accept the current operation.
    /// </summary>
    /// <value><c>true</c> if this instance can accept; otherwise, <c>false</c>.</value>
    bool CanAccept { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance can cancel the current operation.
    /// </summary>
    /// <value><c>true</c> if this instance can cancel; otherwise, <c>false</c>.</value>
    bool CanCancel { get; set; }

    /// <summary>
    /// Gets a value indicating whether the ViewModel is running in design mode.
    /// </summary>
    /// <value><c>true</c> if in design mode; otherwise, <c>false</c>.</value>
    bool InDesignMode { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is performing a background operation.
    /// </summary>
    /// <value><c>true</c> if busy; otherwise, <c>false</c>.</value>
    bool IsBusy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance has unsaved changes.
    /// </summary>
    /// <value><c>true</c> if dirty; otherwise, <c>false</c>.</value>
    bool IsDirty { get; set; }

    /// <summary>
    /// Gets the logger for diagnostic and error logging.
    /// </summary>
    /// <value>The logger.</value>
    ILogger Logger { get; }

    /// <summary>
    /// Registers messenger receivers for inter-component communication.
    /// </summary>
    void RegisterMessengerReceivers();

    /// <summary>
    /// Called when the view is loaded. Used for initialization logic.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ViewLoaded();
}
