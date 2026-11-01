#!/bin/sh
# Genera appsettings.json a partir de la plantilla con la URL pública de la API (obligatoria).
set -e
: "${URL_BASE_API:?Defina URL_BASE_API con la URL pública de la API, sin barra final}"
RAIZ=/usr/share/nginx/html
sed "s|REEMPLAZAR_EN_DESPLIEGUE|${URL_BASE_API}|g" "$RAIZ/appsettings.template.json" > "$RAIZ/appsettings.json"
