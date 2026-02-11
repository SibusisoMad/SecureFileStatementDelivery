using SecureFileStatementDelivery.Application.Interfaces;
using SecureFileStatementDelivery.Domain.Audit;

namespace SecureFileStatementDelivery.Application.Downloads;

public sealed class CreateDownloadLinkService
{
    private readonly ITimeProvider _time;
    private readonly IStatementRepository _statementRepo;
    private readonly IDownloadTokenService _tokens;

    private const int LinkTtlMinutes = 5;

    public CreateDownloadLinkService(ITimeProvider time, IStatementRepository statementRepo, IDownloadTokenService tokens)
    {
        _time = time;
        _statementRepo = statementRepo;
        _tokens = tokens;
    }

    public async Task<CreateDownloadLinkResult?> CreateAsync(CreateDownloadLinkRequest request, CancellationToken ct)
    {
        ValidateRequest(request);

        var statement = await _statementRepo.GetStatementAsync(request.StatementId, ct);
        if (statement is null)
        {
            return null;
        }

        if (!string.Equals(statement.CustomerId, request.CustomerId, StringComparison.Ordinal))
        {
            return null;
        }

        var expiresAt = _time.UtcNow.AddMinutes(LinkTtlMinutes);
        var token = _tokens.CreateToken(new CreateDownloadTokenRequest(statement.Id, request.CustomerId, expiresAt));

        await _statementRepo.AddAuditEventAsync(new AuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = "DownloadLinkGenerated",
            StatementId = statement.Id,
            CustomerId = request.CustomerId,
            Actor = request.Actor,
            Timestamp = _time.UtcNow,
            DetailsJson = $"{{\"expiresAtUtc\":\"{expiresAt:O}\"}}"
        }, ct);

        return new CreateDownloadLinkResult(token, expiresAt);
    }

    private static void ValidateRequest(CreateDownloadLinkRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            throw new ArgumentException("customerId is required");
        }
    }
}
