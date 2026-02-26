using System.IO;

namespace LiteDB.Studio.Wpf.Services;

/// <summary>Abstracts file-system read/write operations for easier testing.</summary>
public interface IFileService
{
    /// <summary>Reads all text from the file at <paramref name="path"/> asynchronously.</summary>
    /// <param name="path">Absolute path to the file.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Writes <paramref name="contents"/> to the file at <paramref name="path"/> asynchronously.</summary>
    /// <param name="path">Absolute path to the destination file.</param>
    /// <param name="contents">Text content to write.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);
}

/// <summary><see cref="System.IO.File"/>-backed implementation of <see cref="IFileService"/>.</summary>
public sealed class FileService : IFileService
{
    /// <inheritdoc />
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
    {
        return File.ReadAllTextAsync(path, cancellationToken);
    }

    /// <inheritdoc />
    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        return File.WriteAllTextAsync(path, contents, cancellationToken);
    }
}
