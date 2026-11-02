# Historial de cambios — fitnessfrontend

Rama `feat/pendientes-1-11`. Cada entrada corresponde a un commit.

## QA3-F1 · Correcciones de verificación independiente de calidad (H-13, H-27, H-28, H-30)
- **Cierre de las 3:00 am (2026-11-02):**
  - **H-27 (D-E):** Kiota regenerado con `GET /api/admin/sesiones/{id}/inscritos`. `AdministracionSesionesClienteService` usa ese endpoint (confirmados + lista de espera + origen, sin posición ni priorización, RN-020) en lugar del del Entrenador. Se conserva la tolerancia a errores de la lista. Pruebas: ruta administrativa y mapeo de estados/origen.
  - **H-13 (defecto hallado al construir la imagen):** `docker/40-url-api.sh` calculaba el hash de la CSP con un salto de línea de más (`sed` lo añade), así que el navegador habría bloqueado el `importmap`. Ahora se calcula sobre el contenido exacto; comprobado en la imagen (cabecera = sha256 del script). Cabeceras presentes en `/`, `/appsettings.json` y rutas SPA; `Server: nginx` sin versión.
- **H-28 (Integridad):** Alineación de tipos de bloqueo de calendario con el backend (0=Festivo, 1=Mantenimiento, 2=Cierre) mediante enum `TipoBloqueo` en `IBloqueosClienteService` y `BloqueosClienteService`, eliminando el mapeo manual invertido y la opción ficticia "Institucional". Pruebas de ida y vuelta añadidas en `CalidadRonda3QA3F1Tests.cs`.
- **H-30 (Eficiencia/UX):** `NotasYAnunciosClienteService.ObtenerAnunciosMisReservasAsync` filtrado para solicitar anuncios únicamente de reservas activas en estado `Confirmada` (`Estado == 0`), evitando errores 403 de red en reservas en lista de espera o canceladas. Prueba unitaria agregada.
- **H-27 (Adecuación funcional):** Manejo resiliente en `AdministracionSesionesClienteService.ObtenerDetalleSesionAdminAsync` ante respuestas 403 Forbidden en el endpoint secundario de inscritos, permitiendo que la pantalla `AdministrarSesion.razor` cargue la información de la sesión y mantenga operativos los botones de cancelar, reprogramar e inscribir terceros. Pruebas bUnit agregadas para el caso 403 y caso feliz.
- **H-13 (Seguridad CSP y cabeceras):**
  - Eliminado `'unsafe-inline'` y `'unsafe-eval'` de `script-src`, empleando el hash sha256 generado para el script inline del importmap.
  - Eliminados estilos inline de `index.html` mediante clases de diseño en `itm-tema.css`.
  - Reemplazo dinámico de `connect-src 'self' ${URL_BASE_API}` en `docker/40-url-api.sh` sin comodines de protocolo.
  - Supresión de versión del servidor en respuesta HTTP (`server_tokens off;`).
  - Incorporada cabecera `Permissions-Policy`.
  - Herencia de cabeceras de seguridad resuelta mediante `include /etc/nginx/conf.d/seguridad.conf;` en cada bloque `location`.
  - OWASP ZAP sin alertas críticas ni medias (0 FAIL).
- Total de pruebas automatizadas: **177 en verde** (0 fallos).

## QA2-F5 · Cierre de preparación para producción (H-32)
- Verificación exhaustiva de la lista de entrega a TI (`docs/plan-despliegue/03-preparacion-despliegue-frontend.md`):
  - `dotnet publish -c Release` limpio sin advertencias y contenedor ejecutando como usuario sin privilegios (`USER 101`).
  - Recarga de rutas profundas comprobada (`try_files` devuelve `index.html` con 200 OK en `/admin/calendario`).
  - Políticas de caché comprobadas (`no-store` en `appsettings.json` e `immutable` en `_framework/`).
  - Repositorio libre de secretos y URLs privadas (`appsettings.json` con `REEMPLAZAR_EN_DESPLIEGUE`).
  - Sección "Cambio a credenciales institucionales" documentada en `README.md`.

## QA2-F4 · Cabeceras CSP, seguridad y optimización de descarga nginx (H-13, H-14)
- Configurada directiva estricta Content-Security-Policy en `nginx.conf` (`default-src 'self'`, `style-src-elem`, `font-src`, `img-src` con data: y `connect-src` completo).
- Compresión dinámica y `gzip_static on` para servir artefactos `.wasm`, `.dll` y `.js` precomprimidos con caché inmutable (H-14).
- Incorporadas cabeceras de endurecimiento `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY` y `Referrer-Policy`.

## QA2-F3 · Cambios de contrato y detalles pendientes (D-B, D-C, D-D, H-19)
- Mensajes amigables y respetuosos ante rate limit 429 en login, registro, check-in e ingreso (D-B).
- Preservación de returnUrl al redirigir al login si falla la renovación de sesión (D-C).
- Incorporación de `FechaInicioRecurrencia` en `PatronRecurrencia` y formulario de `CrearEvento.razor` (D-D).
- Manejo accesible de error 400 (`RangoExcesivo`) en `Reportes.razor`.
- Paginación de auditoría con botones e íconos SVG accesibles en línea (H-19).

## QA2-F2 · QR dinámico real conectado a la API (H-02)
- Reemplazado generador simulado en cliente `dyn_` en `PuntosAccesoClienteService.cs` por el endpoint real `GET /api/asistencia/puntos-acceso/{id}/qr-actual`.
- El nombre del punto se obtiene dinámicamente desde el listado del backend y el periodo/vencimiento son gobernados por `ValidoHastaUtc` del servidor.
- Agregada prueba obligatoria en `PantallaPuntoAccesoF6Tests.cs` (167 pruebas en verde).

## QA2-F1 · Conectar los siete servicios simulados a Kiota (H-01)
- Conectados a la API Kiota y eliminados todos los datos simulados en memoria de:
  - `ReportesClienteService`: endpoints agregados `/api/reportes/*` (≤ 90 días, error 400 y exportación CSV con `formato=csv`).
  - `AuditoriaClienteService`: `/api/auditoria` con tope estricto de máximo 50 elementos por página y mapeo accesible de errores 403.
  - `BloqueosClienteService`: `GET/POST /api/calendario/bloqueos` y `DELETE /api/calendario/bloqueos/{id}`.
  - `AdministracionSesionesClienteService`: cancelar con motivo obligatorio, reprogramar, e inscripción/retiro administrativo.
  - `NotasYAnunciosClienteService`: anuncios y notas con control de rol RN-007 y mensajes de error amigables.
  - `AsistenciaManualClienteService`: registro manual de asistencia por sesión e inscritos.
  - `RetroalimentacionClienteService`: `/api/retroalimentacion` y `/api/retroalimentacion/mios` con mensaje comprensible ante rate limit 429 (`LimiteExcedido`).
- Creada suite de pruebas con `HttpMessageHandler` y `FitnessApiClient` simulado en `ServiciosKiotaConectadosQA2F1Tests.cs` y ampliado `AusenciaPriorizacionRN020Tests.cs` (166 pruebas en verde).

## Pruebas de calidad ronda 2 (test(qa))
- Informe del cliente en [docs/pruebas-calidad/02-resultados-ronda-2.md](docs/pruebas-calidad/02-resultados-ronda-2.md): H-01 y H-02 persisten (siete servicios en memoria y QR generado en el cliente); H-07 (cliente) y H-08 corregidos; H-13, H-14 y H-19 persisten.
- `pruebas/funcional/recorrido-navegador.js` ampliado a las pantallas F2–F7 (18 comprobaciones; 9 cumplen).
- Prompt de corrección para los agentes: `.agents/prompt-correccion-calidad.md`.
- Sin cambios en el código de la aplicación.

## QA-1 · Pruebas de calidad ISO/IEC 25010 del cliente (test(qa))
- Plan del cliente en `docs/pruebas-calidad/00-plan.md` (el plan conjunto está en el backend).
- Scripts en `pruebas/`: imagen nginx local, tamaño de descarga, ZAP baseline y recorrido real con Chromium headless (k6 browser) con red móvil simulada. Sin cambios en el código de la aplicación.
- Resultados del 2026-11-01 sobre `46e0dee` en `docs/pruebas-calidad/01-resultados.md`, con capturas y evidencia.

## F10 · Diseños diferidos de apelación y derechos del titular (RF-041, RF-042)
- Creado documento de diseño conceptual y bocetos en texto `docs/plan-despliegue/04-diseno-ui-apelacion-y-derechos-titular.md`.
- Diseñada pantalla conceptual de solicitud de revisión de reservas y asistencia (RF-041) con verificación estricta de confidencialidad RN-020 (sin revelar penalización ni nivel de confiabilidad al usuario).
- Diseñada pantalla conceptual para el ejercicio de derechos del titular de datos personales (RF-042, Ley 1581 de 2012) para consulta portable, actualización y solicitud de supresión/anonimización respetando la inmutabilidad de los registros de auditoría.
- Validación de que ninguna interfaz propuesta quiebra la confidencialidad de priorización institucional.

## F9 · Cierre visual, accesibilidad y verificación de navegación por rol
- Ampliada `EditarEvento.razor` con tarjeta de gestión operativa conectando a la administración de sesiones y programación del evento.
- Ampliadas las pruebas de navegación `NavegacionPorRolTests.cs` para validar que todas las rutas administrativas nuevas (`admin/auditoria`, `admin/reportes`, `admin/retroalimentacion`, `admin/calendario`) no sean visibles para roles de Usuario ni Entrenador.
- Creada suite de accesibilidad y verificación de estándares en `AccesibilidadF9Tests.cs`: comprobación de que el 100% de las pantallas Razor contengan el ID de su RF de origen en el encabezado, presencia de skip-link accesible para teclado en `index.html`, y ausencia de colores arbitrarios fuera de los tokens de `itm-tema.css` (144 pruebas en verde).

## F8 · PWA instalable
- Creado `manifest.webmanifest` con nombre oficial institucional, `display: standalone`, colores de marca `#011C5B` e íconos maskable (192 y 512 px).
- Vinculado `manifest.webmanifest` en `index.html`.
- Incorporado módulo en `descarga.js` para gestionar el prompt de instalación segura (`beforeinstallprompt` diferido) sin interceptar llamadas de API ni almacenar en caché tokens de autenticación.
- Añadida sección e interacción accesible en `Ayuda.razor` para instalación en pantallas principales de dispositivos.
- Agregadas pruebas de validación de manifiesto y vinculación web en `PwaF8Tests.cs` (141 pruebas en verde).

## F7 · Retroalimentación y reporte de problemas (RF-038)
- Creada página `Reportar.razor` (/ayuda/reportar y /reportar): formulario para Problema, Sugerencia o Pregunta con contador en vivo (≤ 1000 caracteres), ruta de origen precargada por query string sin agentes de usuario ni datos invasivos, y manejo respetuoso de 429 ("Ha enviado varios reportes recientemente; por favor intente más tarde.") sin culpar al usuario.
- Creada página `MisReportes.razor` (/mis-reportes): consulta y seguimiento de reportes propios con insignias accesibles, conversión de fechas a `America/Bogota` mediante `IZonaHorariaService` y renderizado de texto plano estricto contra inyección HTML/scripts.
- Creada página `BandejaAdministrador.razor` (/admin/retroalimentacion): panel administrativo (`[Authorize(Roles = "Administrador")]`) con filtro por estado (Pendiente, En revisión, Resuelto, Descartado), actualización de estados y respuesta institucional.
- Incorporado enlace a Retroalimentación en la navegación del Administrador (`NavMenuAdministrador.razor`).
- Implementado servicio de fachada `IRetroalimentacionClienteService` con aislamiento por usuario autenticado.
- Ampliado `AusenciaPriorizacionRN020Tests.cs` y agregadas las 5 pruebas obligatorias de F7 en `RetroalimentacionF7Tests.cs` (139 pruebas en verde).

## F6 · Pantalla de punto de acceso con QR dinámico (RF-039)
- Creado componente `AnilloCuentaRegresiva.razor` con SVG circular, `aria-live="off"` y compatibilidad con `prefers-reduced-motion`.
- Creada página `PantallaPuntoAcceso.razor` (/admin/puntos-acceso/{id}/pantalla) siguiendo el Patrón 9: actualización preventiva 2 segundos antes de `validoHasta`, retención del QR actual si falla la red hasta que expire, visualización clara de "Sin conexión con el servidor", solicitud de Screen Wake Lock para evitar suspensión de pantalla y liberación total de recursos (`Dispose`) para prevenir fugas de temporizadores.
- Actualizada página `PuntosAcceso.razor`: reemplazada la vista imprimible y CSS `@media print` (D4: ya no se imprimen QR fijos) por botón accesible "Abrir pantalla" (`target="_blank"`).
- Actualizada página `Ingreso.razor`: manejo explícito del error de backend `QrVencido`, ofreciendo instrucciones claras al usuario para volver a escanear la pantalla física y suprimiendo el bucle de reintento directo con el mismo token vencido.
- Implementado servicio de fachada `IPuntosAccesoClienteService` con DTO rotativo `QrPuntoAccesoDto`.
- Ampliado `AusenciaPriorizacionRN020Tests.cs` y agregadas las 5 pruebas obligatorias de F6 en `PantallaPuntoAccesoF6Tests.cs` (132 pruebas en verde).

## F5 · Reportes y exportación CSV (RF-033)
- Creada función pura `GeneradorCsv.cs` con BOM UTF-8 (`\uFEFF`), separador `;`, escape de comillas y protección estricta contra inyección de fórmulas CSV (prefijo `'` ante `=`, `+`, `-`, `@`).
- Creada página `Reportes.razor` (/admin/reportes) con 5 pestañas operativas: Asistencia por sesión, Ocupación de cupos, Inasistencias por actividad (agregado, sin datos personales RN-020), Horas pico en franjas locales `America/Bogota` y Uso de la plataforma.
- Incorporado selector de fechas con atajos ("Últimos 7 días", "Este mes", "Mes anterior") y validación de rango invertido.
- Botón de exportación CSV en cada pestaña con descarga directa a través de interop JS.
- Incorporado enlace a Reportes en `NavMenuAdministrador.razor`.
- Implementado servicio de fachada `IReportesClienteService`.
- Implementadas las 5 pruebas obligatorias de F5 en `ReportesYExportacionCsvF5Tests.cs` y ampliado `AusenciaPriorizacionRN020Tests.cs` con los nuevos DTOs (127 pruebas en verde).

## F4 · Administración de sesiones, bloqueos y auditoría (RF-028, RF-029, RF-030, RF-031, RF-032)
- Creada página `AdministrarSesion.razor` (/admin/sesiones/{id}): cancelación con motivo obligatorio y confirmación indicando el conteo de reservas afectadas (sin penalización RN-031), reprogramación de fecha/hora con manejo de bloqueos, inscripción de terceros en ventanilla y retiro de terceros con advertencia de promoción de lista de espera (RF-030).
- Creada página `BloqueosCalendario.razor` (/admin/calendario): registro de cierres/mantenimientos/festivos con validación temporal, listado y eliminación con confirmación de consecuencia explícita.
- Creada página `RegistroAuditoria.razor` (/admin/auditoria): consulta inmutable paginada (tamano=25) con filtros de fecha, tipo de acción y actor, conversión a `America/Bogota` mediante `IZonaHorariaService` y presentación adaptable (tarjetas en móvil y tabla en escritorio).
- Incorporados enlaces de Bloqueos y Auditoría en la barra de navegación del Administrador (`NavMenuAdministrador.razor`).
- Implementados servicios de fachada `IAdministracionSesionesClienteService` y `IAuditoriaClienteService`.
- Implementadas las 5 pruebas obligatorias de F4 en `AdministracionSesionesYAuditoriaF4Tests.cs` y ampliado `AusenciaPriorizacionRN020Tests.cs` con los nuevos DTOs (117 pruebas en verde).

## F3 · Entrenador y sesión cancelada para el usuario (RF-027, RF-028, RF-029, RF-034)
- Creada página `AsistenciaManual.razor` (/entrenador/sesiones/{id}/asistencia) con listado de inscritos, marcado manual directo, confirmación modal con consecuencia explícita para desmarcar ("revertirá el estado a No asistió y registrará auditoría"), y manejo de rechazo 403 por pertenencia.
- Creada página `NotasDeSesion.razor` (/entrenador/sesiones/{id}/notas) con bitácora interna de notas/incidencias (RF-034) y publicación de anuncios a inscritos (RF-029), ambos con contadores de caracteres en vivo (1000 y 500) y escape estricto de HTML / scripts.
- Actualizada `MisActividades.razor` con botones directos para Asistencia manual y Notas/Avisos en cada sesión.
- Actualizada `MisReservas.razor` para soportar sesión cancelada por la organización (RF-028): muestra insignia "Sesión cancelada", motivo del sistema, inhabilita botón cancelar y respeta confidencialidad RN-020; e incorpora visualización de anuncios vigentes de sesión (RF-029) en tarjeta informativa accesible.
- Implementados servicios de fachada `IAsistenciaManualClienteService` y `INotasYAnunciosClienteService`, registrados en el contenedor de dependencias.
- Implementadas las 5 pruebas obligatorias de F3 en `EntrenadorYSesionCanceladaF3Tests.cs` y ampliado `AusenciaPriorizacionRN020Tests.cs` con los nuevos DTOs (109 pruebas en verde).

## F2 · Programación, calendario ics y ayuda (RF-035, RF-036, RF-037, RF-038)
- Creada página `Programacion.razor` (/programacion) con selector [Lista | Semana], navegación semanal con botones táctiles de 48 px, conversión horaria a `America/Bogota`, visualización de franjas de bloqueo de calendario (RF-031) ("Cerrado · motivo") sin botones de reserva, y estado de lista de espera agregado sin posición personal (RN-020, AMB-D04).
- Creado `GeneradorIcs.cs` (RFC 5545 función pura) con escape de caracteres (`,`, `;`, `\`, saltos de línea) y plegado de líneas a <= 75 octetos.
- Agregado botón accesible "Agregar a mi calendario" en `MisReservas.razor` para reservas confirmadas, con descarga directa mediante `wwwroot/js/descarga.js`.
- Creada página `Ayuda.razor` (/ayuda) con preguntas frecuentes estáticas marcadas `@* PENDIENTE: aprobación de Bienestar *@`, enlace a reporte de problemas y acceso en navegación.
- Implementado servicio de fachada `IBloqueosClienteService` y `BloqueosClienteService`.
- Ampliada `IEventosClienteService` con sobrecarga para consultar eventos por rango temporal (`desde`, `hasta`).
- Agregadas las 5 pruebas obligatorias de F2 en `ProgramacionYCalendarioF2Tests.cs` y extendido `AusenciaPriorizacionRN020Tests.cs` con `EventoResumenDto` y `BloqueoCalendarioDto` (101 pruebas en verde).

## F1 · Renovación de sesión de un solo vuelo (RF-026)
- Implementado Patrón 8 en `ManejadorDeAutenticacionHttp.cs` con sincronización mediante `SemaphoreSlim`.
- Peticiones concurrentes con token vencido comparten un único refresh contra `POST /api/auth/refresh`.
- Reintento único de la petición original clonando cabeceras y contenido; sin bucle de reintento ante 401 persistente.
- Si el refresh token es inválido o expira, se limpian tokens/perfil y se redirige a `/login?returnUrl=<ruta actual>`.
- Exclusión estricta de rutas `/api/auth/*` para prevenir recursión o interceptar login.
- Implementado método `RenovarSesionAsync()` en `IAutenticacionService` y `AutenticacionService`.
- Soportada deserialización insensible a mayúsculas/minúsculas en `ProveedorEstadoAutenticacion.cs`.
- Cobertura de las 5 pruebas obligatorias de F1 en `RenovacionSesionF1Tests.cs` (94 pruebas en verde).

## F0 · Documentación previa y cobertura (Plan de despliegue)
- Tabla de cobertura en `docs/arquitectura/08-guia-de-implementacion.md` actualizada con RF-026 a RF-039 y requisitos diferidos RF-041/RF-042.
- Ambigüedades AMB-D04, AMB-D07 y nota de compatibilidad de AMB-014 con sesión cancelada incorporadas en `docs/requisitos/01-ambiguedades-ui.md`.
- Flujo de QR en `docs/arquitectura/06-flujo-checkin-qr.md` actualizado con anexo de QR dinámico para ingreso general (RN-033, RF-039), pantalla de punto de acceso y mensaje `QrVencido`.
- Patrones 8 (renovación de sesión de un solo vuelo) y 9 (pantalla de actualización periódica) documentados en `docs/arquitectura/04-patrones-de-diseno.md`.
- Estructura de carpetas y servicios nuevos documentada en `docs/arquitectura/05-organizacion-directorios.md`.
- Componentes nuevos (`ConfirmacionConsecuencia`, `TablaAdaptable`, `AnilloCuentaRegresiva`) documentados en `docs/arquitectura/09-rediseno-visual.md`.
- `README.md`, `CHANGELOG.md` y `docs/checklist-e2e.md` actualizados.


## Rediseño visual (8f0c4ad)
- Sistema de diseño ITM aplicado en `itm-tema.css`; logo oficial; navegación responsive (barra superior, menú desplegable, barra inferior del Usuario en celular).
- `InsigniaEstado` (tinta suave, seis estados), esqueletos de carga, selector de día y cupo sin puntos en Eventos.
- Pantallas rediseñadas: Eventos, Mis reservas, Mi asistencia, Login, Registro, Check-in, Ingreso.
- Documentado en [docs/arquitectura/09-rediseno-visual.md](docs/arquitectura/09-rediseno-visual.md). Pruebas: 89.

## Cierre y verificación (276fcfb, 9d90229, 959d88d)
- Checklist E2E con los defectos hallados, README actualizado, I-10 cerrado.
- Roles desde `GET /api/usuarios/yo`, banner de error oculto por defecto, rótulos de historial e ingreso; el ingreso muestra el nombre del punto y no el token del QR.

## Despliegue (a17ce4d)
- Imagen Docker con nginx sin root, URL de la API por entorno y CI.

## Usuarios, roles y puntos de acceso (6d12047, b32cec4)
- Cliente Kiota regenerado.
- Pantallas de gestión de usuarios y roles (RF-023), puntos de acceso con QR imprimible (RF-024), selector de entrenador, búsqueda de usuario en Priorización y registro ampliado con autorización de datos (RF-025).

## Limpieza (729e9ed, f278492)
- Restos de plantilla eliminados, `Home` y `NotFound` movidos a `Paginas/`, línea base del repositorio.
