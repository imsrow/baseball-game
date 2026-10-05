// 네트워크 우선 + 캐시 대체.
// Unity 기본 PWA 템플릿은 캐시 우선이라 새 빌드를 올려도 홈 화면 앱이 옛 index.html을 계속 쓴다.
// 여기서는 온라인이면 항상 새로 받고(받은 것은 캐시에 저장), 오프라인일 때만 캐시를 쓴다.
const cacheName = "baseball-proto-v1";

self.addEventListener('install', function (e) {
    self.skipWaiting();
});

self.addEventListener('activate', function (e) {
    e.waitUntil(self.clients.claim());
});

self.addEventListener('fetch', function (e) {
    if (e.request.method !== 'GET') {
        return;
    }

    e.respondWith((async function () {
        try {
            const response = await fetch(e.request, { cache: 'no-cache' });
            if (response && response.ok) {
                const cache = await caches.open(cacheName);
                cache.put(e.request, response.clone());
            }
            return response;
        } catch (err) {
            const cached = await caches.match(e.request);
            if (cached) {
                return cached;
            }
            throw err;
        }
    })());
});
