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
