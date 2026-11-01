#!/usr/bin/env bash
# Eficiencia de desempeño (cliente) · utilización de recursos: bytes que un navegador descarga en la PRIMERA visita
# (sin caché) a la app servida por la imagen nginx del repo, con Accept-Encoding: br, gzip.
# Estima el tiempo de descarga en perfiles de red móvil (estimación lineal, sin latencia ni paralelismo).
set -euo pipefail
WEB="${WEB:-http://127.0.0.1:5081}"
tot=0; sin=0; n=0; tmp=$(mktemp)
archivos=$(docker exec qa25010-web sh -c 'cd /usr/share/nginx/html && find _framework css js img -type f ! -name "*.br" ! -name "*.gz" 2>/dev/null; ls index.html manifest*.json 2>/dev/null; true' | sort -u)
for f in $archivos; do
  read -r c s <<<"$(curl -s -o /dev/null -H 'Accept-Encoding: br, gzip' -w '%{size_download} %{http_code}' "$WEB/$f")"
  u=$(curl -s -o /dev/null -w '%{size_download}' "$WEB/$f")
  echo "$c $u $f" >> "$tmp"; tot=$((tot + c)); sin=$((sin + u)); n=$((n + 1))
done
enc=$(curl -s -D - -o /dev/null -H 'Accept-Encoding: br, gzip' "$WEB/_framework/$(docker exec qa25010-web sh -c 'ls /usr/share/nginx/html/_framework | grep -m1 "^dotnet.native.*wasm$"')" | grep -i '^content-encoding' | tr -d '\r' || echo "content-encoding: (ninguna)")
echo "archivos=$n transferidos=$((tot/1024)) KiB sin_compresion=$((sin/1024)) KiB  [$enc en el .wasm principal]"
awk -v t=$tot 'BEGIN{ printf "estimación 3G (1,6 Mbps)=%.1f s · 4G lento (5 Mbps)=%.1f s · 4G (20 Mbps)=%.1f s\n", t*8/1.6e6, t*8/5e6, t*8/20e6 }'
echo "10 archivos más pesados (transferido / sin comprimir, bytes):"; sort -rn "$tmp" | head -10
rm -f "$tmp"
