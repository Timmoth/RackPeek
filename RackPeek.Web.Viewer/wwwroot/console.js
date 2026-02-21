window.consoleEmulatorScroll = (el) => {
    if (el) el.scrollTop = el.scrollHeight;
};

window.consoleHasSelection = () => {
    const sel = window.getSelection();
    return sel && sel.toString().length > 0;
};

window.scrollToAnchor = (anchor) => {
    if (!anchor) return;

    let attempts = 0;
    const maxAttempts = 30;

    const tryScroll = () => {
        const el = document.getElementById(anchor);

        if (el) {
            el.scrollIntoView({behavior: "smooth", block: "start"});
            return;
        }

        attempts++;
        if (attempts < maxAttempts) {
            setTimeout(tryScroll, 50);
        }
    };

    tryScroll();
};