using Microsoft.Win32;

namespace LiteDB.Studio.Wpf.Services;

/// <summary>Options for an open-file dialog.</summary>
public sealed class OpenFileDialogOptions
{
    /// <summary>Gets the file-type filter string (e.g. <c>"SQL files (*.sql)|*.sql"</c>).</summary>
    public string Filter { get; init; } = "All files (*.*)|*.*";
    /// <summary>Gets the dialog title, or <c>null</c> to use the default.</summary>
    public string? Title { get; init; }
    /// <summary>Gets the directory shown when the dialog opens, or <c>null</c> to use the default.</summary>
    public string? InitialDirectory { get; init; }
}

/// <summary>Options for a save-file dialog.</summary>
public sealed class SaveFileDialogOptions
{
    /// <summary>Gets the file-type filter string.</summary>
    public string Filter { get; init; } = "All files (*.*)|*.*";
    /// <summary>Gets the dialog title, or <c>null</c> to use the default.</summary>
    public string? Title { get; init; }
    /// <summary>Gets the suggested file name pre-populated in the dialog.</summary>
    public string? FileName { get; init; }
    /// <summary>Gets the directory shown when the dialog opens, or <c>null</c> to use the default.</summary>
    public string? InitialDirectory { get; init; }
}

/// <summary>Provides methods for showing open-file and save-file dialogs.</summary>
public interface IFileDialogService
{
    /// <summary>Shows an open-file dialog and returns the selected path, or <c>null</c> if cancelled.</summary>
    /// <param name="options">Dialog configuration options.</param>
    string? OpenFile(OpenFileDialogOptions options);
    /// <summary>Shows a save-file dialog and returns the chosen path, or <c>null</c> if cancelled.</summary>
    /// <param name="options">Dialog configuration options.</param>
    string? SaveFile(SaveFileDialogOptions options);
}

/// <summary>Win32 <see cref="Microsoft.Win32.OpenFileDialog"/>/<see cref="Microsoft.Win32.SaveFileDialog"/>-backed implementation of <see cref="IFileDialogService"/>.</summary>
public sealed class FileDialogService : IFileDialogService
{
    /// <inheritdoc />
    public string? OpenFile(OpenFileDialogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var dialog = new OpenFileDialog
        {
            Filter = options.Filter,
            Title = options.Title,
            InitialDirectory = options.InitialDirectory
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <inheritdoc />
    public string? SaveFile(SaveFileDialogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.FileName == null) { return null; }

        var dialog = new SaveFileDialog
        {
            Filter = options.Filter,
            Title = options.Title,
            FileName = options.FileName,
            InitialDirectory = options.InitialDirectory
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;

    }
}
