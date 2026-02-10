namespace SecureFileStatementDelivery.Application.Downloads;

public sealed record CreateDownloadLinkResult(string Token, DateTimeOffset ExpiresAtUtc);
