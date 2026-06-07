// Self-contained Freighter API loader so the StellarWallet package does not
// depend on the consuming project's WebGL template to include the script.
Module['StellarWalletFreighterReady'] = (function () {
    // #region agent log
    var __dbg = function (message, data) {
        try {
            fetch('http://127.0.0.1:7532/ingest/d6cddc79-b52b-4cf0-a18c-440d158ba1e4', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'X-Debug-Session-Id': '2bf4d9' },
                body: JSON.stringify({ sessionId: '2bf4d9', runId: 'pkg-loader', hypothesisId: 'A', location: 'StellarClient.jspre', message: message, data: data, timestamp: Date.now() })
            }).catch(function () {});
        } catch (e) {}
    };
    // #endregion

    if (typeof window === 'undefined' || typeof document === 'undefined') {
        // #region agent log
        __dbg('no DOM available, cannot load Freighter', {});
        // #endregion
        return Promise.resolve(false);
    }

    if (window.freighterApi) {
        // #region agent log
        __dbg('Freighter already present at startup', { keys: Object.keys(window.freighterApi || {}) });
        // #endregion
        return Promise.resolve(true);
    }

    return new Promise(function (resolve) {
        var existing = document.querySelector('script[data-stellar-freighter]');
        var onReady = function () {
            // #region agent log
            __dbg('Freighter script onload fired', {
                freighterApiType: typeof window.freighterApi,
                hasFreighterApi: !!window.freighterApi,
                freighterKeys: window.freighterApi ? Object.keys(window.freighterApi) : null,
                globalCandidates: {
                    freighterApi: typeof window.freighterApi,
                    freighter: typeof window.freighter
                }
            });
            // #endregion
            resolve(!!window.freighterApi);
        };
        var onFail = function (e) {
            // #region agent log
            __dbg('Freighter script failed to load', { error: e && e.message ? e.message : String(e) });
            // #endregion
            resolve(false);
        };

        if (existing) {
            // #region agent log
            __dbg('Freighter script tag already exists, waiting for load', {});
            // #endregion
            existing.addEventListener('load', onReady);
            existing.addEventListener('error', onFail);
            return;
        }

        var s = document.createElement('script');
        s.src = 'https://cdnjs.cloudflare.com/ajax/libs/stellar-freighter-api/3.0.0/index.min.js';
        s.async = true;
        s.setAttribute('data-stellar-freighter', '1');
        s.onload = onReady;
        s.onerror = onFail;
        // #region agent log
        __dbg('Injecting Freighter script', { src: s.src });
        // #endregion
        document.head.appendChild(s);
    });
})();
