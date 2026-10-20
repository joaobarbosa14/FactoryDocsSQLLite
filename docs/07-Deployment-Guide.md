# Local Deployment Guide

## Prerequisites
- .NET SDK 10
- SQL Server / SQL Server Express / LocalDB
- Node.js LTS and npm
- Angular CLI

## Configuration
Set `ConnectionStrings__FactoryDocs` to a valid SQL Server connection string. Keep credentials out of source control.

## API
Run from `src/FactoryDocs.Api`:

```bash
dotnet restore
dotnet run
```

The local MVP calls `EnsureCreated` on startup for convenience. For a production system, replace this with reviewed EF Core migrations and a controlled deployment process.

## Frontend
Create an Angular workspace in `src/FactoryDocs.Web` if one does not already exist, then copy the supplied `src/app` files into it. Run `npm install` and `ng serve`.

## Importer
Run the importer with the CSV path and API base URL:

```bash
dotnet run -- ../../samples/sample-documents.csv http://localhost:5000
```

Use the actual URL printed by the API at startup.

## Production Readiness
Before real deployment, add authentication, role-based authorization, secure file storage, malware scanning, HTTPS configuration, structured logs, backups, secret management, monitoring and a security review.


## Local development on macOS (SQLite)

1. Install the .NET 10 SDK for Apple Silicon (Arm64).
2. From `src/FactoryDocs.Api`, run `dotnet restore` and `dotnet run`.
3. SQLite requires no server or Docker container; `FactoryDocs.db` is created in the API project folder on first run.
4. To use SQL Server instead, set `DatabaseProvider=SqlServer` and `ConnectionStrings__FactoryDocs` before starting the API. The SQL Server host must be compatible and reachable from the machine running the API.
