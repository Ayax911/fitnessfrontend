# Guía de implementación del frontend para el despliegue

> **Documento de trabajo del constructor.** Los requisitos están en el backend: [`01-requisitos-nuevos.md`](../../../fitnessbackend/docs/plan-despliegue/01-requisitos-nuevos.md). Contrato: `fitnessbackend/docs/arquitectura/07-contrato-api.md` (lo actualiza B0) y el `/openapi/v1.json` en ejecución.

## Reglas de trabajo (se suman a las del `AGENTS.md`)

1. **Una fase = un commit** (`feat(F3): …`) sobre la rama indicada por el usuario, nunca sobre `main`.
2. **Cada pantalla nueva** lleva el ID de su RF (`@* RF-027: … *@`), usa solo los componentes y tokens del sistema de diseño (`InsigniaEstado`, `EstadoDeCarga`, `EsqueletoTarjetas`, `EsqueletoLista`, clases `itm-*`) y **tiene su esqueleto de carga**, su estado vacío y su estado de error con texto del sistema.
3. **Protección contra errores (ISO/IEC 25010):** toda acción destructiva o masiva (cancelar sesión, retirar reserva, eliminar bloqueo) pide **confirmación que explica la consecuencia** en lenguaje del usuario, con el botón principal en peligro y el secundario "Volver". Nada se confirma con Enter accidental.
4. **Operación:** objetivos táctiles de 48 px, formularios con etiqueta visible, ayuda bajo el campo y errores con texto (`validation-message`), foco que se mueve al error.
5. **Responsive en tres cortes** (< 720, 720–1023, ≥ 1024). Las tablas de administración se convierten en **tarjetas apiladas en celular** (no desbordan horizontalmente).
6. **RN-020:** cada fase que toque datos de Usuario/Entrenador **amplía** `AusenciaPriorizacionRN020Tests` con las pantallas nuevas. No se muestra posición en lista de espera, nivel, penalización ni efecto sobre el indicador.
7. **Fechas** solo con `IZonaHorariaService`. Los reportes y la vista semanal convierten UTC → `America/Bogota` aquí y en ningún otro sitio.
8. **No se reimplementa ninguna regla de negocio** (cupos, ventanas, bloqueos, validez del QR): el cliente muestra lo que responde el backend.
9. **Autenticación detrás de `IAutenticacionService`** (D1): ninguna pantalla nueva depende de cómo se inicia sesión.
10. Cada fase termina con `dotnet build`, `dotnet test` en verde, revisión de `revisor-fitnessfrontend`, y la actualización de `docs/checklist-e2e.md`.

## Mapa de rutas y archivos nuevos (propuesto)

| Ruta | Rol | Archivo | RF |
|---|---|---|---|
| `/programacion` | Autenticados | `Paginas/Eventos/Programacion.razor` | RF-035 |
| `/ayuda` | Autenticados | `Paginas/Ayuda/Ayuda.razor` | RF-038 |
| `/ayuda/reportar` | Autenticados | `Paginas/Retroalimentacion/Reportar.razor` | RF-038 |
| `/mis-reportes` | Autenticados | `Paginas/Retroalimentacion/MisReportes.razor` | RF-038 |
| `/entrenador/sesiones/{id}/asistencia` | Entrenador, Administrador | `Paginas/Asistencia/AsistenciaManual.razor` | RF-027 |
| `/entrenador/sesiones/{id}/notas` | Entrenador, Administrador | `Paginas/Eventos/NotasDeSesion.razor` | RF-034 |
| `/admin/sesiones/{id}` | Administrador | `Paginas/Eventos/Administrar/AdministrarSesion.razor` (cancelar, reprogramar, inscribir, retirar, anuncios) | RF-028, RF-029, RF-030 |
| `/admin/calendario` | Administrador | `Paginas/Eventos/Administrar/BloqueosCalendario.razor` | RF-031 |
| `/admin/auditoria` | Administrador | `Paginas/Auditoria/RegistroAuditoria.razor` | RF-032 |
| `/admin/reportes` | Administrador | `Paginas/Reportes/Reportes.razor` | RF-033 |
| `/admin/retroalimentacion` | Administrador | `Paginas/Retroalimentacion/BandejaAdministrador.razor` | RF-038 |
| `/admin/puntos-acceso/{id}/pantalla` | Administrador | `Paginas/Asistencia/Administrar/PantallaPuntoAcceso.razor` | RF-039 |

Servicios fachada nuevos en `Servicios/<Area>/` con el patrón de `PriorizacionClienteService` (envuelven en `EstadoDeCarga<T>`, capturan `ErrorDetalleDto` y `ApiException` 403/404/409/429): `SesionesAdminClienteService`, `BloqueosClienteService`, `AuditoriaClienteService`, `ReportesClienteService`, `RetroalimentacionClienteService`, y ampliaciones de `AsistenciaClienteService` y `ReservasClienteService`.

Los menús (`NavMenu*.razor`) incorporan los destinos nuevos. El Usuario en celular mantiene **3 destinos** en la barra inferior; "Programación" y "Ayuda" van dentro de "Eventos" (selector Lista/Semana) y en el pie, para no pasar de 3 pestañas.

---

## F0 · Documentación previa

Solo **añade**.
- [ ] `docs/arquitectura/08-guia-de-implementacion.md`: filas RF-026 a RF-039 en la tabla de cobertura RF → fase → pantalla.
- [ ] `docs/requisitos/01-ambiguedades-ui.md`: AMB-D04 (posición en lista de espera: **no se muestra**), AMB-D07 (quién cancela sesiones), y la nota de compatibilidad de AMB-014 con "sesión cancelada" (el mensaje de cancelación es de la organización y **no** revela nivel).
- [ ] `docs/arquitectura/06-flujo-checkin-qr.md`: **anexo** que explica el QR dinámico del ingreso general (RN-033), la pantalla del punto de acceso y el mensaje de `QrVencido`; el flujo del QR de sesión no cambia. No reescribir el texto existente: marcar lo reemplazado.
- [ ] `docs/arquitectura/04-patrones-de-diseno.md`: **Patrón 8 — Renovación de sesión de un solo vuelo** (`DelegatingHandler` con un único refresh concurrente y un reintento) y **Patrón 9 — Pantalla de actualización periódica** (sondeo con temporizador basado en la hora de vencimiento del servidor, no del cliente).
- [ ] `docs/arquitectura/05-organizacion-directorios.md`: carpetas nuevas de `Paginas/`.
- [ ] `docs/arquitectura/07-identidad-visual-…` / `09-rediseno-visual.md`: documentar los componentes nuevos de esta guía (`ConfirmacionConsecuencia`, `TablaAdaptable`, `AnilloCuentaRegresiva`).
- [ ] `README.md` y `CHANGELOG.md`: enlazar este plan.
**DoD:** el revisor confirma que toda RF nueva tiene fase y pantalla en la tabla, y que no se editó texto existente (solo anexos).

## F1 · Renovación de sesión (RF-026)

**Dónde:** `Servicios/Identidad/ManejadorDeAutenticacionHttp.cs` y `AutenticacionService`.
**Comportamiento.** (1) Ante un 401 en una petición **que no sea** `/api/auth/*`, intenta **un** refresh con el refresh token guardado (`POST /api/auth/refresh`), guarda los tokens nuevos y **repite la petición original una sola vez**. (2) Si hay varias peticiones en vuelo, **un único refresh compartido** (`SemaphoreSlim` o `Task` compartida). (3) Si el refresh falla (401/400/red), cierra sesión y navega a `login?returnUrl=<ruta actual>`. (4) Opcional y recomendado: renovar **de forma proactiva** cuando falte menos de `X` de la vigencia (`expiresIn` de la respuesta de login; `X` = 20 % como valor técnico).
**Pruebas obligatorias:**
1. 5 peticiones simultáneas con token vencido provocan **exactamente 1** llamada a refresh y las 5 terminan bien.
2. Un 401 tras el refresh **no** entra en bucle (máximo un reintento).
3. Refresh inválido → sesión cerrada y `returnUrl` correcto.
4. Las llamadas a `/api/auth/refresh` y `/api/auth/login` no disparan refresh recursivo.
5. El check-in (`/checkin/{token}`) con token vencido se completa tras renovar, sin pasar por la pantalla de login.
**DoD:** patrón 8 documentado en F0; checklist E2E actualizado con "sesión vencida durante un check-in".

## F2 · Programación, calendario y ayuda (RF-035, RF-036, RF-037, RF-038 parte estática)

- **Selector Lista | Semana** en Eventos. La vista semanal: 7 columnas en escritorio, **lista por día** en celular, con los bloqueos del calendario (RF-031) como franjas "Cerrado · motivo" (el motivo viene del backend). Consume `GET /api/eventos?desde=&hasta=` y `GET /api/calendario/bloqueos`. Navegación semana anterior/siguiente con botones de 48 px y la semana actual por defecto.
- **Paginación:** la lista usa `pagina/tamano` ("Cargar más" accesible, sin desplazamiento infinito automático).
- **Lista de espera (RF-037):** cuando la sesión está llena se muestra "N personas en lista de espera" (agregado). **Nunca posición** (AMB-D04).
- **.ics (RF-036):** `Servicios/Calendario/GeneradorIcs` (función pura): `BEGIN:VCALENDAR … VEVENT` con `DTSTART/DTEND` en UTC (`…Z`), `UID` estable (`<reservaId>@gimnasio.itm`), `SUMMARY`, `DESCRIPTION`, escape de `, ; \ \n` y líneas ≤ 75 octetos con plegado. Botón "Agregar a mi calendario" en Mis reservas (reservas `Confirmada`). Descarga mediante un interop mínimo (`wwwroot/js/descarga.js`).
- **Ayuda (`/ayuda`):** preguntas frecuentes **estáticas** (cómo reservar, qué es la lista de espera, cómo funciona el QR, qué hacer si el QR no funciona, cómo cancelar). El texto se marca `@* PENDIENTE: aprobación de Bienestar *@`. Enlace permanente en el pie y en el menú del usuario.
**Pruebas obligatorias:** (1) el `.ics` generado es válido (pruebas con casos de escape y de plegado, y una que lo parsea de vuelta); (2) la vista semanal convierte a `America/Bogota` (una sesión a las 23:30 locales cae en el día correcto aunque su UTC sea el siguiente); (3) con bloqueos se ven franjas y no botones de reserva; (4) ninguna pantalla muestra posición en lista de espera; (5) el esqueleto de la vista semanal se anuncia.
**DoD:** responsive verificado en 390, 768 y 1280 px; checklist E2E ampliado.

## F3 · Entrenador y sesión cancelada para el usuario (RF-027, RF-029, RF-034, RF-028 lado usuario)

- **`AsistenciaManual`** (desde `InscritosDeSesion`): lista de inscritos `Confirmada` con interruptor **Presente** por fila (el estado visible viene del backend), motivo opcional, confirmación al **desmarcar**, mensaje del sistema si la sesión ya está consolidada. Indica el **origen** (QR/Manual) de cada asistencia con texto, no solo color.
- **`NotasDeSesion`:** lista y alta de notas e incidencias (texto plano, contador de caracteres, límite 1000).
- **Anuncios:** el entrenador y el administrador publican (≤ 500); el usuario los ve en "Mis reservas" (por reserva) y en el detalle de la sesión, el más reciente primero. Texto siempre **escapado**.
- **Usuario:** una reserva cuya sesión fue cancelada muestra `InsigniaEstado tipo="cancelada"` con "Sesión cancelada" y el motivo del sistema; sin botón de cancelar. **Sin** ninguna mención de nivel ni de efecto en su historial.
**Pruebas obligatorias:** (1) un entrenador ajeno recibe el mensaje de 403 sin ver datos; (2) desmarcar pide confirmación; (3) las notas no se renderizan como HTML (prueba con `<script>`); (4) el usuario con sesión cancelada no ve el botón de cancelar ni texto de priorización (RN-020); (5) estado vacío y de error de cada pantalla.
**DoD:** rutas añadidas al menú del Entrenador; E2E: entrenador marca asistencia manual y la consolidación la cuenta (con backend real).

## F4 · Administración de sesiones, bloqueos, reservas de terceros y auditoría (RF-028, RF-029, RF-030, RF-031, RF-032)

- **`AdministrarSesion`:** cabecera con la sesión, sus reservas (en tarjetas en celular) y acciones: **Cancelar sesión** (motivo obligatorio; confirmación que dice *"Se cancelarán N reservas y se liberará el cupo. Los usuarios verán que la sesión fue cancelada. No se les penalizará."*, N = inscritos reales que ya trae la pantalla), **Reprogramar** (nuevo inicio/fin y motivo; el backend rechaza bloqueos y el mensaje se muestra tal cual), **Inscribir a un usuario** (buscador de usuarios con el selector existente; motivo), **Retirar** una reserva (motivo + confirmación), **Publicar anuncio**.
- **`BloqueosCalendario`:** alta (rango, tipo, motivo), lista y eliminación con confirmación. Al crear, si el backend devuelve sesiones existentes dentro del rango, se muestran como lista con acceso directo a cancelar cada una.
- **`RegistroAuditoria`:** tabla paginada con filtros (fechas, acción, actor, entidad); en celular, tarjetas. Solo lectura.
- Los enlaces a estas pantallas se añaden a `EditarEvento` y a la lista de sesiones del administrador.
**Pruebas obligatorias:** (1) cancelar exige motivo y confirmación, y el texto de la confirmación incluye el número de reservas; (2) 409 de sesión llena al inscribir se muestra con el mensaje del backend; (3) los menús de administración **no** aparecen para Usuario ni Entrenador (extender `NavegacionPorRolTests`); (4) la auditoría no ofrece ninguna acción de edición; (5) las pantallas administrativas no desbordan en 390 px.
**DoD:** E2E: cancelar una sesión con lista de espera y comprobar que el usuario ve "Sesión cancelada"; crear un bloqueo y comprobar que no se puede reservar dentro de él.

## F5 · Reportes y exportación (RF-033)

`Reportes.razor` con pestañas (Asistencia por sesión · Ocupación · Inasistencias por evento · Horas pico · Uso de la plataforma), selector de rango (por defecto: últimos 30 días, máximo el que fije el backend), tabla accesible y, donde ayude, **gráfico simple** (barras CSS/SVG) **siempre acompañado de la tabla** con los mismos datos. Botón **Exportar CSV** (llama al endpoint con `formato=csv` y descarga). Las horas pico se agrupan en hora local en el cliente.
**Pruebas obligatorias:** (1) los números renderizados coinciden con el DTO; (2) la descarga CSV usa el endpoint correcto y el nombre de archivo incluye el rango; (3) rol distinto de Administrador no ve la ruta; (4) ninguna columna ni texto de priorización (RN-020); (5) esqueleto y estado vacío ("No hay datos en este rango").
**DoD:** responsive (tablas → tarjetas); revisión de contraste del gráfico (no depende solo del color).

## F6 · Pantalla del punto de acceso con QR dinámico (RF-039)

**Pantalla** `/admin/puntos-acceso/{id}/pantalla`, pensada para un monitor o tableta en la entrada: fondo claro, logo oficial azul, nombre del punto, **QR grande** (`CodigoQr`, codifica `{BaseUri}ingreso/{token}`), texto "Escanea para registrar tu ingreso" y un **anillo de cuenta regresiva** hasta el cambio.
**Patrón 9 (actualización periódica).** Pide `GET /api/asistencia/puntos-acceso/{id}/qr-actual`; programa la siguiente petición **2 s antes de `validoHasta`** (hora del servidor; no se usa el reloj del cliente para decidir validez). Si la petición falla, **sigue mostrando el QR hasta su vencimiento**, y después muestra un estado claro "Sin conexión con el servidor. Reintentando…" y reintenta con espera creciente; nunca muestra un QR vencido como si fuera válido. Pide **Screen Wake Lock** por interop (si no está disponible, no falla). Al salir de la pantalla cancela temporizadores (`IDisposable`).
**Gestión de puntos de acceso (`PuntosAcceso.razor`):** se **retira** la vista imprimible del QR fijo y el CSS `@media print` asociado; el botón pasa a "Abrir pantalla del punto".
**Ingreso (`Ingreso.razor`):** el rechazo `QrVencido` muestra el mensaje del sistema con una instrucción clara ("Escanee de nuevo el código de la pantalla") y no ofrece reintentar con el mismo enlace.
**Pruebas obligatorias:** (1) la siguiente petición se programa antes de `validoHasta` (reloj falso de bUnit); (2) con fallo de red el QR vigente se mantiene hasta vencer y luego aparece el estado "sin conexión"; (3) al desmontarse no quedan temporizadores; (4) el QR codificado es `{BaseUri}ingreso/{token}` con el token del servidor; (5) `QrVencido` se muestra sin bucle de reintento; (6) la pantalla es solo de Administrador; (7) la pantalla tiene `aria-live` **desactivado** para la cuenta regresiva (no anunciar cada segundo) y respeta `prefers-reduced-motion`.
**DoD:** E2E con backend real: dos escaneos con 3 periodos de diferencia → el segundo se rechaza con mensaje; prueba manual en un teléfono real (ver [02](02-pruebas-en-dispositivos-y-accesibilidad.md)).

## F7 · Retroalimentación (RF-038)

- **`Reportar`:** tipo (Problema, Sugerencia, Pregunta), mensaje con contador (≤ 1000) y la ruta desde la que se abrió (pasada por parámetro; **no** se envía el agente de usuario ni datos extra). Mensaje de éxito del sistema y manejo del 429 ("Ha enviado varios reportes; intente más tarde") sin culpar.
- **`MisReportes`:** estado de cada reporte.
- **`BandejaAdministrador`:** filtro por estado, cambio de estado (auditado) y detalle.
- Enlace "Reportar un problema" en el pie y en `/ayuda`.
**Pruebas obligatorias:** (1) el contenido se renderiza como texto plano; (2) 429 se muestra con el texto del sistema; (3) un usuario no ve reportes ajenos (fachada falsa); (4) Usuario/Entrenador no ven la bandeja.
**DoD:** E2E: enviar un reporte y verlo en la bandeja del administrador.

## F8 · PWA instalable (opcional, *Could*)

Solo si F1 a F7 están cerrados. Manifiesto (`manifest.webmanifest`, íconos 192/512, `display: standalone`, `theme_color` = azul institucional), y *service worker* **solo para los archivos estáticos** (con versión por *fingerprint*; **nunca** cachea `/api/*` ni tokens). Pantalla "Sin conexión" propia cuando no hay red. Botón "Instalar" discreto en la ayuda.
**Pruebas obligatorias:** el *service worker* no intercepta `/api/`; tras una actualización la versión antigua se reemplaza (no queda un cliente viejo contra una API nueva); Lighthouse PWA sin errores bloqueantes. **Riesgo:** un *service worker* mal versionado deja usuarios con un cliente desactualizado; si no se puede garantizar, **no se entrega** (decisión del revisor).

## F9 · Cierre visual, accesibilidad y E2E

- **Verificación visual pendiente** del rediseño (ver [02](02-pruebas-en-dispositivos-y-accesibilidad.md) §3): Registro y pantallas de administración en 390 px, Check-in e Ingreso en pantalla, esqueletos en cada vista.
- **Pantallas de administración existentes** (`GestionUsuarios`, `PuntosAcceso`, `CrearEvento`, `EditarEvento`, `MisActividades`, `InscritosDeSesion`, `QrDeSesion`, `ConfiguracionSistema`, `IndicadorUsuario`): adoptar `EstadoDeCarga` con esqueleto, `InsigniaEstado`, tablas adaptables y confirmaciones con consecuencia donde falten.
- **Estados de interacción:** recorrer con teclado cada pantalla nueva (Tab, Enter, Esc) y corregir foco atrapado u olvidado.
- **E2E:** actualizar `docs/checklist-e2e.md` con todos los flujos nuevos y ejecutarlo contra backend real con los datos de demostración (B8).
- **Republicar el sistema de diseño** (artefacto de Claude Design) con las insignias de tinta suave y los componentes nuevos, si el usuario lo pide.
**DoD:** checklist E2E 100 % marcado o con defectos documentados; auditoría de accesibilidad manual hecha ([02](02-pruebas-en-dispositivos-y-accesibilidad.md)).

## F10 · Diseños diferidos (RF-041, RF-042)

Solo **documentación**: bocetos de pantalla (en texto) para "Solicitar revisión" y "Mis datos", alineados con `fitnessbackend/docs/plan-despliegue/06-diseno-apelacion-y-derechos-titular.md`. Verificar en papel que **ninguna** pantalla propuesta rompe RN-020. No se escribe código.

---

## Criterio de cierre del plan (frontend)

- [ ] F0 a F9 cerrados con DoD y aprobación del revisor; F10 documentado.
- [ ] `dotnet test` en verde; `AusenciaPriorizacionRN020Tests` cubre todas las pantallas nuevas.
- [ ] Ninguna pantalla muestra posición en lista de espera, nivel, penalización ni fecha de apertura (RN-020, AMB-014).
- [ ] `grep` de colores y fuentes sueltos fuera de `itm-tema.css`: sin resultados nuevos.
- [ ] Responsive verificado en 390, 768 y 1280 px en todas las pantallas nuevas.
- [ ] `ApiCliente/` regenerado con Kiota tras cada contrato (nunca editado a mano) y `kiota-lock.json` actualizado.
- [ ] `CHANGELOG.md` y `docs/checklist-e2e.md` actualizados.
