mergeInto(LibraryManager.library, {
    JSCheckWallet: async function()
    {
        try {
            const FreighterApi = window.freighterApi;
            if (!FreighterApi) {
                Module.SendUnityMessage("_JSCheckWallet", -1, `JSCheckWallet() failed because Freighter API not detected.`);
                return;
            }
            const isConnectedRes = await FreighterApi.isConnected();
            if (isConnectedRes && isConnectedRes.error)
            {
                Module.SendUnityMessage("_JSCheckWallet", -2, `JSCheckWallet() isConnectedRes error: ${JSON.stringify(isConnectedRes)}`);
                return;
            }
            console.log("isConnected res: ", isConnectedRes);
            const isConnected = (isConnectedRes && isConnectedRes.isConnected) || false;
            if (!isConnected) {
                Module.SendUnityMessage("_JSCheckWallet", -3, `JSCheckWallet() failed because isConnected false`);
                return;
            }
            Module.SendUnityMessage("_JSCheckWallet", 1, `JSCheckWallet() success`);
            return;
        } catch (e) {
            console.error("JSCheckWallet() unspecified error:", e);
            Module.SendUnityMessage("_JSCheckWallet", -666, (e && e.message) ? e.message : String(e));
            return;
        }
    },
    
    JSGetFreighterAddress: async function(isTestnet)
    {
        console.log(UTF8ToString(isTestnet));
        const SDK = window.StellarSdk || window.stellarSdk || window.StellarSDK || window.stellarsdk || window.Stellar;
        if (!SDK) {
            Module.SendUnityMessage("_JSGetFreighterAddress", -1, `JSGetFreighterAddress() failed because Stellar SDK global not found`);
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
            Module.SendUnityMessage("_JSGetFreighterAddress", -1, `JSGetAddress() getNetworkRes error: ${getNetworkRes}`);
            return;
        }
        if (getNetworkRes.networkPassphrase !== currentNetwork) {
            Module.SendUnityMessage("_JSGetFreighterAddress", -2, `JSGetAddress() is on wrong network ${getNetworkRes.networkPassphrase} currentNetwork is ${currentNetwork}`);
            return;
        }
        const requestAccessRes = await FreighterApi.requestAccess();
        if (requestAccessRes.error) {
            console.error("requestAccessRes error: ", requestAccessRes.error);
            Module.SendUnityMessage("_JSGetFreighterAddress", -3, `JSGetAddress() requestAccessRes error: ${requestAccessRes}`);
            return;
        }
        Module.SendUnityMessage("_JSGetFreighterAddress", 1, requestAccessRes.address);
    },

    JSGetNetworkDetails: async function()
    {
        const FreighterApi = window.freighterApi;
        const getNetworkDetailsRes = await FreighterApi.getNetworkDetails();
        if (getNetworkDetailsRes.error) {
            Module.SendUnityMessage("_JSGetNetworkDetails", -1, `JSGetNetworkDetails() getNetworkDetailsRes error: ${getNetworkDetailsRes}`)
            return;
        }
        const resultString = JSON.stringify(getNetworkDetailsRes);
        Module.SendUnityMessage("_JSGetNetworkDetails", 1, resultString);
    },
    
    JSSignTransaction: async function(unsignedTransactionEnvelope, passphrase)
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
                Module.SendUnityMessage("_JSSignTransaction", responseCode, serializedError);
                return;
            }
            Module.SendUnityMessage("_JSSignTransaction", 1, signTransactionRes.signedTxXdr);
            return;
        }
        catch (e)
        {
            console.error("JSSignTransaction() unspecified error: ", e);
            Module.SendUnityMessage("_JSSignTransaction", -666, (e && e.message) ? e.message : String(e));
            return;
        }
    }
});
