using SecureFileStatementDelivery.Application.Enums;

namespace SecureFileStatementDelivery.Application.Downloads;

public sealed record DownloadStatementResult(
    DownloadOutcome Outcome,
    Guid? StatementId,
    Stream? Stream,
    string? ContentType,
    string? FileName)
{
    public static DownloadStatementResult NotFound() => new(DownloadOutcome.NotFound, null, null, null, null);

    public static DownloadStatementResult Forbidden(Guid? statementId = null) => new(DownloadOutcome.Forbidden, statementId, null, null, null);

    public static DownloadStatementResult Ok(Guid statementId, Stream stream, string contentType, string fileName) =>
        new(DownloadOutcome.Ok, statementId, stream, contentType, fileName);
}
