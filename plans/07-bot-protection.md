# Plan: Bot Protection

## Goal
Prevent bot signups, brute-force logins, and spam. Listed roughly in order of priority and implementation effort.

## Context
- Self-registration is **enabled** — all items below are now active concerns.
- The app is publicly exposed via **Cloudflare Tunnel**. Cloudflare's free tier provides Bot Fight Mode but not WAF rate limiting (that requires a paid plan).
- Login brute-force and bot signups are the most immediate active threats.

---

## ✅ Item 1: Login Lockout (Done)

ASP.NET Identity lockout is enabled. After 5 failed login attempts, the account is locked for 15 minutes. Configured in `Program.cs` and `Login.razor`.

---

## ✅ Item 2: Email Verification (Done)

`RequireConfirmedAccount = true`. All new registrations must confirm their email before signing in. Admin-created users are pre-confirmed. See `.features/email-sending.md`.

---

## ✅ Item 3: Server-Side Rate Limiting (Done)

ASP.NET Core's built-in rate limiting middleware is applied to all sensitive account `POST` endpoints, partitioned by client IP (`CF-Connecting-IP` header). Implemented in `ServiceExtensions/RateLimitingServiceExtensions.cs`.

**Note:** Cloudflare WAF rate limiting (edge-level) requires a paid Cloudflare plan. Server-side rate limiting serves as the primary rate limiting layer.

---

## ✅ Item 4: Cloudflare Bot Fight Mode (Done)

Enabled in Cloudflare Dashboard → Security → Bots. Blocks known bot IPs at the edge before they reach the server. Free tier.

---

## Item 5: Cloudflare Turnstile (CAPTCHA Replacement)

**Threat:** Automated registration and login attempts that bypass rate limits.

Turnstile is Cloudflare's free, user-friendly CAPTCHA alternative — invisible to real users (no puzzles). Well-suited here since the app is already on Cloudflare.

### Setup
1. In Cloudflare Dashboard → Turnstile → add a site. Get a **Site Key** and **Secret Key**.
2. Store the secret key in User Secrets:
   ```json
   "Turnstile": {
     "SecretKey": "..."
   }
   ```
3. Add the Turnstile JS widget to the login/register form:
   ```html
   <script src="https://challenges.cloudflare.com/turnstile/v0/api.js" async defer></script>
   <div class="cf-turnstile" data-sitekey="YOUR_SITE_KEY"></div>
   ```
4. On form submit, validate the `cf-turnstile-response` token server-side:
   ```csharp
   // POST https://challenges.cloudflare.com/turnstile/v0/siteverify
   // with secret + token. Returns { success: bool }
   ```
5. Reject the login/registration if validation fails.

**Files touched:** `Components/Account/Pages/Login.razor`, `Components/Account/Pages/Register.razor`, `Program.cs` or a new `TurnstileService`

**NuGet:** No official package — use `HttpClient` directly or a community wrapper.

---

## Item 6: Honeypot Fields

**Threat:** Simple bots that fill out all form fields automatically.

Add a hidden field to forms that real users will never see or fill in. If it has a value on submit, the request is from a bot — silently reject it (don't tell the bot it failed).

```html
<!-- In Login.razor or Register.razor, inside the EditForm -->
<!-- Hidden via CSS, NOT via type="hidden" (bots ignore display:none) -->
<div style="display:none" aria-hidden="true">
    <InputText @bind-Value="Input.Website" autocomplete="off" tabindex="-1" />
</div>
```

```csharp
// In the InputModel:
public string Website { get; set; } = "";

// In the submit handler, before processing:
if (!string.IsNullOrEmpty(Input.Website))
    return; // bot detected — silently do nothing
```

Name the field something that sounds real to a bot: `website`, `url`, `phone`, `company`, `address`.

**Files touched:** `Components/Account/Pages/Login.razor`, `Components/Account/Pages/Register.razor`

---

## Item 7: Google Sign-In Only Registration

**Threat:** Throwaway email signups and bot account creation.

If email/password registration proves to be a spam vector, registration could be restricted to Google Sign-In only — every account would then be backed by a real Google account. See **plan 04-google-login.md**.

Option: Keep email/password login for existing users but funnel new registrations exclusively through Google.

---

## Item 8: Disposable Email Blocking

**Threat:** Signups using temporary/throwaway email services (mailinator, guerrillamail, etc.).

Maintain a blocklist of known disposable email domains and reject them at registration:
- Use a community-maintained list (e.g., the `disposable-email-domains` GitHub repo).
- Check the domain of the submitted email against the list.
- Alternatively, use an API service (e.g., Abstract API, Hunter.io) to validate email deliverability.

**Files touched:** New `EmailValidationService`, `Components/Account/Pages/Register.razor`

---

## Item 9: API Endpoint Rate Limiting (Authenticated)

**Threat:** A malicious authenticated user scripting hundreds of workout/routine writes against the WASM API endpoints.

Lower priority than account endpoint rate limiting because the attacker must first have a valid confirmed account and active session. But still worth adding as a safeguard.

**Implementation note:** Use a **named policy partitioned by user ID** (not IP) applied via `.RequireRateLimiting()` directly on the endpoint group in `WasmEndpointExtensions.cs`. Partitioning by user ID is correct here — IP-based limits would punish legitimate users on shared networks (office, VPN).

Example:
```csharp
// In RateLimitingServiceExtensions.cs — add a named policy:
options.AddFixedWindowLimiter("api-writes", o =>
{
    o.PermitLimit = 50;
    o.Window = TimeSpan.FromMinutes(1);
});

// In WasmEndpointExtensions.cs — apply to write endpoints:
app.MapPost("/api/workouts", ...).RequireRateLimiting("api-writes");
```

Only apply to write operations (POST, PUT, DELETE) — GET requests don't need limiting.

**Files touched:** `ServiceExtensions/RateLimitingServiceExtensions.cs`, `EndpointExtensions/WasmEndpointExtensions.cs`

---

## Summary Table

| Item | Effort | Status |
|------|--------|--------|
| 1. Login lockout | Trivial | ✅ Done |
| 2. Email verification | Low | ✅ Done |
| 3. Server-side rate limiting (account) | Low | ✅ Done |
| 4. Cloudflare Bot Fight Mode | Trivial | ✅ Done |
| 5. Turnstile | Medium | Not done |
| 6. Honeypot fields | Low | Not done |
| 7. Google-only registration | Low (needs plan 04) | Not done |
| 8. Disposable email blocking | Medium | Not done |
| 9. API endpoint rate limiting | Low | Not done |
