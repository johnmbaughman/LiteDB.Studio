using Microsoft.Win32;

namespace LiteDB.Studio.Wpf.Services;

public sealed class OpenFileDialogOptions
{
    public string Filter { get; init; } = "All files (*.*)|*.*";
    public string? Title { get; init; }
    public string? InitialDirectory { get; init; }
}

public sealed class SaveFileDialogOptions
{
    public string Filter { get; init; } = "All files (*.*)|*.*";
    public string? Title { get; init; }
    public string? FileName { get; init; }
    public string? InitialDirectory { get; init; }
}

public interface IFileDialogService
{
    string? OpenFile(OpenFileDialogOptions options);
    string? SaveFile(SaveFileDialogOptions options);
}

public sealed class FileDialogService : IFileDialogService
{
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

    public string? SaveFile(SaveFileDialogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

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
