/* Finance Tracker PWA — Service Worker
  Update-friendly strategy:
  - network-first for app shell so new releases arrive quickly
  - cache fallback for offline use */

importScripts('./js/version.js');

const APP_VERSION = self.FINANCE_TRACKER_VERSION || '1.0.1';
const SW_VERSION = `v${APP_VERSION}`;
const CACHE_NAME = `finance-tracker-${SW_VERSION}`;

const PRECACHE_ASSETS = [
  './',
  './index.html',
  './manifest.json',
  './icon.svg',
  './icon-maskable.svg',
  './css/app.css',
  './js/version.js',
  './js/db.js',
  './js/app.js',
  'https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css',
  'https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css',
  'https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js',
];

const APP_SHELL_PATHS = new Set([
  '/',
  '/index.html',
  '/manifest.json',
  '/css/app.css',
  '/js/version.js',
  '/js/firebase-config.js',
  '/js/db.js',
  '/js/app.js',
  '/icon.svg',
  '/icon-maskable.svg',
]);

function toNoStoreRequest(request) {
  if (request.mode === 'navigate') {
    return new Request('./index.html', { cache: 'no-store' });
  }
  return new Request(request, { cache: 'no-store' });
}

function isSameOriginAppShell(url) {
  return url.origin === self.location.origin && APP_SHELL_PATHS.has(url.pathname);
}

async function networkFirst(request, fallbackToIndex = false) {
  const cache = await caches.open(CACHE_NAME);
  try {
    const response = await fetch(toNoStoreRequest(request));
    if (response && response.ok) {
      await cache.put(request, response.clone());
    }
    return response;
  } catch (_) {
    const cached = await cache.match(request);
    if (cached) return cached;
    if (fallbackToIndex) {
      const fallback = await cache.match('./index.html');
      if (fallback) return fallback;
    }
    throw _;
  }
}

async function staleWhileRevalidate(request) {
  const cache = await caches.open(CACHE_NAME);
  const cached = await cache.match(request);
  const networkPromise = fetch(request)
    .then(response => {
      if (response && response.ok) {
        cache.put(request, response.clone());
      }
      return response;
    })
    .catch(() => null);

  return cached || networkPromise;
}

// Install: cache core shell
self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.addAll(PRECACHE_ASSETS))
      .then(() => self.skipWaiting())
  );
});

// Activate: remove old caches
self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys()
      .then(keys => Promise.all(
        keys
          .filter(key => key !== CACHE_NAME)
          .map(key => caches.delete(key))
      ))
      .then(() => self.clients.claim())
  );
});

// Fetch strategy:
// - navigation + app shell: network-first (fast rollout)
// - other GET requests: stale-while-revalidate
self.addEventListener('fetch', event => {
  if (event.request.method !== 'GET') return;

  const url = new URL(event.request.url);

  if (event.request.mode === 'navigate') {
    event.respondWith(networkFirst(event.request, true));
    return;
  }

  if (isSameOriginAppShell(url)) {
    event.respondWith(networkFirst(event.request, false));
    return;
  }

  event.respondWith(staleWhileRevalidate(event.request));
});
