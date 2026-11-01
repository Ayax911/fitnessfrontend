# Auditoría Final Independiente — ITM Fitness

> **Fecha**: 2026-09-28  
> **Auditor**: Modelo independiente (no participó en construcción ni revisiones de fase)  
> **Modo**: Solo lectura — este informe reporta, no corrige  
> **Alcance**: `fitnessbackend/` (API .NET 10) + `fitnessfrontend/` (Blazor WebAssembly Standalone)

---

## Resumen Ejecutivo

**El sistema NO es seguro de desplegar tal cual.** Se identificaron **8 hallazgos bloqueantes** que requieren corrección antes de cualquier despliegue, incluyendo una fuga de seguridad del token QR a usuarios no autorizados, condiciones de carrera que permiten sobreventa de cupos y reservas duplicadas, una consolidación no atómica que puede corromper indicadores de priorización de forma irreversible, y un desfase de zona horaria que desplaza todas las sesiones recurrentes 5 horas.

| Severidad | Cantidad |
|-----------|----------|
| 🔴 BLOQUEANTE | 8 |
| 🟠 IMPORTANTE | 13 |
| 🟡 MENOR | 9 |
| **Total** | **30** |

La arquitectura general es sólida: separación modular clara, DTOs de confidencialidad bien aislados (RN-020), máquina de estados de reservas robusta, y buena cobertura de trazabilidad (173 comentarios RN/RF en backend, 48 en frontend, 100% de IDs válidos). Los problemas son de implementación, no de diseño.

---

## Hallazgos Bloqueantes

### B-01 · Fuga del TokenQR en DTO público de listado de eventos
- **Archivo**: [`SesionResumenDto.cs:16`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Eventos/Dtos/SesionResumenDto.cs#L16) y [`EventoService.cs:131`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Eventos/Servicios/EventoService.cs#L131)
- **Secciones**: 6
- **Descripción**: `SesionResumenDto` contiene la propiedad `string TokenQR`. En `EventoService.ObtenerEventosDisponiblesAsync`, se asigna directamente `s.TokenQR` para todas las sesiones. Este endpoint (`GET /api/eventos`) está expuesto a **cualquier usuario autenticado** — incluyendo el rol `Usuario`. Viola directamente RN-022, RF-012 y RNF-004: el código QR debe ser generado y presentado exclusivamente por el entrenador a cargo durante la sesión.
- **Escenario**: Cualquier usuario consulta `GET /api/eventos`, inspecciona el JSON, extrae el `TokenQR`, y realiza auto check-in fraudulento desde su casa ejecutando `POST /api/asistencia/checkin/{token}` sin asistir al gimnasio.
- **Recomendación**: Eliminar `TokenQR` de `SesionResumenDto` o devolver `string.Empty` para roles no-Entrenador/Administrador. La consulta del token debe restringirse al endpoint autenticado `GET /api/asistencia/sesiones/{id}/qr`.

---

### B-02 · Desfase de zona horaria en generación de sesiones recurrentes (+5h)
- **Archivo**: [`GeneradorDeSesiones.cs:67-75`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Eventos/Servicios/GeneradorDeSesiones.cs#L67-L75)
- **Secciones**: 1, 5
- **Descripción**: Dos fallas:
  1. **Línea 67**: Usa `FechaAperturaReservas.UtcDateTime` como fecha de inicio de la recurrencia (debería ser la fecha de la primera clase, no la apertura de inscripciones).
  2. **Líneas 74-75**: Construye `DateTimeOffset` con `TimeSpan.Zero` (UTC), pero las horas de inicio/fin capturadas en el formulario son hora local Colombia (UTC-5). Resultado: las clases se almacenan como si 07:00 AM local fuera 07:00 UTC.
- **Escenario**: Admin programa clase recurrente para lunes 07:00 AM → Backend almacena `07:00 UTC` → Frontend convierte a local → Usuario ve la clase a las **02:00 AM**. Check-in a las 07:00 AM real es rechazado por `FueraDeVentana` (la sesión "terminó" hace 4 horas en UTC).
- **Recomendación**: Convertir las horas locales a UTC reconociendo `America/Bogota` (UTC-5) antes de almacenar, y desacoplar la fecha de inicio de recurrencia de `FechaAperturaReservas`.

---

### B-03 · Enum NivelPriorizacion desfasado en interfaz de Administrador
- **Archivo**: [`IndicadorUsuario.razor:59-81`](file:///home/akira/snap/steam/ITM/fitnessfrontend/src/FitnessFrontend/Paginas/Priorizacion/IndicadorUsuario.razor#L59-L81) vs [`NivelPriorizacion.cs:4-10`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Priorizacion/Entidades/NivelPriorizacion.cs#L4-L10)
- **Secciones**: 1, 3
- **Descripción**: El enum del backend define `Nuevo=0, Prioritario=1, Normal=2, Penalizado=3`. El switch en el frontend mapea `case 0: Prioritario`, `case 1: Normal`, `case 2: Penalizado`, omitiendo `Nuevo` por completo. Desfase de +1 en todos los niveles.
- **Escenario**: Usuario nuevo (Nivel 0) → Admin ve "Prioritario" ⭐. Usuario Normal (Nivel 2) → Admin ve "Penalizado" 🔴. Usuario Penalizado (Nivel 3) → cae en `default`, muestra "Nivel 3" genérico. Toda la auditoría administrativa de priorización es incorrecta.
- **Recomendación**: Corregir el switch: `case 0: Nuevo`, `case 1: Prioritario`, `case 2: Normal`, `case 3: Penalizado`.
- **Estado:** ✅ Corregido en Ronda 1

---

### B-04 · Sobreventa de cupos por carrera entre cancelación/nueva reserva/promoción
- **Archivo**: [`ReservaService.cs:177-183`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Reservas/Servicios/ReservaService.cs#L177-L183) y [`PromoverListaEsperaAlLiberarCupo.cs:58-65`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Reservas/ManejadoresEventos/PromoverListaEsperaAlLiberarCupo.cs#L58-L65)
- **Secciones**: 2
- **Descripción**: Cuando un usuario cancela, un nuevo usuario puede tomar el cupo liberado antes de que el evento de dominio `CupoLiberadoEvent` promueva al primero en lista de espera. El manejador `PromoverListaEsperaAlLiberarCupo` **no valida cupo máximo** ni toca `xmin` de `SesionEvento`, así que promueve incondicionalmente → la sesión termina con **CupoMaximo + 1** confirmados.
- **Escenario**: Sesión 10/10 cupos, 2 en espera → Usuario cancela → Nuevo usuario reserva (9 < 10, OK) → Manejador promueve primer esperador → 11 confirmados para 10 cupos.
- **Recomendación**: (a) En `ReservarAsync`: si hay usuarios en `EnListaEspera`, nueva reserva debe ir a cola obligatoriamente. (b) En `PromoverListaEsperaAlLiberarCupo`: validar `confirmadas < CupoMaximo` y tocar `xmin` para competir bajo bloqueo optimista.

---

### B-05 · Reservas duplicadas por TOCTOU sin índice único en BD
- **Archivo**: [`ReservaService.cs:39-48`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Reservas/Servicios/ReservaService.cs#L39-L48) y [`ApplicationDbContext.cs:75-91`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Infraestructura/Datos/ApplicationDbContext.cs#L75-L91)
- **Secciones**: 2
- **Descripción**: La verificación de reserva previa (`reservaActivaExistente`) ocurre **fuera** del ciclo de reintento optimista sobre `xmin`. Dos peticiones simultáneas del mismo usuario ambas ven `false`. Al reintentar por `DbUpdateConcurrencyException`, la segunda petición **no revuelve a verificar** duplicados. La tabla `Reservas` no tiene índice único sobre `(UsuarioId, SesionEventoId)`.
- **Escenario**: Doble clic en "Reservar" → ambas peticiones pasan validación → usuario termina con 2 reservas activas o 2 puestos en lista de espera.
- **Recomendación**: (a) Agregar índice único filtrado: `HasIndex(r => new { r.UsuarioId, r.SesionEventoId }).HasFilter("Estado IN ('Confirmada','EnListaEspera')").IsUnique()`. (b) Mover validación de duplicados dentro del ciclo de reintento.

---

### B-06 · Consolidación no atómica ni idempotente — corrupción irreversible de indicadores
- **Archivo**: [`ConsolidacionSesionBackgroundService.cs:33-43`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Asistencia/Trabajos/ConsolidacionSesionBackgroundService.cs#L33-L43) y [`AsistenciaService.cs:257-294`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Asistencia/Servicios/AsistenciaService.cs#L257-L294)
- **Secciones**: 2, 3
- **Descripción**: Tres problemas graves:
  1. **Sin transacción envolvente**: `ConsolidarReservasDeSesionAsync` hace `SaveChanges`, luego `MarcarSesionComoConsolidadaAsync` hace otro `SaveChanges`, luego el evento de dominio hace N `SaveChanges` por usuario. Si el proceso muere entre pasos 1 y 2, la sesión queda `Consolidada = false` pero las reservas ya se cambiaron de estado → al reiniciar, la reconsolidación encuentra 0 reservas pendientes → los indicadores de priorización quedan permanentemente perdidos.
  2. **Sin exclusión mutua**: Si hay múltiples réplicas del backend, ambas ejecutan el `BackgroundService` sin candado distribuido → contadores de asistencia/inasistencia se incrementan ciegamente múltiples veces.
  3. **`RegistrarResultadoSesionAsync` no es idempotente**: Incrementa contadores (`TotalSesionesAsistidas++`, `TotalInasistencias++`) sin verificar si ya procesó esa sesión para ese usuario.
- **Escenario**: Servidor reinicia a mitad de consolidación de sesión con 30 inscritos → indicadores de esos 30 usuarios quedan permanentemente desincronizados → penalizaciones o privilegios injustos irreversibles.
- **Recomendación**: (a) Envolver cada consolidación en `BeginTransactionAsync`/`CommitAsync`. (b) Implementar `pg_try_advisory_lock` o `SELECT FOR UPDATE SKIP LOCKED`. (c) Hacer idempotente `RegistrarResultadoSesionAsync` guardando `(SesionEventoId, UsuarioId)` ya procesados.

---

### B-07 · Mapeo incorrecto de errores de validación → 409 en vez de 400
- **Archivo**: [`ResultadoHttpExtensions.cs:53-59`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Infraestructura/Resultados/ResultadoHttpExtensions.cs#L53-L59)
- **Secciones**: 4
- **Descripción**: `MapFallo` mapea a `400 Bad Request` solo si el código es exactamente `"Invalido"`, `"DatosInvalidos"`, `"ArgumentoInvalido"` o `"PuntoInvalido"`. Códigos como `"NombreRequerido"`, `"CupoInvalido"`, `"TokenInvalido"`, `"ErrorGeneracionSesiones"` caen al `else` por defecto → `409 Conflict`. Contradice el contrato OpenAPI y `07-contrato-api.md`.
- **Escenario**: `POST /api/eventos` sin nombre → backend devuelve `409 Conflict` con `NombreRequerido` → Kiota genera `ApiException` genérica (código 409 no declarado en OpenAPI) → frontend muestra "Error al crear el evento (409)" sin mensaje explicativo. El contrato declara `400` para validación.
- **Recomendación**: Extender la condición de 400: `codigo.EndsWith("Invalido") || codigo.EndsWith("Requerido")`. Considerar usar un `TipoResultadoError` enum en lugar de strings arbitrarias.

---

### B-08 · RN-030 violado: intervalo mínimo de ingreso devuelve éxito en vez de fallo
- **Archivo**: [`AsistenciaService.cs:113-117`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Asistencia/Servicios/AsistenciaService.cs#L113-L117)
- **Secciones**: 1
- **Descripción**: Cuando un usuario escanea el QR de un torniquete dentro del intervalo mínimo (`TotalMinutes < intervaloMin`), el código retorna `Resultado.Exito(new CheckInResultadoDto(true, "Ingreso general ya registrado recientemente..."))` — con `Exitoso = true`. El endpoint responde `200 OK` y el frontend muestra "¡Ingreso Autorizado!" con icono verde.
- **Escenario**: Parámetro `IntervaloMinimoCheckInGeneralMinutos = 120`. Usuario ingresa a las 10:00. A las 10:15 vuelve a escanear → Backend dice éxito → Torniquete muestra acceso autorizado → La regla anti-abuso RN-030 queda completamente anulada.
- **Recomendación**: Retornar `Resultado.Fallo("IntervaloMinimoNoCumplido", "Debe esperar al menos {intervaloMin} minutos...")` para que el endpoint emita `400/409` y el frontend muestre rechazo.

---

## Hallazgos Importantes

### I-01 · Eventos de dominio fuera de transacción (contradicen arquitectura documentada)
- **Archivo**: [`DespachadorDeEventosDeDominio.cs:8`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Infraestructura/EventosDeDominio/DespachadorDeEventosDeDominio.cs#L8) y [`ReservaService.cs:177-183`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Reservas/Servicios/ReservaService.cs#L177-L183)
- **Secciones**: 2
- **Descripción**: Los eventos de dominio se despachan **después** de `SaveChangesAsync()` sin transacción circundante. Si el manejador falla (ej. error transitorio), la reserva queda cancelada pero la promoción de lista de espera no ocurre. El comentario en línea 8 dice *"despacho síncrono dentro de la misma transacción"* — es falso, `BeginTransactionAsync` no se usa en ningún punto del proyecto.
- **Recomendación**: Implementar Transactional Outbox o despachar antes de `CommitAsync` dentro de una `IDbContextTransaction`.

---

### I-02 · Falta de cota superior en ventana de reserva — reservas para sesiones pasadas
- **Archivo**: [`EvaluadorVentanaDeReserva.cs:23`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Reservas/Servicios/EvaluadorVentanaDeReserva.cs#L23) y [`ReservaService.cs:68-81`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Reservas/Servicios/ReservaService.cs#L68-L81)
- **Secciones**: 5
- **Descripción**: `EstaVentanaAbierta` solo valida `momentoActual >= aperturaUsuario` (cota inferior). `ReservaService.CrearReservaAsync` no verifica si la sesión ya inició (`FechaHoraInicio > ahoraUtc`) ni si está consolidada.
- **Escenario**: Un usuario envía reserva para una sesión de hace 3 semanas → `ahoraUtc >= aperturaUsuario` es true → reserva aceptada para sesión extemporánea.
- **Recomendación**: Validar `sesion.FechaHoraInicio > ahoraUtc && !sesion.Consolidada` antes de aceptar reservas.

---

### I-03 · Conversiones manuales de zona horaria fuera de ZonaHorariaService
- **Archivo**: [`CrearEvento.razor:201, 262, 287, 288`](file:///home/akira/snap/steam/ITM/fitnessfrontend/src/FitnessFrontend/Paginas/Eventos/Administrar/CrearEvento.razor#L201) y [`EditarEvento.razor:186`](file:///home/akira/snap/steam/ITM/fitnessfrontend/src/FitnessFrontend/Paginas/Eventos/Administrar/EditarEvento.razor#L186)
- **Secciones**: 5
- **Descripción**: Usa `DateTime.Today` (reloj del navegador) y `TimeSpan.FromHours(-5)` hardcodeado en vez de `IZonaHorariaService`. El servicio de zona horaria no ofrece un método `AUtc()`.
- **Escenario**: Un usuario con navegador en zona horaria diferente (intercambio, VPN) crea un evento con fecha desfasada.
- **Recomendación**: Extender `IZonaHorariaService` con `AUtc(DateTime local)` y `ObtenerFechaHoyLocal()`.
- **Estado:** ✅ Corregido en Ronda 4

---

### I-04 · Inconsistencia de valores por defecto entre seed y fallbacks en código
- **Archivo**: [`ApplicationDbContext.cs:146-151`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Infraestructura/Datos/ApplicationDbContext.cs#L146-L151), [`PriorizacionService.cs:49-50, 83`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Priorizacion/Servicios/PriorizacionService.cs#L49-L50), [`AsistenciaService.cs:106`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Asistencia/Servicios/AsistenciaService.cs#L106)
- **Secciones**: 5, 3
- **Descripción**: 
  - `DesplazamientoNivelNormalMinutos`: Seed=**60** vs Fallback=**120**.
  - `DesplazamientoNivelPenalizadoMinutos`: Seed=**120** vs Fallback=**1440** (24h).
  - `IntervaloMinimoCheckInGeneralMinutos`: Seed=**120** vs Fallback=**60**.
  - En `PriorizacionService:83`, ante config `null` se instancia `new ConfiguracionPriorizacion()` con todos los enteros en **0** → `UmbralInasistenciasPenalizacion = 0` → **todos** los usuarios (incluso con 0 inasistencias) se clasifican como Penalizado.
- **Recomendación**: Unificar en una clase `ConfiguracionPriorizacionDefaults`. Exigir `UmbralInasistenciasPenalizacion > 0`.

---

### I-05 · Trampa de penalización permanente — violación de RN-018
- **Archivo**: [`CalculadorNivelPriorizacion.cs:22-39`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Priorizacion/Servicios/CalculadorNivelPriorizacion.cs#L22-L39)
- **Secciones**: 3
- **Descripción**: RN-018 dice *"un usuario penalizado puede volver a un estado normal o prioritario"*. Pero `totalInasistencias` es un acumulador histórico que nunca decrementa. Una vez superado el umbral, la primera rama se evalúa `true` siempre → el usuario puede aspirar como máximo a `Normal` mediante racha, pero **jamás** a `Prioritario`, sin importar cuántas asistencias consecutivas acumule.
- **Escenario**: Usuario acumula 3 inasistencias (Penalizado), luego asiste 20 sesiones consecutivas → motor de cálculo lo deja en Normal eternamente, jamás recupera Prioritario.
- **Recomendación**: Evaluar `UmbralAsistenciasPrioritario` dentro de la rama de recuperación, permitiendo escalar a Prioritario con racha sobresaliente.

---

### I-06 · Resoluciones silenciosas de AMB-003, AMB-010, AMB-012 sin PENDIENTE en backend
- **Archivo**: [`ReservaService.cs:158`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Reservas/Servicios/ReservaService.cs#L158), [`ReservaEndpoints.cs:42`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Reservas/Endpoints/ReservaEndpoints.cs#L42), [`EventoService.cs:301`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Eventos/Servicios/EventoService.cs#L301)
- **Secciones**: 6
- **Descripción**: El backend resolvió silenciosamente AMB-003 (cancelación sin plazo), AMB-010 (entrenadores reservan como usuario) y AMB-012 (eliminación de evento = borrado lógico) sin comentarios `// PENDIENTE`. El frontend sí documenta estas mismas ambigüedades con `@* PENDIENTE *@`. Asimetría documental entre repos.
- **Recomendación**: Añadir `// PENDIENTE (AMB-003)`, `// PENDIENTE (AMB-010)` y `// PENDIENTE (AMB-012)` en los puntos correspondientes del backend.

---

### I-07 · Cupos disponibles hardcodeados = CupoMaximo en listado de eventos
- **Archivo**: [`EventoService.cs:126-128`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Eventos/Servicios/EventoService.cs#L126-L128)
- **Secciones**: 1, 6
- **Descripción**: Al mapear `SesionResumenDto`, `CuposOcupados` siempre es `0` y `CuposDisponibles` siempre es `CupoMaximo`, con comentario *"gestionado por IReservaService en Fase 2"*. Nunca se consultó el conteo real de reservas confirmadas.
- **Escenario**: Sesión con 15/15 cupos llenos → API reporta "15 cupos disponibles" → usuario intenta reservar → enviado a lista de espera contradictoriamente.
- **Recomendación**: Consultar `_db.Reservas.CountAsync(r => r.SesionEventoId == s.Id && r.Estado == EstadoReserva.Confirmada)` y calcular disponibilidad real.

---

### I-08 · Exposición de fecha de apertura de reservas contradice RN-020
- **Archivo**: [`ListaEventos.razor:72, 88, 128-135, 204-208`](file:///home/akira/snap/steam/ITM/fitnessfrontend/src/FitnessFrontend/Paginas/Eventos/ListaEventos.razor#L72)
- **Secciones**: 6
- **Descripción**: El texto de ventana cerrada ("Reservas aún no disponibles") es genuinamente genérico ✓. Pero (a) la fecha base de apertura se muestra públicamente, y (b) el botón "Reservar" está habilitado inicialmente → el usuario con nivel Penalizado ve la fecha pasada, intenta reservar, recibe el rechazo, y deduce lógicamente que tiene un bloqueo personal.
- **Recomendación**: Omitir la fecha de apertura o mostrar la fecha ajustada al nivel del usuario (requiere endpoint adicional). Deshabilitar el botón preventivamente si la ventana no está abierta para el usuario.
- **Estado:** ✅ Corregido en Ronda 1

---

### I-09 · Pérdida del flujo de ingreso por torniquete para no autenticados
- **Archivo**: [`Ingreso.razor:2-3`](file:///home/akira/snap/steam/ITM/fitnessfrontend/src/FitnessFrontend/Paginas/Asistencia/Ingreso.razor#L2-L3) y [`App.razor:18`](file:///home/akira/snap/steam/ITM/fitnessfrontend/src/FitnessFrontend/App.razor#L18)
- **Secciones**: 7
- **Descripción**: `Ingreso.razor` tiene `[Authorize]`. Cuando un usuario sin sesión escanea el QR del torniquete, `App.razor` renderiza `<Login />` inline pero la URL no incluye `?returnUrl=...`. Tras autenticarse, se redirige a `/eventos` → el ingreso nunca se registra.
- **Recomendación**: Redirigir a `/login?returnUrl={UriActual}` o implementar el patrón de `CheckIn.razor` (evaluar autenticación antes de requerir autorización).
- **Estado:** ✅ Corregido en Ronda 2

---

### I-10 · Metadatos OpenAPI incompletos degradan los clientes Kiota del frontend
- **Archivo**: [`ReservaEndpoints.cs:33-56, 73-86`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Reservas/Endpoints/ReservaEndpoints.cs#L33-L56) y [`AsistenciaEndpoints.cs:73-89`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Asistencia/Endpoints/AsistenciaEndpoints.cs#L73-L89)
- **Secciones**: 4
- **Descripción**: Múltiples endpoints no declaran sus códigos de error en `.Produces<ErrorDetalleDto>(...)`. Kiota solo deserializa `ErrorDetalleDto` para códigos declarados en OpenAPI → `catch (ErrorDetalleDto)` en el frontend es **código muerto** → usuario ve "Error al cancelar la reserva (404)" en vez del mensaje explicativo.
- **Recomendación**: Declarar todos los códigos de error posibles con `.Produces<ErrorDetalleDto>(Status4xxXxx)`.
- **Estado:** ✅ Corregido (2026-10-31): cliente Kiota regenerado contra el backend con las Rondas 1-5 y los endpoints nuevos (`/api/usuarios`, `/api/asistencia/puntos-acceso`, `/api/usuarios/yo`); ya no contiene `TokenQR` en `SesionResumenDto` (B-01).

---

### I-11 · EstadoDeCarga.razor — pantalla vacía cuando Valor es null
- **Archivo**: [`EstadoDeCarga.razor:41-45`](file:///home/akira/snap/steam/ITM/fitnessfrontend/src/FitnessFrontend/Componentes/Compartidos/EstadoDeCarga.razor#L41-L45)
- **Secciones**: 4
- **Descripción**: Si `EsExito == true` y `Valor == null`, ninguna rama del componente se activa → pantalla totalmente en blanco sin spinner, sin error, sin éxito.
- **Recomendación**: Cambiar condición a `else if (Estado.EsExito)` o agregar bloque `else` con mensaje para valores nulos.
- **Estado:** ✅ Corregido en Ronda 3

---

### I-12 · Violación de límites modulares: acoplamiento directo a entidades de Priorizacion
- **Archivo**: [`EventoService.cs:42-47`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Eventos/Servicios/EventoService.cs#L42-L47), [`AsistenciaService.cs:105`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Asistencia/Servicios/AsistenciaService.cs#L105), [`EvaluadorVentanaDeReserva.cs:2`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Reservas/Servicios/EvaluadorVentanaDeReserva.cs#L2), [`ComparadorOrdenListaEspera.cs:3`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Reservas/Servicios/ComparadorOrdenListaEspera.cs#L3)
- **Secciones**: 3
- **Descripción**: Los módulos `Eventos`, `Asistencia` y `Reservas` acceden directamente a `_db.ConfiguracionPriorizacion` y crean instancias de entidades de `Priorizacion`, violando las Reglas 2, 4 y 7 de `05-organizacion-directorios.md`.
- **Recomendación**: Obtener parámetros a través de `IPriorizacionService.ObtenerConfiguracionAsync()`. Mover `NivelPriorizacion` a un espacio compartido si es requerido como parámetro cruzado.

---

### I-13 · Adopción unilateral de valores de color en disputa sin homologación
- **Archivo**: [`itm-tema.css:8-19`](file:///home/akira/snap/steam/ITM/fitnessfrontend/src/FitnessFrontend/wwwroot/css/itm-tema.css#L8-L19)
- **Secciones**: 8
- **Descripción**: Tres valores de color documentados como *"en disputa"* en `07-identidad-visual-y-sistema-de-diseno.md` fueron adoptados como tokens definitivos sin homologación de la Dirección de Comunicaciones del ITM: azul primario (`#011C5B`, inferido de RGB), gris (`#C6C6C6`, una de dos lecturas contradictorias del manual), y celeste de acento (`#56ACDE`, ausente en el manual base). Solo el celeste tiene comentario `PENDIENTE`.
- **Recomendación**: Documentar la condición provisional con `/* PENDIENTE */` para azul y gris. Elevar consulta a Dirección de Comunicaciones.
- **Estado:** ✅ Corregido en Ronda 3

---

## Hallazgos Menores

### M-01 · Prueba de concurrencia no fuerza contención real (falso-positivo)
- **Archivo**: [`ReservaServiceConcurrenciaTests.cs:130-144`](file:///home/akira/snap/steam/ITM/fitnessbackend/tests/FitnessBackend.Tests/Integracion/ReservaServiceConcurrenciaTests.cs#L130-L144)
- **Descripción**: `Task.WhenAll(task1, task2)` sin barrera de sincronización. Una ejecución secuencial produce el mismo resultado. Si se elimina el mecanismo `xmin`, la prueba sigue pasando.
- **Recomendación**: Forzar traslape con `SemaphoreSlim` o `TaskCompletionSource`.

---

### M-02 · Falta de validación de entrada en ActualizarConfiguracionAsync
- **Archivo**: [`PriorizacionService.cs:176-196`](file:///home/akira/snap/steam/ITM/fitnessbackend/src/FitnessBackend/Modulos/Priorizacion/Servicios/PriorizacionService.cs#L176-L196)
- **Descripción**: Acepta valores negativos sin validación. Un admin puede configurar `DesplazamientoNivelPenalizadoMinutos = 30` < `DesplazamientoNivelNormalMinutos = 120`, invirtiendo la política punitiva.
- **Recomendación**: Validar `Penalizado > Normal > 0` y umbrales `> 0`.

---

### M-03 · Pruebas de RN-020 no verifican payload JSON serializado
- **Archivo**: [`VerificacionTransversalTests.cs:34-49`](file:///home/akira/snap/steam/ITM/fitnessbackend/tests/FitnessBackend.Tests/Unitarias/VerificacionTransversalTests.cs#L34-L49)
- **Descripción**: Verifican propiedades por reflexión pero no la serialización real JSON. Un `JsonExtensionData` podría filtrar campos sensibles sin ser detectado.
- **Recomendación**: Serializar DTOs a JSON y buscar cadenas prohibidas.

---

### M-04 · Ruta rota en menú de Administrador → /admin/priorizacion
- **Archivo**: [`NavMenuAdministrador.razor:14`](file:///home/akira/snap/steam/ITM/fitnessfrontend/src/FitnessFrontend/Layout/NavMenuAdministrador.razor#L14)
- **Descripción**: El enlace apunta a `/admin/priorizacion` pero no existe esa ruta. El componente define `/admin/priorizacion/usuarios`.
- **Recomendación**: Cambiar href a `admin/priorizacion/usuarios` o añadir `@page "/admin/priorizacion"` a `IndicadorUsuario.razor`.
- **Estado:** ✅ Corregido en Ronda 2

---

### M-05 · Pérdida de returnUrl al ir de Login a Registro
- **Archivo**: [`Login.razor:60`](file:///home/akira/snap/steam/ITM/fitnessfrontend/src/FitnessFrontend/Paginas/Identidad/Login.razor#L60) y [`Registro.razor:94`](file:///home/akira/snap/steam/ITM/fitnessfrontend/src/FitnessFrontend/Paginas/Identidad/Registro.razor#L94)
- **Descripción**: Al clicar "Registrarse" desde `/login?returnUrl=/checkin/{token}`, el parámetro `returnUrl` no se propaga a `/registro`. Tras completar registro → redirige a `/login` limpio → pierde el check-in.
- **Recomendación**: Propagar `returnUrl` en ambos enlaces.
- **Estado:** ✅ Corregido en Ronda 2

---

### M-06 · Falta de cobertura de tests para edge cases de ventana temporal
- **Archivo**: [`EvaluadorVentanaDeReservaTests.cs:10-46`](file:///home/akira/snap/steam/ITM/fitnessbackend/tests/FitnessBackend.Tests/Reservas/EvaluadorVentanaDeReservaTests.cs#L10-L46)
- **Descripción**: No prueba cruce de medianoche, cambio de mes, desplazamiento cero ni valores negativos.
- **Recomendación**: Añadir `[InlineData]` parametrizados.

---

### M-07 · Literal hexadecimal hardcodeado en hover de botón de priorización
- **Archivo**: [`itm-tema.css:213`](file:///home/akira/snap/steam/ITM/fitnessfrontend/src/FitnessFrontend/wwwroot/css/itm-tema.css#L213)
- **Descripción**: `.btn-itm-priorizacion:hover` usa `#520c68` directamente en vez de un token CSS.
- **Recomendación**: Usar `filter: brightness(0.85)` o declarar variable CSS.
- **Estado:** ✅ Corregido en Ronda 3

---

### M-08 · Estilos CSS inline en componentes Razor
- **Archivo**: [`MainLayout.razor:40`](file:///home/akira/snap/steam/ITM/fitnessfrontend/src/FitnessFrontend/Layout/MainLayout.razor#L40), [`EstadoDeCarga.razor:13`](file:///home/akira/snap/steam/ITM/fitnessfrontend/src/FitnessFrontend/Componentes/Compartidos/EstadoDeCarga.razor#L13), [`App.razor:23`](file:///home/akira/snap/steam/ITM/fitnessfrontend/src/FitnessFrontend/App.razor#L23)
- **Descripción**: Tres `style=` inline para dimensiones. Dificulta mantenimiento responsivo.
- **Recomendación**: Extraer a clases utilitarias en `itm-tema.css`.
- **Estado:** ✅ Corregido en Ronda 3

---

### M-09 · Discrepancias de estructura física de carpetas vs documentación
- **Archivo**: Carpetas `{ManejadoresDeEventos}` (con llaves), `ManejadoresDeEventos/` vacía, `Opciones/` en Notificaciones
- **Descripción**: Carpetas huérfanas, nombradas con caracteres especiales, y duplicadas que no coinciden con `05-organizacion-directorios.md`.
- **Recomendación**: Limpiar carpetas vacías/mal nombradas y unificar convención.

---

## Lo que SÍ está bien

> [!TIP]
> Las siguientes decisiones de diseño se sostienen bien bajo escrutinio y **no deben tocarse** durante las correcciones.

1. **Aislamiento estricto de confidencialidad (RN-020)**: Los DTOs expuestos a Usuario (`ReservaResumenDto`, `HistorialAsistenciaDto`) y Entrenador (`ReservaInscritoDto`, `ActividadAsignadaDto`) no contienen ninguna propiedad de priorización ni confiabilidad. El único DTO con esa información (`IndicadorAdministradorDto`) está protegido con `[Authorize(Roles = "Administrador")]`. Zero dynamic/JsonElement/Dictionary leaks en roles no-admin.

2. **Máquina de estados de reservas (Patrón 10)**: `EvaluadorTransicionReserva.cs` implementa una FSM pura y desacoplada que garantiza que ninguna reserva alcance estados terminales ilegales. Bien cubierta con tests.

3. **Trazabilidad ejemplar**: 173 comentarios `// RN-xxx` / `// RF-xxx` en backend y 48 en frontend. El 100% de los IDs citados existen en la documentación normativa. Las 14 pantallas del frontend tienen su cabecera `@* RF-xxx *@`.

4. **Cobertura de RF 100%**: Los 22 RF están implementados (20 Must Have + 2 Should Have). Los 5 RF internos (RF-014, RF-015, RF-017, RF-019, RF-022) correctamente ausentes de la interfaz.

5. **Flujo de check-in QR (Caso B y C)**: `CheckIn.razor` implementa correctamente los 3 casos documentados. El token se preserva en `returnUrl`, las pruebas verifican la redirección de vuelta tras login, y el Caso C nunca confunde error de token con falta de sesión.

6. **Identidad visual centralizada**: Cero literales hexadecimales y cero `font-family` fuera de `itm-tema.css` en todo el código Razor/C#. El celeste de acento (`#56ACDE`) nunca se usa como color de texto sobre fondo blanco — cumple WCAG AA.

7. **Accesibilidad estructural**: Skip links, labels asociados, `<th scope="col">`, `prefers-reduced-motion`, tipografías cargadas correctamente.

8. **Concurrencia optimista (mecanismo base)**: El uso de `xmin` como token de concurrencia en `SesionEvento` con reintento acotado (3 intentos) es correcto en principio — los problemas son de implementación periférica (TOCTOU, falta de validación en el manejador), no del mecanismo optimista en sí.

9. **Texto neutro de AMB-014**: El mensaje "Reservas aún no disponibles para esta sesión" es genuinamente genérico — no menciona nivel, motivo ni fecha estimada. Cumple RN-020. El problema periférico es la fecha de apertura visible (I-08), no el mensaje en sí.

10. **Resultado<T> como patrón**: El Railway-oriented programming con `Resultado`/`Resultado<T>` está bien implementado como patrón — el problema es el mapeo a HTTP (B-07), no la abstracción.
