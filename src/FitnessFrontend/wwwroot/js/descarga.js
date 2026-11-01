window.itmDescarga = {
    descargarTexto: function (nombreArchivo, tipoMime, texto) {
        var blob = new Blob([texto], { type: tipoMime });
        var url = URL.createObjectURL(blob);
        var link = document.createElement('a');
        link.download = nombreArchivo;
        link.href = url;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }
};

window.itmPantalla = {
    _wakeLock: null,
    solicitarWakeLock: async function () {
        if ('wakeLock' in navigator) {
            try {
                this._wakeLock = await navigator.wakeLock.request('screen');
            } catch (err) {
                // Silencioso si no se permite o está minimizado
            }
        }
    },
    liberarWakeLock: function () {
        if (this._wakeLock) {
            try {
                this._wakeLock.release();
            } catch (err) {}
            this._wakeLock = null;
        }
    }
};

let itmDeferredPrompt = null;
window.addEventListener('beforeinstallprompt', function (e) {
    e.preventDefault();
    itmDeferredPrompt = e;
});

window.itmInstalarPwa = async function () {
    if (itmDeferredPrompt) {
        itmDeferredPrompt.prompt();
        try {
            await itmDeferredPrompt.userChoice;
        } catch (e) {}
        itmDeferredPrompt = null;
    } else {
        alert('Para instalar la aplicación, abre el menú de opciones de tu navegador (los tres puntos o el botón Compartir) y selecciona «Instalar aplicación» o «Agregar a la pantalla principal».');
    }
};
