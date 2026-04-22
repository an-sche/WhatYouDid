# CI/CD Secrets Setup

This document explains every GitHub Actions secret required by `.github/workflows/deploy.yml` and how to obtain each one.

Secrets are set at: **GitHub repo → Settings → Secrets and variables → Actions → New repository secret**

---

## Secrets required now (build, test & stage)

### `DEPLOY_HOST`
The public IP address of the Windows IIS server.

- Find your public IP at [whatismyip.com](https://whatismyip.com) from the server's network.
- Your ISP may change this occasionally — if a deployment ever fails with a timeout, check whether the IP has changed.
- **Important:** do not use the server's local IP (`192.168.x.x`) — that is only reachable from inside your network. GitHub's runners connect over the public internet.

---

### `DEPLOY_USER`
The SSH username on the Windows server.

- You can use your existing Windows user account — no need to create a separate deploy user.
- The account must be in the Administrators group (required for the `administrators_authorized_keys` file below).

---

### `DEPLOY_SSH_KEY`
The **private** SSH key used to authenticate the GitHub runner with the server.

**1. Generate a key pair on your local machine:**
```bash
ssh-keygen -t ed25519 -C "github-actions-deploy" -f deploy_key
```
This produces `deploy_key` (private) and `deploy_key.pub` (public). Do **not** set a passphrase.

**2. Fix permissions on the private key (required by SSH):**

On Linux/Mac:
```bash
chmod 600 ./deploy_key
```
On Windows:
```powershell
icacls .\deploy_key /inheritance:r /grant "<YOUR_USER>:(F)"
```

**3. Install and start OpenSSH Server on the Windows server:**
```powershell
# Check if installed (should show 'Installed', not 'NotPresent')
Get-WindowsCapability -Online -Name OpenSSH.Server*

# Install if needed
Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0

# Start and set to auto-start
Start-Service sshd
Set-Service -Name sshd -StartupType Automatic
```

**4. Open port 22 in the Windows firewall:**
```powershell
New-NetFirewallRule -Name sshd -DisplayName 'OpenSSH Server' -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22
```

**5. Add the public key to `administrators_authorized_keys`:**

Because your user is in the Administrators group, Windows OpenSSH ignores the regular `~\.ssh\authorized_keys` file and reads from a system-wide file instead:

```powershell
New-Item -Path "C:\ProgramData\ssh\administrators_authorized_keys" -ItemType File -Force
Add-Content -Path "C:\ProgramData\ssh\administrators_authorized_keys" -Value "ssh-ed25519 <YOUR_PUBLIC_KEY_HERE> github-actions-deploy"
```

Then lock down the permissions on that file (OpenSSH will reject it if permissions are too broad):
```powershell
icacls C:\ProgramData\ssh\administrators_authorized_keys /inheritance:r /grant "SYSTEM:(F)" /grant "BUILTIN\Administrators:(F)"
```

**6. Paste the private key as the secret:**

Paste the **entire contents** of `deploy_key` as the `DEPLOY_SSH_KEY` secret value — including the `-----BEGIN OPENSSH PRIVATE KEY-----` and `-----END OPENSSH PRIVATE KEY-----` lines.

**7. Delete the key files from your local machine once saved.**

---

### `STAGING_TARGET_PATH`
The absolute Windows path to the **root staging folder** on the server. Tagged build subfolders are created inside it.

- Example: `C:\inetpub\staging`
- Each workflow run creates: `C:\inetpub\staging\What You Did (<tag>)\`
- Create the root folder on the server if it doesn't exist:
  ```powershell
  New-Item -ItemType Directory -Force -Path "C:\inetpub\staging"
  ```
- Use a **Windows-style path with backslashes** as the secret value.

---

## Verify SSH connectivity

Test from your local machine **while tethered to your phone** (not on the same WiFi as the server — most home routers don't support NAT hairpinning so the public IP won't work from inside the network):

```bash
ssh -i ./deploy_key -o StrictHostKeyChecking=no <DEPLOY_USER>@<DEPLOY_HOST> "echo connected"
```

You should see `connected` with no errors. If it prompts for a password, the key wasn't accepted — see the troubleshooting section below.

---

## Troubleshooting SSH key rejection

If SSH keeps falling back to password auth, run with `-v` to diagnose:

```bash
ssh -v -i ./deploy_key -o StrictHostKeyChecking=no <DEPLOY_USER>@<DEPLOY_HOST>
```

Look for `Offering public key` followed by `Authentications that can continue` — that means the server rejected the key. Common causes:

- **Wrong authorized_keys file** — Administrator-group users must use `C:\ProgramData\ssh\administrators_authorized_keys`, not `~\.ssh\authorized_keys`.
- **Bad permissions on `administrators_authorized_keys`** — re-run the `icacls` command in step 5. OpenSSH silently ignores the file if permissions are too broad.
- **Bad permissions on the private key** — re-run the `chmod 600` / `icacls` command in step 2.
- **Key mismatch** — confirm the public key in `administrators_authorized_keys` matches `deploy_key.pub` exactly (single line, no wrapping).

---

## Secrets required for promote to live

---

### `DEPLOY_SCRIPT_PATH`
The absolute Windows path to `deploy.ps1` on the server. The script is **not uploaded by the workflow** — you place it manually once and it stays there permanently. This prevents a malicious PR from modifying what runs on your server.

- Copy `scripts/deploy.ps1` from the repo to the server over RDP or SCP, then set this secret to its location.
- Example: `C:\deploy\deploy.ps1`
- To set it up:
  ```powershell
  New-Item -ItemType Directory -Force -Path "C:\deploy"
  # then copy deploy.ps1 into C:\deploy\ manually
  ```
- To update the script in future, copy the new version to the server manually — do not wire it into the workflow.

---

### `DEPLOY_TARGET_PATH`
The absolute Windows path to the **live IIS site folder** — where the running app files live.

- Example: `C:\inetpub\wwwroot\WhatYouDid`
- To find it: IIS Manager → select the site → Basic Settings → Physical Path.
- Use a **Windows-style path with backslashes** as the secret value.

---

### `DEPLOY_CONFIG_BACKUP_PATH`
The absolute Windows path to the folder containing your backed-up config files (e.g. `appsettings.Production.json`).

- Example: `C:\inetpub\config-backups\WhatYouDid`
- After a deploy, the live folder is fully wiped and replaced with the new publish output, then these backup files are copied in on top. This ensures production secrets are never in source control but are always restored after a deploy.
- Create the folder and place your config files there once; they persist across deployments:
  ```powershell
  New-Item -ItemType Directory -Force -Path "C:\inetpub\config-backups\WhatYouDid"
  Copy-Item appsettings.Production.json "C:\inetpub\config-backups\WhatYouDid\"
  ```
- Use a **Windows-style path with backslashes** as the secret value.

---

### `DEPLOY_SITE_NAME`
The name of the IIS website exactly as shown in IIS Manager.

- Example: `WhatYouDid`
- To list all site names:
  ```powershell
  Import-Module WebAdministration
  Get-Website | Select-Object Name
  ```

---

### `DEPLOY_APP_POOL`
The name of the IIS Application Pool associated with the site.

- Example: `WhatYouDidPool`
- To find it:
  ```powershell
  Import-Module WebAdministration
  (Get-Website -Name "WhatYouDid").applicationPool
  ```
