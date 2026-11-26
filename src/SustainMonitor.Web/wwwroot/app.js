// Small helpers called from Blazor through JS interop.
window.sm = {
    // Download a file streamed from the server (evidence).
    downloadFromStream: async (fileName, streamRef) => {
        const buffer = await streamRef.arrayBuffer();
        const url = URL.createObjectURL(new Blob([buffer]));
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName ?? 'download';
        a.click();
        a.remove();
        URL.revokeObjectURL(url);
    },
    // Draw a QR code into an element (qrcodejs from cdnjs).
    qr: (elementId, text) => {
        const el = document.getElementById(elementId);
        if (!el) return;
        el.innerHTML = '';
        const draw = () => new QRCode(el, { text, width: 120, height: 120, correctLevel: QRCode.CorrectLevel.M });
        if (window.QRCode) draw();
        else window.addEventListener('load', draw, { once: true });
    },
    print: () => window.print(),
    origin: () => window.location.origin,
    // Light/dark theme: saved choice wins, otherwise the system setting.
    theme: () => document.documentElement.dataset.theme || 'dark',
    toggleTheme: () => {
        const next = (document.documentElement.dataset.theme === 'light') ? 'dark' : 'light';
        document.documentElement.dataset.theme = next;
        try { localStorage.setItem('sm-theme', next); } catch { }
        document.querySelector('meta[name=theme-color]')?.setAttribute('content', next === 'light' ? '#eef3f9' : '#060a14');
        return next;
    },
};
