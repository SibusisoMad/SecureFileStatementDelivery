using System.Security.Claims;
using SecureFileStatementDelivery.Api.Auth;
using SecureFileStatementDelivery.Api.Models;
using SecureFileStatementDelivery.Application.Downloads;
using SecureFileStatementDelivery.Application.Enums;
using SecureFileStatementDelivery.Application.Statements;

namespace SecureFileStatementDelivery.Api.Routes;

internal static class StatementRoutes
{
    public static IEndpointRouteBuilder MapStatementRoutes(this IEndpointRouteBuilder app)
    {
        var statements = app.MapGroup("/statements").WithTags("Statements");

        statements.MapPost("/", UploadStatement)
            .RequireAuthorization("AdminOnly")
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<UploadStatementResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithName("UploadStatement")
            .WithOpenApi();

        statements.MapGet("/", ListStatements)
            .Produces<IEnumerable<StatementListItemDto>>(StatusCodes.Status200OK)
            .WithName("ListStatements")
            .WithOpenApi();

        statements.MapPost("/{id:guid}/download-link", CreateDownloadLink)
            .Produces<DownloadLinkResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("CreateDownloadLink")
            .WithOpenApi();

        app.MapGet("/downloads/{token}", DownloadByToken)
            .WithTags("Downloads")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .WithName("DownloadByToken")
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> UploadStatement(
        HttpRequest request,
        ClaimsPrincipal user,
        ILoggerFactory loggerFactory,
        UploadStatementService service,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("Statements");

        if (!request.HasFormContentType)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid content type", detail: "multipart/form-data required");
        }

        var form = await request.ReadFormAsync(ct);
        var file = form.Files.GetFile("file");
        if (file is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Missing file", detail: "file is required");
        }

        var customerId = form["customerId"].ToString();
        var accountId = form["accountId"].ToString();
        var period = form["period"].ToString();

        if (string.IsNullOrWhiteSpace(customerId) || string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(period))
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Missing fields", detail: "customerId, accountId, period are required");
        }

        if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid file type", detail: "Only application/pdf is allowed");
        }

        const long maxBytes = 25 * 1024 * 1024;
        if (file.Length <= 0 || file.Length > maxBytes)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid file size", detail: "File must be between 1 byte and 25MB");
        }

        await using var input = file.OpenReadStream();
        await using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        try
        {
            var result = await service.UploadAsync(new UploadStatementRequest(
                CustomerId: customerId,
                AccountId: accountId,
                Period: period,
                OriginalFileName: file.FileName,
                ContentType: file.ContentType,
                SizeBytes: file.Length,
                Actor: user.GetActor(),
                Content: buffer), ct);

            logger.LogInformation("Statement uploaded. statementId={StatementId} customerId={CustomerId} sizeBytes={SizeBytes}", result.StatementId, customerId, file.Length);
            return Results.Created($"/statements/{result.StatementId}", new UploadStatementResponse(result.StatementId));
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid request", detail: ex.Message);
        }
    }

    private static async Task<IResult> ListStatements(
        ClaimsPrincipal user,
        ListStatementsService service,
        string? accountId,
        string? period,
        int skip,
        int take,
        CancellationToken ct)
    {
        var customerId = user.GetCustomerId();
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return Results.Unauthorized();
        }

        IReadOnlyList<SecureFileStatementDelivery.Domain.Statements.Statement> statements;
        try
        {
            statements = await service.ListAsync(new ListStatementsRequest(customerId, accountId, period, skip, take), ct);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid request", detail: ex.Message);
        }

        var response = statements.Select(s => new StatementListItemDto(
            s.Id,
            s.AccountId,
            s.Period,
            s.OriginalFileName,
            s.SizeBytes,
            s.CreatedAtUtc));

        return Results.Ok(response);
    }

    private static async Task<IResult> CreateDownloadLink(
        Guid id,
        ClaimsPrincipal user,
        ILoggerFactory loggerFactory,
        CreateDownloadLinkService service,
        HttpContext http,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("Downloads");

        var customerId = user.GetCustomerId();
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return Results.Unauthorized();
        }

        CreateDownloadLinkResult? result;
        try
        {
            result = await service.CreateAsync(new CreateDownloadLinkRequest(id, customerId, user.GetActor()), ct);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid request", detail: ex.Message);
        }

        if (result is null)
        {
            return Results.NotFound();
        }

        var url = $"{http.Request.Scheme}://{http.Request.Host}/downloads/{result.Token}";

        logger.LogInformation("Download link generated. statementId={StatementId} customerId={CustomerId} expiresAtUtc={ExpiresAtUtc}", id, customerId, result.ExpiresAtUtc);
        return Results.Ok(new DownloadLinkResponse(url, result.ExpiresAtUtc));
    }

    private static async Task<IResult> DownloadByToken(
        string token,
        ClaimsPrincipal user,
        ILoggerFactory loggerFactory,
        DownloadStatementService service,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("Downloads");

        var customerId = user.GetCustomerId();
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return Results.Unauthorized();
        }

        DownloadStatementResult result;
        try
        {
            result = await service.DownloadAsync(new DownloadStatementRequest(token, customerId, user.GetActor()), ct);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid request", detail: ex.Message);
        }

        if (result.Outcome == DownloadOutcome.NotFound)
        {
            return Results.NotFound();
        }

        if (result.Outcome == DownloadOutcome.Forbidden)
        {
            return Results.Forbid();
        }

        logger.LogInformation("Statement downloaded. statementId={StatementId} customerId={CustomerId}", result.StatementId, customerId);
        return Results.File(result.Stream!, result.ContentType!, fileDownloadName: result.FileName!, enableRangeProcessing: true);
    }
}
