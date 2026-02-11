namespace SecureFileStatementDelivery.Application.Downloads;

public sealed record ValidatedDownloadToken(
    Guid TokenId,
    Guid StatementId,
    string CustomerId,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc);
