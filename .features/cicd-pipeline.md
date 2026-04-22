# CI/CD Pipeline

## Overview

Manual GitHub Actions workflow that builds, tests, and deploys to a self-hosted Windows IIS server over SSH. Triggered via `workflow_dispatch` — a deployment engineer runs it by hand with a release tag.

See `.github/workflows/deploy.yml` for the full workflow and `scripts/deploy.ps1` for the server-side deploy logic.

---

## How to Deploy

1. Go to **GitHub → Actions → Build, Test & Stage → Run workflow**
2. Enter a release tag (e.g. `v1.2.0`) — this names the staging folder on the server
3. Optionally check **Skip tests** to deploy directly without running the test suite
4. Click **Run workflow**

---

## What Happens

### Job 1 — Test (skippable)
- Restores and builds the full solution in Debug
- Runs integration tests (Testcontainers / SQL Server in Docker)
- Runs Playwright UI tests
- Uploads Playwright traces on failure

### Job 2 — Publish & Deploy
Runs only if tests passed (or were skipped).

1. Publishes the app as a framework-dependent win-x64 build
2. Zips the publish output into a single archive
3. SCPs the archive to the staging folder on the server
4. Extracts it into `Staging\What You Did (<tag>)\`
5. SSHs into the server and runs `deploy.ps1` with parameters

---

## The Deploy Script

`deploy.ps1` lives **permanently on the server** at `C:\WhatYouDid\deploy.ps1`. It is **not uploaded by the workflow** — this is intentional. If the script were uploaded from the repo, a merged PR could modify what runs on the production server. Instead, updating the deploy script requires manual server access (RDP or one-off SCP).

The canonical source is `scripts/deploy.ps1` in this repo. Copy it to the server manually when changes are needed.

The workflow injects everything as parameters — the script itself contains no hardcoded paths or secrets:

```
deploy.ps1
  -SiteName          IIS website name
  -AppPool           IIS application pool name
  -LivePath          Path to the live production folder
  -StagingPath       Path to the staging root folder
  -Tag               Release tag (identifies the staged build)
  -ConfigBackupPath  Path to the folder containing appsettings backups
  -BackupPath        Path to the folder where old production builds are archived
```

**What the script does:**
1. Validates all paths exist before touching anything
2. Stops the IIS site and app pool
3. Waits for `w3wp.exe` to fully exit (releases file locks), force-kills after 30s if needed
4. Moves the current live folder to `Backups\Production_<timestamp>`
5. Copies the new build from staging into the live folder
6. Overlays config files from `AppSettings\` on top
7. Starts the IIS app pool and site (`finally` block — always runs even if deploy fails)

---

## Server Folder Structure

```
C:\WhatYouDid\
  Production\          ← live IIS site folder
  Staging\             ← staged builds land here
    What You Did (v1.2.0)\
    publish.zip
  AppSettings\         ← appsettings.Production.json lives here permanently
  Backups\             ← timestamped snapshots of old Production folders
    Production_2026-04-21_14-30-00\
  deploy.ps1           ← deploy script (placed here manually)
```

---

## Server Prerequisites

- **Windows OpenSSH Server** running with the deploy public key in `C:\ProgramData\ssh\administrators_authorized_keys`
- **.NET 10 Hosting Bundle** installed (`winget install Microsoft.DotNet.HostingBundle.10`) — required for framework-dependent deployment under IIS
- **PowerShell execution policy** — either set `RemoteSigned` (`Set-ExecutionPolicy RemoteSigned -Scope LocalMachine`) or the workflow passes `-ExecutionPolicy Bypass`
- All four folders under `C:\WhatYouDid\` created before first run
- `deploy.ps1` copied to `C:\WhatYouDid\deploy.ps1`
- `appsettings.Production.json` placed in `C:\WhatYouDid\AppSettings\`

---

## GitHub Secrets

| Secret | Description |
|---|---|
| `DEPLOY_HOST` | Server public IP |
| `DEPLOY_USER` | SSH username (Windows administrator account) |
| `DEPLOY_SSH_KEY` | SSH private key |
| `DEPLOY_SITE_NAME` | IIS website name |
| `DEPLOY_APP_POOL` | IIS application pool name |
| `DEPLOY_SCRIPT_PATH` | `C:\WhatYouDid\deploy.ps1` |
| `DEPLOY_TARGET_PATH` | `C:\WhatYouDid\Production` |
| `STAGING_TARGET_PATH` | `C:\WhatYouDid\Staging` |
| `DEPLOY_BACKUP_PATH` | `C:\WhatYouDid\Backups` |
| `DEPLOY_CONFIG_BACKUP_PATH` | `C:\WhatYouDid\AppSettings` |

For setup instructions for each secret see `.features/cicd-secrets-setup.md`.

---

## Rollback

Each deploy moves the previous `Production\` folder to `Backups\Production_<timestamp>\`. To roll back:

```powershell
Import-Module WebAdministration
Stop-WebSite -Name '<site>'; Stop-WebAppPool -Name '<pool>'
Remove-Item -Recurse -Force C:\WhatYouDid\Production
Move-Item C:\WhatYouDid\Backups\Production_<timestamp> C:\WhatYouDid\Production
Start-WebAppPool -Name '<pool>'; Start-WebSite -Name '<site>'
```

Old backups accumulate over time — prune `C:\WhatYouDid\Backups\` periodically.
