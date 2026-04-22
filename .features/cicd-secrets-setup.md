# CI/CD Secrets Setup

All secrets are set at: **GitHub repo → Settings → Secrets and variables → Actions → New repository secret**

---

## Quick reference

| Secret | Value |
|---|---|
| `DEPLOY_HOST` | Server public IP address |
| `DEPLOY_USER` | Windows username on the server |
| `DEPLOY_SSH_KEY` | Private SSH key (see setup below) |
| `DEPLOY_SCRIPT_PATH` | `C:\WhatYouDid\deploy.ps1` |
| `DEPLOY_TARGET_PATH` | `C:\WhatYouDid\Production` |
| `STAGING_TARGET_PATH` | `C:\WhatYouDid\Staging` |
| `DEPLOY_BACKUP_PATH` | `C:\WhatYouDid\Backups` |
| `DEPLOY_CONFIG_BACKUP_PATH` | `C:\WhatYouDid\AppSettings` |
| `DEPLOY_SITE_NAME` | IIS website name (from IIS Manager) |
| `DEPLOY_APP_POOL` | IIS application pool name (from IIS Manager) |

---

## `DEPLOY_HOST`
The server's public IP address. Do not use the local IP (`192.168.x.x`) — GitHub's runners connect over the public internet.

---

## `DEPLOY_USER`
Your Windows username on the server. Must be in the Administrators group.

---

## `DEPLOY_SSH_KEY`
The **private** SSH key used to authenticate the GitHub runner with the server.

**1. Generate a key pair on your local machine:**
```bash
ssh-keygen -t ed25519 -C "github-actions-deploy" -f deploy_key
```
This produces `deploy_key` (private) and `deploy_key.pub` (public). Do **not** set a passphrase.

**2. Fix permissions on the private key:**

Linux/Mac:
```bash
chmod 600 ./deploy_key
```
Windows:
```powershell
icacls .\deploy_key /inheritance:r /grant "<YOUR_USER>:(F)"
```

**3. Install and start OpenSSH Server on the Windows server:**
```powershell
Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0
Start-Service sshd
Set-Service -Name sshd -StartupType Automatic
```

**4. Open port 22 in the Windows firewall:**
```powershell
New-NetFirewallRule -Name sshd -DisplayName 'OpenSSH Server' -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22
```

**5. Add the public key to `administrators_authorized_keys`:**

Administrator-group users must use this file — OpenSSH ignores `~\.ssh\authorized_keys` for admins:
```powershell
New-Item -Path "C:\ProgramData\ssh\administrators_authorized_keys" -ItemType File -Force
Add-Content -Path "C:\ProgramData\ssh\administrators_authorized_keys" -Value "ssh-ed25519 <YOUR_PUBLIC_KEY_HERE> github-actions-deploy"
icacls C:\ProgramData\ssh\administrators_authorized_keys /inheritance:r /grant "SYSTEM:(F)" /grant "BUILTIN\Administrators:(F)"
```

**6. Paste the entire contents of `deploy_key` as the secret value** — including the `-----BEGIN OPENSSH PRIVATE KEY-----` and `-----END OPENSSH PRIVATE KEY-----` lines.

**7. Delete the key files from your local machine.**

---

## `DEPLOY_SCRIPT_PATH`
Path to `deploy.ps1` on the server. The script is placed manually — the workflow never uploads it (see `cicd-pipeline.md` for why).

Copy `scripts/deploy.ps1` from the repo to the server, then set this secret to its path.

---

## `DEPLOY_SITE_NAME`
The IIS website name exactly as shown in IIS Manager. To find it:
```powershell
Import-Module WebAdministration
Get-Website | Select-Object Name
```

---

## `DEPLOY_APP_POOL`
The IIS application pool name. To find it:
```powershell
Import-Module WebAdministration
(Get-Website -Name "WhatYouDid").applicationPool
```

---

## Verify SSH connectivity

Test from outside your home network (tether to your phone — most home routers don't support NAT hairpinning so the public IP won't work from inside):

```bash
ssh -i ./deploy_key -o StrictHostKeyChecking=no <DEPLOY_USER>@<DEPLOY_HOST> "echo connected"
```

---

## Troubleshooting SSH key rejection

Run with `-v` to diagnose:
```bash
ssh -v -i ./deploy_key -o StrictHostKeyChecking=no <DEPLOY_USER>@<DEPLOY_HOST>
```

Common causes:
- **Wrong file** — admins must use `C:\ProgramData\ssh\administrators_authorized_keys`, not `~\.ssh\authorized_keys`
- **Bad permissions on `administrators_authorized_keys`** — re-run the `icacls` command in step 5
- **Bad permissions on the private key** — re-run step 2
- **Key mismatch** — confirm the public key in `administrators_authorized_keys` matches `deploy_key.pub` exactly (single line, no wrapping)
