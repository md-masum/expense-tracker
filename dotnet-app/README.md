# Finance Tracker (.NET MVC)

This folder contains the .NET MVC conversion of the finance tracker using a simplified DDD structure inside a single web project.

## Target Framework

- .NET 10 (`net10.0`)

## Structure

- `Domain/Entities`: Core domain models and business behavior
- `Application/Abstractions`: Minimal repository contracts and unit-of-work
- `Application/Services`: Use-case level services
- `Infrastructure/Data`: EF Core `FinanceDbContext`
- `Infrastructure/Repositories`: Repository implementations
- `Controllers`, `Views`, `ViewModels`: MVC web layer

## Database

- EF Core SQL Server (Code First)
- Configure connection string in:
  - `appsettings.json`
  - `appsettings.Development.json`

## Run

```bash
dotnet restore
dotnet run --project dotnet-app/FinanceTracker.Web.csproj
```

## Migrations

```bash
dotnet ef migrations add InitialCreate --project dotnet-app/FinanceTracker.Web.csproj
dotnet ef database update --project dotnet-app/FinanceTracker.Web.csproj
```
