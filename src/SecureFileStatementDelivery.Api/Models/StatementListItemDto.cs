namespace SecureFileStatementDelivery.Api.Models;

public sealed record StatementListItemDto(
    Guid Id,
    string AccountId,
    string Period,
    string OriginalFileName,
    long SizeBytes,
    DateTimeOffset CreatedAtUtc);
