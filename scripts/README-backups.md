# SQL Express Database Backups

SQL Server Express has no SQL Server Agent, so we use a PowerShell script
(`Backup-Database.ps1`) driven by **Windows Task Scheduler**.

The script takes a native `BACKUP DATABASE` to a timestamped `.bak` file and
deletes backups older than the retention window.

## 1. Fill in the placeholders

Edit the `param()` defaults at the top of `Backup-Database.ps1`, **or** pass them
when scheduling (see below):

| Parameter         | Example                              | Notes                                    |
| ----------------- | ------------------------------------ | ---------------------------------------- |
| `-ServerInstance` | `.\SQLEXPRESS`                       | SQL Express default instance name        |
| `-Database`       | `WhatYouDid`                         | Database to back up                      |
| `-BackupFolder`   | `C:\SqlBackups\WhatYouDid`           | Created automatically if missing         |
| `-RetentionDays`  | `30`                                 | Older local `.bak` files are deleted     |
| `-MinLocalBackups` | `7`                                 | Always keep this many newest, regardless of age (outage safety net) |
| `-RcloneRemote`   | `r2`                                 | rclone remote name; empty = skip upload  |
| `-RcloneRemotePath` | `whatyoudid-backups/SqlBackups`    | `<bucket>/<path>` in R2                   |
| `-RcloneConfig`   | `C:\Users\<USERNAME>\AppData\Roaming\rclone\rclone.conf` | Needed when the task runs as **SYSTEM** (see below) |

> Remote retention (deleting old backups in R2) is handled by a **bucket
> lifecycle rule in Cloudflare**, not by this script.

> `sqlcmd` must be on PATH. It ships with SQL Server / the
> [command-line tools](https://learn.microsoft.com/sql/tools/sqlcmd/sqlcmd-utility).
> Check with: `sqlcmd -?`

## 1b. Set up Cloudflare R2 upload (rclone)

The script pushes each `.bak` to **Cloudflare R2** using
[rclone](https://rclone.org). R2 is S3-compatible, so rclone uses its `s3`
backend with provider `Cloudflare`. It's **token-based** — no browser OAuth,
which makes it ideal for an unattended server. R2 also has no egress fees and a
10 GB free tier.

### Create the bucket and an API token (Cloudflare dashboard)

1. **R2** → **Create bucket**, e.g. `whatyoudid-backups`. Note the location.
2. **R2** → **Manage R2 API Tokens** → **Create API token**.
   - Permission: **Object Read & Write** (scope it to just this bucket).
   - Create it, then copy the **Access Key ID** and **Secret Access Key**
     (the secret is shown only once).
3. Note your **Account ID** (R2 overview page). Your S3 endpoint is:
   `https://<ACCOUNT_ID>.r2.cloudflarestorage.com`

### Install rclone

```powershell
winget install Rclone.Rclone
# or download the zip from https://rclone.org/downloads/ and add it to PATH
rclone version
```

### Configure the remote (one-time)

Run `rclone config` on the server. No browser needed — you just paste the keys:

1. `n` for a new remote.
2. Name it `r2` (this is your `-RcloneRemote` value).
3. Storage type: `s3`.
4. Provider: `Cloudflare`.
5. `env_auth`: `false` (enter keys manually).
6. `access_key_id`: paste the R2 Access Key ID.
7. `secret_access_key`: paste the R2 Secret Access Key.
8. `region`: `auto` (or leave blank).
9. `endpoint`: `https://<ACCOUNT_ID>.r2.cloudflarestorage.com`
10. `Edit advanced config?` → `n`. Confirm `y`, then `q` to quit.

> You can also skip the wizard and paste this straight into `rclone.conf`:
>
> ```ini
> [r2]
> type = s3
> provider = Cloudflare
> access_key_id = <ACCESS_KEY_ID>
> secret_access_key = <SECRET_ACCESS_KEY>
> endpoint = https://<ACCOUNT_ID>.r2.cloudflarestorage.com
> region = auto
> ```

Test it (note: for S3/R2 the bucket is the first path segment):

```powershell
rclone lsd r2:                                  # lists buckets
rclone mkdir r2:whatyoudid-backups/SqlBackups   # ensure the path exists
```

> **Where are the keys stored?** In `rclone.conf` under the running user's
> profile (`%APPDATA%\rclone\rclone.conf`). The scheduled task must run as the
> **same account** you configured rclone under, or point it at that file with
> `--config`. See "Running as SYSTEM with rclone" below.

## 2. Test it manually first

Open PowerShell **as the account that will run the task** and run:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\path\to\scripts\Backup-Database.ps1" `
    -ServerInstance ".\SQLEXPRESS" -Database "WhatYouDid" -BackupFolder "C:\SqlBackups\WhatYouDid" `
    -RcloneRemote "r2" -RcloneRemotePath "whatyoudid-backups/SqlBackups"
```

Confirm a `.bak` file appears in the backup folder **and** in the R2 bucket
(`rclone ls r2:whatyoudid-backups/SqlBackups`). If you get a permissions error,
see "Permissions" below. (Omit the `-Rclone*` params to test a local-only backup
first.)

## 3. Schedule it

Run this in an **elevated** PowerShell to create the scheduled task.

### Daily at 3:00 AM

```powershell
$script = "C:\WhatYouDid\Backup-Database.ps1"

$action  = New-ScheduledTaskAction -Execute "powershell.exe" `
    -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$script`" -Database `"WhatYouDid`" -BackupFolder `"C:\Backups`" -RcloneRemote `"r2`" -RcloneRemotePath `"whatyoudid-backups/SqlBackups`" -RcloneConfig `"C:\Users\<USERNAME>\AppData\Roaming\rclone\rclone.conf`""

$trigger = New-ScheduledTaskTrigger -Daily -At 3:00AM

# Run whether or not the user is logged on; use SYSTEM so it needs no password.
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest

$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -DontStopOnIdleEnd

Register-ScheduledTask -TaskName "SQL Backup - WhatYouDid" `
    -Action $action -Trigger $trigger -Principal $principal -Settings $settings `
    -Description "Daily backup of the WhatYouDid SQL Express database."
```

### Weekly instead (e.g. Sundays at 3 AM)

Replace the trigger line with:

```powershell
$trigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Sunday -At 3:00AM
```

## 4. Verify the scheduled task

```powershell
# See it / run it on demand
Get-ScheduledTask -TaskName "SQL Backup - WhatYouDid"
Start-ScheduledTask -TaskName "SQL Backup - WhatYouDid"

# Check the last run result (0x0 = success)
Get-ScheduledTaskInfo -TaskName "SQL Backup - WhatYouDid"
```

## Permissions

The account running the task (SYSTEM above, or `NT SERVICE\MSSQL$SQLEXPRESS`)
needs:

- **Write access** to `-BackupFolder`.
- **`BACKUP DATABASE` permission** in SQL Server. The SQL Server service account
  writes the file, so that account also needs write access to the folder. Using
  a folder on the same machine the SQL service can reach (local disk) avoids
  most issues.

If SYSTEM can't connect to SQL, grant it a login once:

```sql
CREATE LOGIN [NT AUTHORITY\SYSTEM] FROM WINDOWS;   -- if it doesn't exist
ALTER SERVER ROLE [dbcreator] ADD MEMBER [NT AUTHORITY\SYSTEM]; -- or grant BACKUP on the db
```

Alternatively, run the task as your own Windows account (it already has rights)
by setting `-UserId "<DOMAIN\You>" -LogonType Password` and supplying a password.

## Running as SYSTEM with rclone

The task runs as `SYSTEM`, but you configured rclone under **your** Windows
account, so its `rclone.conf` lives in your profile (`%APPDATA%\rclone\`) and
SYSTEM looks in its own profile instead — so rclone reports "config file not
found" and the upload fails (even though the SQL backup succeeds).

**Fix (used in the schedule example above): pass `-RcloneConfig`** pointing at
your config file. SYSTEM has full filesystem access, so it can read your
user-profile config directly — no copying, no duplicate keys:

```text
-RcloneConfig "C:\Users\<USERNAME>\AppData\Roaming\rclone\rclone.conf"
```

This is why SYSTEM works for *both* steps here: `NT AUTHORITY\SYSTEM` is a
sysadmin login (so Windows-auth `BACKUP` works), and `-RcloneConfig` gives it
the rclone credentials. R2 uses static API keys, so there's nothing to refresh.

Alternatives, if you prefer:

- **Run the task as your own account** — `-UserId "<DOMAIN\You>" -LogonType
  Password`. Your account already has both SQL rights and the rclone config in
  its profile, so you can drop `-RcloneConfig`. Register prompts for your
  password once.
- **Copy `rclone.conf` to SYSTEM's profile**:
  `C:\Windows\System32\config\systemprofile\AppData\Roaming\rclone\rclone.conf`.

## Restoring

```sql
RESTORE DATABASE [WhatYouDid]
FROM DISK = N'C:\SqlBackups\WhatYouDid\WhatYouDid_2026-06-20_0300.bak'
WITH REPLACE, RECOVERY;
```

## Off-machine copies

A `.bak` on the same disk doesn't protect against drive/machine loss — which is
exactly why the rclone upload to Cloudflare R2 (step 1b above) matters. The
local copy gives you fast restores; the R2 copy survives the server dying.
Verify the upload actually runs for a few days before relying on it.

Old backups in R2 are expired by the bucket's **lifecycle rule** (configured in
Cloudflare), so storage doesn't grow unbounded. The script only uploads.
