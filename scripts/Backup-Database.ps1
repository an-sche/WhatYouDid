<#
.SYNOPSIS
    Backs up a SQL Server (Express) database and prunes old backups.

.DESCRIPTION
    Runs a native SQL BACKUP DATABASE via sqlcmd, writing a timestamped .bak
    file to the backup folder. Deletes backups older than the retention window.

    SQL Express has no SQL Server Agent, so schedule this with Windows Task
    Scheduler (see scripts/README-backups.md).

.NOTES
    Fill in the placeholders below (or override via parameters when scheduling).
#>

param(
    # SQL Server instance. SQL Express default is ".\SQLEXPRESS".
    [string]$ServerInstance = ".\SQLEXPRESS",

    # Database to back up.
    [string]$Database = "WhatYouDid",

    # Folder where .bak files are written. Will be created if missing.
    [string]$BackupFolder = "C:\Backups",

    # How many days of backups to keep locally. Older .bak files are deleted.
    [int]$RetentionDays = 30,

    # Safety floor: always keep at least this many of the NEWEST local backups,
    # even if they're older than RetentionDays. Prevents a long backup outage
    # from wiping every copy the next time pruning runs.
    [int]$MinLocalBackups = 7,

    # --- Cloudflare R2 upload (optional, via rclone) ---
    # Name of the configured rclone remote. Leave empty to skip the upload.
    # e.g. "r2" if you ran: rclone config -> n -> name "r2" -> s3 -> Cloudflare
    [string]$RcloneRemote = "r2",

    # Path within the remote. For R2/S3 the FIRST segment is the bucket name,
    # e.g. "<bucket>/SqlBackups". Created if missing.
    [string]$RcloneRemotePath = "whatyoudid-backups/SqlBackups",

    # Path to rclone.exe. "rclone" works if it's on PATH.
    [string]$RclonePath = "rclone",

    # Path to rclone.conf. Leave empty to use the running user's default
    # (%APPDATA%\rclone\rclone.conf). REQUIRED when the scheduled task runs as
    # SYSTEM, because SYSTEM can't see your user-profile config — point this at
    # the config you set up
    [string]$RcloneConfig = "",

    # Folder for per-run logs (one timestamped .log each run). Defaults to a
    # "logs" subfolder of BackupFolder. Crucial for debugging the scheduled
    # task, which runs invisibly with no console.
    [string]$LogFolder = ""

    # Note: remote retention is handled by an R2 bucket lifecycle rule in
    # Cloudflare, so the script does not prune the remote.
)

$ErrorActionPreference = "Stop"

# Ensure the backup folder exists.
if (-not (Test-Path -LiteralPath $BackupFolder)) {
    New-Item -ItemType Directory -Path $BackupFolder -Force | Out-Null
}

# Start a per-run log so a scheduled (invisible) run is debuggable. Errors are
# captured here too, since transcript output is flushed even on a thrown error.
if ([string]::IsNullOrWhiteSpace($LogFolder)) { $LogFolder = Join-Path $BackupFolder "logs" }
if (-not (Test-Path -LiteralPath $LogFolder)) {
    New-Item -ItemType Directory -Path $LogFolder -Force | Out-Null
}
$logFile = Join-Path $LogFolder "backup_$(Get-Date -Format 'yyyy-MM-dd_HHmm').log"
Start-Transcript -Path $logFile -Append | Out-Null

# Timestamped file name, e.g. MyDb_2026-06-20_0300.bak
$timestamp  = Get-Date -Format "yyyy-MM-dd_HHmm"
$backupFile = Join-Path $BackupFolder "$($Database)_$timestamp.bak"

# COPY_ONLY keeps the backup from disturbing the differential/log backup chain.
$tsql = @"
BACKUP DATABASE [$Database]
TO DISK = N'$backupFile'
WITH INIT, COPY_ONLY, STATS = 10,
     NAME = N'$Database-Full Backup';
"@

Write-Host "[$(Get-Date -Format s)] Backing up [$Database] to $backupFile"

# -E = trusted (Windows) auth. Swap for -U/-P if you use a SQL login.
# -b = abort and return a nonzero exit code on error (so Task Scheduler sees failures).
sqlcmd -S $ServerInstance -E -b -Q $tsql

if ($LASTEXITCODE -ne 0) {
    throw "sqlcmd failed with exit code $LASTEXITCODE; backup not completed."
}

Write-Host "[$(Get-Date -Format s)] Backup complete."

# Prune backups older than the retention window — but always keep the newest
# $MinLocalBackups files regardless of age, so a long outage can't wipe them all.
$cutoff = (Get-Date).AddDays(-$RetentionDays)
$allBackups = @(Get-ChildItem -LiteralPath $BackupFolder -Filter "*.bak" |
                Sort-Object LastWriteTime -Descending)

# Skip (protect) the newest $MinLocalBackups, then delete only the expired rest.
$old = @($allBackups |
         Select-Object -Skip $MinLocalBackups |
         Where-Object { $_.LastWriteTime -lt $cutoff })

foreach ($file in $old) {
    Write-Host "[$(Get-Date -Format s)] Removing old backup $($file.Name)"
    Remove-Item -LiteralPath $file.FullName -Force
}

Write-Host "[$(Get-Date -Format s)] Done. Pruned $($old.Count) local backup(s) older than $RetentionDays day(s); kept the newest $MinLocalBackups regardless of age."

# --- Upload to Cloudflare R2 via rclone (optional) ---
if ([string]::IsNullOrWhiteSpace($RcloneRemote)) {
    Write-Host "[$(Get-Date -Format s)] RcloneRemote not set; skipping R2 upload."
    Stop-Transcript | Out-Null
    return
}

$remote = "$($RcloneRemote):$RcloneRemotePath"

# Pass --config when an explicit path is given (needed when running as SYSTEM,
# which can't see the config in your user profile).
$rcloneArgs = @()
if (-not [string]::IsNullOrWhiteSpace($RcloneConfig)) {
    $rcloneArgs += @("--config", $RcloneConfig)
}

Write-Host "[$(Get-Date -Format s)] Uploading $backupFile to $remote"

# Copy just the new file (not the whole folder) to the remote.
& $RclonePath @rcloneArgs copyto $backupFile "$remote/$([System.IO.Path]::GetFileName($backupFile))"
if ($LASTEXITCODE -ne 0) {
    throw "rclone upload failed with exit code $LASTEXITCODE."
}

Write-Host "[$(Get-Date -Format s)] Upload complete."

# Remote retention is handled by the R2 bucket lifecycle rule in Cloudflare;
# nothing to prune here.
Write-Host "[$(Get-Date -Format s)] All done."
Stop-Transcript | Out-Null
