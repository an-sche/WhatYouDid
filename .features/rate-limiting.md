# Rate Limiting

## Overview
Account endpoints are rate limited to protect against brute-force attacks, credential stuffing, and email bombing. Limits are enforced server-side using ASP.NET Core's built-in rate limiting middleware, partitioned per client IP.

## IP Resolution
The real client IP is read from the `CF-Connecting-IP` header, which Cloudflare sets on every request and cannot be spoofed by the client. This is preferred over `X-Forwarded-For` (which can be chained or faked) since all public traffic reaches the app through a Cloudflare Tunnel. In local development where Cloudflare is not present, the middleware falls back to the TCP connection's remote address.

## Protected Endpoints
Only `POST` requests to Account pages are rate limited. All other traffic passes through unrestricted.

| Endpoint | Threat |
|----------|--------|
| Login | Brute-force password attacks |
| Register | Automated account creation / bot signups |
| Forgot Password | Email bombing |
| Resend Email Confirmation | Email bombing |
| Reset Password | Token enumeration |
| Login with 2FA | TOTP brute-force (1M possible codes) |
| Login with Recovery Code | High-value recovery code exhaustion |

## Behaviour on Rejection
Requests that exceed a limit receive a `429 Too Many Requests` response. The window resets automatically — no manual intervention is needed.

## Relationship to Other Protections
Rate limiting is one layer of a broader bot protection strategy:
- **Account lockout** (ASP.NET Identity) — triggers after repeated failed login attempts, independent of the rate limiter
- **Cloudflare Bot Fight Mode** — blocks known bot IPs at the edge before they reach the server
- **Email verification** — ensures accounts are backed by a real, reachable email address
