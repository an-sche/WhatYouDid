const CACHE_NAME = 'whatyoudid-v1';

const APP_SHELL = [
    '/',
    '/app.css',
    '/WhatYouDid.styles.css',
    '/bootstrap/bootstrap.min.css',
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => cache.addAll(APP_SHELL))
    );
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil((async () => {
        const keys = await caches.keys();
        await Promise.all(keys.filter(k => k !== CACHE_NAME).map(k => caches.delete(k)));
        await self.clients.claim();
        const clients = await self.clients.matchAll({ type: 'window' });
        clients.forEach(c => c.postMessage({ type: 'SW_UPDATED' }));
    })());
});

self.addEventListener('fetch', event => {
    if (event.request.method !== 'GET') return;

    const url = new URL(event.request.url);

    // Always use network for API calls and Blazor framework assets
    if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/_framework/')) return;

    // Network-first: try network, cache the response, fall back to cache
    event.respondWith(
        fetch(event.request)
            .then(response => {
                const clone = response.clone();
                caches.open(CACHE_NAME).then(cache => cache.put(event.request, clone));
                return response;
            })
            .catch(() => caches.match(event.request))
    );
});
