#!/usr/bin/env bash
# Construye la imagen del frontend (Dockerfile + nginx.conf del repo) y la sirve en http://localhost:5081,
# conectada a la API del entorno qa25010 del backend (pruebas/entorno/levantar.sh del repo fitnessbackend).
# RAIZ permite probar una copia exacta de un commit. Solo local (127.0.0.1).
set -euo pipefail
AQUI="$(cd "$(dirname "$0")" && pwd)"; RAIZ="${RAIZ:-$(cd "$AQUI/../.." && pwd)}"
URL_API="${URL_API:-http://127.0.0.1:18080}"
docker build -q -t qa25010-web "$RAIZ" >/dev/null
docker rm -f qa25010-web >/dev/null 2>&1 || true
docker run -d --name qa25010-web --network qa25010_default --network-alias web -p 127.0.0.1:5081:8080 \
  -e URL_BASE_API="$URL_API" qa25010-web >/dev/null
for i in $(seq 1 30); do curl -sf -o /dev/null http://127.0.0.1:5081/ && { echo "Frontend en http://localhost:5081 → API $URL_API"; exit 0; }; sleep 1; done
echo "el frontend no respondió" >&2; exit 1
