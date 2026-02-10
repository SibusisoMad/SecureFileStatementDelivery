namespace SecureFileStatementDelivery.Api.Errors;

internal static class ApiErrors
{
    public static IResult BadRequest(string title, string detail)
        => ErrorResult(StatusCodes.Status400BadRequest, title, detail);

    public static IResult InvalidRequest(string detail)
        => ErrorResult(StatusCodes.Status400BadRequest, "Invalid request", detail);

    private static IResult ErrorResult(int statusCode, string title, string detail)
        => Results.Problem(statusCode: statusCode, title: title, detail: detail);
}
