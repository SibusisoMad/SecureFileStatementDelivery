using SecureFileStatementDelivery.Domain.Statements;

namespace SecureFileStatementDelivery.Application.Statements;

public sealed record ListStatementsRequest(
    string CustomerId,
    string? AccountId,
    AccountType? AccountType,
    string? Period,
    int? LastMonths,
    int Skip,
    int Take);
