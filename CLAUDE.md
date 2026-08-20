# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Run these from the **repo root** (where `WhatYouDid.slnx` lives) unless noted.

**Build:**
```bash
dotnet build WhatYouDid.slnx
```

**Run (development)** — run the *server* project; it hosts the WASM client, so never run `WhatYouDid.Client` directly:
```bash
dotnet run --project WhatYouDid/WhatYouDid/WhatYouDid.csproj
```
Serves https://localhost:7145. `DevDataSeeder` seeds `admin@test.com` / `Admin1234!` and `test@test.com` / `Test1234!` (both confirmed) with workout history.

**Test** — both suites need **Docker running** (Testcontainers starts SQL Server); they fail at fixture init with `DockerUnavailableException` otherwise, which means *unrun*, not failing:
```bash
dotnet test WhatYouDid.slnx                                  # everything
dotnet test WhatYouDid.Tests/WhatYouDid.Tests.csproj         # API + tenant isolation
dotnet test WhatYouDid.UITests/WhatYouDid.UITests.csproj     # Playwright
dotnet test WhatYouDid.slnx --filter "FullyQualifiedName~Weight"
```
The UI tests additionally need browsers installed once:
```bash
pwsh WhatYouDid.UITests/bin/Debug/net10.0/playwright.ps1 install chromium
```
If a build fails with `MSB3027 ... file is locked by: "WhatYouDid"`, a running instance of the app is holding `WhatYouDid.exe` — stop it first.

**Database migrations** (run from `WhatYouDid/WhatYouDid/`):
```bash
dotnet ef migrations add <MigrationName> --project WhatYouDid.csproj
dotnet ef database update --project WhatYouDid.csproj
dotnet ef migrations list --project WhatYouDid.csproj   # shows which are (Pending)
```

**Publish (production, Windows x64)** — framework-dependent; normally done by the deploy workflow:
```bash
dotnet publish WhatYouDid/WhatYouDid/WhatYouDid.csproj -c Release -r win-x64
```

## Architecture

This is an ASP.NET Core Blazor Web App using **Auto render mode** (Interactive Server + WebAssembly). The solution (`WhatYouDid.slnx`, at the repo root) has five projects:

- **`WhatYouDid/WhatYouDid/`** — Server project. Hosts the app, EF Core, ASP.NET Identity, the Home/Error/Admin pages, and the `/api/*` minimal API endpoints the WASM client calls.
- **`WhatYouDid/WhatYouDid.Client/`** — WebAssembly (WASM) client project. Holds **most** of the app's pages plus their components and HTTP services.
- **`WhatYouDid.Shared/`** — DTOs and the service interfaces implemented on both sides (`WorkoutDto`, `DashboardDto`, `IWeightService`, `IBrowserStorage`, …).
- **`WhatYouDid.Tests/`** — xUnit integration tests. Boots the real host against SQL Server via Testcontainers (**needs Docker**).
- **`WhatYouDid.UITests/`** — Playwright browser tests, built on the same factory (also needs Docker, plus `playwright.ps1 install chromium`).

### Multi-tenancy (user isolation)

Data is isolated per user through a tenant pattern rather than conventional per-query filtering:

1. `TenantResolutionMiddleware` runs on every HTTP request and calls `ITenantService.SetTenant(userId)` from the authenticated user's claims.
2. `TenantCircuitHandler` does the same for SignalR (server-side Blazor) circuits.
3. `ApplicationDbContext` registers EF Core global query filters on `Workout`, `Routine`, `Exercise`, `WorkoutExercise`, `WorkoutExerciseSet`, and `WeightEntry`, comparing `ApplicationUserId` to `tenantService.Tenant` (directly, or by walking the relationship to the owning row). All queries are therefore scoped to the current user without explicit filtering in service code.
4. `Routine` also allows rows where `IsPublic == true` to be visible to all users.
5. `Workout`'s filter also excludes soft-deleted rows (`!w.IsDeleted`).

**Do not filter by user id in service code.** Set `ApplicationUserId = tenantService.Tenant` on insert and let the filters do the rest — a "not found" for another user's row is the filter working as intended.

### Data access pattern — the vertical slice

There is **one service interface per feature**, not a single API interface. Each is declared in `WhatYouDid.Shared` and implemented twice — once on the server against EF Core, once on the client against HTTP:

| Interface | Server impl (`WhatYouDid/Services/`) | Client impl (`WhatYouDid.Client/Services/`) | Endpoints |
|---|---|---|---|
| `IRoutineService` | `RoutineService` | `RoutineHttpService` | `/api/routines` |
| `IWorkoutService` | `WorkoutService` | `WorkoutHttpService` | `/api/workouts` |
| `IDashboardService` | `DashboardService` | `DashboardHttpService` | `/api/dashboard` |
| `IWeightService` | `WeightService` | `WeightHttpService` | `/api/weight` |
| `IAdminService` | `AdminService` | — | `/api/admin` |

Because both sides implement the same interface, a page injects `IWeightService` and works identically whether it renders on the server or in the browser.

**Adding a feature** means adding one of each: DTO + interface in `WhatYouDid.Shared` → server service taking `IDbContextFactory<ApplicationDbContext>` + `ITenantService` → a `*EndpointExtensions` class registered in `ApiEndpointExtensions.MapApiEndpoints()` → a client `*HttpService` → the page. Register the server impl in `WhatYouDid/Program.cs` and the client impl in `WhatYouDid.Client/Program.cs`. Copy `RoutineService` / `RoutineEndpointExtensions` / `RoutineHttpService` as the reference.

Server services use `IDbContextFactory` (not an injected `DbContext`) and query EF Core directly — there is no repository layer.

`IBrowserStorage` abstracts session/local storage. On the server it is implemented by `ServerBrowserStorage`; the WASM client uses a JS interop implementation. `WorkoutDto.GetBrowserStorageId` generates the storage key for in-progress workouts.

### Key data model

- `Routine` — a named workout template owned by a user (or public). Contains ordered `Exercise` records.
- `Exercise` — a step in a routine (name, sets, flags for reps/weights/duration).
- `Workout` — an instance of a routine performed by a user (`StartTime`/`EndTime`, soft-deleted via `IsDeleted`). Keyed by a `Guid` because the client holds the id in browser storage while a workout is in progress, before it is ever saved.
- `WorkoutExercise` — one exercise within a workout.
- `WorkoutExerciseSet` — an individual recorded set (reps/weight/duration, plus `Alternate*` variants and a note).
- `WeightEntry` — a body weight reading in pounds at a `DateTimeOffset`. Multiple per day are allowed; the Progress chart averages each day.

Most entities use `int` keys. `Workout` and `WeightEntry` use `Guid` (`Guid.CreateVersion7()` for sequential inserts) — reach for a `Guid` only when the id must exist client-side before persistence or must appear in a URL.

### Server pages vs. WASM pages

**Most pages are in the WASM client**, `WhatYouDid.Client/Pages/`, all `@rendermode InteractiveWebAssembly`: `/routines`, `/workouts`, `/dashboard`, `/progress`, `/createroutine`, `/weight`, and `/workout/{RoutineId:int}` (`StartWorkout.razor`, which uses `new InteractiveWebAssemblyRenderMode(prerender: false)`). They reach the server through their `*HttpService` against `/api/*`.

Only these stay server-side in `WhatYouDid/Components/Pages/`: `/` (Home), `Error`, and the admin pages (`/admin`, `/admin/createuser`, `/admin/resetpassword`). Identity/account pages live under `WhatYouDid/Components/Account/Pages/`.

Shared client components are in `WhatYouDid.Client/Components/`.

### UI components

The app uses **MudBlazor** components throughout. All routes require authentication (`AuthorizeRouteView` in `Routes.razor` redirects unauthenticated users to login).

### Deployment

The app is self-hosted on a Windows IIS server, exposed publicly via a **Cloudflare Tunnel** (installed as a Windows service via winget; remaining config is on the Cloudflare website).

Deploys run through `.github/workflows/deploy.yml` ("Build, Test & Stage"), triggered manually with a `tag` input (and an optional `skip_tests`). It tests, publishes `-c Release -r win-x64` (**framework-dependent** — the server needs the .NET 10 Hosting Bundle), uploads to a tagged staging folder, then SSHes in and runs the deploy script to promote to live. The script stops IIS, moves the live folder aside to `Backups\Production_{timestamp}`, copies the new build, overlays config from `AppSettings\`, and always restarts IIS in a `finally`.

The script executes from `C:\WhatYouDid\deploy.ps1` **on the server** and is deliberately never uploaded by the workflow, so a merged PR cannot change what runs on production. `scripts/deploy.ps1` in this repo is the canonical source — copy it across by hand when it changes.

Pending migrations apply **automatically on first production startup** via `BackupAndMigrateSqlDatabase()` (`Program.cs`), which takes a full `BACKUP DATABASE` to `DatabaseBackupPath` before calling `Migrate()`. It **throws on startup** if `DatabaseBackupPath` is unset or the directory is missing.

Rollback is two independent halves: swap back `Production_{timestamp}` for files; restore the `.bak` or `dotnet ef database update <PreviousMigration>` for the database. Additive migrations are safe to leave in place when rolling back files only.

See `README.md` for the full deploy reference, including the required repository secrets.

### Configuration (User Secrets)

The following are stored in User Secrets. Also configure the `Admins` section with your email to enable admin features:

```json
{
  "ConnectionStrings:DevelopmentConnection": "<sql-server-connection-string>",
  "Resend": {
    "ApiKey": "<resend-api-key>",
    "FromAddress": "noreply@what-you-did.com"
  },
  "Authentication": {
    "Google": {
      "ClientId": "<google-client-id>",
      "ClientSecret": "<google-client-secret>"
    }
  },
  "Admins": [
    "your@email.here"
  ]
}
```

- **Resend** — transactional email provider. API key from resend.com.
- **Authentication:Google** — OAuth2 credentials from Google Cloud Console. Required for Google login.

In **production** the equivalents live in `appsettings.Production.json` on the server (kept in `C:\WhatYouDid\AppSettings\`, outside source control), using `ConnectionStrings:ProductionConnection` instead, plus `DatabaseBackupPath` — the directory the pre-migration backup is written to. It must exist, or the app throws on startup. See `.features/deployment.md`.
