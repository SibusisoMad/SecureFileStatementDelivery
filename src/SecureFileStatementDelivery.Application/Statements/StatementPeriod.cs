using System.Globalization;

namespace SecureFileStatementDelivery.Application.Statements;

public static class StatementPeriod
{
    public static bool TryParseYearMonth(string periodText, out int yearMonth)
    {
        yearMonth = default;

        if (string.IsNullOrWhiteSpace(periodText))
        {
            return false;
        }

        
        if (!DateTime.TryParseExact(periodText, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return false;
        }

        yearMonth = (dt.Year * 100) + dt.Month;
        return true;
    }

    public static int ParseYearMonth(string periodText)
    {
        if (!TryParseYearMonth(periodText, out var yearMonth))
        {
            throw new ArgumentException("Invalid period format. Expected 'YYYY-MM'.", nameof(periodText));
        }

        return yearMonth;
    }

    public static int CurrentYearMonth(DateTimeOffset now)
        => (now.Year * 100) + now.Month;

    public static int YearMonthMonthsAgo(DateTimeOffset now, int monthsAgo)
    {
        var dt = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-monthsAgo);
        return (dt.Year * 100) + dt.Month;
    }
}
