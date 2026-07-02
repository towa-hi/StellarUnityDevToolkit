using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public static class LinkOpener
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    static extern void JSOpenLinkInNewTab(string url);
#endif

    public static void Open(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // Application.OpenURL is unreliable in WebGL; window.open with
        // noopener from a user gesture opens a proper new tab instead.
        JSOpenLinkInNewTab(url);
#else
        Application.OpenURL(url);
#endif
    }
}
