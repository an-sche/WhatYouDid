// Cache name is derived from the ?v= build id App.razor registers this worker with, so
// every build gets its own cache and `activate` deletes the previous one. It MUST vary
// per build: this file previously used a constant name, so nothing was ever evicted.
const VERSION = new URL(self.location.href).searchParams.get('v') || 'dev';
const CACHE_NAME = `whatyoudid-${VERSION}`;

const APP_SHELL = ['/'];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => cache.addAll(APP_SHELL))
    );
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil((async () => {
        const keys = await caches.keys();
        const oldKeys = keys.filter(k => k !== CACHE_NAME);
        await Promise.all(oldKeys.map(k => caches.delete(k)));
        await self.clients.claim();
        if (oldKeys.length > 0) {
            const clients = await self.clients.matchAll({ type: 'window' });
            clients.forEach(c => c.postMessage({ type: 'SW_UPDATED' }));
        }
    })());
});

// MapStaticAssets content-addresses files as name.{10-char hash}.ext (app.rbdkneuuxo.css).
const FINGERPRINTED = /\.[a-z0-9]{10}\.[a-z0-9]+$/;

function cachePut(request, response) {
    // Redirected responses are skipped: replaying one for a navigation is a network error.
    if (response.ok && response.type === 'basic' && !response.redirected) {
        const clone = response.clone();
        caches.open(CACHE_NAME).then(cache => cache.put(request, clone));
    }
    return response;
}

self.addEventListener('fetch', event => {
    if (event.request.method !== 'GET') return;

    const url = new URL(event.request.url);

    // Leave alone: other origins (font CDN), live data, the server circuit (initializers
    // and long-polling are GETs), Blazor's own asset loading (it manages /_framework/
    // integrity itself), and this script.
    if (url.origin !== self.location.origin) return;
    if (url.pathname.startsWith('/api/')) return;
    if (url.pathname.startsWith('/_blazor')) return;
    if (url.pathname.startsWith('/_framework/')) return;
    if (url.pathname === '/service-worker.js') return;

    // Fingerprinted assets: cache-first, no revalidation. A changed file gets a new URL,
    // so a cached entry can never be the wrong version.
    if (FINGERPRINTED.test(url.pathname)) {
        event.respondWith(
            caches.match(event.request).then(cached =>
                cached || fetch(event.request).then(response => cachePut(event.request, response)))
        );
        return;
    }

    // Everything else - HTML pages (both full navigations and Blazor's enhanced-navigation
    // fetches, which are mode 'cors', not 'navigate'), favicon, manifest - is not
    // fingerprinted, so it must come from the network; cache is only an offline fallback.
    event.respondWith(
        fetch(event.request)
            .then(response => cachePut(event.request, response))
            .catch(async () =>
                (await caches.match(event.request)) ||
                (event.request.mode === 'navigate' ? await caches.match('/') : Response.error()))
    );
});
