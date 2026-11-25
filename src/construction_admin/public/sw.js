// Keeps the app shell so the page opens with no signal (a basement, a lift, a site in the hills). It never touches
// the API: data is the app's own business, and a stale answer from here would be worse than none.
//
// Network first, cache as the fallback. A deploy therefore shows up at once when there is signal, and nothing here
// can pin a phone to an old version. Hashed files under /assets are kept as they are fetched.
const CACHE = 'shell-v1';

self.addEventListener('install', () => self.skipWaiting());

self.addEventListener('activate', (event) => {
  event.waitUntil(
    (async () => {
      for (const key of await caches.keys()) {
        if (key !== CACHE) await caches.delete(key);
      }

      await self.clients.claim();
    })(),
  );
});

self.addEventListener('fetch', (event) => {
  const request = event.request;
  const url = new URL(request.url);

  if (request.method !== 'GET' || url.origin !== self.location.origin || url.pathname.startsWith('/api/')) return;

  event.respondWith(
    (async () => {
      const cache = await caches.open(CACHE);
      const navigation = request.mode === 'navigate';
      // Every screen is the same document, so a route the phone has never opened is answered with the cached one.
      const key = navigation ? '/index.html' : request;

      try {
        const controller = new AbortController();
        const timer = setTimeout(() => controller.abort(), navigation ? 6000 : 15000);
        const response = await fetch(request, { signal: controller.signal });

        clearTimeout(timer);

        if (response.ok) await cache.put(key, response.clone());

        return response;
      } catch (error) {
        const cached = await cache.match(key);

        if (cached) return cached;

        throw error;
      }
    })(),
  );
});
