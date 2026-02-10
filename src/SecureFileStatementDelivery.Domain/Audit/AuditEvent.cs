namespace SecureFileStatementDelivery.Domain.Audit;

public sealed class AuditEvent
{
    public Guid Id { get; set; }

    public required string EventType { get; set; }

    public Guid? StatementId { get; set; }
    public required string CustomerId { get; set; }

    public required string Actor { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public string? DetailsJson { get; set; }
}
