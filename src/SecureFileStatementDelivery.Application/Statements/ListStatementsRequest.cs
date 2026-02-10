namespace SecureFileStatementDelivery.Application.Statements;

public sealed record ListStatementsRequest(
    string CustomerId,
    string? AccountId,
    string? Period,
    int Skip,
    int Take);
