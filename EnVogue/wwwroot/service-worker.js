const cacheName = 'smartwardrobe-v1';

self.addEventListener('fetch', event => {
    if (event.request.method !== 'GET' || event.request.url.startsWith('chrome-extension')) {
        return;
    }

    const shouldServeIndexHtml = event.request.mode === 'navigate';

    if (shouldServeIndexHtml) {
        event.respondWith(
            caches.open(cacheName).then(cache =>
                cache.match('index.html').then(cached => {
                    return cached || fetch('index.html');
                })
            )
        );
    } else {
        event.respondWith(
            caches.match(event.request).then(response =>
                response || fetch(event.request)
            )
        );
    }
});
