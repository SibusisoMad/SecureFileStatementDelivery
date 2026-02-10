namespace SecureFileStatementDelivery.Application.Downloads;

public sealed record DownloadStatementRequest(
    string Token,
    string CustomerId,
    string Actor);
