namespace SecureFileStatementDelivery.Api.Models;

public sealed record StatementListItemDto(
    Guid Id,
    string AccountId,
    string AccountType,
    string Period,
    string FileName,
    long FileSize,
    DateTimeOffset CreatedAt);
