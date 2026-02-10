namespace SecureFileStatementDelivery.Infrastructure.Tokens;

public sealed class DownloadTokenOptions
{
    public const string SectionName = "DownloadTokens";

    // Base64 recommended; plain text also works for local/dev.
    public required string Secret { get; init; }
}
