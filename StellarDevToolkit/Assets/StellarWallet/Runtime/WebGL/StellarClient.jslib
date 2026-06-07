mergeInto(LibraryManager.library, {
    // Marshals a (requestId, code, data) result back into the static C# callback pointer.
    $WalletRespond: function (requestId, callback, code, data) {
        var str = (data === null || data === undefined) ? "" : String(data);
        var size = lengthBytesUTF8(str) + 1;
        var ptr = _malloc(size);
        stringToUTF8(str, ptr, size);
        {{{ makeDynCall('viii', 'callback') }}}(requestId, code, ptr);
        _free(ptr);
    },

    JSCheckWallet__deps: ['$WalletRespond'],
    JSCheckWallet: async function(requestId, callback)
    {
        try {
            // Ensure the package-loaded Freighter API has finished loading.
            if (Module.StellarWalletFreighterReady && typeof Module.StellarWalletFreighterReady.then === "function") {
                await Module.StellarWalletFreighterReady;
            }
            const FreighterApi = window.freighterApi;
            if (!FreighterApi) {
                WalletRespond(requestId, callback, -1, `JSCheckWallet() failed because Freighter API not detected.`);
                return;
            }
            const isConnectedRes = await FreighterApi.isConnected();
            if (isConnectedRes && isConnectedRes.error)
            {
                WalletRespond(requestId, callback, -2, `JSCheckWallet() isConnectedRes error: ${JSON.stringify(isConnectedRes)}`);
                return;
            }
            console.log("isConnected res: ", isConnectedRes);
            const isConnected = (isConnectedRes && isConnectedRes.isConnected) || false;
            if (!isConnected) {
                WalletRespond(requestId, callback, -3, `JSCheckWallet() failed because isConnected false`);
                return;
            }
            WalletRespond(requestId, callback, 1, `JSCheckWallet() success`);
            return;
        } catch (e) {
            console.error("JSCheckWallet() unspecified error:", e);
            WalletRespond(requestId, callback, -666, (e && e.message) ? e.message : String(e));
            return;
        }
    },

    JSGetFreighterAddress__deps: ['$WalletRespond'],
    JSGetFreighterAddress: async function(requestId, callback, isTestnet)
    {
        console.log(UTF8ToString(isTestnet));
        const SDK = window.StellarSdk || window.stellarSdk || window.StellarSDK || window.stellarsdk || window.Stellar;
        if (!SDK) {
            WalletRespond(requestId, callback, -1, `JSGetFreighterAddress() failed because Stellar SDK global not found`);
            return;
        }
        const {Networks} = (SDK.Networks ? SDK : (SDK.networks ? { Networks: SDK.networks } : SDK));
        let currentNetwork = Networks.PUBLIC;
        if (UTF8ToString(isTestnet) == "true")
        {
            currentNetwork = Networks.TESTNET;
        }
        const FreighterApi = window.freighterApi;
        const getNetworkRes = await FreighterApi.getNetwork();
        if (getNetworkRes.error) {
            WalletRespond(requestId, callback, -1, `JSGetAddress() getNetworkRes error: ${getNetworkRes}`);
            return;
        }
        if (getNetworkRes.networkPassphrase !== currentNetwork) {
            WalletRespond(requestId, callback, -2, `JSGetAddress() is on wrong network ${getNetworkRes.networkPassphrase} currentNetwork is ${currentNetwork}`);
            return;
        }
        const requestAccessRes = await FreighterApi.requestAccess();
        if (requestAccessRes.error) {
            console.error("requestAccessRes error: ", requestAccessRes.error);
            WalletRespond(requestId, callback, -3, `JSGetAddress() requestAccessRes error: ${requestAccessRes}`);
            return;
        }
        WalletRespond(requestId, callback, 1, requestAccessRes.address);
    },

    JSGetNetworkDetails__deps: ['$WalletRespond'],
    JSGetNetworkDetails: async function(requestId, callback)
    {
        const FreighterApi = window.freighterApi;
        const getNetworkDetailsRes = await FreighterApi.getNetworkDetails();
        if (getNetworkDetailsRes.error) {
            WalletRespond(requestId, callback, -1, `JSGetNetworkDetails() getNetworkDetailsRes error: ${getNetworkDetailsRes}`);
            return;
        }
        const resultString = JSON.stringify(getNetworkDetailsRes);
        WalletRespond(requestId, callback, 1, resultString);
    },

    JSSignTransaction__deps: ['$WalletRespond'],
    JSSignTransaction: async function(requestId, callback, unsignedTransactionEnvelope, passphrase)
    {
        try {
            const FreighterApi = window.freighterApi;
            const unsignedEnvelope = UTF8ToString(unsignedTransactionEnvelope);
            const networkPassphrase = UTF8ToString(passphrase);
            console.log(`JSSignTransaction: `, unsignedEnvelope);
            const signTransactionRes = await FreighterApi.signTransaction(unsignedEnvelope, {networkPassphrase: networkPassphrase});
            console.log(`JSSignTransaction completed: `, signTransactionRes);
            if (signTransactionRes && signTransactionRes.error)
            {
                const error = signTransactionRes.error;
                const userRejected =
                    (error && typeof error === "object" && error !== null && (error.message === "The user rejected this request." || error.code === -4)) ||
                    (typeof error === "string" && error === "The user rejected this request.");
                let serializedError;
                try {
                    serializedError = typeof error === "string" ? error : JSON.stringify(error);
                } catch (serializeErr) {
                    console.warn("JSSignTransaction() failed to serialize error payload", serializeErr);
                    serializedError = String(error);
                }
                const responseCode = userRejected ? -9 : -1;
                console.error("JSSignTransaction() failed to sign error: ", error);
                WalletRespond(requestId, callback, responseCode, serializedError);
                return;
            }
            WalletRespond(requestId, callback, 1, signTransactionRes.signedTxXdr);
            return;
        }
        catch (e)
        {
            console.error("JSSignTransaction() unspecified error: ", e);
            WalletRespond(requestId, callback, -666, (e && e.message) ? e.message : String(e));
            return;
        }
    }
});
