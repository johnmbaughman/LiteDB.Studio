using System.IO;

namespace LiteDB.Studio.Wpf.Services;

public interface IFileService
{
    Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);
}

public sealed class FileService : IFileService
{
    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        return File.WriteAllTextAsync(path, contents, cancellationToken);
    }
}
