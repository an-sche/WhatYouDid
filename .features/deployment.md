# Deployment

## Overview
The app is deployed manually to a self-hosted Windows IIS server. There is no automated CI/CD — the GitHub Actions workflow (`.github/workflows/main_whatyoudid-app.yml`) targets Azure App Service and is **no longer used**.

## Deploy Steps
1. Publish the server project in Visual Studio: **Release / net10.0 / Self-contained / win-x64**
2. RDP into the production server
3. Copy the published files to a staging folder on the server
4. Stop the IIS website and its Application Pool
5. Copy and overwrite the production files from the staging folder
6. Verify `appsettings.json` has the correct `ProductionConnection` string (do not overwrite it)
7. Restart the IIS website and Application Pool

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
