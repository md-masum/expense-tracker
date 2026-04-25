/**
 * Finance Tracker – Service Worker
 * Strategy: Cache-First for static assets, Network-First for HTML pages
 */
const CACHE_NAME = 'ft-v1';
const OFFLINE_URL = '/offline.html';

const STATIC_ASSETS = [
    '/offline.html',
    '/css/site.css',
    '/manifest.json',
    '/favicon.ico',
];

// ── Install: pre-cache essential assets ─────────────────────
self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME).then((cache) => {
            return cache.addAll(STATIC_ASSETS);
        }).then(() => self.skipWaiting())
    );
});

// ── Activate: remove old caches ──────────────────────────────
self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys().then((cacheNames) =>
            Promise.all(
                cacheNames
                    .filter((name) => name !== CACHE_NAME)
                    .map((name) => caches.delete(name))
            )
        ).then(() => self.clients.claim())
    );
});

// ── Fetch ────────────────────────────────────────────────────
self.addEventListener('fetch', (event) => {
    const req = event.request;

    // Only handle GET requests from same origin
    if (req.method !== 'GET') return;

    const url = new URL(req.url);

    // Don't intercept cross-origin (CDN: Bootstrap, Bootstrap Icons)
    if (url.origin !== self.location.origin) return;

    // Skip ASP.NET Identity / API paths to avoid caching auth tokens
    if (url.pathname.startsWith('/Account') ||
        url.pathname.startsWith('/api/')) {
        return;
    }

    const isNavigationRequest =
        req.mode === 'navigate' ||
        req.headers.get('accept')?.includes('text/html');

    if (isNavigationRequest) {
        // Network-First with offline fallback for HTML pages
        event.respondWith(
            fetch(req)
                .then((response) => {
                    if (response.ok) {
                        const clone = response.clone();
                        caches.open(CACHE_NAME).then((cache) =>
                            cache.put(req, clone)
                        );
                    }
                    return response;
                })
                .catch(() =>
                    caches.match(req)
                        .then((cached) => cached || caches.match(OFFLINE_URL))
                )
        );
    } else {
        // Cache-First for static assets (CSS, JS, images, etc.)
        event.respondWith(
            caches.match(req).then((cached) => {
                if (cached) return cached;

                return fetch(req).then((response) => {
                    if (response.ok) {
                        const clone = response.clone();
                        caches.open(CACHE_NAME).then((cache) =>
                            cache.put(req, clone)
                        );
                    }
                    return response;
                }).catch(() => {
                    // For image requests, could return placeholder
                    return new Response('', { status: 503 });
                });
            })
        );
    }
});

