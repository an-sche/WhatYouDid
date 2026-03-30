# API & Service Refactor Roadmap

Goal: Support mobile and web clients by exposing a proper HTTP API and splitting the monolithic `IWhatYouDidApi` into focused service interfaces.

---

## Phase 1 — Internal Refactor (no behavior change)

### 1. Split `IWhatYouDidApi` into focused service interfaces

Replace the monolithic interface with:

- **`IRoutineService`** — `GetUserRoutinesAsync`, `GetRoutineAsync`, `GetExercisesAsync`, `AddRoutineAsync`
- **`IWorkoutService`** — `GetStartWorkoutDtoAsync`, `SaveWorkoutAsync`, `GetWorkoutsAsync`, `GetWorkoutsCountAsync`, `GetCompletedWorkoutDtoAsync`, `UpdateWorkoutExerciseAsync`, `DeleteWorkoutAsync`
- **`IDashboardService`** — already exists, no change

Split `WhatYouDidApiDirectAccess` into `RoutineService` and `WorkoutService`. Register new services in DI.

### 2. Update Blazor components to inject new interfaces

| Component | Old | New |
|-----------|-----|-----|
| `Routines.razor` | `IWhatYouDidApi` | `IRoutineService` |
| `RoutineCard.razor` | `IWhatYouDidApi` | `IRoutineService` |
| `CreateRoutine.razor` | `IWhatYouDidApi` | `IRoutineService` |
| `Workouts.razor` | `IWhatYouDidApi` | `IWorkoutService` |
| `WorkoutCard.razor` | `IWhatYouDidApi` | `IWorkoutService` |
| `Dashboard.razor` | `IDashboardService` | no change |

---

## Phase 2 — HTTP Endpoint Expansion

### 3. Expose `/api/routines` endpoints

```
GET    /api/routines                        → GetUserRoutinesAsync()
GET    /api/routines/{routineId}            → GetRoutineAsync()
GET    /api/routines/{routineId}/exercises  → GetExercisesAsync()
POST   /api/routines                        → AddRoutineAsync()
```

All require authorization. Add `RoutineEndpointExtensions`.

### 4. Expose `/api/workouts` history endpoints

```
GET    /api/workouts                        → GetWorkoutsAsync() (query: startIndex, count, search)
GET    /api/workouts/count                  → GetWorkoutsCountAsync() (query: search)
GET    /api/workouts/{workoutId}            → GetCompletedWorkoutDtoAsync()
PATCH  /api/workouts/{workoutId}/exercises/{exerciseId} → UpdateWorkoutExerciseAsync()
DELETE /api/workouts/{workoutId}            → DeleteWorkoutAsync()
```

> The existing `POST /api/workouts` and `GET /api/workouts/start/{routineId}` stay in place.

### 5. Expose `/api/dashboard` endpoint

```
GET    /api/dashboard                       → GetDashboardForUserAsync() (query: year)
```

Add `DashboardEndpointExtensions`. Delegates to existing `IDashboardService`.

### 6. Create `IAdminService` and `/api/admin` endpoints

Extract `UserManager` calls from page code-behind into a service:

- `CreateUserAsync(email, password)`
- `GetUsersAsync()`
- `ResetPasswordAsync(userId, newPassword)`

```
GET    /api/admin/users                     → GetUsersAsync()
POST   /api/admin/users                     → CreateUserAsync()
POST   /api/admin/users/{userId}/reset-password → ResetPasswordAsync()
```

All require `[Authorize(Roles = "Admin")]`.

---

## Phase 3 — External Client Support

### 7. Add JWT authentication for mobile/external clients

Current cookie auth works for browsers but not mobile. Add token-based auth alongside it:

- `POST /api/auth/login` — returns JWT on valid credentials
- `POST /api/auth/refresh` — token refresh
- Configure JWT bearer middleware alongside existing cookie auth
- All `/api/*` endpoints accept either cookie **or** JWT bearer

Blazor server app continues using cookie auth. Mobile/web clients use JWT.

### 8. Reorganize endpoint registration + add OpenAPI

- Create a top-level `MapApiEndpoints()` extension that calls into all endpoint groups
- Use `MapGroup("/api")` to apply shared auth policy and prefix once
- Add OpenAPI/Swagger (built-in .NET 9 OpenAPI or Swashbuckle) for discoverability
- Consolidate/remove `WasmEndpointExtensions` into the new structure

---

## Dependency Order

```
#1 (split interfaces)
  └── #2 (update Blazor components)
        └── #3, #4, #5, #6 (add endpoints) ← can run in parallel
              └── #7 (JWT auth)
                    └── #8 (cleanup + OpenAPI)
```

Tasks #1–2 are safe internal refactors. Tasks #3–6 expand the HTTP surface. Task #7 is the prerequisite for shipping a mobile client.
