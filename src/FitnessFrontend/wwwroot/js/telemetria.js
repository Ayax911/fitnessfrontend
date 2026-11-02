// RF-033 (analítica de uso): tipo de dispositivo para la telemetría anónima de pantallas.
// Devuelve null si el navegador pidió "No rastrear": en ese caso no se mide nada.
window.itmTelemetria = {
    dispositivo: function () {
        if (navigator.doNotTrack === '1' || window.doNotTrack === '1') return null;
        var ancho = window.innerWidth || document.documentElement.clientWidth;
        if (ancho < 768) return 'movil';
        if (ancho < 1200) return 'tableta';
        return 'escritorio';
    }
};
