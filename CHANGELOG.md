# Historial de cambios — fitnessfrontend

Rama `feat/pendientes-1-11`. Cada entrada corresponde a un commit.

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
