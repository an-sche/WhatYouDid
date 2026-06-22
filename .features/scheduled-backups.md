# Scheduled Database Backups

## Overview
A nightly backup of the SQL Server Express database, run by **Windows Task Scheduler** (Express has no SQL Agent). This is separate from the pre-migration backup in [deployment.md](deployment.md). Script: `scripts/Backup-Database.ps1` — full setup in `scripts/README-backups.md`.

## How It Works
1. `BACKUP DATABASE` via `sqlcmd` → timestamped `.bak` in `C:\Backups`
2. Prunes local backups older than 30 days, but always keeps the newest 7
3. Uploads each `.bak` to Cloudflare R2 via rclone
4. Writes a per-run log to `C:\Backups\logs`

## Scheduled Task
| Setting | Value |
|---|---|
| Task name | `SQL Backup - WhatYouDid` |
| Runs as | `NT AUTHORITY\SYSTEM` (sysadmin login; Windows auth) |
| Schedule | Daily, 3:00 AM |

## Off-Site Storage (Cloudflare R2)
Uploads go to the `whatyoudid-backups` bucket via rclone remote `r2` (S3 backend, provider Cloudflare). Credentials live in `rclone.conf` (**never committed**). Remote retention is handled by an R2 bucket lifecycle rule, not the script.
