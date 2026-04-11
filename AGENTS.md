# AGENTS Guide

## Runtime Surfaces (treat as two apps)
- Root is a browser PWA (`index.html`, `js/app.js`, `js/db.js`, `sw.js`) deployed to GitHub Pages.
- `dotnet-app/` is a separate ASP.NET Core MVC app (`Program.cs`, Controllers/Services/EF Core).
- They share domain language (Project/Category/Transaction), but **do not share storage or runtime**.

## Big-Picture Architecture
- MVC request flow: Controller -> Application Service -> Repository -> `FinanceDbContext` -> SQL Server.
- DI wiring is centralized in `dotnet-app/Program.cs` (repositories + services are scoped).
- `FinanceDbContext` is also `IUnitOfWork`; services call `unitOfWork.SaveChangesAsync(...)` after mutations.
- Domain entities hold simple business helpers (for example `Project.TotalIncome()` in `dotnet-app/Domain/Entities/Project.cs`).
- PWA is a hash-routed SPA (`#/dashboard`, `#/projects`, etc.) controlled in `js/app.js` `route()`.
- PWA data layer is Firestore-per-user collections under `users/{uid}/{store}` (`js/db.js` `col(store)`).

## Data and Behavior You Must Preserve
- Transaction ordering is consistent across both stacks: date DESC, then sequence DESC (`js/db.js`, `TransactionRepository.cs`).
- Sequence number is project-scoped (`getNextProjectSeq` in PWA, `GetNextSeqNoAsync` in MVC repo).
- Category deletion behavior differs by stack:
  - MVC blocks deletion when in use (`CategoryService.DeleteAsync` throws `InvalidOperationException`).
  - PWA allows deletion; existing transactions show "Unknown" (`js/app.js` `deleteCategory`).
- Project deletion in both stacks removes related transactions first (`ProjectService.DeleteWithTransactionsAsync`, `FinanceDB.deleteTransactionsByProject`).

## Developer Workflows
- Run PWA locally with a static server (service worker requires HTTP):
  - `python3 -m http.server 8080` from repo root, then open `http://localhost:8080`.
- Run MVC app:
  - `dotnet restore`
  - `dotnet run --project dotnet-app/FinanceTracker.Web.csproj`
- EF migrations (MVC):
  - `dotnet ef migrations add <Name> --project dotnet-app/FinanceTracker.Web.csproj`
  - `dotnet ef database update --project dotnet-app/FinanceTracker.Web.csproj`

## Project-Specific Conventions
- Prefer constructor injection and cancellation-token plumbing from controllers through services/repositories.
- Keep validation split as implemented:
  - UI/ViewModel validation (`ViewModels/*InputModel.cs`, `ModelState`).
  - EF schema constraints in `FinanceDbContext.OnModelCreating`.
- Use `TempData["Success"]` / `TempData["Error"]` for MVC user feedback (`Views/Shared/_Layout.cshtml`).
- Keep default category seeds aligned across stacks (`FinanceDbContext` HasData and `FinanceDB.seedDefaults()`).

## Integration and Deployment
- Firebase config is template-driven: edit `js/firebase-config.template.js`; never commit real `js/firebase-config.js` (`.gitignore`).
- GitHub Pages deploy (`.github/workflows/deploy.yml`) injects Firebase secrets into generated `js/firebase-config.js` before publishing `gh-pages`.
- PWA offline behavior: service worker precache + runtime cache in `sw.js`; Firestore offline persistence is enabled in `firebase-config.template.js`.

## Known Doc Drift (important for agents)
- Root `README.md` still describes IndexedDB, but current implementation uses Firebase Auth + Firestore (`index.html`, `js/db.js`).
- Prefer code as source of truth when README and runtime behavior conflict.

