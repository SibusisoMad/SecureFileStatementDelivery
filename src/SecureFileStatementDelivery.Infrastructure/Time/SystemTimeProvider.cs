using SecureFileStatementDelivery.Application.Interfaces;

namespace SecureFileStatementDelivery.Infrastructure.Time;

public sealed class SystemTimeProvider : ITimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
