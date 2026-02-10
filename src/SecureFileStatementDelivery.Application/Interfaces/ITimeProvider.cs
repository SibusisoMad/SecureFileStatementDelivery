namespace SecureFileStatementDelivery.Application.Interfaces;

public interface ITimeProvider
{
    DateTimeOffset UtcNow { get; }
}
