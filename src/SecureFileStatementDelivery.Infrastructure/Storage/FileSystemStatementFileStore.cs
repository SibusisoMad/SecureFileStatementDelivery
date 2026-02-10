using SecureFileStatementDelivery.Application.Interfaces;

namespace SecureFileStatementDelivery.Infrastructure.Storage;

public sealed class FileSystemStatementFileStore : IStatementFileStore
{
    private readonly string _baseDir;

    public FileSystemStatementFileStore(string baseDir)
    {
        _baseDir = baseDir;
    }

    public async Task SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken)
    {
        var fullPath = GetFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(output, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken)
    {
        var fullPath = GetFullPath(relativePath);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    private string GetFullPath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException("Relative path must not be rooted.");
        }

        var combined = Path.Combine(_baseDir, relativePath);
        var full = Path.GetFullPath(combined);
        var baseFull = Path.GetFullPath(_baseDir);

        if (!full.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid relative path.");
        }

        return full;
    }
}
