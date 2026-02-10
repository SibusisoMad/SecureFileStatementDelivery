namespace SecureFileStatementDelivery.Api.Models;

public sealed record DownloadLinkResponse(string Url, DateTimeOffset ExpiresAtUtc);
