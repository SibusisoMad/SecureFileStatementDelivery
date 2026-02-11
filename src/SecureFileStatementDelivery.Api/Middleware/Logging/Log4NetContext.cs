namespace SecureFileStatementDelivery.Api.Middleware.Logging;

internal static class Log4NetContext
{
    public static IDisposable PushProperty(string name, object? value)
    {
        var previousValue = log4net.LogicalThreadContext.Properties[name];
        log4net.LogicalThreadContext.Properties[name] = value;

        return new Restore(() => log4net.LogicalThreadContext.Properties[name] = previousValue);
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
