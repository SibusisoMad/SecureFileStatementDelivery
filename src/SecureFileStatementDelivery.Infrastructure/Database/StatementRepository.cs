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
        string? period,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        IQueryable<Statement> query = _db.Statements.AsNoTracking().Where(s => s.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            query = query.Where(s => s.AccountId == accountId);
        }

        if (!string.IsNullOrWhiteSpace(period))
        {
            query = query.Where(s => s.Period == period);
        }

        var filtered = await query.ToListAsync(cancellationToken);
        return filtered
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToList();
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
