# WhatYouDid

WhatYouDid is a Blazor + SQL project for logging workouts. The repository contains a Blazor WebAssembly front-end and server-side pieces that communicate with a SQL database to store workout logs.

# Deployment

Deploys run through the **Build, Test & Stage** GitHub Actions workflow
(`.github/workflows/deploy.yml`). It is triggered manually and goes all the way to live —
despite the name, the last step promotes the build to production.

```bash
gh workflow run deploy.yml -f tag=v1.3.0
```

| Input | Required | Notes |
|---|---|---|
| `tag` | yes | Names the staging folder, e.g. `What You Did (v1.3.0)` |
| `skip_tests` | no (default `false`) | Skips the test job. Leave `false` when the release contains a migration. |

**Job 1 — Test** (ubuntu): restore → build → integration tests (Testcontainers spins up SQL
Server in Docker) → install Playwright chromium → UI tests. Playwright traces are uploaded as
an artifact if anything fails.

**Job 2 — Publish & Stage** (only if tests passed or were skipped): publishes
`-c Release -r win-x64`, zips it, `scp`s it to the server, extracts it into the tagged staging
folder, then SSHes in and runs the deploy script to promote it.

The deploy script stops the site and app pool, waits up to 30s for `w3wp.exe` to release its file
handles (force-killing it if needed), moves the current live folder aside to
`Backups\Production_{timestamp}`, copies in the new build, overlays config from `AppSettings\`,
and always restarts IIS in a `finally` block — even if the copy failed.

> The script runs from **`C:\WhatYouDid\deploy.ps1` on the server**, not from this repo. That is
> deliberate: if the workflow uploaded it, a merged PR could change what executes on production.
> `scripts/deploy.ps1` here is the canonical source — copy it to the server by hand when it changes.

**Full reference** — server folder layout, prerequisites, the ten required GitHub secrets, and
step-by-step rollback commands: [`.features/cicd-pipeline.md`](.features/cicd-pipeline.md)
(secret setup: [`.features/cicd-secrets-setup.md`](.features/cicd-secrets-setup.md)).

### Database migrations

Pending migrations are applied **automatically on the first production startup** —
`BackupAndMigrateSqlDatabase()` in `Program.cs` takes a full `BACKUP DATABASE` first, writing to
the `DatabaseBackupPath` from `appsettings.json`, then runs `Migrate()`. The backup file is named
for the transition, e.g. `WhatYouDid_20260820_193656_curr-AddAlternateSetData_to-AddWeightEntries.bak`.

Two consequences worth knowing:

- If `DatabaseBackupPath` is missing, or the directory does not exist, **the app throws on
  startup**. Config comes from the config backup path restored by `deploy.ps1`, so verify it there.
- The backup runs synchronously before the app serves traffic, so the first request after a
  migration deploy waits on it.

### Rollback

Files and database roll back independently:

- **Files** — swap `Backups\Production_{timestamp}` back into place and restart IIS
  (exact commands in [`.features/cicd-pipeline.md`](.features/cicd-pipeline.md)).
- **Database** — not automatic. Restore the `.bak`, or `dotnet ef database update <PreviousMigration>`.

For a purely additive migration (a new table or column), rolling back the files alone is safe:
the previous build simply never touches the new schema.

### Runtime prerequisite

The publish is **framework-dependent** (`-r win-x64` without `--self-contained`), so the server
needs the **.NET 10 Hosting Bundle** (`winget install Microsoft.DotNet.HostingBundle.10`) for IIS
to host it. A missing runtime shows up as a startup failure, not a build failure.

### Manual fallback

If the pipeline is unavailable: publish the server project (`Release`, `net10.0`, `win-x64`), RDP
in, stop the IIS site and app pool, swap the files, preserve `appsettings.Production.json`, and restart.

# Hosting Information

* Cloudflare Tunnel 
	- Installed on IIS server using winget
	- Running as a windows service
	- Remaining configurations on Cloudflare website
* Can Log into Cloudflare to see tunnel status and metrics

# Configuration

Quick local setup
1. Install prerequisites: .NET 10 SDK
2. Install SQL and create a database. Put that database in `appsettings` under `DevelopmentConnection` (Can also use container)
3. Apply migrations to your local database `dotnet ef database update`
5. Run the application `f5` or `dotnet run`

In development mode, the app automatically seeds two test users on first run:

| Email | Password | Role |
|---|---|---|
| `admin@test.com` | `Admin1234!` | Admin |
| `test@test.com` | `Test1234!` | User |

Both users are seeded with workout history. The admin account also requires `admin@test.com` to be present in the `Admins` config (already set in `appsettings.Development.json`).

```
"Admins" : [
  "admin@test.here"
]
```

# Information

Key constraints and priorities
- Target framework: .NET 10. Make sure any changes keep compatibility with .NET 10.
- UI: Blazor (prioritize Blazor-specific patterns over Razor Pages or MVC).
	- Specifically, prefer MudBlazor components for UI consistency.
- Platform: WebAssembly client exists in the workspace. Preserve client-server communication patterns.
- Most important feature to preserve: reliable logging of workouts and existing data integrity.

