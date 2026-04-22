param(
    [Parameter(Mandatory)] [string] $SiteName,
    [Parameter(Mandatory)] [string] $AppPool,
    [Parameter(Mandatory)] [string] $LivePath,
    [Parameter(Mandatory)] [string] $StagingPath,
    [Parameter(Mandatory)] [string] $Tag,
    [Parameter(Mandatory)] [string] $ConfigBackupPath,
    [Parameter(Mandatory)] [string] $BackupPath
)

$ErrorActionPreference = 'Stop'
Import-Module WebAdministration

$stagingFolder  = Join-Path $StagingPath "What You Did ($Tag)"
$timestamp      = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$backupDest     = Join-Path $BackupPath "Production_$timestamp"

# ── Validate up front so we fail before touching anything ───────────────────
if (-not (Test-Path $LivePath))         { throw "Live path not found: $LivePath" }
if (-not (Test-Path $stagingFolder))    { throw "Staging folder not found: $stagingFolder" }
if (-not (Test-Path $ConfigBackupPath)) { throw "Config backup path not found: $ConfigBackupPath" }

try {
    # ── Stop IIS ─────────────────────────────────────────────────────────────
    Write-Output "Stopping site '$SiteName' and pool '$AppPool'..."
    Stop-WebSite -Name $SiteName
    Stop-WebAppPool -Name $AppPool

    # Wait for w3wp.exe to fully exit and release file handles
    Write-Output 'Waiting for worker process to exit...'
    $waited = 0
    while ((Get-Process -Name 'w3wp' -ErrorAction SilentlyContinue) -and $waited -lt 30) {
        Start-Sleep -Seconds 1
        $waited++
    }
    if (Get-Process -Name 'w3wp' -ErrorAction SilentlyContinue) {
        Write-Output 'Timeout reached — force-stopping w3wp.exe...'
        Get-Process -Name 'w3wp' | Stop-Process -Force
        Start-Sleep -Seconds 2
    }
    Write-Output 'IIS stopped.'

    # ── Backup live folder ───────────────────────────────────────────────────
    Write-Output "Backing up '$LivePath' -> '$backupDest'..."
    Move-Item -Path $LivePath -Destination $backupDest
    New-Item -ItemType Directory -Force -Path $LivePath | Out-Null
    Write-Output 'Backup complete.'

    # ── Copy new build ───────────────────────────────────────────────────────
    Write-Output "Copying build from '$stagingFolder'..."
    Copy-Item -Recurse -Force "$stagingFolder\*" "$LivePath\"
    Write-Output 'Build copied.'

    # ── Restore config ───────────────────────────────────────────────────────
    Write-Output "Restoring config from '$ConfigBackupPath'..."
    Copy-Item -Recurse -Force "$ConfigBackupPath\*" "$LivePath\"
    Write-Output 'Config restored.'
}
finally {
    # Always restart IIS, even if the deploy failed
    Write-Output "Starting pool '$AppPool' and site '$SiteName'..."
    Start-WebAppPool -Name $AppPool
    Start-WebSite -Name $SiteName
    Write-Output 'IIS started.'
}

Write-Output 'Deploy complete.'
