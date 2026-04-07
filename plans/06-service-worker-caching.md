# Plan: Service Worker Caching Strategy

## Goal
Make the app feel snappy on repeat visits by caching the Blazor WASM framework assets locally instead of re-downloading them on every page load.

## The Problem

The current service worker bypasses `/_framework/` entirely:

```js
if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/_framework/')) return;
```

This means the full Blazor runtime — `dotnet.native.wasm`, all app DLLs, ICU data files — is downloaded from the network on every visit. These assets are several MB and are the dominant cost of loading any WASM page.

The bypass was added to avoid serving stale cached DLLs after a new deploy. That concern is legitimate but unnecessary: .NET's publish pipeline content-hashes all `/_framework/` filenames. A new build produces new URLs, making cache poisoning impossible by design.

**Confirmed in build output:**
```
dotnet.native.befq3iek54.wasm       ← hash in filename
dotnet.runtime.2tx45g8lli.js
CodeBeam.MudBlazor.Extensions.qdmx5q6j9j.wasm
icudt_EFIGS.tptq2av103.dat
```

A filename that exists in the cache from an old build will never be requested again after a new build — new hashes mean new URLs.

## Proposed Caching Strategy

| Asset type | Strategy | Reason |
|---|---|---|
| `/_framework/*` | Cache-first | Content-hashed filenames — safe to cache forever, never stale |
| `/api/*` | Network-only | User data must always be fresh |
| Everything else | Network-first | HTML shell, CSS — fresh when possible, cache as fallback |

## Implementation

Update the `fetch` handler in `service-worker.js`:

```js
self.addEventListener('fetch', event => {
    if (event.request.method !== 'GET') return;

    const url = new URL(event.request.url);

    // Always use network for API calls
    if (url.pathname.startsWith('/api/')) return;

    // Cache-first for Blazor framework assets (content-hashed filenames)
    if (url.pathname.startsWith('/_framework/')) {
        event.respondWith(
            caches.match(event.request).then(cached => {
                if (cached) return cached;
                return fetch(event.request).then(response => {
                    caches.open(CACHE_NAME).then(cache => cache.put(event.request, response.clone()));
                    return response;
                });
            })
        );
        return;
    }

    // Network-first for everything else
    event.respondWith(
        fetch(event.request)
            .then(response => {
                caches.open(CACHE_NAME).then(cache => cache.put(event.request, response.clone()));
                return response;
            })
            .catch(() => caches.match(event.request))
    );
});
```

## Cache Versioning

Bump `CACHE_NAME` (e.g. `whatyoudid-v2`) when deploying this change. The `activate` handler will delete the old cache and force a fresh install. After this, bumping `CACHE_NAME` on future deploys is less critical (network-first handles the HTML/CSS, and framework assets are hash-addressed) but remains good hygiene.

## Expected Impact

On repeat visits, the browser loads the entire Blazor runtime from local cache — no network round-trips for the heavy assets. Only API calls and the HTML shell hit the network. The app should feel significantly faster to load on return visits, especially on mobile or poor connections.

## Notes

- `blazor.webassembly.js` and `dotnet.js` do NOT have hashes in their filenames — they are entry points that Blazor uses to bootstrap and then load the hashed assets. These will be caught by the network-first fallback, which is correct.
- The `APP_SHELL` pre-cache list does not need to include `/_framework/` assets — they will be cached on first request automatically.
