# Deployment

## Overview
The app runs on a self-hosted Windows IIS server, exposed via a Cloudflare Tunnel.

Deploys are **automated** through the `Build, Test & Stage` GitHub Actions workflow
(`.github/workflows/deploy.yml`), run manually with a release tag. See
[`cicd-pipeline.md`](cicd-pipeline.md) for how to trigger it, the server folder layout,
prerequisites, secrets, and rollback.

This page covers what the *application* needs at runtime — configuration keys and the
migration-time backup behaviour.

## Manual Deploy (fallback only)
Use only if the pipeline is unavailable:

1. Publish the server project: **Release / net10.0 / win-x64** (framework-dependent — the server
   has the .NET 10 Hosting Bundle installed, so a self-contained publish is not needed)
2. RDP into the production server
3. Copy the published files to a staging folder on the server
4. Stop the IIS website and its Application Pool
5. Copy and overwrite the production files from the staging folder
6. Verify `appsettings.Production.json` (kept in `C:\WhatYouDid\AppSettings\`) is overlaid with the
   correct `ProductionConnection` string — do not overwrite it with a repo copy
7. Restart the IIS website and Application Pool

## IIS: WebDAV must stay disabled (DELETE endpoints)

IIS ships with WebDAV enabled. Its module and handler claim `DELETE` and `PUT` and answer them
with **405 Method Not Allowed** before the ASP.NET Core Module ever runs. That silently breaks
every DELETE endpoint in the app — `DELETE /api/workouts/{id}` and `DELETE /api/weight/{id}`.

This bit production and was diagnosed on 2026-08-20. The fix is committed in
`WhatYouDid/WhatYouDid/web.config`, which removes **both** registrations:

```xml
<modules><remove name="WebDAVModule" /></modules>
<handlers><remove name="WebDAV" /><add name="aspNetCore" ... /></handlers>
```

Removing only one is not enough — WebDAV registers a module and a handler separately.

Two ways this can silently regress:

- **A `web.config` in `C:\WhatYouDid\AppSettings\`.** `deploy.ps1` overlays that folder on top of
  the build, so a stale copy there overwrites the published one. That folder should contain only
  `appsettings.Production.json`.
- **Hand-editing `web.config` on the server.** The live folder is replaced wholesale on every
  deploy; the committed file is the only durable home for this.

**No automated test catches this.** Both `WhatYouDid.Tests` and `WhatYouDid.UITests` run the app
on Kestrel/TestServer via `ApiWebApplicationFactory` and never traverse IIS, so a green suite says
nothing about whether IIS is rejecting verbs. Verify DELETE by hand against the real site after a
deploy that touches `web.config` or after any server rebuild.

## Configuration (appsettings.json)
The following keys must be present in the production `appsettings.json` (not committed to source control):

| Key | Description |
|---|---|
| `ConnectionStrings:ProductionConnection` | SQL Server connection string |
| `DatabaseBackupPath` | Directory path on the server where pre-migration backups are written |
| `Resend:ApiKey` | API key from the Resend dashboard |
| `Resend:FromAddress` | Sending address, e.g. `noreply@what-you-did.com` |
| `Admins` | JSON array of email addresses to grant the Admin role on startup |

Admin emails example:
```json
"Admins": [
  "your@email.here"
]
```

## Automatic Database Backup Before Migrations
On startup in production, the app checks for pending EF Core migrations. If any exist, it automatically takes a SQL Server backup **before** applying them.

- Backup path is read from `DatabaseBackupPath` in `appsettings.json` — the directory must already exist
- Backup filename includes the database name, timestamp, and current/target migration names, e.g.:
  `WhatYouDid_20260403_120000_curr-AddWorkouts_to-AddSoftDelete.bak`
- Command timeout: 5 minutes
- If `DatabaseBackupPath` is missing or the directory doesn't exist, the app throws on startup rather than proceeding

This logic lives in `Program.cs` and uses a raw SQL `BACKUP DATABASE` command (SQL Server Express compatible — no `COMPRESSION` option).

## Development Seed Data
In the `Development` environment, `DevDataSeeder` runs at startup and populates:
- Two test accounts: `admin@test.com / Admin1234!` and `test@test.com / Test1234!`
- One public "Leg Day" routine visible to all users

This does not run in production.
