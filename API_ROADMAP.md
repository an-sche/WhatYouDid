# API & Service Refactor Roadmap

Goal: Support mobile and web clients by exposing a proper HTTP API and splitting the monolithic `IWhatYouDidApi` into focused service interfaces.

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
