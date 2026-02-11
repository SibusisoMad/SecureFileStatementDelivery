using SecureFileStatementDelivery.Application.Interfaces;

namespace SecureFileStatementDelivery.Api.IntegrationTests;

internal sealed class ManualTimeProvider : ITimeProvider
{
    private DateTimeOffset _utcNow;
    private readonly object _lock = new();

    public ManualTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public DateTimeOffset UtcNow
    {
        get
        {
            lock (_lock)
            {
                return _utcNow;
            }
        }
    }

    public void Advance(TimeSpan by)
    {
        lock (_lock)
        {
            _utcNow = _utcNow.Add(by);
        }
    }
}
