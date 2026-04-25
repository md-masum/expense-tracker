# Finance Tracker (.NET MVC)

This folder contains the ASP.NET Core MVC application for Finance Tracker. It is a server-rendered multi-user app using ASP.NET Identity + EF Core SQL Server.

## Tech Stack

- .NET 10 (`net10.0`)
- ASP.NET Core MVC + Razor Views
- ASP.NET Core Identity (`ApplicationUser`, auth cookies)
- Entity Framework Core 10 + SQL Server
- Bootstrap 5 + Bootstrap Icons

## Architecture

Request flow is:

`Controller -> Application Service -> Repository -> FinanceDbContext -> SQL Server`

Main layers:

- `Domain/Entities`: Core entities (`Project`, `FinanceTransaction`, `Company`, etc.) and domain helpers.
- `Application/Abstractions`: Repository and unit-of-work interfaces.
- `Application/Services`: Use-case/business services.
- `Infrastructure/Data`: `FinanceDbContext`, migrations, EF model configuration.
- `Infrastructure/Repositories`: EF-backed repository implementations.
- `Infrastructure/Files`: Local file storage for company banners and invoice images.
- `Infrastructure/Filters`: Active company middleware and onboarding filter.
- `Controllers`, `Views`, `ViewModels`: MVC web layer.

Dependency wiring is centralized in `Program.cs`.

## Core Features

### Authentication and Access

- Email/password register and login with ASP.NET Identity.
- Cookie auth with fallback policy requiring authenticated users by default.
- Company onboarding guard:
  - Users without company access are redirected to `Companies` before using the rest of the app.

### Company Workspace Model

- Users can create companies (owner role), edit/delete owned companies, and upload banner images.
- Users can request to join other companies.
- Company owners can approve/reject join requests and remove members.
- Each user can select an active/default company.
- All project/transaction dashboard data is scoped to the active company context.

### Project and Project Type Management

- CRUD for project types (with protection against deleting types in use).
- CRUD for projects inside active company.
- Project delete removes project transactions first, then project.

### Category Management

- CRUD for debit/credit categories.
- Categories used by any transaction cannot be deleted.

### Transaction Management

- Create/edit/delete transactions within project.
- Optional invoice image upload, replacement, and deletion.
- Transaction list supports:
  - Note search
  - Type filter
  - Category filter
  - Pagination
- Ordering is consistent: `Date DESC`, then `SeqNo DESC`.
- Sequence number is project-scoped (`GetNextSeqNoAsync`).

### Dashboard and Reporting

- Dashboard cards show total credit/debit/balance across active company projects.
- Per-project summary cards with transaction counts.
- CSV export per selected project.

### Backup and Restore

- JSON export includes active-company projects, transactions, and categories used by those transactions.
- JSON import currently performs a full replacement by removing all rows from:
  - `Transactions`
  - `Projects`
  - `Categories`
  then inserting payload rows.

Important behavior and limits:

- Import is not company-scoped in current implementation; it operates at table level.
- Identity/auth tables are not part of backup/restore.
- Company, company-user mapping, join-request data are not part of backup/restore.
- Uploaded image files (`wwwroot/uploads/...`) are not included in JSON backup.

## Data Model Notes

- Seeded categories: Sand, Labour, Brick, Materials, Investment, Sale.
- Seeded project types: Construction, Agriculture, Business, Other.
- Key constraints configured in `FinanceDbContext`:
  - Unique `ProjectType.Name`
  - Unique `(Category.Name, Category.Type)`
  - FK `FinanceTransaction -> Category` is `Restrict`
  - FK `Project -> Company` is `Cascade`

## Configuration

Set SQL Server connection string in:

- `appsettings.json`
- `appsettings.Development.json`

Default key:

- `ConnectionStrings:DefaultConnection`

## Local Run

```bash
dotnet restore dotnet-app/FinanceTracker.Web.csproj
dotnet run --project dotnet-app/FinanceTracker.Web.csproj
```

## EF Migrations

```bash
dotnet ef migrations add <Name> --project dotnet-app/FinanceTracker.Web.csproj
dotnet ef database update --project dotnet-app/FinanceTracker.Web.csproj
```

## Production Database Script

A production helper script is provided at:

- `dotnet-app/scripts/deploy-db.sh`

What it does:

1. Restores the project.
2. Generates an idempotent SQL migration script artifact.
3. Applies migrations to target SQL Server using a provided connection string.

Usage:

```bash
export FINANCE_TRACKER_CONNECTION_STRING='Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;Encrypt=True'
./dotnet-app/scripts/deploy-db.sh
```

Optional: generate SQL only without applying migrations.

```bash
export FINANCE_TRACKER_CONNECTION_STRING='Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;Encrypt=True'
SKIP_APPLY=true ./dotnet-app/scripts/deploy-db.sh
```

