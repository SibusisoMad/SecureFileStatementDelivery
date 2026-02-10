namespace SecureFileStatementDelivery.Application.Interfaces;

public interface IStatementFileStore
{
    Task SaveAsync(
        string relativePath,
        Stream content,
        CancellationToken cancellationToken);

    Task<string> SavePdfAsync(
        string relativePath,
        Stream content,
        CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(
        string relativePath,
        CancellationToken cancellationToken);
}
