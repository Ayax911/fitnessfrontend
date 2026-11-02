# Prompt de corrección — pruebas de calidad ISO/IEC 25010, ronda 2 (fitnessfrontend)

> Pegar en Antigravity con el workspace en `fitnessfrontend/`, agente `constructor-fitnessfrontend` seleccionado con `/agents`.
> Fuente: [`docs/pruebas-calidad/02-resultados-ronda-2.md`](../docs/pruebas-calidad/02-resultados-ronda-2.md) y el informe conjunto `fitnessbackend/docs/pruebas-calidad/02-resultados-ronda-2.md` (frontend `0d22e37` contra backend `08f7e02`, medido el 2026-11-02 en el entorno local `qa25010`). Los hallazgos tienen evidencia y un caso exacto que debe pasar a CUMPLE: **no re-cuestiones su existencia**. Si al implementar una descripción no coincide con el código, detente y repórtalo.

---

Vas a corregir los hallazgos de las pruebas de calidad que aplican a este repositorio (cliente). Es **mantenimiento sobre código que ya funciona**: no reescribas lo que el hallazgo no señala; `ApiCliente/` es código generado y **no se edita a mano**; los estilos usan solo los tokens de `docs/arquitectura/07-identidad-visual-y-sistema-de-diseno.md`; las reglas de negocio viven en el backend (no las repliques); RN-020 es bloqueante (nada de priorización, confiabilidad ni posición en la lista de espera hacia Usuario o Entrenador); el backend es UTC y la única conversión a `America/Bogota` es `IZonaHorariaService`.

## Prerrequisito (orden entre repos)

Los hallazgos H-01 y H-02 **dependen del backend en Development** para regenerar el cliente. Antes de la Ronda 1:

1. `cd ../fitnessbackend && dotnet run --project src/FitnessBackend --launch-profile http` (http://localhost:5224, requiere PostgreSQL: `docker compose up -d db`).
2. `cd src/FitnessFrontend && kiota update -o ./ApiCliente`.
3. Comprueba que `ApiCliente/Api/` ahora trae: `Reportes`, `Auditoria`, `Retroalimentacion`, `Calendario` (bloqueos), las acciones de `Eventos/sesiones/{id}` (`cancelar`, `reprogramar`, `anuncios`, `notas`), `Asistencia/sesiones/{id}/manual`, `Asistencia/puntos-acceso/{id}/qr-actual` y `Reservas/sesiones/{id}/inscribir` y `{id}/administrativa`. Hoy solo existen `Asistencia`, `Auth`, `Eventos`, `Priorizacion`, `Reservas` y `Usuarios`.
4. Si el backend aún no incluye los cambios de contrato de la ronda 2 de correcciones del backend (429 en login, 400 en rangos de reportes, `fechaInicioRecurrencia`), **regenera de nuevo cuando los publique** y reporta qué cambió.

## Reglas de trabajo

- Orden exacto de las rondas. Al cerrar cada ronda: `dotnet build`, `dotnet test`, la prueba obligatoria nueva (skill `pruebas-obligatorias-fitnessfrontend`) y luego invoca a `revisor-fitnessfrontend` con la lista de hallazgos de esa ronda. **No avances de ronda con bloqueantes del revisor sobre tu propia corrección.**
- Cada pantalla lleva `@* RF-0xx: ... *@` al inicio; ambigüedades abiertas `@* PENDIENTE (AMB-xxx) *@`. No inventes valores.
- Pruebas actuales: 144. Ninguna debe romperse. **Las pruebas de los servicios en memoria prueban la simulación, no el contrato**: al conectar cada servicio, reemplázalas por pruebas contra el cliente (con `HttpMessageHandler` simulado que devuelva el JSON real del contrato, `fitnessbackend/docs/arquitectura/07-contrato-api.md`).
- Si hay `dotnet run` activo y haces `dotnet build/test`, reinícialo.
- Verificación de cierre: el recorrido en navegador (`pruebas/funcional/recorrido-navegador.js`) y `fitnessbackend/pruebas/funcional/trazabilidad.sh`; el usuario los vuelve a ejecutar al final (entorno local `qa25010`).
- No hagas commits; yo los reviso por ronda.

## Ronda 1 — Conectar los siete servicios simulados (crítica)

**Hallazgo: H-01.** Ningún servicio de la lista usa `FitnessApiClient` (evidencia: [`evidencia/ronda-2/servicios-en-memoria.txt`](../docs/pruebas-calidad/evidencia/ronda-2/servicios-en-memoria.txt)). Cada uno debe llamar al backend, **sin datos sembrados ni valores por defecto inventados**, y mostrar `EstadoDeCarga` real (cargando, error, vacío).

| Servicio (`src/FitnessFrontend/Servicios/…`) | Endpoints del backend | RF | Pantalla |
|---|---|---|---|
| `Reportes/ReportesClienteService.cs` | `GET /api/reportes/{asistencia-por-sesion,ocupacion,inasistencias-por-evento,horas-pico,uso-plataforma}?desde&hasta&formato=csv` (rango ≤ 90 días; 400 `RangoInvalido`) | RF-033 | `/admin/reportes` |
| `Auditoria/AuditoriaClienteService.cs` | `GET /api/auditoria?desde&hasta&accion&actorId&entidadTipo&pagina&tamano` (**máx. 50 por página**) | RF-032 | `/admin/auditoria` |
| `Bloqueos/BloqueosClienteService.cs` | `GET/POST /api/calendario/bloqueos`, `DELETE /api/calendario/bloqueos/{id}` (el POST devuelve `{ bloqueo, sesionesAfectadas }`) | RF-031 | `/admin/calendario` |
| `Sesiones/AdministracionSesionesClienteService.cs` | `POST /api/eventos/sesiones/{id}/cancelar` (motivo obligatorio), `PUT …/reprogramar`, `POST /api/reservas/sesiones/{id}/inscribir`, `DELETE /api/reservas/{id}/administrativa` | RF-028, RF-030 | `/admin/sesiones/{id}` |
| `Eventos/NotasYAnunciosClienteService.cs` | `GET/POST /api/eventos/sesiones/{id}/anuncios`, `GET/POST …/notas` (notas solo Entrenador/Administrador) | RF-029, RF-034 | `/entrenador/sesiones/{id}/notas` |
| `Asistencia/AsistenciaManualClienteService.cs` | `PUT /api/asistencia/sesiones/{id}/manual` `{ usuarioId, presente, motivo? }` | RF-027 | `/entrenador/sesiones/{id}/asistencia` |
| `Retroalimentacion/RetroalimentacionClienteService.cs` | `POST /api/retroalimentacion`, `GET …/mios`, `GET …?estado&pagina&tamano`, `PUT …/{id}/estado` (429 al 6.º envío por hora) | RF-038 | `/ayuda/reportar`, `/mis-reportes`, `/admin/retroalimentacion` |

Reglas específicas:
- **Mapea los errores del contrato** a mensajes en español: 400 (validación), 403, 404, 409 (`VentanaNoAbierta`, `SinCupoDisponible`, conflicto), 429 (`LimiteExcedido`: mensaje "Has alcanzado el límite de envíos por hora"). Usa `ErrorDetalleDto`, no excepciones sueltas.
- **Reportes:** los enteros del DTO vienen **agregados**; no calcules porcentajes ni totales en el cliente (regla de negocio solo en el backend). La exportación CSV usa `?formato=csv` y descarga el archivo tal cual (BOM y `;` ya vienen del backend).
- **Auditoría:** la paginación es de 50 como máximo; no pidas `tamano=200`.
- **Fechas:** todo llega en UTC → conviértelo solo con `IZonaHorariaService`.
- **RN-020:** ninguna pantalla de Usuario o Entrenador muestra priorización; las de administración sí pueden.
- **Pantallas de Entrenador:** respeta RN-007 (solo su sesión): si el backend responde 403/404, muestra el mensaje, no un error inesperado.

**Prueba obligatoria nueva (por servicio):** con un `HttpMessageHandler` simulado, el servicio construye la petición correcta (método, ruta, query y cuerpo) y deserializa la respuesta real del contrato; 4xx → `EstadoDeCarga` de error con el mensaje del contrato.

**Cierre:**
- `pruebas/funcional/recorrido-navegador.js`: pasan las comprobaciones "H-01 · …" (Reportes y Auditoría sin datos ficticios, Calendario con los bloqueos reales, Bandeja de retroalimentación, Notas de sesión, Asistencia manual).
- `fitnessbackend/pruebas/funcional/trazabilidad.sh . ../fitnessfrontend` → ningún RF con servicio `MEMORIA` ("Backend listo, UI no conectada" desaparece para RF-027…RF-034).
- Elimina los datos de demostración fabricados de esos servicios. **No elimines** nada de `seed_test_data.sql` ni de documentación (no se ejecuta en producción).

## Ronda 2 — QR dinámico real (alta)

**Hallazgo: H-02.** `Servicios/Asistencia/PuntosAccesoClienteService.cs:65-76` arma `dyn_{guid}_{periodo}` en el cliente, con 15 s y el nombre "Punto de Acceso Principal".

- Reemplázalo por `GET /api/asistencia/puntos-acceso/{id}/qr-actual` (rol Administrador): `{ token, validoHastaUtc, periodoSegundos }`. **Quita el generador del cliente.** El nombre del punto sale de `GET /api/asistencia/puntos-acceso`.
- La pantalla (`PantallaPuntoAcceso.razor`) renueva el QR por `validoHastaUtc` (con un margen), no por un periodo fijo en el cliente; el contador usa `periodoSegundos` del servidor. Si el servidor responde error, muestra el aviso y reintenta con espera; nunca dibujes un QR de un token vencido.
- No expongas ni registres el token fuera del QR.
- **Prueba obligatoria nueva:** el servicio llama a `qr-actual`; ningún token de la pantalla empieza por `dyn_`; el QR cambia cuando cambia `validoHastaUtc`.
- **Cierre:** `recorrido-navegador.js` → "La pantalla del punto muestra el punto real ("QA Torniquete")". Prueba manual con teléfono real: el QR escaneado → `POST /api/asistencia/ingreso/{token}` → 200; el mismo QR 40 s después → `QrVencido` (400).

## Ronda 3 — Cabeceras, descarga y detalles visuales

**Hallazgos: H-13, H-14, H-19**

1. **H-13.** En `nginx.conf` añade `Content-Security-Policy` (la mínima que permita Blazor WASM: `script-src 'self' 'wasm-unsafe-eval'`; ajusta `connect-src` a `URL_BASE_API` en `docker/40-url-api.sh`, **no la dejes abierta a `*`**), `X-Frame-Options: DENY` (o `frame-ancestors 'none'`), `Referrer-Policy: no-referrer`, `Permissions-Policy` restrictiva (cámara solo si el escaneo del QR la necesita: revisa `Ingreso.razor`/`CheckIn.razor`), y `server_tokens off;`. **Cierre:** `pruebas/seguridad/zap-frontend.sh` sin alertas de cabeceras; `curl -I` sin `Server: nginx/<versión>`.
2. **H-14.** Sirve los `.br`/`.gz` precomprimidos (`gzip_static on;` o el módulo brotli) y añade `application/octet-stream` a `gzip_types` para que los `icudt_*.dat` (2,6 MB) viajen comprimidos. Caché larga (`immutable`) solo para `_framework` con huella; `no-store` se mantiene para `appsettings.json`. Evalúa la globalización invariante o solo `es-CO` (`InvariantGlobalization`/`BlazorWebAssemblyLoadAllGlobalizationData=false`) **solo si no rompe `IZonaHorariaService`** (verifica `America/Bogota`). **Cierre:** `pruebas/desempeno/tamano-descarga.sh` ≤ 3,5 MiB transferidos (hoy 6.748 KiB) y arranque a login en 4G lento ≤ 6 s (hoy 8,84 s; el objetivo es provisional, `PENDIENTE`).
3. **H-19.** Los botones de paginación de `Auditoria/RegistroAuditoria.razor` ("□ Anterior / Siguiente □") usan una fuente de iconos que no carga: reemplázala por SVG en línea (con `aria-hidden="true"` y texto accesible). **Cierre:** `PAGINACION_AUDITORIA=icono,icono` en el recorrido.

## Qué dejarme al terminar

- Resumen por ronda: hallazgos corregidos, archivos tocados, pruebas nuevas y resultado del revisor.
- Lista de lo que **no** se corrigió y por qué.
- Cualquier **cambio de contrato** que detectaste al regenerar Kiota y que el backend aún debe corregir.
- Servicios que sigan en memoria (debe ser ninguno) y pantallas que dependan de un endpoint que el backend aún no tenga.
