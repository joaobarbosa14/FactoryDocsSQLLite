# FactoryDocs quick start — macOS Apple Silicon

The project uses SQLite by default, so you do not need Docker or a local SQL Server container.

## 1. Check the .NET SDK

```bash
dotnet --version
```

If missing, install the .NET 10 SDK for Arm64 from https://dotnet.microsoft.com/en-us/download/dotnet/10.0.

## 2. Remove the failed SQL Server container (optional cleanup)

```bash
docker rm factorydocs-sql
```

If it says the container does not exist, continue. This does not uninstall Docker.

## 3. Start the API

From the extracted `FactoryDocs` folder:

```bash
cd src/FactoryDocs.Api
dotnet restore
dotnet run
```

The console displays the API URL. In another Terminal window, use that port in these commands (replace `5000` if the actual port differs):

```bash
curl http://localhost:5000/api/health
curl http://localhost:5000/api/documents
```

The API creates `FactoryDocs.db` in the API project folder automatically. Do not delete this file if you want to keep the demo data.

## 4. Switch to SQL Server later

```bash
export DatabaseProvider=SqlServer
export ConnectionStrings__FactoryDocs='Server=YOUR_SQL_SERVER;Database=FactoryDocs;User Id=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True'
cd src/FactoryDocs.Api
dotnet run
```

Use a real, reachable SQL Server instance and keep credentials out of source control. To return to SQLite in that terminal, run `unset DatabaseProvider` and `unset ConnectionStrings__FactoryDocs`.
