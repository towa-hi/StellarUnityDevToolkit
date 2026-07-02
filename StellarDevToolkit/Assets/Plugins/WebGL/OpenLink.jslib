mergeInto(LibraryManager.library, {
    JSOpenLinkInNewTab: function (urlPtr) {
        var url = UTF8ToString(urlPtr);
        window.open(url, "_blank", "noopener,noreferrer");
    },
});
