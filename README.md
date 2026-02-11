# Build, Run, and Test Guide (Secure File Statement Delivery)

This project contains a .NET 8 backend for uploading PDF statements, listing statements per customer, generating short-lived download links, and downloading via **JWT + time-limited token**.

## Prerequisites

- .NET SDK 8.x (`dotnet --version`)
- Docker Desktop (for the Docker flow)
- (Optional) VS Code REST Client extension to run the `.http` file

## Configuration

Secrets are expected via environment variables.

### Docker Desktop or Hub is (recommended)

Docker Compose reads from the root `.env` file automatically.

Required keys:
- `JWT_SIGNING_KEY` 32+ chars
- `DOWNLOAD_TOKEN_SECRET` 32+ chars

See `.env.example` for a template.

### Local run

For local `dotnet run`, set these environment variables in your shell:
- `DATA_DIR` 
- `Jwt__Issuer` 
- `Jwt__Audience` 
- `Jwt__SigningKey`
- `DownloadTokens__Secret`

PowerShell example:

```powershell
$env:DATA_DIR = "C:\temp\sfsd-data"
$env:Jwt__Issuer = "SecureFileStatementDelivery"
$env:Jwt__Audience = "SecureFileStatementDelivery"
$env:Jwt__SigningKey = "32+ chars"
$env:DownloadTokens__Secret = "32+ chars"
```

## Build

From the repo root:

```powershell
dotnet restore
dotnet build -c Release .\SecureFileStatementDelivery.sln
```

## Run locally

```powershell
dotnet run -c Release --project .\src\SecureFileStatementDelivery.Api\SecureFileStatementDelivery.Api.csproj --urls "http://localhost:8080"
```

Verify:
- `GET http://localhost:8080/status`

## Run in Docker

Build and start:

```powershell
docker compose up -d --build
```


Open in browser:
- Swagger UI: `http://localhost:8080/swagger`
- Download portal page: `http://localhost:8080/statementDownloads.html`

Logs / status:

```powershell
docker compose ps
docker compose logs --tail 200 api
```

Stop:

```powershell
docker compose down
```

## Test

### Run all tests (recommended)

```powershell
dotnet test -c Release .\SecureFileStatementDelivery.sln -m:1
```

If you ever see an MSBuild “Attempting to cancel the build…” with no errors, try:

```powershell
dotnet build -c Release .\SecureFileStatementDelivery.sln
dotnet test -c Release .\SecureFileStatementDelivery.sln --no-build -m:1
```

### Run just the integration tests (fastest)

```powershell
dotnet test -c Release .\tests\SecureFileStatementDelivery.Api.IntegrationTests\SecureFileStatementDelivery.Api.IntegrationTests.csproj
```

## Quick end-to-end checks

### VS Code `.http` flow

Use the REST Client file:
- `src/SecureFileStatementDelivery.Api/SecureFileStatementDelivery.Api.http`

You must paste valid JWTs into `@adminToken` and `@customerToken`.

### Docker end-to-end script

This script mints local JWTs from `.env`, uploads a tiny PDF, lists, creates a link, and downloads it:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\e2e-docker.ps1
```

## Download portal (why it exists)

The backend enforces **JWT Bearer auth** even for time-limited download tokens.

Because a plain browser link can’t add the `Authorization` header, the project serves a small helper page:
- `http://localhost:8080/statementDownloads.html?token=...`

It performs a `fetch()` to `/downloads/{token}` with `Authorization: Bearer <jwt>` and triggers the browser download.
