namespace SecureFileStatementDelivery.Application.Statements;

public sealed record UploadStatementRequest(
    string CustomerId,
    string AccountId,
    string Period,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Actor,
    Stream Content);
