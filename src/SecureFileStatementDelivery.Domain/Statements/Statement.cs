namespace SecureFileStatementDelivery.Domain.Statements;

public sealed class Statement
{
    public Guid Id { get; set; }

    public required string CustomerId { get; set; }
    public required string AccountId { get; set; }
    public required string Period { get; set; }

    public required string OriginalFileName { get; set; }
    public required string ContentType { get; set; }

    public long SizeBytes { get; set; }
    public required string Sha256 { get; set; }

    public required string StoredPath { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
