namespace SecureFileStatementDelivery.Application.Statements;

public sealed record StatementListItem(
    Guid Id,
    string AccountId,
    string Period,
    string FileName,
    long FileSize,
    DateTimeOffset CreatedAt);
