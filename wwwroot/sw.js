const CACHE_NAME = 'qr-attendance-v1';
const STATIC_ASSETS = [
    '/',
    '/css/site.css',
    '/js/theme.js',
    '/js/toast.js',
    'https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.rtl.min.css',
    'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css',
];

// Install
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(STATIC_ASSETS))
            .then(() => self.skipWaiting())
    );
});

// Activate
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(keys
                .filter(k => k !== CACHE_NAME)
                .map(k => caches.delete(k))
            )
        ).then(() => self.clients.claim())
    );
});

// Fetch — Network First للـ API، Cache First للـ Assets
self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);

    // Skip non-GET و SignalR
    if (event.request.method !== 'GET'
        || url.pathname.startsWith('/hubs'))
        return;

    // Static assets — Cache First
    if (url.pathname.match(
        /\.(css|js|png|jpg|svg|ico|woff2?)$/)) {
        event.respondWith(
            caches.match(event.request).then(
                cached => cached || fetch(event.request)
                    .then(res => {
                        const clone = res.clone();
                        caches.open(CACHE_NAME).then(
                            c => c.put(event.request, clone));
                        return res;
                    })
            )
        );
        return;
    }

    // Pages — Network First
    event.respondWith(
        fetch(event.request)
            .catch(() => caches.match(event.request)
                || caches.match('/'))
    );
});