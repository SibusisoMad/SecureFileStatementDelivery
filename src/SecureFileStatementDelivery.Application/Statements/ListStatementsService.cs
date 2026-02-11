using SecureFileStatementDelivery.Application.Interfaces;
using SecureFileStatementDelivery.Domain.Statements;

namespace SecureFileStatementDelivery.Application.Statements;

public sealed class ListStatementsService
{
    private readonly IStatementRepository _statementRepo;
    private readonly ITimeProvider _timer;

    private const int DefaultTake = 50;
    private const int MaxTake = 200;

    public ListStatementsService(IStatementRepository statementRepo, ITimeProvider timer)
    {
        _statementRepo = statementRepo;
        _timer = timer;
    }

    public async Task<IReadOnlyList<Statement>> ListAsync(ListStatementsRequest request, CancellationToken ct)
    {
        ValidateRequest(request);

        var take = request.Take;
        if (take <= 0)
        {
            take = DefaultTake;
        }
        else if (take > MaxTake)
        {
            take = MaxTake;
        }

        var skip = request.Skip;
        if (skip < 0)
        {
            skip = 0;
        }

        int? fromPeriod = null;
        int? toPeriod = null;

        if (!string.IsNullOrWhiteSpace(request.Period))
        {
            var periodKey = StatementPeriod.ParseYearMonth(request.Period);
            fromPeriod = periodKey;
            toPeriod = periodKey;
        }
        else if (request.LastMonths is not null)
        {
            var lastMonths = request.LastMonths.Value;
            if (lastMonths is not (1 or 3))
            {
                throw new ArgumentException("lastMonths must be 1 or 3", nameof(request.LastMonths));
            }

            var now = _timer.UtcNow;
            toPeriod = StatementPeriod.CurrentYearMonth(now);
            fromPeriod = StatementPeriod.YearMonthMonthsAgo(now, lastMonths - 1);
        }

        return await _statementRepo.ListStatementsAsync(
            request.CustomerId,
            request.AccountId,
            request.AccountType,
            fromPeriod,
            toPeriod,
            skip,
            take,
            ct);
    }

    private static void ValidateRequest(ListStatementsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            throw new ArgumentException("customerId is required");
        }
    }
}
