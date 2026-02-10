namespace SecureFileStatementDelivery.Application.Interfaces;

public interface IStatusProbe
{
    Task<ServiceStatus> GetAsync(CancellationToken ct);
}
