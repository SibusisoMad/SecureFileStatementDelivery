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

        var periodKey = StatementPeriod.ParseYearMonth(request.Period);

        var statementId = Guid.NewGuid();
        var relativePath = Path.Combine(request.CustomerId, statementId.ToString("N") + ".pdf");
        var sha256 = await _files.SavePdfAsync(relativePath, request.Content, ct);

        var statement = new Statement
        {
            Id = statementId,
            CustomerId = request.CustomerId,
            AccountId = request.AccountId,
            AccountType = request.AccountType,
            Period = request.Period,
            PeriodKey = periodKey,
            FileName = request.FileName,
            ContentType = request.ContentType,
            FileSize = request.FileSize,
            Sha256 = sha256,
            StoredPath = relativePath,
            CreatedAt = _time.UtcNow
        };

        await _statementRepo.AddStatementAsync(statement, ct);
        await _statementRepo.AddAuditEventAsync(new AuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = "StatementUploaded",
            StatementId = statement.Id,
            CustomerId = request.CustomerId,
            Actor = request.Actor,
            Timestamp = _time.UtcNow,
            DetailsJson = $"{{\"sha256\":\"{sha256}\",\"FileSize\":{request.FileSize}}}"
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

        if (!StatementPeriod.TryParseYearMonth(request.Period, out _))
        {
            throw new ArgumentException("period must be in 'YYYY-MM' format");
        }

        if (!string.Equals(request.ContentType, PdfContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Only {PdfContentType} is allowed");
        }

        if (request.FileSize <= 0 || request.FileSize > MaxBytes)
        {
            throw new ArgumentException("File must be between 1 byte and 25MB");
        }
    }
}
