using SecureFileStatementDelivery.Application.Interfaces;
using System.Security.Cryptography;

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

    public async Task<string> SavePdfAsync(string relativePath, Stream content, CancellationToken cancellationToken)
    {
        if (!content.CanRead)
        {
            throw new ArgumentException("Content stream must be readable", nameof(content));
        }

        var fullPath = GetFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        var header = new byte[5];
        await ReadExactlyAsync(content, header, cancellationToken);

        if (!LooksLikePdfHeader(header))
        {
            throw new ArgumentException("File does not look like a PDF");
        }

        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hasher.AppendData(header);

        await using var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await output.WriteAsync(header, cancellationToken);

        var buffer = new byte[1024 * 64];
        while (true)
        {
            var read = await content.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            hasher.AppendData(buffer, 0, read);
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        var hash = hasher.GetHashAndReset();
        return Convert.ToHexString(hash).ToLowerInvariant();
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

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken);
            if (read == 0)
            {
                throw new ArgumentException("File does not look like a PDF");
            }

            offset += read;
        }
    }

    private static bool LooksLikePdfHeader(byte[] header)
    {
        return header.Length == 5
            && header[0] == (byte)'%'
            && header[1] == (byte)'P'
            && header[2] == (byte)'D'
            && header[3] == (byte)'F'
            && header[4] == (byte)'-';
    }
}
