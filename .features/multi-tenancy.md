# Multi-Tenancy (User Data Isolation)

## Overview
All user data is automatically scoped to the authenticated user. There is no shared data between users (except explicitly public routines). Isolation is enforced at the database query level via EF Core global query filters — service code never needs to manually filter by user.

## How It Works

The tenant is just the authenticated user's ID (`ApplicationUserId`). It flows through a scoped `ITenantService` that is set at the start of every request or circuit connection.

### 1. HTTP Requests — `TenantResolutionMiddleware`
Runs on every HTTP request after authentication. Reads the user ID from `ClaimTypes.NameIdentifier` and calls `tenantService.SetTenant(userId)`. Runs after `UseAuthentication()` / `UseAuthorization()` in the pipeline.

### 2. Blazor Server Circuits — `TenantCircuitHandler`
Server-side Blazor components communicate over SignalR, not HTTP, so the middleware above doesn't apply. `TenantCircuitHandler` extends `CircuitHandler` and sets the tenant when a circuit connects (`OnConnectionUpAsync`). It also **clears** the tenant when a circuit disconnects (`OnConnectionDownAsync`) to prevent the tenant ID from leaking into other scopes.

### 3. EF Core Global Query Filters — `ApplicationDbContext`
Every query against user-owned entities is automatically filtered. Filters are applied in `OnModelCreating`:

Because these are global query filters, they apply to every query including `.Include()` and navigation property loads — no opt-in required in service code.

## Public Routines
Routines have an `IsPublic` flag. Public routines (and their exercises) are visible to **all** authenticated users. Currently there is no UI for making a routine public — it must be set directly in the database or via the admin tools.

## Gotchas
- **WASM pages bypass this entirely.** The `StartWorkout.razor` page runs in the browser and calls `/api/*` endpoints via `HttpClient`. Those endpoints are authenticated via cookies but go through normal HTTP middleware, so `TenantResolutionMiddleware` handles tenant resolution just like any other request.
- **If `ITenantService.Tenant` is empty, all filtered queries return nothing.** This is the safe failure mode — unauthenticated or errored requests get an empty dataset rather than someone else's data.
- **`IgnoreQueryFilters()`** can be used in admin service code to bypass filters when needed (e.g., listing all users' workouts). Use carefully.
