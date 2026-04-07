# Plan Priority Order

## Up Next
| Priority | Plan | Notes |
|----------|------|-------|
| 1 | `07-bot-protection.md` Item 6: Honeypot fields | Low effort, registration is now open — addresses dumb bots immediately |
| 3 | `07-bot-protection.md` Item 5: Turnstile | Medium effort, strongest automated signup protection |
| 4 | `07-bot-protection.md` Item 9: API rate limiting | Low effort, protects write endpoints from authenticated abuse — use named policy by user ID in `WasmEndpointExtensions.cs` |
| 5 | `05-playwright-ui-tests.md` | Developer quality investment, not user-facing |
| 5 | `06-service-worker-caching.md` | Performance win, nothing is broken without it |
| 6 | `07-bot-protection.md` Items 7–8: Google-only registration, disposable email blocking | Only needed if spam becomes a real problem |
