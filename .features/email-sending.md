# Email Sending

## Overview
Transactional email is handled via [Resend](https://resend.com). Resend sends emails on behalf of the app's verified domain.

## Provider
**Resend** — [resend.com](https://resend.com)
- Free tier: 3,000 emails/month, 100/day (permanent)
- Sending domain: `what-you-did.com`
- Dashboard: [resend.com/overview](https://resend.com/overview)

## Configuration
Configured in `appsettings` for production and `User Secrets` in dev.
| Key | Description |
|---|---|
| `Resend:ApiKey` | API key from Resend dashboard — Sending access only |
| `Resend:FromAddress` | The from address, e.g. `noreply@what-you-did.com` |

## DNS Records (Cloudflare)
The sending domain is verified via DNS records in Cloudflare. To re-verify or check status, go to **Domains** in the Resend dashboard. Three record types are required: SPF, DKIM, and DMARC.

## Features Using Email
| Feature | Description |
|---|---|
| Password Reset | User requests a reset link, valid for 1 hour |
| Email Confirmation | Not yet active — scaffolding is in place |
