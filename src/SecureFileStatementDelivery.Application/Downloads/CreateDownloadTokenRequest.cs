namespace SecureFileStatementDelivery.Application.Downloads;

public sealed record CreateDownloadTokenRequest(
    Guid StatementId,
    string CustomerId,
    DateTimeOffset ExpiresAtUtc);
