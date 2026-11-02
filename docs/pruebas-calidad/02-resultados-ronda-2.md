# Resultados de las pruebas de calidad ISO/IEC 25010 · frontend · Ronda 2 · 2026-11-02

> **Plan:** [00-plan.md](00-plan.md) (sigue vigente). **Ronda 1:** [01-resultados.md](01-resultados.md). **Evidencia:** [`evidencia/ronda-2/`](evidencia/ronda-2/).
> **Commit:** frontend `0d22e37`, contra el backend `08f7e02`, ambos en el entorno local `qa25010`.
> **Hallazgos:** la numeración H-xx es la del informe conjunto `fitnessbackend/docs/pruebas-calidad/02-resultados-ronda-2.md`, que también da el veredicto global y los hallazgos del backend.
> **Código de la aplicación modificado:** ninguno. Solo `pruebas/` y `docs/pruebas-calidad/`.

## Veredicto del frontend

**Sigue sin estar listo para un piloto con usuarios reales.** El flujo del estudiante funciona y mejoró: la lista de actividades ya es reservable y ligera. Pero **las pantallas de administración y de entrenador de las fases F2–F7 siguen mostrando datos inventados** aunque el backend ya tiene todos sus endpoints (B2–B10 verificados: 46/46).

- **H-01 (crítica) persiste:** 7 servicios en memoria. El backend dejó de ser el bloqueo: el cliente Kiota (`ApiCliente/Api`) **no incluye** Reportes, Auditoría, Retroalimentación, calendario de bloqueos, sesiones (cancelar/reprogramar), anuncios/notas ni asistencia manual. Hay que regenerarlo y conectar los servicios.
- **H-02 (alta) persiste:** la pantalla del punto de acceso genera el QR en el navegador. El backend ya emite y valida el token dinámico; el QR del cliente **no sirve para ingresar**.
- **Mejoró:** H-07 en el cliente (24 tarjetas y 6.896 px en vez de 2.115 y 615.627 px) y H-08 (la primera tarjeta "Reservar cupo" se confirma).

| Característica | Resultado |
|---|---|
| Adecuación funcional | Pruebas bUnit **144/144**. Flujo del estudiante: CU-01, Mis reservas, Mi historial y RN-020 cumplen. **Completitud: no cumple** (H-01, H-02): 9 de 18 comprobaciones del recorrido cumplen. |
| Eficiencia de desempeño | **No cumple** en 4G lento: 8,84 s hasta el login y 6.748 KiB en la primera visita (H-14). En red local cumple (0,9 s). La lista pinta 24 tarjetas. |
| Seguridad | Sin paquetes vulnerables. ZAP: 0 FAIL y 7 advertencias. **Faltan CSP, `X-Frame-Options`, `Permissions-Policy` y `Referrer-Policy`**; `Server: nginx/1.31.7` (H-13). |
| Fiabilidad | 0 errores de consola de la aplicación en todo el recorrido. |

## 1. Adecuación funcional

### 1.1 Recorrido real en navegador

`pruebas/funcional/recorrido-navegador.js` (Chromium headless, k6 browser, celular de 390×844 y escritorio de 1280×900), ampliado en esta ronda con las pantallas F2–F7 ([recorrido.log](evidencia/ronda-2/recorrido.log), capturas en [ui/](evidencia/ronda-2/ui/)).

| Comprobación | Ronda 1 | Ronda 2 |
|---|---|---|
| CU-01 · inicio de sesión | Cumple | **Cumple** |
| RF-001 · la lista muestra sesiones reservables | Cumple | **Cumple** (24 tarjetas) |
| RF-001/RN-009 · el primer "Reservar cupo" es reservable | **No cumple** (sesión pasada, H-08) | **Cumple** — "¡Reserva confirmada exitosamente!" |
| CU-01 · reservar una sesión de mañana | Cumple | **Cumple** (el primer clic ya la había reservado; el aviso es "Ya tienes una reserva") |
| RF-020 · Mis reservas lista la reserva | Cumple | **Cumple** |
| RN-020 · sin priorización en Mis reservas | Cumple | **Cumple** |
| RF-021 · Mi historial | Cumple | **Cumple** |
| RF-023 · gestión de usuarios con datos reales | Cumple | **Cumple** |
| RF-033 · Reportes con datos reales | **No cumple** | **No cumple** (H-01) |
| RF-032 · Auditoría con acciones reales | **No cumple** | **No cumple** (H-01) |
| **Nuevas:** Reportes sin actividades ficticias | — | **No cumple**: "Spinning Matutino", "Crossfit Intenso", "Natación Libre" |
| **Nuevas:** Auditoría sin actores ficticios | — | **No cumple**: "Admin Sistema", "pedro.entrenador@…" |
| **Nuevas:** Calendario muestra los bloqueos reales | — | **No cumple**: "Bloqueos programados (0)" con bloqueos creados en el backend |
| **Nuevas:** Bandeja de retroalimentación muestra los reportes reales | — | **No cumple** |
| **Nuevas:** Notas de sesión muestran las notas reales | — | **No cumple** |
| **Nuevas:** Asistencia manual lista a los inscritos reales | — | **No cumple** |
| **Nuevas:** Administrar sesión carga la sesión real | — | Cumple (sin error; no valida contenido) |
| **Nuevas:** Pantalla del punto muestra el punto real ("QA Torniquete") | — | **No cumple**: nombre fijo "Punto de Acceso Principal" (H-02) |

**Resultado: 9/18 comprobaciones cumplen.** En una primera corrida el check del calendario dio un falso positivo (la pantalla contiene la palabra "Mantenimiento" en su propio formulario); se endureció para exigir los textos de bloqueos reales y la evidencia es la del recorrido final.

**Servicios en memoria** ([servicios-en-memoria.txt](evidencia/ronda-2/servicios-en-memoria.txt)): `NotasYAnunciosClienteService`, `AdministracionSesionesClienteService`, `RetroalimentacionClienteService`, `BloqueosClienteService`, `ReportesClienteService`, `AsistenciaManualClienteService`, `AuditoriaClienteService`. **Ninguno usa `FitnessApiClient`.**

**QR dinámico (RF-039, H-02).** `PuntosAccesoClienteService.ObtenerQrActualAsync` (`Servicios/Asistencia/PuntosAccesoClienteService.cs:65-76`) sigue armando `dyn_{guid}_{periodo}` con **periodo de 15 s** y devolviendo el nombre "Punto de Acceso Principal". El backend emite el token con `GET /api/asistencia/puntos-acceso/{id}/qr-actual` (**30 s**, `{ token, validoHastaUtc, periodoSegundos }`) y el ingreso lo valida y lo vence (probado en el backend, S74b y S74c).

### 1.2 Pruebas bUnit

**144/144** ([evidencia](evidencia/ronda-2/dotnet-test-frontend.txt)). Las pruebas de los servicios en memoria pasan porque prueban la simulación, no el contrato (la causa de que H-01 no se vea en CI).

## 2. Eficiencia de desempeño (cliente)

| Medición | Ronda 1 | Ronda 2 |
|---|---|---|
| Arranque en frío, red local | 0,9 s | **0,94 s** |
| Arranque a login, 4G lento (150 ms, 5 Mbps) | 8,85 s | **8,84 s** (No cumple, H-14) |
| Lista de eventos en red local | — | 0,69 s |
| DOM de `/eventos` en un celular | 2.115 tarjetas, 33.912 nodos, 615.627 px | **24 tarjetas, 453 nodos, 6.896 px** (corregido) |
| Primera visita, transferido | 6,7 MB | **6.748 KiB** de 12.562 KiB sin comprimir (83 archivos) |
| Estimación 3G 1,6 Mbps / 4G lento / 4G | — | 34,6 s / 11,1 s / 2,8 s |

**Causas de la descarga (H-14), sin cambios** ([tamano-descarga.txt](evidencia/ronda-2/tamano-descarga.txt)):
- nginx comprime con **gzip** (`Content-Encoding: gzip` en el `.wasm` principal), no sirve los `.br` precomprimidos.
- Los `icudt_no_CJK`, `icudt_CJK` y `icudt_EFIGS` (2,6 MB) **se transfieren sin comprimir** (transferido = original).
- Los diez archivos más pesados suman ≈ 5,6 MB.

## 3. Seguridad

- **Paquetes vulnerables:** ninguno (`dotnet list package --vulnerable`, ver el informe del backend).
- **ZAP baseline** ([zap/zap-frontend.md](evidencia/ronda-2/zap/zap-frontend.md)): **0 FAIL, 7 WARN, 61 PASS**:
  - Falta anti-clickjacking (`X-Frame-Options` / `frame-ancestors`).
  - `Server: nginx/1.31.7` revela la versión.
  - **Falta CSP** (agrava que los tokens estén en `localStorage`).
  - Falta `Permissions-Policy`.
  - Falta `Cross-Origin-Embedder-Policy`.
  - Contenido almacenable y sin integridad de subrecursos.
- Cabecera presente: `X-Content-Type-Options: nosniff`.

## 4. Fiabilidad

0 errores de consola de la aplicación en todo el recorrido (`ERRORES_CONSOLA=0`).

## 5. Hallazgos del frontend

| ID | Severidad | Estado ronda 2 | Resumen | Verificación de cierre |
|---|---|---|---|---|
| H-01 | **Crítica** | **Persiste** | 7 servicios en memoria con datos ficticios. | `recorrido-navegador.js`: las 8 comprobaciones "H-01" pasan, y `trazabilidad.sh` ya no marca `MEMORIA` en ningún RF. Depende de regenerar Kiota (`kiota update`) con el backend en Development. |
| H-02 | **Alta** | **Persiste** | El QR dinámico se genera en el cliente (15 s, token `dyn_`). | `recorrido-navegador.js`: "La pantalla del punto muestra el punto real". Prueba bUnit nueva que verifique que el servicio llama a `qr-actual`. Escaneo real con un teléfono contra `ingreso/{token}` → 200. |
| H-07 | Media | **Corregido en el cliente** | 24 tarjetas por defecto. | `EVENTOS_DOM tarjetas ≤ 100`. Falta la compresión en la API (backend). |
| H-08 | Media | **Corregido** | El primer "Reservar cupo" se confirma. | Check "RF-001/RN-009" del recorrido. |
| H-13 | Media | **Persiste** | Sin CSP, `X-Frame-Options`, `Permissions-Policy`, `Referrer-Policy`; `Server: nginx/1.31.7`. | `zap-frontend.sh`: 0 WARN de cabeceras; `curl -I` sin `Server` con versión. |
| H-14 | Media | **Persiste** | 6.748 KiB; 8,84 s en 4G lento; `icudt_*.dat` sin comprimir. | `tamano-descarga.sh` ≤ 3,5 MB y el check de 4G lento ≤ 6 s (`PENDIENTE` hasta que haya objetivo de negocio). |
| H-19 | Baja | **Persiste** | Botones de paginación de Auditoría sin icono. | `PAGINACION_AUDITORIA=icono,icono` en el recorrido. |

**No hay hallazgos nuevos propios del frontend en esta ronda.** Los hallazgos nuevos H-20…H-24 son del backend.

## 6. Limitaciones

- **Sin pruebas en teléfonos reales ni accesibilidad manual.** Siguen a cargo del usuario (`docs/plan-despliegue/02-pruebas-en-dispositivos-y-accesibilidad.md`).
- **El recorrido no valida el contenido** de "Administrar sesión" ni de la asistencia manual de un entrenador con sesión viva más allá de que listen datos reales; se corregirá con la conexión a la API.
- **Las capturas** se guardan solo con la ventana visible.
