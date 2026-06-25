// Self-contained Freighter API loader so the StellarWallet package does not
// depend on the consuming project's WebGL template to include the script.
Module['StellarWalletFreighterReady'] = (function () {
    if (typeof window === 'undefined' || typeof document === 'undefined') {
        return Promise.resolve(false);
    }

    if (window.freighterApi) {
        return Promise.resolve(true);
    }

    return new Promise(function (resolve) {
        var existing = document.querySelector('script[data-stellar-freighter]');
        var onReady = function () {
            resolve(!!window.freighterApi);
        };
        var onFail = function (e) {
            resolve(false);
        };

        if (existing) {
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
        document.head.appendChild(s);
    });
})();
