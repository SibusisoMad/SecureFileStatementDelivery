namespace SecureFileStatementDelivery.Domain.Statements;

public sealed class Statement
{
    public Guid Id { get; set; }

    public required string CustomerId { get; set; }
    public required string AccountId { get; set; }
    public AccountType AccountType { get; set; } = AccountType.Main;
    public required string Period { get; set; }
    public int PeriodKey { get; set; }

    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long FileSize { get; set; }
    public required string Sha256 { get; set; }

    public required string StoredPath { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
