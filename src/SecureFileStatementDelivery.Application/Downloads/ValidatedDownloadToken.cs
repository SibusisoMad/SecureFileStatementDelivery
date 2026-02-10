namespace SecureFileStatementDelivery.Application.Downloads;

public sealed record ValidatedDownloadToken(
    Guid StatementId,
    string CustomerId,
    DateTimeOffset ExpiresAtUtc);
