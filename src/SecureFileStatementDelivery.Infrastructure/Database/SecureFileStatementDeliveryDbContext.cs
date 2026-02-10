using Microsoft.EntityFrameworkCore;
using SecureFileStatementDelivery.Domain.Audit;
using SecureFileStatementDelivery.Domain.Statements;

namespace SecureFileStatementDelivery.Infrastructure.Database;

public sealed class SecureFileStatementDeliveryDbContext : DbContext
{
    public SecureFileStatementDeliveryDbContext(DbContextOptions<SecureFileStatementDeliveryDbContext> options) : base(options)
    {
    }

    public DbSet<Statement> Statements => Set<Statement>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Statement>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.CustomerId).IsRequired();
            b.Property(x => x.AccountId).IsRequired();
            b.Property(x => x.Period).IsRequired();
            b.Property(x => x.OriginalFileName).IsRequired();
            b.Property(x => x.ContentType).IsRequired();
            b.Property(x => x.Sha256).IsRequired();
            b.Property(x => x.StoredPath).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();

            b.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc });
            b.HasIndex(x => new { x.CustomerId, x.AccountId, x.Period });
        });

        modelBuilder.Entity<AuditEvent>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.EventType).IsRequired();
            b.Property(x => x.CustomerId).IsRequired();
            b.Property(x => x.Actor).IsRequired();
            b.Property(x => x.Timestamp).IsRequired();
        });
    }
}
