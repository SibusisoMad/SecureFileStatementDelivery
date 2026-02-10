namespace SecureFileStatementDelivery.Application.Downloads;

public sealed record CreateDownloadLinkRequest(
    Guid StatementId,
    string CustomerId,
    string Actor);
