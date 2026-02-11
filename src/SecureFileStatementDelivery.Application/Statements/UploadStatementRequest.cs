using SecureFileStatementDelivery.Domain.Statements;

namespace SecureFileStatementDelivery.Application.Statements;

public sealed record UploadStatementRequest(
    string CustomerId,
    string AccountId,
    AccountType AccountType,
    string Period,
    string FileName,
    string ContentType,
    long FileSize,
    string Actor,
    Stream Content);
    
