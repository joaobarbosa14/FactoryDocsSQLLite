# FactoryDocs — Interview MVP

A small, independent Document Management System (DMS) demonstration for an industrial IT environment.

**Technology:** C# / ASP.NET Core Web API, Entity Framework Core, SQLite (local default) with optional Microsoft SQL Server provider, Angular / TypeScript, VB.NET CSV importer.

> This is a portfolio/demo project. It is not an official Mürdter application and contains no company-confidential data.

## MVP scope

- Document metadata API with list, search, create and status update.
- SQL Server persistence using EF Core.
- Document categories, revisions and audit-log entities in the initial data model.
- Angular starter UI.
- VB.NET CSV importer demonstrating integration with a legacy application.
- English planning, architecture, testing and deployment documentation.

## Prerequisites

- .NET SDK 10 (or adapt target frameworks to an installed SDK)
- .NET SDK 10; SQLite is included and requires no separate server
- Optional: a reachable Microsoft SQL Server instance for SQL Server mode
- Node.js LTS and npm
- Angular CLI (`npm install -g @angular/cli`)
- Visual Studio 2022 or VS Code

## Quick start

### 1. Database setup (recommended: SQLite on macOS)

No Docker or database server is required for the local demo. The API defaults to SQLite and creates `src/FactoryDocs.Api/FactoryDocs.db` automatically on first run.

To use SQL Server later, set both environment variables before starting the API:

```bash
export DatabaseProvider=SqlServer
export ConnectionStrings__FactoryDocs='Server=YOUR_SQL_SERVER;Database=FactoryDocs;User Id=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True'
```

Use a reachable SQL Server instance and do not commit real passwords or production credentials. To switch back to SQLite in the current terminal:

```bash
unset DatabaseProvider
unset ConnectionStrings__FactoryDocs
```

### 2. Run the API

```bash
cd src/FactoryDocs.Api
dotnet restore
dotnet run
```

The API creates the database schema on startup for this interview MVP using EF Core `EnsureCreated`. SQLite is the default; SQL Server remains available through configuration. Swagger/OpenAPI is available in Development mode. The console output shows the local API URL.

### 3. Run the Angular frontend

The `src/FactoryDocs.Web` folder contains the Angular source starter. Generate a standard Angular workspace in that folder if needed:

```bash
cd src/FactoryDocs.Web
npm install
ng serve
```

If this is a fresh folder without Angular workspace files, run `ng new factorydocs-web --directory . --routing --style css` first, then copy the supplied `src/app` files into the generated workspace.

### 4. Run the VB.NET CSV importer

```bash
cd src/FactoryDocs.LegacyImporter
dotnet run -- ../../samples/sample-documents.csv http://localhost:5000
```

Use the actual API URL printed by `dotnet run`. The sample CSV is in `samples/sample-documents.csv`. The importer expects the API's `POST /api/documents` endpoint.

## API endpoints

- `GET /api/health`
- `GET /api/documents?search=maintenance`
- `GET /api/documents/{id}`
- `POST /api/documents`
- `PATCH /api/documents/{id}/status`

Example JSON for `POST /api/documents`:

```json
{
  "documentNumber": "WI-MFG-001",
  "title": "Machine Start-up Work Instruction",
  "category": "Production",
  "description": "Demonstration work instruction.",
  "status": "Draft"
}
```

## Switching database providers

The API selects the provider in `Program.cs` using the `DatabaseProvider` setting. The default is `Sqlite`; use `SqlServer` plus `ConnectionStrings__FactoryDocs` to target SQL Server. Both EF Core providers are referenced in the project. The model and API are shared, while provider-specific schema and SQL behavior may differ. For production, replace `EnsureCreated` with EF Core migrations and test migrations separately against each provider.

## Important MVP limitations

This starter is intended for a local interview demo. Before real business use, add production-grade authentication and role-based authorization, robust migration deployment, file storage and malware scanning, upload validation, structured logging, secrets management, backups, and security review. Do not expose it to the public internet as-is.
