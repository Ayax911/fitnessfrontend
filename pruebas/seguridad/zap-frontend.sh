#!/usr/bin/env bash
# OWASP ZAP baseline (pasivo + spider) contra el frontend servido por la imagen nginx del repo (local).
# Requiere levantar-frontend.sh. Informes en $SALIDA (por defecto pruebas/resultados).
set -uo pipefail
AQUI="$(cd "$(dirname "$0")" && pwd)"; SALIDA="${SALIDA:-$AQUI/../resultados}"; mkdir -p "$SALIDA/zap"; chmod a+rwX "$SALIDA/zap"
docker run --rm --network qa25010_default -v "$SALIDA/zap:/zap/wrk:rw" ghcr.io/zaproxy/zaproxy:stable zap-baseline.py \
  -t http://web:8080/ -r zap-frontend.html -w zap-frontend.md -J zap-frontend.json -I > "$SALIDA/zap-frontend.log" 2>&1
grep -E '^(WARN|FAIL)' "$SALIDA/zap-frontend.log"; tail -1 "$SALIDA/zap-frontend.log"
H=$(curl -s -D - -o /dev/null http://127.0.0.1:5081/)
for h in Content-Security-Policy X-Frame-Options X-Content-Type-Options Referrer-Policy Permissions-Policy Strict-Transport-Security; do
  printf '%-26s %s\n' "$h" "$(grep -qi "^$h:" <<< "$H" && echo presente || echo AUSENTE)"
done
