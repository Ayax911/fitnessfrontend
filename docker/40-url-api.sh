#!/bin/sh
# Genera appsettings.json y configura CSP en seguridad.conf a partir de URL_BASE_API y el hash de importmap.
set -e
: "${URL_BASE_API:?Defina URL_BASE_API con la URL pública de la API, sin barra final}"
RAIZ=/usr/share/nginx/html

# 1. Regenerar appsettings.json a partir de la plantilla
sed "s|REEMPLAZAR_EN_DESPLIEGUE|${URL_BASE_API}|g" "$RAIZ/appsettings.template.json" > "$RAIZ/appsettings.json"

# 2. H-13: Calcular hash sha256 del script inline del importmap en index.html
# La CSP exige el hash EXACTO del contenido: $(...) elimina el salto de línea final que añade sed y printf '%s' no agrega otro.
IMPORTMAP=$(sed -n '/<script type="importmap">/,/<\/script>/p' "$RAIZ/index.html" | sed '1s/.*<script type="importmap">//; $s/<\/script>.*//')
HASH=$(printf '%s' "$IMPORTMAP" | openssl dgst -sha256 -binary 2>/dev/null | openssl base64 -A 2>/dev/null || true)

if [ -n "$HASH" ]; then
    HASH_CSP="'sha256-${HASH}'"
else
    HASH_CSP=""
fi

if [ -f /etc/nginx/conf.d/seguridad.conf ]; then
    sed -i "s|HASH_IMPORTMAP|${HASH_CSP}|g" /etc/nginx/conf.d/seguridad.conf
    sed -i "s|URL_BASE_API_MARCADOR|${URL_BASE_API}|g" /etc/nginx/conf.d/seguridad.conf
fi
