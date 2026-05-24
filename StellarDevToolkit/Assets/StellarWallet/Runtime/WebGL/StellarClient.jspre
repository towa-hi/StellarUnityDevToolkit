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
