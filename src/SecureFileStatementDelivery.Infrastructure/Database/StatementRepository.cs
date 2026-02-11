using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureFileStatementDelivery.Application.Interfaces;
using SecureFileStatementDelivery.Domain.Audit;
using SecureFileStatementDelivery.Domain.Statements;

namespace SecureFileStatementDelivery.Infrastructure.Database;

public sealed class StatementRepository : IStatementRepository
{
    private readonly SecureFileStatementDeliveryDbContext _db;
    private readonly ILogger<StatementRepository> _logger;

    public StatementRepository(SecureFileStatementDeliveryDbContext db, ILogger<StatementRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task AddStatementAsync(Statement statement, CancellationToken cancellationToken)
    {
        _db.Statements.Add(statement);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<Statement?> GetStatementAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.Statements.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Statement>> ListStatementsAsync(
        string customerId,
        string? accountId,
        AccountType? accountType,
        int? fromPeriod,
        int? toPeriod,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        IQueryable<Statement> query = _db.Statements.AsNoTracking().Where(s => s.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            query = query.Where(s => s.AccountId == accountId);
        }

        if (accountType is not null)
        {
            query = query.Where(s => s.AccountType == accountType);
        }

        if (fromPeriod is not null)
        {
            query = query.Where(s => s.PeriodKey >= fromPeriod);
        }

        if (toPeriod is not null)
        {
            query = query.Where(s => s.PeriodKey <= toPeriod);
        }

        // SQLite provider cannot translate DateTimeOffset in ORDER BY reliably.
        // PeriodKey is an INTEGER and provides a natural, user-facing sort (latest month first).
        return await query
            .OrderByDescending(s => s.PeriodKey)
            .ThenByDescending(s => s.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Audit persisted: {EventType} statementId={StatementId} customerId={CustomerId} actor={Actor}",
            auditEvent.EventType,
            auditEvent.StatementId,
            auditEvent.CustomerId,
            auditEvent.Actor);

        _db.AuditEvents.Add(auditEvent);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
