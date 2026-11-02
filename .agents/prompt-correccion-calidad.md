# Prompt de corrección — pruebas de calidad ISO/IEC 25010, ronda 2 (fitnessfrontend)

> Pegar en Antigravity con el workspace en `fitnessfrontend/` y el agente `constructor-fitnessfrontend` seleccionado con `/agents`.
>
> **Fuentes, en este orden de autoridad:**
> 1. `../fitnessbackend/docs/pruebas-calidad/03-decisiones-correccion.md`: las decisiones del usuario D-A…D-D, ya tomadas.
> 2. [`docs/pruebas-calidad/02-resultados-ronda-2.md`](../docs/pruebas-calidad/02-resultados-ronda-2.md) y el informe conjunto `../fitnessbackend/docs/pruebas-calidad/02-resultados-ronda-2.md`: frontend `0d22e37` contra backend `08f7e02`, medido el 2026-11-02 en el entorno local `qa25010`.
>
> Los hallazgos están verificados y cada uno tiene un caso exacto que debe pasar a CUMPLE: **no re-cuestiones su existencia**. Si una descripción no coincide con el código, detente y repórtalo.
>
> **Objetivo:** dejar el cliente **listo para producción** en lo que depende del repositorio. Al terminar, `verificador-calidad-fitnessbackend` (en el repo del backend) repite el set completo con `../fitnessbackend/.agents/prompt-verificacion-calidad.md`.

---

Vas a corregir **todos** los hallazgos de las pruebas de calidad que aplican al cliente. Es **mantenimiento sobre código que ya funciona**. Reglas fijas:
- No reescribas lo que el hallazgo no señala.
- `ApiCliente/` es código generado: **no se edita a mano**.
- Los estilos solo usan los tokens de `docs/arquitectura/07-identidad-visual-y-sistema-de-diseno.md`.
- Las reglas de negocio viven en el backend: no las repliques.
- **RN-020 es bloqueante:** nada de priorización, confiabilidad ni posición en la lista de espera hacia Usuario o Entrenador.
- El backend trabaja en UTC; la única conversión a `America/Bogota` es `IZonaHorariaService`.

## Antes de empezar (orden entre repositorios)

1. **El backend debe haber cerrado su `prompt-correccion-calidad.md`**, o al menos sus rondas R0–R4. Lee la sección "Qué dejarme al terminar" de su entrega o su `CHANGELOG.md` para conocer los **cambios de contrato**:
   - 429 `LimiteExcedido` en login, registro, check-in e ingreso;
   - 400 en lugar de 409 para `Rango*` en reportes;
   - `fechaInicioRecurrencia` en `PatronRecurrencia`;
   - nuevo formato de `/health*`.
2. `git status` limpio, en la rama que indique el usuario (nunca `main`). Línea base: `dotnet build` y `dotnet test` en verde (**144** pruebas).
3. Regenera el cliente:
   ```bash
   cd ../fitnessbackend && docker compose up -d db && dotnet run --project src/FitnessBackend --launch-profile http   # http://localhost:5224
   cd ../fitnessfrontend/src/FitnessFrontend && kiota update -o ./ApiCliente
   ```
   Comprueba que `ApiCliente/Api/` trae ahora:
   - `Reportes`, `Auditoria`, `Retroalimentacion` y `Calendario` (bloqueos);
   - en `Eventos/sesiones/{id}`: `cancelar`, `reprogramar`, `anuncios` y `notas`;
   - `Asistencia/sesiones/{id}/manual` y `Asistencia/puntos-acceso/{id}/qr-actual`;
   - `Reservas/sesiones/{id}/inscribir` y `Reservas/{id}/administrativa`.

   Hoy solo existen `Asistencia`, `Auth`, `Eventos`, `Priorizacion`, `Reservas` y `Usuarios`. Si falta algo, **detente y repórtalo**: no escribas `HttpClient` a mano.

## Reglas de trabajo

- Sigue las rondas **en orden**.
- **Al cerrar cada ronda:**
  1. `dotnet build` sin advertencias nuevas y `dotnet test` en verde.
  2. Las pruebas obligatorias de la ronda (skill `pruebas-obligatorias-fitnessfrontend`, sección "Correcciones de calidad").
  3. Invoca a `revisor-fitnessfrontend` con el identificador `QA2-F<n>` y la lista de hallazgos.
  4. Corrige los bloqueantes. **No avances con bloqueantes abiertos.**
  5. **Un commit por ronda** cuando el revisor apruebe: `fix(QA2-F<n>): … (H-xx)`. Actualiza `CHANGELOG.md` y `docs/checklist-e2e.md`.
- Cada pantalla lleva `@* RF-0xx: ... *@` al inicio; las ambigüedades abiertas se marcan `@* PENDIENTE (AMB-xxx) *@`. No inventes valores.
- **Las pruebas de los servicios en memoria prueban la simulación, no el contrato.** Al conectar cada servicio, reemplázalas por pruebas contra el cliente Kiota con un `HttpMessageHandler` simulado que devuelva el JSON **real** del contrato (`../fitnessbackend/docs/arquitectura/07-contrato-api.md`).
- Si hay un `dotnet run` activo y haces `dotnet build` o `dotnet test`, reinícialo.
- **Lo que hoy está bien y debe seguir igual:**
  - flujo del estudiante (CU-01, Mis reservas, Mi historial);
  - RN-020 en pantalla;
  - la lista de actividades ligera (24 tarjetas) y reservable;
  - 0 errores de consola;
  - renovación de sesión de un solo vuelo (F1).

## Ronda 1 — Conectar los siete servicios simulados (`QA2-F1`) · H-01 (crítica)

Ningún servicio de la lista usa `FitnessApiClient` (evidencia: [`evidencia/ronda-2/servicios-en-memoria.txt`](../docs/pruebas-calidad/evidencia/ronda-2/servicios-en-memoria.txt)). Cada uno debe llamar al backend **sin datos sembrados ni valores por defecto inventados** y mostrar `EstadoDeCarga` real: cargando, error y vacío.

| Servicio (`src/FitnessFrontend/Servicios/…`) | Endpoints del backend | RF | Pantalla |
|---|---|---|---|
| `Reportes/ReportesClienteService.cs` | `GET /api/reportes/{asistencia-por-sesion,ocupacion,inasistencias-por-evento,horas-pico,uso-plataforma}?desde&hasta&formato=csv` (rango ≤ 90 días, 400) | RF-033 | `/admin/reportes` |
| `Auditoria/AuditoriaClienteService.cs` | `GET /api/auditoria?desde&hasta&accion&actorId&entidadTipo&pagina&tamano` (**máx. 50 por página**) | RF-032 | `/admin/auditoria` |
| `Bloqueos/BloqueosClienteService.cs` | `GET/POST /api/calendario/bloqueos`, `DELETE /api/calendario/bloqueos/{id}` (el POST devuelve `{ bloqueo, sesionesAfectadas }`) | RF-031 | `/admin/calendario` |
| `Sesiones/AdministracionSesionesClienteService.cs` | `POST /api/eventos/sesiones/{id}/cancelar` (motivo obligatorio), `PUT …/reprogramar`, `POST /api/reservas/sesiones/{id}/inscribir`, `DELETE /api/reservas/{id}/administrativa` | RF-028, RF-030 | `/admin/sesiones/{id}` |
| `Eventos/NotasYAnunciosClienteService.cs` | `GET/POST /api/eventos/sesiones/{id}/anuncios`, `GET/POST …/notas` (notas solo para Entrenador y Administrador) | RF-029, RF-034 | `/entrenador/sesiones/{id}/notas` y el detalle de la sesión para el inscrito |
| `Asistencia/AsistenciaManualClienteService.cs` | `PUT /api/asistencia/sesiones/{id}/manual` `{ usuarioId, presente, motivo? }`; inscritos con `GET /api/reservas/sesiones/{id}/inscritos` | RF-027 | `/entrenador/sesiones/{id}/asistencia` |
| `Retroalimentacion/RetroalimentacionClienteService.cs` | `POST /api/retroalimentacion`, `GET …/mios`, `GET …?estado&pagina&tamano`, `PUT …/{id}/estado` (429 al sexto envío por hora) | RF-038 | `/ayuda/reportar`, `/mis-reportes`, `/admin/retroalimentacion` |

Reglas específicas:
- **Errores del contrato con mensajes en español** a partir de `ErrorDetalleDto` (nunca excepciones sueltas): 400 (validación), 403, 404, 409 (`SinCupoDisponible`, `CupoMenorQueOcupados`, conflictos de estado) y 429 (`LimiteExcedido`: "Has alcanzado el límite de envíos por hora. Intenta más tarde").
- **Reportes:** los números vienen agregados. No calcules porcentajes ni totales en el cliente. La descarga CSV usa `?formato=csv` y guarda el archivo tal cual (el BOM y el `;` ya vienen del backend). El gráfico va **siempre** con su tabla equivalente.
- **Auditoría:** paginación de 50 como máximo. Al conectar, revisa los botones de paginación (H-19, ronda 3).
- **Fechas:** todo llega en UTC; conviértelo **solo** con `IZonaHorariaService`.
- **RN-007:** si el backend responde 403 o 404 en una pantalla de Entrenador, muestra el mensaje, no un "error inesperado".
- **Acciones destructivas** (cancelar sesión, retirar reserva, eliminar bloqueo, desmarcar asistencia): confirmación que explica la consecuencia, con el motivo obligatorio cuando el backend lo exige.
- **Texto de usuario** (anuncios, notas, reportes): siempre escapado, nunca `MarkupString`.
- **Elimina los datos fabricados** de los siete servicios.

**Prueba obligatoria nueva, una por servicio:** con un `HttpMessageHandler` simulado, el servicio construye la petición correcta (método, ruta, query y cuerpo) y deserializa la respuesta real del contrato; un 4xx produce un `EstadoDeCarga` de error con el mensaje del contrato. Amplía `AusenciaPriorizacionRN020Tests` a las pantallas de Entrenador tocadas.

**Cierre:**
- `pruebas/funcional/recorrido-navegador.js`: pasan las 8 comprobaciones "H-01 · …", incluidas "RF-033 reportes reflejan datos reales" y "RF-032 auditoría muestra acciones reales".
- `../fitnessbackend/pruebas/funcional/trazabilidad.sh . ../fitnessfrontend` (desde el backend) → ningún RF con servicio `MEMORIA`.

## Ronda 2 — QR dinámico real (`QA2-F2`) · H-02 (alta)

`Servicios/Asistencia/PuntosAccesoClienteService.cs:65-76` arma `dyn_{guid}_{periodo}` en el cliente, con un periodo de 15 s y el nombre fijo "Punto de Acceso Principal".

- Usa `GET /api/asistencia/puntos-acceso/{id}/qr-actual` (rol Administrador), que devuelve `{ token, validoHastaUtc, periodoSegundos }`. **Quita el generador del cliente.** El nombre del punto sale de `GET /api/asistencia/puntos-acceso`.
- `PantallaPuntoAcceso.razor` programa la siguiente petición por `validoHastaUtc` **del servidor**, con un margen pequeño, no por un periodo fijo del cliente. El contador usa `periodoSegundos`, sin `aria-live`.
- Si la petición falla, muestra un aviso y reintenta con espera. **Nunca dibujes un QR vencido.** Libera los temporizadores al salir.
- El token no se registra ni se muestra fuera del QR.
- **Prueba obligatoria nueva:**
  - el servicio llama a `qr-actual`;
  - no queda rastro de `dyn_`;
  - el QR cambia cuando cambia `validoHastaUtc`;
  - con un error de red se muestra el aviso y no el QR anterior.
- **Cierre:** `recorrido-navegador.js` → "La pantalla del punto muestra el punto real ("QA Torniquete")". Prueba manual con un teléfono real (la hace el usuario): escanear el QR → `POST /api/asistencia/ingreso/{token}` → 200; el mismo QR 70 s después → `QrVencido`.

## Ronda 3 — Cambios de contrato del backend y detalles (`QA2-F3`) · D-B, D-C, D-D, H-19

1. **D-B · 429 en login, registro, check-in e ingreso.** Muestra un mensaje claro con el tiempo de espera si llega `Retry-After`: "Demasiados intentos. Espera un momento antes de volver a intentarlo". El login de una cuenta bloqueada ya no recibe `LockedOut`: **no** muestres un mensaje distinto al de credenciales incorrectas. En check-in e ingreso (de pie, frente al lector), el mensaje debe ser corto y sin bucle de reintento automático. Prueba bUnit por pantalla.
2. **D-C · Refresh de 1 día.** Verifica que la renovación de sesión (F1) maneja el vencimiento del refresh: redirige al login **conservando la ruta de retorno** y avisa "Tu sesión expiró". Si hay un formulario a medias (crear evento, reportar un problema), no se pierde sin aviso: confirmación o borrador en `sessionStorage`, sin guardar tokens. Prueba con el handler simulando 401 en el refresh.
3. **D-D · `fechaInicioRecurrencia`.** Campo opcional "Fecha de la primera clase" en `CrearEvento.razor` y `EditarEvento.razor` cuando el evento es recurrente. Interpretación en hora de Bogotá vía `IZonaHorariaService`. El error 400 `FechaInicioRecurrenciaInvalida` se muestra junto al campo. **No valides en el cliente** la relación con la fecha de fin: lo decide el backend. Prueba bUnit: el campo viaja en el cuerpo y el error se muestra.
4. **Reportes:** el rango excedido ahora llega como **400** (antes 409); ajusta el manejo si dependía del código.
5. **H-19 · Paginación de Auditoría.** Los botones "□ Anterior / Siguiente □" de `Auditoria/RegistroAuditoria.razor` usan una fuente de iconos que no carga. Reemplázala por SVG en línea con `aria-hidden="true"` y texto accesible. **Cierre:** `PAGINACION_AUDITORIA=icono,icono` en el recorrido.

## Ronda 4 — Cabeceras y descarga (`QA2-F4`) · H-13, H-14

1. **H-13 · Cabeceras en `nginx.conf`:**
   - `Content-Security-Policy`, la mínima que permita Blazor WASM: `default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self'` (más lo que exijan las fuentes del tema; nada de `unsafe-inline` si se puede evitar); `img-src 'self' data:`; `connect-src 'self' <URL_BASE_API>`; `frame-ancestors 'none'`; `base-uri 'self'`; `form-action 'self'`.
   - `connect-src` se completa en el arranque con `docker/40-url-api.sh` a partir de `URL_BASE_API`; **nunca `*`**.
   - `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` y `Permissions-Policy` restrictiva. La cámara solo se permite (`camera=(self)`) si el escaneo de QR la usa: revisa `Ingreso.razor` y `CheckIn.razor`.
   - `server_tokens off;`.
   - **Cierre:** `pruebas/seguridad/zap-frontend.sh` sin alertas de cabeceras, `curl -I` sin versión de nginx y **0 errores de CSP en la consola** en todas las pantallas del recorrido.
2. **H-14 · Descarga:**
   - Sirve los `.br` y `.gz` precomprimidos: `gzip_static on;`, y Brotli si la imagen `nginx-unprivileged` tiene el módulo (si no, documenta por qué no).
   - Añade `application/octet-stream` y `application/wasm` a `gzip_types` para que los `icudt_*.dat` (2,6 MB) viajen comprimidos.
   - Caché `immutable` de un año solo para `_framework` con huella; `appsettings.json` mantiene `no-store`.
   - Evalúa `BlazorWebAssemblyLoadAllGlobalizationData=false` o la carga de solo los datos ICU necesarios para `es-CO`, **solo si no rompe** `IZonaHorariaService` ni el formato de fechas y números: prueba la conversión a `America/Bogota` y el formato `es-CO`.
   - **Cierre:** `pruebas/desempeno/tamano-descarga.sh` ≤ 3,5 MiB transferidos (hoy 6.748 KiB) y arranque a login en 4G lento ≤ 6 s (hoy 8,84 s; objetivo provisional, `PENDIENTE`).

## Ronda 5 — Cierre de preparación para producción (`QA2-F5`)

Recorre la "Lista de verificación antes de entregar a TI" de `docs/plan-despliegue/03-preparacion-despliegue-frontend.md`. Por cada punto, **demuestra** que está hecho (archivo, prueba o comando) o complétalo si es del repositorio:
- `dotnet publish -c Release` sin advertencias nuevas y la imagen arranca sin privilegios.
- Las rutas profundas se pueden recargar.
- Cabeceras y caché verificadas con `curl -I`.
- CSP sin errores en todas las pantallas.
- Sin secretos ni URLs privadas; `appsettings.json` solo con el marcador `REEMPLAZAR_EN_DESPLIEGUE`.
- Sección "Cambio a credenciales institucionales" en `README.md`.
- Ningún servicio en memoria. `seed_test_data.sql` documentado como prohibido en producción.

Lo que depende del entorno desplegado (Lighthouse, `docs/checklist-e2e.md` contra el entorno de pruebas, teléfonos reales) queda como tarea del usuario o de TI en `docs/plan-despliegue/02-pruebas-en-dispositivos-y-accesibilidad.md`, con el estado actual. Commit `chore(QA2-F5): …`.

## Qué dejarme al terminar

- Resumen por ronda: hallazgos corregidos, archivos tocados, pruebas nuevas, total de pruebas y veredicto del revisor.
- Lo que **no** se corrigió y por qué.
- Cualquier diferencia entre el contrato documentado y el OpenAPI real que encontraste al regenerar Kiota (el backend debe corregirla).
- Servicios que sigan en memoria (debe ser ninguno).
- Recordatorio para el usuario: ejecutar `../fitnessbackend/.agents/prompt-verificacion-calidad.md` con `verificador-calidad-fitnessbackend` para el veredicto conjunto (ronda 3).
