using System.Security.Cryptography;
using SecureFileStatementDelivery.Application.Interfaces;
using SecureFileStatementDelivery.Domain.Audit;
using SecureFileStatementDelivery.Domain.Statements;

namespace SecureFileStatementDelivery.Application.Statements;

public sealed class UploadStatementService
{
    private readonly ITimeProvider _time;
    private readonly IStatementRepository _statementRepo;
    private readonly IStatementFileStore _files;

    private const long MaxBytes = 25 * 1024 * 1024;
    private const string PdfContentType = "application/pdf";

    public UploadStatementService(ITimeProvider time, IStatementRepository statementRepo, IStatementFileStore files)
    {
        _time = time;
        _statementRepo = statementRepo;
        _files = files;
    }

    public async Task<UploadStatementResult> UploadAsync(UploadStatementRequest request, CancellationToken ct)
    {
        ValidateRequest(request);
        ResetToStartIfSeekable(request.Content);

        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(request.Content, ct);
        var sha256 = Convert.ToHexString(hash).ToLowerInvariant();

        ResetToStartIfSeekable(request.Content);

        var statementId = Guid.NewGuid();
        var relativePath = Path.Combine(request.CustomerId, statementId.ToString("N") + ".pdf");
        await _files.SaveAsync(relativePath, request.Content, ct);

        var statement = new Statement
        {
            Id = statementId,
            CustomerId = request.CustomerId,
            AccountId = request.AccountId,
            Period = request.Period,
            OriginalFileName = request.OriginalFileName,
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            Sha256 = sha256,
            StoredPath = relativePath,
            CreatedAtUtc = _time.UtcNow
        };

        await _statementRepo.AddStatementAsync(statement, ct);
        await _statementRepo.AddAuditEventAsync(new AuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = "StatementUploaded",
            StatementId = statement.Id,
            CustomerId = request.CustomerId,
            Actor = request.Actor,
            Timestamp = _time.UtcNow
        }, ct);

        return new UploadStatementResult(statement.Id);
    }

    private static void ValidateRequest(UploadStatementRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId)
            || string.IsNullOrWhiteSpace(request.AccountId)
            || string.IsNullOrWhiteSpace(request.Period))
        {
            throw new ArgumentException("customerId, accountId, period are required");
        }

        if (!string.Equals(request.ContentType, PdfContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Only {PdfContentType} is allowed");
        }

        if (request.SizeBytes <= 0 || request.SizeBytes > MaxBytes)
        {
            throw new ArgumentException("File must be between 1 byte and 25MB");
        }

        if (!LooksLikePdf(request.Content))
        {
            throw new ArgumentException("File does not look like a PDF");
        }
    }

    private static void ResetToStartIfSeekable(Stream stream)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
    }

    private static bool LooksLikePdf(Stream stream)
    {
        if (!stream.CanRead)
        {
            return false;
        }

        var originalPosition = stream.CanSeek ? stream.Position : 0;
        try
        {
            var header = new byte[5];
            var read = stream.Read(header, 0, header.Length);
            if (read != header.Length)
            {
                return false;
            }

            return header[0] == (byte)'%'
                && header[1] == (byte)'P'
                && header[2] == (byte)'D'
                && header[3] == (byte)'F'
                && header[4] == (byte)'-';
        }
        finally
        {
            if (stream.CanSeek)
            {
                stream.Position = originalPosition;
            }
        }
    }
}
