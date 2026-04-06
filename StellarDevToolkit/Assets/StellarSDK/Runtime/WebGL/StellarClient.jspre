
Module['SendUnityMessage'] = function(functionName, code, data) {
    const response = { function: functionName, code: code, data: data };
    
    // Log message for debugging
    console.log("SendUnityMessage:", response);

    // Ensure SendMessage exists in the WebGL context before calling it
    if (typeof SendMessage !== "undefined") {
        SendMessage("WalletManager", "StellarResponse", JSON.stringify(response));
    } else {
        console.warn("SendMessage is not available in WebGL context.");
    }
};

//
// Module['DecodeStellarXDR'] = function(xdrBase64) {
//     if (!window.StellarXDR || !window.StellarXDR.module) {
//         console.error("Stellar XDR WASM not loaded yet.");
//         Module['SendUnityMessage'](1, "WASM module not initialized");
//         return;
//     }
//
//     try {
//         var decoded = window.StellarXDR.module.decode_xdr(xdrBase64);
//         Module['SendUnityMessage'](0, decoded);
//     } catch (error) {
//         console.error("XDR Decoding Error:", error);
//         Module['SendUnityMessage'](1, "XDR Decoding Failed");
//     }
// };
