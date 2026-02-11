using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;

namespace SecureFileStatementDelivery.Api.IntegrationTests;

public sealed class StatementApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private static string CreateAdminToken(StatementDeliveryApiFactory factory, DateTimeOffset nowUtc)
    {
        return JwtTokens.Create(
            signingKey: factory.JwtSigningKey,
            issuer: StatementDeliveryApiFactory.Issuer,
            audience: StatementDeliveryApiFactory.Audience,
            subject: "admin-001",
            customerId: null,
            roles: new[] { "admin" },
            nowUtc: nowUtc,
            lifetime: TimeSpan.FromMinutes(30));
    }

    private static string CreateCustomerToken(StatementDeliveryApiFactory factory, DateTimeOffset nowUtc, string subject, string customerId)
    {
        return JwtTokens.Create(
            signingKey: factory.JwtSigningKey,
            issuer: StatementDeliveryApiFactory.Issuer,
            audience: StatementDeliveryApiFactory.Audience,
            subject: subject,
            customerId: customerId,
            roles: Array.Empty<string>(),
            nowUtc: nowUtc,
            lifetime: TimeSpan.FromMinutes(30));
    }

    [Fact]
    public async Task ListCustomerScope()
    {
        await using var api = new StatementDeliveryApiFactory();
        using var client = api.CreateClient();

        var now = api.Clock.UtcNow;
        var adminToken = CreateAdminToken(api, now);
        var cust1Token = CreateCustomerToken(api, now, subject: "cust-001", customerId: "cust-001");
        var cust2Token = CreateCustomerToken(api, now, subject: "cust-002", customerId: "cust-002");

        var statementId = await UploadStatementAsync(client, adminToken, customerId: "cust-001");

        var list1 = await ListStatementsAsync(client, cust1Token);
        Assert.Contains(list1, s => s.Id == statementId);

        var list2 = await ListStatementsAsync(client, cust2Token);
        Assert.DoesNotContain(list2, s => s.Id == statementId);
    }

    [Fact]
    public async Task DownloadCustomerScope()
    {
        await using var api = new StatementDeliveryApiFactory();
        using var client = api.CreateClient();

        var now = api.Clock.UtcNow;
        var adminToken = CreateAdminToken(api, now);
        var cust1Token = CreateCustomerToken(api, now, subject: "cust-001", customerId: "cust-001");
        var cust2Token = CreateCustomerToken(api, now, subject: "cust-002", customerId: "cust-002");

        var statementId = await UploadStatementAsync(client, adminToken, customerId: "cust-001");

        var downloadToken = await CreateDownloadTokenAsync(client, cust1Token, statementId);

        var response = await GetAsync(client, $"/downloads/{downloadToken}", cust2Token);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DownloadLink_Expires()
    {
        await using var api = new StatementDeliveryApiFactory();
        using var client = api.CreateClient();

        var now = api.Clock.UtcNow;
        var adminToken = CreateAdminToken(api, now);
        var customerToken = CreateCustomerToken(api, now, subject: "cust-001", customerId: "cust-001");

        var statementId = await UploadStatementAsync(client, adminToken, customerId: "cust-001");
        var downloadToken = await CreateDownloadTokenAsync(client, customerToken, statementId);

        api.Clock.Advance(TimeSpan.FromMinutes(6));

        var response = await GetAsync(client, $"/downloads/{downloadToken}", customerToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FileSizeUpload()
    {
        await using var api = new StatementDeliveryApiFactory();
        using var client = api.CreateClient();

        var now = api.Clock.UtcNow;
        var adminToken = CreateAdminToken(api, now);

        // Non-PDF content-type
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent("cust-001"), "customerId");
            content.Add(new StringContent("acct-main-001"), "accountId");
            content.Add(new StringContent("2026-01"), "period");

            var bytes = Encoding.UTF8.GetBytes("hello");
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            content.Add(file, "file", "not-a-pdf.txt");

            var response = await PostAsync(client, "/statements", adminToken, content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent("cust-001"), "customerId");
            content.Add(new StringContent("acct-main-001"), "accountId");
            content.Add(new StringContent("2026-01"), "period");

            const int maxBytes = 25 * 1024 * 1024;
            var bytes = new byte[maxBytes + 1];
            var header = Encoding.ASCII.GetBytes("%PDF-1.4\n");
            Buffer.BlockCopy(header, 0, bytes, 0, header.Length);

            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(file, "file", "too-big.pdf");

            var response = await PostAsync(client, "/statements", adminToken, content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [Fact]
    public async Task List_LastMonths_FiltersByPeriod()
    {
        await using var api = new StatementDeliveryApiFactory();
        using var client = api.CreateClient();

        var now = api.Clock.UtcNow;
        var adminToken = CreateAdminToken(api, now);
        var customerToken = CreateCustomerToken(api, now, subject: "cust-001", customerId: "cust-001");

        var period0 = now.ToString("yyyy-MM");
        var period1 = now.AddMonths(-1).ToString("yyyy-MM");
        var period2 = now.AddMonths(-2).ToString("yyyy-MM");
        var period3 = now.AddMonths(-3).ToString("yyyy-MM");

        await UploadStatementAsync(client, adminToken, customerId: "cust-001", accountId: "acct-main-001", period: period0);
        await UploadStatementAsync(client, adminToken, customerId: "cust-001", accountId: "acct-main-001", period: period1);
        await UploadStatementAsync(client, adminToken, customerId: "cust-001", accountId: "acct-main-001", period: period2);
        await UploadStatementAsync(client, adminToken, customerId: "cust-001", accountId: "acct-main-001", period: period3);

        var list3 = await ListStatementsAsync(client, customerToken, "/statements?lastMonths=3&skip=0&take=50");
        Assert.Contains(list3, s => s.Period == period0);
        Assert.Contains(list3, s => s.Period == period1);
        Assert.Contains(list3, s => s.Period == period2);
        Assert.DoesNotContain(list3, s => s.Period == period3);

        var list1 = await ListStatementsAsync(client, customerToken, "/statements?lastMonths=1&skip=0&take=50");
        Assert.Contains(list1, s => s.Period == period0);
        Assert.DoesNotContain(list1, s => s.Period == period1);
        Assert.DoesNotContain(list1, s => s.Period == period2);
        Assert.DoesNotContain(list1, s => s.Period == period3);
    }

    [Fact]
    public async Task List_AccountType_FiltersMainVsSavings()
    {
        await using var api = new StatementDeliveryApiFactory();
        using var client = api.CreateClient();

        var now = api.Clock.UtcNow;
        var adminToken = CreateAdminToken(api, now);
        var customerToken = CreateCustomerToken(api, now, subject: "cust-001", customerId: "cust-001");

        var period = now.ToString("yyyy-MM");

        var mainId = await UploadStatementAsync(
            client,
            adminToken,
            customerId: "cust-001",
            accountId: "acct-main-001",
            period: period,
            accountType: "Main");

        var savingsId = await UploadStatementAsync(
            client,
            adminToken,
            customerId: "cust-001",
            accountId: "acct-savings-001",
            period: period,
            accountType: "Savings");

        var listMain = await ListStatementsAsync(client, customerToken, "/statements?accountType=main&skip=0&take=50");
        Assert.Contains(listMain, s => s.Id == mainId);
        Assert.DoesNotContain(listMain, s => s.Id == savingsId);

        var listSavings = await ListStatementsAsync(client, customerToken, "/statements?accountType=savings&skip=0&take=50");
        Assert.Contains(listSavings, s => s.Id == savingsId);
        Assert.DoesNotContain(listSavings, s => s.Id == mainId);
    }

    private static Task<Guid> UploadStatementAsync(HttpClient client, string adminBearerToken, string customerId)
        => UploadStatementAsync(client, adminBearerToken, customerId, accountId: "acct-main-001", period: "2026-01");

    private static async Task<Guid> UploadStatementAsync(
        HttpClient client,
        string adminBearerToken,
        string customerId,
        string accountId,
        string period,
        string? accountType = null)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(customerId), "customerId");
        content.Add(new StringContent(accountId), "accountId");
        content.Add(new StringContent(period), "period");
        if (!string.IsNullOrWhiteSpace(accountType))
        {
            content.Add(new StringContent(accountType), "accountType");
        }

        var pdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4\n%\u00E2\u00E3\u00CF\u00D3\n1 0 obj\n<<>>\nendobj\n");
        var file = new ByteArrayContent(pdfBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "file", "statement.pdf");

        var response = await PostAsync(client, "/statements", adminBearerToken, content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        static bool TryGetGuidProperty(JsonElement root, string propertyName, out Guid value)
        {
            value = Guid.Empty;
            return root.TryGetProperty(propertyName, out var el)
                && el.ValueKind == JsonValueKind.String
                && Guid.TryParse(el.GetString(), out value);
        }

        if (TryGetGuidProperty(doc.RootElement, "statementId", out var statementId)
            || TryGetGuidProperty(doc.RootElement, "id", out statementId))
        {
            Assert.True(statementId != Guid.Empty, json);
            return statementId;
        }

        Assert.Fail(json);
        return Guid.Empty;
    }

    private static Task<List<StatementListItemDto>> ListStatementsAsync(HttpClient client, string customerBearerToken)
        => ListStatementsAsync(client, customerBearerToken, "/statements?skip=0&take=50");

    private static async Task<List<StatementListItemDto>> ListStatementsAsync(HttpClient client, string customerBearerToken, string path)
    {
        var response = await GetAsync(client, path, customerBearerToken);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected 200 OK but got {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<List<StatementListItemDto>>(json, JsonOptions);
        return payload ?? new List<StatementListItemDto>();
    }

    private static async Task<string> CreateDownloadTokenAsync(HttpClient client, string customerBearerToken, Guid statementId)
    {
        var response = await PostAsync(client, $"/statements/{statementId:D}/download-link", customerBearerToken, new StringContent(string.Empty));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("url", out var urlEl));
        var url = urlEl.GetString();
        Assert.False(string.IsNullOrWhiteSpace(url));

        var uri = new Uri(url!);
        var token = uri.Segments.Last().Trim('/');
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token;
    }

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string path, string bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string path, string bearerToken, HttpContent content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return await client.SendAsync(request);
    }

    private sealed record StatementListItemDto(
        Guid Id,
        string AccountId,
        string AccountType,
        string Period,
        string FileName,
        long FileSize,
        DateTimeOffset CreatedAt);
}
