window.consoleEmulatorScroll = (el) => {
    if (el) el.scrollTop = el.scrollHeight;
};

window.consoleHasSelection = () => {
    const sel = window.getSelection();
    return sel && sel.toString().length > 0;
};
