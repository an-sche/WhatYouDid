# CI/CD Secrets Setup

This document explains every GitHub Actions secret required by `.github/workflows/deploy.yml` and how to obtain each one.

Secrets are set at: **GitHub repo → Settings → Secrets and variables → Actions → New repository secret**

---

## Secrets required now (build, test & stage)

### `DEPLOY_HOST`
The IP address or hostname of the Windows IIS server.

- If using a static IP, paste it directly (e.g. `192.168.1.100`).
- If using a hostname/DNS name, ensure it resolves from GitHub's runners (i.e. it must be publicly reachable or tunnelled).
- To find the current IP on the server: open PowerShell and run `(Get-NetIPAddress -AddressFamily IPv4).IPAddress`.

---

### `DEPLOY_USER`
The SSH username on the Windows server.

- This must be a local Windows account (or domain account) with OpenSSH access configured.
- Typically `Administrator` or a dedicated deploy user you create.
- To create a dedicated user via PowerShell on the server:
  ```powershell
  New-LocalUser -Name "deploy" -NoPassword
  Add-LocalGroupMember -Group "Administrators" -Member "deploy"
  ```

---

### `DEPLOY_SSH_KEY`
The **private** SSH key used to authenticate the GitHub runner with the server.

1. On your local machine (or the server), generate a key pair:
   ```bash
   ssh-keygen -t ed25519 -C "github-actions-deploy" -f deploy_key
   ```
   This produces `deploy_key` (private) and `deploy_key.pub` (public). Do **not** set a passphrase.

2. On the Windows server, append the public key to the deploy user's `authorized_keys`:
   - File location: `C:\Users\<DEPLOY_USER>\.ssh\authorized_keys`
   - If the `.ssh` folder doesn't exist, create it.
   - Paste the contents of `deploy_key.pub` into `authorized_keys`.
   - Ensure OpenSSH Server is installed and running:
     ```powershell
     Get-WindowsCapability -Online -Name OpenSSH.Server*
     Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0
     Start-Service sshd
     Set-Service -Name sshd -StartupType Automatic
     ```
   - Allow SSH through the firewall:
     ```powershell
     New-NetFirewallRule -Name sshd -DisplayName 'OpenSSH Server' -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22
     ```

3. Paste the **entire contents** of `deploy_key` (the private key file) as the secret value — including the `-----BEGIN...-----` and `-----END...-----` lines.

4. Delete `deploy_key` and `deploy_key.pub` from your local machine once the secret is saved.

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

## Secrets required later (promote to live — currently commented out)

These are only needed when you uncomment the IIS stop/swap/start steps in the workflow.

---

### `DEPLOY_TARGET_PATH`
The absolute Windows path to the **live IIS site folder** — where the running app files live.

- Example: `C:\inetpub\wwwroot\WhatYouDid`
- This is the physical path configured in IIS for the site's root.
- To find it: open IIS Manager → select the site → Basic Settings → Physical Path.
- Use a **Windows-style path with backslashes** as the secret value.

---

### `DEPLOY_SITE_NAME`
The name of the IIS website exactly as shown in IIS Manager.

- Example: `WhatYouDid`
- To list all site names via PowerShell on the server:
  ```powershell
  Import-Module WebAdministration
  Get-Website | Select-Object Name
  ```

---

### `DEPLOY_APP_POOL`
The name of the IIS Application Pool associated with the site.

- Example: `WhatYouDidPool`
- To find it: IIS Manager → Application Pools, or:
  ```powershell
  Import-Module WebAdministration
  Get-WebApplication | Select-Object applicationPool
  # or check the site directly:
  (Get-Website -Name "WhatYouDid").applicationPool
  ```

---

## Verify SSH connectivity

Before running the workflow, confirm the runner can reach your server by testing from your local machine with the same key:

```bash
ssh -i deploy_key -o StrictHostKeyChecking=no <DEPLOY_USER>@<DEPLOY_HOST> "echo connected"
```

You should see `connected` printed with no errors.
