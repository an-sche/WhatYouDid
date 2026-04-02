# Domain & DNS

## Overview
The domain `what-you-did.com` is registered and managed via [Cloudflare](https://cloudflare.com). Public traffic is routed to the self-hosted Windows IIS server through a **Cloudflare Tunnel** — no open inbound ports are required on the server.

## Domain Registrar
**Cloudflare** — [dash.cloudflare.com](https://dash.cloudflare.com)
- Domain: `what-you-did.com`
- DNS is managed within Cloudflare (nameservers point to Cloudflare)

## Cloudflare Tunnel
The app is not directly exposed to the internet. Instead, a Cloudflare Tunnel runs as a Windows service on the host machine and establishes an outbound connection to Cloudflare's edge. Cloudflare proxies public HTTPS traffic through the tunnel to the local IIS site.

- Tunnel configuration is managed in the Cloudflare dashboard under **Zero Trust → Networks → Tunnels**
- The Windows service was installed via `winget`
- No firewall rules or port forwarding are needed on the host

## DNS Records
| Type | Name | Purpose |
|---|---|---|
| CNAME | `what-you-did.com` | Cloudflare Tunnel — routes web traffic to IIS |
| TXT | `@` | SPF record for Resend email sending |
| TXT | (Resend key) | DKIM record for Resend email signing |
| TXT | `_dmarc` | DMARC policy for email |

## TLS / HTTPS
TLS is handled by Cloudflare — certificates are managed automatically. The tunnel handles termination at the edge so the local IIS site does not need its own public certificate.
