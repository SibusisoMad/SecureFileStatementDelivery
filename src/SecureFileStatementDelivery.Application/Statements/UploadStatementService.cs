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

        var statementId = Guid.NewGuid();
        var relativePath = Path.Combine(request.CustomerId, statementId.ToString("N") + ".pdf");
        var sha256 = await _files.SavePdfAsync(relativePath, request.Content, ct);

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
    }
}
