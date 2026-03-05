# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

All commands should be run from the solution root (`WhatYouDid/WhatYouDid/`).

**Build:**
```bash
dotnet build
```

**Run (development):**
```bash
dotnet run --project WhatYouDid/WhatYouDid.csproj
```

**Publish (production, Windows x64):**
```bash
dotnet publish WhatYouDid/WhatYouDid.csproj -c Release -r win-x64
```

**Database migrations:**
```bash
# Add a new migration (run from the WhatYouDid/ server project directory)
dotnet ef migrations add <MigrationName> --project WhatYouDid.csproj

# Apply migrations locally
dotnet ef database update --project WhatYouDid.csproj
```

## Architecture

This is an ASP.NET Core Blazor Web App using **Auto render mode** (Interactive Server + WebAssembly). The solution has three projects:

- **`WhatYouDid/`** — Server project. Hosts the app, EF Core, ASP.NET Identity, server-side Blazor components, and minimal API endpoints for the WASM client.
- **`WhatYouDid.Client/`** — WebAssembly (WASM) client project. Contains the interactive workout page (`StartWorkout.razor`) that runs in the browser.
- **`WhatYouDid.Shared/`** — Shared DTOs and interfaces used by both server and client (`WorkoutDto`, `DashboardDto`, `IBrowserStorage`).

### Multi-tenancy (user isolation)

Data is isolated per user through a tenant pattern rather than conventional per-query filtering:

1. `TenantResolutionMiddleware` runs on every HTTP request and calls `ITenantService.SetTenant(userId)` from the authenticated user's claims.
2. `TenantCircuitHandler` does the same for SignalR (server-side Blazor) circuits.
3. `ApplicationDbContext` registers EF Core global query filters on `Workout`, `Routine`, `Exercise`, and `WorkoutExercise` that compare `ApplicationUserId` to `tenantService.Tenant`. This means all queries are automatically scoped to the current user without explicit filtering in service code.
4. `Routine` also allows rows where `IsPublic == true` to be visible to all users.

### Data access pattern

`IWhatYouDidApi` is the primary data access interface. On the server it is implemented by `WhatYouDidApiDirectAccess`, which queries EF Core directly (no repository layer). The WASM client calls `/api/*` minimal API endpoints (defined in `WasmEndpointExtensions`) which delegate to the same `IWhatYouDidApi`.

`IBrowserStorage` abstracts session/local storage. On the server it is implemented by `ServerBrowserStorage`; the WASM client uses a JS interop implementation. `WorkoutDto.GetBrowserStorageId` generates the storage key for in-progress workouts.

### Key data model

- `Routine` — a named workout template owned by a user (or public). Contains ordered `Exercise` records.
- `Exercise` — a step in a routine (name, sets, flags for reps/weights/duration).
- `Workout` — a completed instance of a routine performed by a user (has `StartTime`/`EndTime`).
- `WorkoutExercise` — the actual recorded sets/reps/weights/durations for one exercise within a workout.

### Server pages vs. WASM pages

Server-side Blazor pages live in `WhatYouDid/Components/Pages/` (Routines, Workouts, Dashboard, Admin, Home). The active workout page (`StartWorkout.razor`) lives in `WhatYouDid.Client/Pages/` and uses `@rendermode InteractiveWebAssemblyRenderMode` so it runs entirely in the browser. It communicates with the server via `HttpClient` calling the `/api/workouts/*` minimal API endpoints.

### UI components

The app uses **Radzen Blazor** components throughout. All routes require authentication (`AuthorizeRouteView` in `Routes.razor` redirects unauthenticated users to login).

### Deployment

The app is self-hosted on a Windows IIS server, exposed publicly via a **Cloudflare Tunnel** (installed as a Windows service via winget; remaining config is on the Cloudflare website).

**Manual deploy steps:**
1. Publish the server project in Visual Studio: Release, net10.0, Self-contained, win-x64.
2. RDP into the server and copy the published files.
3. Shut down the IIS website and Application Pool.
4. Replace the existing files with the new published files.
5. Preserve (or update) `appsettings.json` with the correct database connection string.
6. Restart the IIS website and Application Pool.

The GitHub Actions workflow (`.github/workflows/main_whatyoudid-app.yml`) targets Azure App Service and is **no longer used**.

### Configuration (User Secrets)

The `DevelopmentConnection` and `ProductionConnection` strings are in User Secrets. Also configure the `Admins` section with your email to enable admin features:

```json
"Admins": [
  "your@email.here"
]
```
