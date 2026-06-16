// RackPeek graph rendering shim.
//
// Public API (called from Blazor via JSInterop):
//   window.rackpeekGraph.render(elementId, mermaidSource)
//     – Wipes the target element, runs Mermaid on the source, and inserts
//       the resulting SVG with pan/zoom enabled.
//
// Assumes Mermaid is already loaded globally (via <script src="mermaid.min.js">).
// The ELK layout plugin is loaded lazily on first render.

(function () {
    "use strict";

    let _initPromise = null;
    let _mermaidScriptPromise = null;

    function loadMermaidScript() {
        // Inject the (large) mermaid UMD bundle on first use rather than
        // shipping it on every page. Keeps non-graph pages light so Blazor's
        // SignalR circuit establishes promptly under load.
        if (_mermaidScriptPromise) return _mermaidScriptPromise;

        _mermaidScriptPromise = new Promise((resolve, reject) => {
            if (window.mermaid) {
                resolve();
                return;
            }
            const script = document.createElement("script");
            // Resolve against <base href> so the script works both at the root
            // (Server, /) and under a subpath (GitHub Pages WASM, /RackPeek/).
            script.src = new URL(
                "_content/Shared.Rcl/js/graph/mermaid.min.js",
                document.baseURI).href;
            script.async = true;
            script.onload = () => resolve();
            script.onerror = () => reject(new Error("Failed to load mermaid.min.js"));
            document.head.appendChild(script);
        });

        return _mermaidScriptPromise;
    }

    async function ensureInitialised() {
        if (_initPromise) return _initPromise;

        _initPromise = (async () => {
            await loadMermaidScript();

            if (!window.mermaid) {
                throw new Error("Mermaid bundle loaded but window.mermaid is undefined");
            }

            // Register the ELK layout loader. Mermaid 11 dispatches by the
            // `layout` config key; "elk" maps to the layered algorithm.
            // Resolve against <base href> for the same reason loadMermaidScript
            // does — dynamic import() in a classic script resolves relative to
            // the document base, not the script URL.
            try {
                const elkUrl = new URL(
                    "_content/Shared.Rcl/js/graph/mermaid-layout-elk.min.mjs",
                    document.baseURI).href;
                const elk = await import(elkUrl);
                window.mermaid.registerLayoutLoaders(elk.default);
            } catch (e) {
                // Fall back silently to the default dagre layout — still
                // renders, just with the less polished arrow routing.
                console.warn("[rackpeekGraph] ELK plugin failed to load:", e);
            }

            window.mermaid.initialize({
                startOnLoad: false,
                securityLevel: "loose",
                theme: "dark",
                fontFamily: "ui-sans-serif, system-ui, -apple-system, sans-serif"
            });
        })();

        return _initPromise;
    }

    async function render(elementId, source) {
        const host = document.getElementById(elementId);
        if (host) host.innerHTML = "";

        // Empty source = "clear" request. Don't hand "" to mermaid.render —
        // it treats that as malformed input and produces a "Syntax error in
        // text" SVG which can leak out into the page if the host element has
        // already been detached (e.g. component disposal during navigation).
        if (!source || !source.trim()) {
            cleanupOrphans();
            return;
        }

        // Defer the (heavy) mermaid + ELK work to browser idle time so the
        // Blazor circuit and nav-click handlers stay responsive on pages
        // that render diagrams (e.g. the homepage). If the host element is
        // gone by the time idle fires (user navigated away), bail.
        await waitForIdle();
        if (!document.getElementById(elementId)) {
            cleanupOrphans();
            return;
        }

        await ensureInitialised();

        // The host may have been detached while ensureInitialised was awaiting
        // (especially on first render). Re-fetch and bail if it's gone.
        const liveHost = document.getElementById(elementId);
        if (!liveHost) {
            cleanupOrphans();
            return;
        }

        // A unique id per render avoids collisions when the same element is
        // re-rendered with different source.
        const renderId = `rpkg-${elementId}-${Date.now()}`;
        let result;
        try {
            result = await window.mermaid.render(renderId, source);
        } finally {
            // Mermaid creates a scratch <div id="d{renderId}"> in <body> for
            // measurement and normally removes it; sweep up just in case.
            const scratch = document.getElementById("d" + renderId);
            if (scratch && scratch.parentElement) scratch.parentElement.removeChild(scratch);
        }

        // Host may have been disposed during the render await.
        const stillLive = document.getElementById(elementId);
        if (!stillLive) {
            cleanupOrphans();
            return;
        }

        stillLive.innerHTML = result.svg;

        const svgEl = stillLive.querySelector("svg");
        if (svgEl) {
            // Render responsively but cap at the diagram's natural size, so a
            // narrow/tall diagram (e.g. the logical view, which stacks subnet →
            // host cards vertically) isn't upscaled to fill the pane — that blew
            // node text and subgraph titles up many times over. Width fills the
            // container only up to the diagram's natural width (from the
            // viewBox); a wider-than-pane diagram still scales down to fit.
            // Height follows the aspect ratio.
            const viewBox = (svgEl.getAttribute("viewBox") || "").trim().split(/\s+/);
            const naturalWidth = viewBox.length === 4 ? parseFloat(viewBox[2]) : NaN;
            svgEl.removeAttribute("width");
            svgEl.removeAttribute("height");
            svgEl.style.width = "100%";
            svgEl.style.height = "auto";
            svgEl.style.maxWidth = Number.isFinite(naturalWidth) ? `${naturalWidth}px` : "100%";
            svgEl.style.maxHeight = "100%";
        }

        if (result.bindFunctions) result.bindFunctions(stillLive);
    }

    function waitForIdle() {
        return new Promise((resolve) => {
            if (typeof window.requestIdleCallback === "function") {
                // 2s timeout means we still fire eventually if the browser
                // never goes idle.
                window.requestIdleCallback(() => resolve(), { timeout: 2000 });
            } else {
                // Safari < 16 has no requestIdleCallback — fall back to a
                // short defer that still yields the current event loop.
                setTimeout(resolve, 50);
            }
        });
    }

    function cleanupOrphans() {
        // Mermaid sometimes leaves "d{renderId}" scratch nodes attached to
        // <body> when the originating host is gone — remove any that match
        // our renderId prefix.
        document.querySelectorAll("body > [id^='drpkg-']").forEach((el) => {
            el.parentElement?.removeChild(el);
        });
    }

    function triggerDownload(blob, filename) {
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        // Defer revoke so Safari has time to start the download.
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }

    function downloadSvg(elementId, filename, background) {
        const host = document.getElementById(elementId);
        if (!host) {
            console.warn(`[rackpeekGraph] element '${elementId}' not found`);
            return;
        }

        const svg = host.querySelector("svg");
        if (!svg) {
            console.warn(`[rackpeekGraph] no SVG in element '${elementId}' to export`);
            return;
        }

        // Clone so the in-page interactive copy isn't modified. Ensure
        // xmlns + a viewBox-derived width/height so the file renders cleanly
        // in any standalone viewer.
        const clone = svg.cloneNode(true);
        if (!clone.getAttribute("xmlns")) {
            clone.setAttribute("xmlns", "http://www.w3.org/2000/svg");
        }
        if (!clone.getAttribute("xmlns:xlink")) {
            clone.setAttribute("xmlns:xlink", "http://www.w3.org/1999/xlink");
        }
        const vb = clone.getAttribute("viewBox");
        if (vb && !clone.getAttribute("width")) {
            const parts = vb.split(/\s+/);
            if (parts.length === 4) {
                clone.setAttribute("width", parts[2]);
                clone.setAttribute("height", parts[3]);
            }
        }

        // Inject a full-bleed background rect so exports match the in-app
        // appearance instead of rendering on a transparent canvas (which
        // shows as white in most viewers / dark in others depending on OS).
        const bg = (background ?? "#18181b").trim();
        if (bg && bg.toLowerCase() !== "transparent" && bg.toLowerCase() !== "none") {
            const rect = document.createElementNS("http://www.w3.org/2000/svg", "rect");
            const vbParts = (vb || "").split(/\s+/);
            if (vbParts.length === 4) {
                rect.setAttribute("x", vbParts[0]);
                rect.setAttribute("y", vbParts[1]);
                rect.setAttribute("width", vbParts[2]);
                rect.setAttribute("height", vbParts[3]);
            } else {
                rect.setAttribute("x", "0");
                rect.setAttribute("y", "0");
                rect.setAttribute("width", "100%");
                rect.setAttribute("height", "100%");
            }
            rect.setAttribute("fill", bg);
            clone.insertBefore(rect, clone.firstChild);
        }

        const serialiser = new XMLSerializer();
        const body = serialiser.serializeToString(clone);
        const xml = '<?xml version="1.0" encoding="UTF-8" standalone="no"?>\n' + body;
        triggerDownload(new Blob([xml], { type: "image/svg+xml;charset=utf-8" }), filename);
    }

    function downloadText(content, filename, mime) {
        triggerDownload(
            new Blob([content ?? ""], { type: (mime ?? "text/plain") + ";charset=utf-8" }),
            filename);
    }

    function buildExportSvg(host, background) {
        const svg = host.querySelector("svg");
        if (!svg) return null;

        const clone = svg.cloneNode(true);
        if (!clone.getAttribute("xmlns")) {
            clone.setAttribute("xmlns", "http://www.w3.org/2000/svg");
        }
        if (!clone.getAttribute("xmlns:xlink")) {
            clone.setAttribute("xmlns:xlink", "http://www.w3.org/1999/xlink");
        }

        // Prefer viewBox dimensions — Mermaid sets `width="100%"` on the
        // live SVG so that the responsive page layout can size it. Parsing
        // that as a number gives 100 (px), producing a postage-stamp PNG.
        // The viewBox carries the real document dimensions.
        const vb = clone.getAttribute("viewBox");
        const vbParts = (vb || "").split(/\s+/);
        let width = 0, height = 0;
        if (vbParts.length === 4) {
            width = parseFloat(vbParts[2]) || 0;
            height = parseFloat(vbParts[3]) || 0;
        }
        if (!width || !height) {
            const rect = svg.getBoundingClientRect();
            if (!width) width = rect.width;
            if (!height) height = rect.height;
        }
        // Pin explicit pixel dimensions so `new Image()` knows how to size
        // the bitmap. Strip any % units inherited from the live element.
        clone.setAttribute("width", String(width));
        clone.setAttribute("height", String(height));
        clone.removeAttribute("style");

        const bg = (background ?? "#18181b").trim();
        if (bg && bg.toLowerCase() !== "transparent" && bg.toLowerCase() !== "none") {
            const rect = document.createElementNS("http://www.w3.org/2000/svg", "rect");
            if (vbParts.length === 4) {
                rect.setAttribute("x", vbParts[0]);
                rect.setAttribute("y", vbParts[1]);
                rect.setAttribute("width", vbParts[2]);
                rect.setAttribute("height", vbParts[3]);
            } else {
                rect.setAttribute("x", "0");
                rect.setAttribute("y", "0");
                rect.setAttribute("width", String(width));
                rect.setAttribute("height", String(height));
            }
            rect.setAttribute("fill", bg);
            clone.insertBefore(rect, clone.firstChild);
        }

        const serialiser = new XMLSerializer();
        const body = serialiser.serializeToString(clone);
        return { xml: body, width, height };
    }

    function downloadSvg(elementId, filename, background) {
        const host = document.getElementById(elementId);
        if (!host) {
            console.warn(`[rackpeekGraph] element '${elementId}' not found`);
            return;
        }
        const built = buildExportSvg(host, background);
        if (!built) {
            console.warn(`[rackpeekGraph] no SVG in element '${elementId}' to export`);
            return;
        }
        const xml = '<?xml version="1.0" encoding="UTF-8" standalone="no"?>\n' + built.xml;
        triggerDownload(new Blob([xml], { type: "image/svg+xml;charset=utf-8" }), filename);
    }

    function downloadPng(elementId, filename, background, scale) {
        const host = document.getElementById(elementId);
        if (!host) {
            console.warn(`[rackpeekGraph] element '${elementId}' not found`);
            return Promise.resolve();
        }
        const built = buildExportSvg(host, background);
        if (!built || !built.width || !built.height) {
            console.warn(`[rackpeekGraph] cannot rasterise SVG in element '${elementId}'`);
            return Promise.resolve();
        }

        // Render at 2× DPI by default so the PNG is sharp on retina displays
        // and when zoomed in for documentation.
        const ratio = scale && scale > 0 ? scale : 2;

        // A data URL (vs Blob URL) avoids a class of foreignObject taint
        // issues in some browsers — the SVG is treated as same-origin and
        // doesn't get caught by the canvas security checks. The trade-off
        // is a longer string, which is fine for diagram-sized payloads.
        const url = "data:image/svg+xml;charset=utf-8," + encodeURIComponent(built.xml);

        return new Promise((resolve) => {
            const img = new Image();
            img.onload = () => {
                try {
                    const canvas = document.createElement("canvas");
                    canvas.width = Math.ceil(built.width * ratio);
                    canvas.height = Math.ceil(built.height * ratio);
                    const ctx = canvas.getContext("2d");
                    ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
                    canvas.toBlob((png) => {
                        if (png) {
                            triggerDownload(png, filename);
                        } else {
                            // Tainted canvas — fall back to toDataURL which
                            // throws SecurityError instead of returning null.
                            try {
                                const dataUrl = canvas.toDataURL("image/png");
                                fetch(dataUrl)
                                    .then(r => r.blob())
                                    .then(b => triggerDownload(b, filename))
                                    .catch(e => console.warn("[rackpeekGraph] PNG fallback failed", e))
                                    .finally(resolve);
                                return;
                            } catch (e) {
                                console.warn("[rackpeekGraph] canvas tainted, cannot export PNG", e);
                            }
                        }
                        resolve();
                    }, "image/png");
                } catch (e) {
                    console.warn("[rackpeekGraph] PNG render failed", e);
                    resolve();
                }
            };
            img.onerror = (err) => {
                console.warn("[rackpeekGraph] SVG could not be loaded as image (likely a foreignObject/HTML-label rendering issue in this browser)", err);
                resolve();
            };
            img.src = url;
        });
    }

    window.rackpeekGraph = { render, downloadSvg, downloadPng, downloadText };
})();
