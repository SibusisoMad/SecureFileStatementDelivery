using SecureFileStatementDelivery.Application.Interfaces;
using SecureFileStatementDelivery.Application.Enums;
using SecureFileStatementDelivery.Domain.Audit;

namespace SecureFileStatementDelivery.Application.Downloads;

public sealed class DownloadStatementService
{
    private readonly ITimeProvider _time;
    private readonly IDownloadTokenService _tokens;
    private readonly IStatementRepository _statementRepo;
    private readonly IStatementFileStore _files;

    public DownloadStatementService(
        ITimeProvider time,
        IDownloadTokenService tokens,
        IStatementRepository statementRepo,
        IStatementFileStore files)
    {
        _time = time;
        _tokens = tokens;
        _statementRepo = statementRepo;
        _files = files;
    }

    public async Task<DownloadStatementResult> DownloadAsync(DownloadStatementRequest request, CancellationToken ct)
    {
        ValidateRequest(request);

        if (!_tokens.TryValidate(request.Token, out var validated))
        {
            return DownloadStatementResult.NotFound();
        }

        if (validated.ExpiresAtUtc <= _time.UtcNow)
        {
            return DownloadStatementResult.NotFound();
        }

        if (!string.Equals(validated.CustomerId, request.CustomerId, StringComparison.Ordinal))
        {
            return DownloadStatementResult.Forbidden(validated.StatementId);
        }

        var statement = await _statementRepo.GetStatementAsync(validated.StatementId, ct);
        if (statement is null)
        {
            return DownloadStatementResult.NotFound();
        }

        if (!string.Equals(statement.CustomerId, request.CustomerId, StringComparison.Ordinal))
        {
            return DownloadStatementResult.Forbidden(statement.Id);
        }

        Stream stream;
        try
        {
            stream = await _files.OpenReadAsync(statement.StoredPath, ct);
        }
        catch (FileNotFoundException)
        {
            return DownloadStatementResult.NotFound();
        }

        await _statementRepo.AddAuditEventAsync(new AuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = "StatementDownloaded",
            StatementId = statement.Id,
            CustomerId = request.CustomerId,
            Actor = request.Actor,
            Timestamp = _time.UtcNow
        }, ct);

        return DownloadStatementResult.Ok(statement.Id, stream, statement.ContentType, statement.OriginalFileName);
    }

    private static void ValidateRequest(DownloadStatementRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            throw new ArgumentException("customerId is required");
        }
    }
}
