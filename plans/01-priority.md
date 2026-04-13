# Plan Priority Order

## Up Next
| Priority | Plan | Notes |
|----------|------|-------|
| 1 | `07-bot-protection.md` Item 6: Honeypot fields | Low effort, registration is now open — addresses dumb bots immediately |
| 2 | `07-bot-protection.md` Item 5: Turnstile | Medium effort, strongest automated signup protection |
| 3 | `07-bot-protection.md` Item 9: API rate limiting | Low effort, protects write endpoints from authenticated abuse — use named policy by user ID in `WasmEndpointExtensions.cs` |
| 4 | `06-service-worker-caching.md` | Performance win, nothing is broken without it |
| 5 | `08-session-lifetime.md` | UX improvement, "Remember me" checkbox currently has no effect |
| 6 | `07-bot-protection.md` Items 7–8: Google-only registration, disposable email blocking | Only needed if spam becomes a real problem |
