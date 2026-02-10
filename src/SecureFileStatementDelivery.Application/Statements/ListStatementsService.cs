using SecureFileStatementDelivery.Application.Interfaces;
using SecureFileStatementDelivery.Domain.Statements;

namespace SecureFileStatementDelivery.Application.Statements;

public sealed class ListStatementsService
{
    private readonly IStatementRepository _statementRepo;

    private const int DefaultTake = 50;
    private const int MaxTake = 200;

    public ListStatementsService(IStatementRepository statementRepo)
    {
        _statementRepo = statementRepo;
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

        return await _statementRepo.ListStatementsAsync(request.CustomerId, request.AccountId, request.Period, skip, take, ct);
    }

    private static void ValidateRequest(ListStatementsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            throw new ArgumentException("customerId is required");
        }
    }
}
