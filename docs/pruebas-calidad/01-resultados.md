# Resultados de las pruebas de calidad ISO/IEC 25010 · frontend · 2026-11-01

> **Plan:** [00-plan.md](00-plan.md). **Evidencia:** [`evidencia/`](evidencia/).
> **Commit:** frontend `46e0dee`, contra el backend `b16bbdb`, ambos en el entorno local `qa25010`.
> **Hallazgos:** la numeración H-xx es la del informe conjunto `fitnessbackend/docs/pruebas-calidad/01-resultados.md`, que también da el veredicto global.

## Veredicto del frontend

**No está listo para un piloto con usuarios reales.** El flujo del estudiante funciona contra el backend real: iniciar sesión, reservar, Mis reservas y Mi historial, con RN-020 respetada. Los bloqueos:

- **H-01 (crítica):** ocho servicios de las fases F2–F7 son simulaciones en memoria. Muestran a un administrador **datos inventados como si fueran reales**.
- **H-02 (alta):** el QR dinámico (RF-039) se genera en el cliente y **no sirve para ingresar**.
- **Desempeño en celular:** la primera carga y la lista de actividades pesan demasiado para un teléfono (H-07, H-14).

| Característica | Resultado |
|---|---|
| Adecuación funcional | Corrección: 144/144 pruebas bUnit y el flujo del estudiante cumple. **Completitud: no cumple** (H-01, H-02). Pertinencia: ofrece reservar sesiones pasadas (H-08). |
| Eficiencia de desempeño | **No cumple** en 4G lento: 8,85 s hasta el login y 6,7 MB en la primera visita. En red local cumple (0,9 s). La lista de actividades pinta 2.115 tarjetas. |
| Seguridad | Sin paquetes vulnerables. ZAP: 0 FAIL y 7 advertencias. **Faltan CSP, `X-Frame-Options`, `Referrer-Policy` y `Permissions-Policy`.** |
| Fiabilidad | 0 errores de consola de la aplicación en el recorrido. La sesión sobrevive al reinicio de la API (prueba del backend). |

## 1. Adecuación funcional

### 1.1 Corrección

- **Pruebas bUnit:** `dotnet test` → **144/144** ([evidencia](evidencia/dotnet-test-frontend.txt)).
- **Recorrido real** con Chromium headless (`pruebas/funcional/recorrido-navegador.js`, celular de 390×844 y escritorio de 1280×900): **7/10 comprobaciones cumplen** ([evidencia/recorrido-navegador.txt](evidencia/recorrido-navegador.txt), [capturas](evidencia/capturas/)).

| Comprobación | Resultado |
|---|---|
| CU-01: el inicio de sesión lleva fuera de `/login` | Cumple |
| RF-001: la lista muestra sesiones reservables | Cumple (2.115 tarjetas) |
| RF-001/RN-009: el primer "Reservar cupo" es reservable | **No cumple**: es una sesión del 30/10 ya pasada. Responde *"No se pueden realizar reservas para sesiones que ya han comenzado…"* (H-08) |
| CU-01: reservar una sesión de mañana (chip "Mañana") | Cumple: "¡Reserva confirmada exitosamente!" |
| RF-020: Mis reservas lista la reserva | Cumple |
| RN-020: sin nivel ni indicador en Mis reservas | Cumple |
| RF-021: Mi historial carga | Cumple |
| RF-023: Gestión de usuarios muestra las cuentas reales | Cumple |
| RF-033: Reportes reflejan datos reales | **No cumple**: muestra "Spinning Matutino", "Crossfit Intenso" y "Natación Libre", que no existen (captura `ui-08`) |
| RF-032: Auditoría refleja acciones reales | **No cumple**: muestra 4 acciones ficticias ("Admin Sistema", "pedro.entrenador@itm.edu.co"). El backend tiene 35 registros reales (captura `ui-09`) |

### 1.2 Completitud: servicios conectados vs. simulados

[evidencia/servicios-api-vs-memoria.txt](evidencia/servicios-api-vs-memoria.txt):

| Llaman a la API real | Simulados en memoria (H-01) |
|---|---|
| Asistencia (RF-005/006/012/021), Puntos de acceso (RF-024), Eventos (RF-001/007–010), Autenticación (RF-013/025), Priorización (RF-016/018), Reservas (RF-002–004/011/020), Usuarios (RF-023) | Asistencia manual (RF-027), Administración de sesiones (RF-028/030), Notas y anuncios (RF-029/034), Bloqueos (RF-031), Reportes (RF-033), Retroalimentación (RF-038), **Auditoría (RF-032, cuyo backend ya existe)** |

**QR dinámico (RF-039, H-02).** `PuntosAccesoClienteService.ObtenerQrActualAsync` arma en el navegador el token `dyn_{guid}_{periodo}`:

- El periodo es de 15 s, cuando el plan provisional dice 30 s (AMB-D06).
- El backend no lo valida, así que el QR de la pantalla del punto de acceso no registra ingresos.
- Expone el GUID del punto, que hoy basta para ingresar (H-05).

## 2. Eficiencia de desempeño (cliente)

| Medición | Resultado | Criterio provisional |
|---|---|---|
| Descarga de la primera visita (`pruebas/desempeno/tamano-descarga.sh`) | **6.748 KiB** transferidos (12.562 KiB sin comprimir), 83 archivos. Estimación: 3G 34,6 s; 4G lento 11,1 s; 4G 2,8 s | ≤ 3 MB → **no cumple** |
| Arranque en frío hasta el formulario de login, red local | 0,89–0,92 s (4 corridas) | Cumple |
| Arranque en frío, 4G lento simulado (150 ms, 5 MB/s ≈ 625 KB/s) | **8,83–8,86 s** | ≤ 5 s → **no cumple** |
| Lista de actividades (`/eventos`) hasta el primer botón | 2,14–2,22 s en red local | Observación |
| DOM de `/eventos` | **2.115 tarjetas, 33.912 nodos, 615.627 px de alto** en un celular | **No cumple** (H-07) |
| Web Vitals (red local, todas las páginas del recorrido) | FCP p95 0,49–2,3 s; LCP mediana 0,38–0,6 s, máximo 8,8 s (la página con red lenta); CLS ≤ 0,002 | Cumple, salvo la página con red lenta |
| Interacción al filtrar por día | En 1 de 4 corridas: INP 568 ms y CLS 0,59 al pulsar el chip de día con 2.115 tarjetas | Observación (consecuencia de H-07) |

**Causas de la descarga** (H-14):
- `nginx.conf` comprime al vuelo con gzip y **no sirve los `.br` precomprimidos** que genera `dotnet publish`.
- Los ICU `.dat` (≈1–1,1 MB cada uno) van **sin comprimir**, porque `application/octet-stream` no está en `gzip_types`.
- Los 10 archivos más pesados están en [evidencia/tamano-descarga.txt](evidencia/tamano-descarga.txt).

## 3. Seguridad

- **Paquetes:** `dotnet list package --vulnerable --include-transitive` → **sin vulnerabilidades** ([evidencia](evidencia/vuln-frontend.txt)).
- **ZAP baseline** contra el sitio servido por nginx: **0 FAIL, 7 WARN, 61 PASS** ([evidencia/zap/](evidencia/zap/)). Las advertencias:
  - Falta la cabecera anti-clickjacking.
  - `Server` revela la versión de nginx.
  - Falta CSP.
  - Contenido almacenable en caché.
  - Falta `Permissions-Policy`.
  - Falta Subresource Integrity.
  - Falta COEP.
- **Cabeceras del sitio:** `X-Content-Type-Options` está presente. **Faltan** `Content-Security-Policy`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy` y `Strict-Transport-Security`. HSTS corresponde al proxy con HTTPS.
- **Tokens en `localStorage` sin CSP:** cualquier XSS podría leer el token de acceso y el refresh token, que además se puede reutilizar (H-09). Recomendación (H-13): añadir CSP en `nginx.conf` con `script-src 'self' 'wasm-unsafe-eval'` (Blazor WebAssembly la necesita), `frame-ancestors 'none'` y `server_tokens off`.
- **RN-020 en pantalla:** cumple en Mis reservas. Las pruebas bUnit `AusenciaPriorizacionRN020Tests` cubren el resto.

## 4. Fiabilidad

- **Errores de consola:** 0 de la aplicación en el recorrido. Hay 409 de red esperados, por la sesión pasada de H-08.
- **Sesión tras reiniciar la API:** el token emitido antes del reinicio sigue siendo válido (`fitnessbackend` `fiabilidad/02`), así que el usuario no pierde la sesión por un reinicio del servidor.
- **Pendiente: pruebas con la API caída desde el navegador.** Hay que comprobar que `EstadoDeCarga` muestre el reintento y que la renovación de RF-026 funcione con el token vencido. Las pruebas bUnit de F1 lo cubren de forma aislada.

## 5. Hallazgos que corresponden al frontend

| ID | Severidad | Hallazgo | Recomendación |
|---|---|---|---|
| H-01 | **Crítica** | 8 servicios simulados en memoria. Reportes y auditoría muestran datos ficticios. | Ocultarlos tras una bandera o marcarlos como "vista previa" hasta que existan B2–B7. Conectar Auditoría ya. |
| H-02 | **Alta** | El QR dinámico se genera en el cliente y no lo valida el backend. Periodo de 15 s. | Esperar a B5 y consumir el token del servidor (Patrón 9 del frontend con `validoHasta`). |
| H-07 | Media | `/eventos` pinta todas las sesiones (2.115) sin paginar ni virtualizar. | Pedir un rango al backend (RF-035, B2). Mostrar por defecto "Hoy" o los próximos 7 días. Considerar `Virtualize`. |
| H-08 | Media | Muestra sesiones pasadas con "Reservar cupo" activo. | Ocultarlas o desactivar el botón con su motivo. |
| H-13 | Media | Faltan CSP, `X-Frame-Options`, `Referrer-Policy` y `Permissions-Policy`. `Server` muestra la versión. | Cabeceras en `nginx.conf` y `server_tokens off`. |
| H-14 | Media | 6,7 MB en la primera visita; 8,85 s en 4G lento. | `gzip_static` o brotli para los `.br` y `.gz` precomprimidos. Añadir `application/octet-stream` a `gzip_types`. `Cache-Control: immutable` en `_framework`. Revisar la globalización. |
| H-19 | Baja | Los botones de paginación de Auditoría muestran un glifo vacío ("□ Anterior", "Siguiente □"): falta la fuente de iconos. | Usar SVG en línea, como en `InsigniaEstado`. |

## 6. Limitaciones

- **Recorrido automatizado, no manual.** No sustituye las pruebas en teléfonos reales ni la auditoría manual de accesibilidad (`docs/plan-despliegue/02-pruebas-en-dispositivos-y-accesibilidad.md`).
- **Red simulada.** La red móvil se simula con un límite de ancho de banda y latencia, no con un teléfono de gama media: no se mide el costo de CPU de iniciar WebAssembly en un teléfono.
