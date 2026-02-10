using SecureFileStatementDelivery.Domain.Audit;
using SecureFileStatementDelivery.Domain.Statements;

namespace SecureFileStatementDelivery.Application.Interfaces;

public interface IStatementRepository
{
    Task AddStatementAsync(Statement statement, CancellationToken cancellationToken);
    Task<Statement?> GetStatementAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Statement>> ListStatementsAsync(string customerId, string? accountId, string? period, int skip, int take, CancellationToken cancellationToken);

    Task AddAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}
