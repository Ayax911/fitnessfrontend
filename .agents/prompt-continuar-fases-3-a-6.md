# Prompt para Antigravity — completar fitnessfrontend (Fases 3 a 6)

> Pegar en Antigravity con el workspace en `fitnessfrontend/`, agente `constructor-fitnessfrontend` seleccionado con `/agents`.
> Contexto: las Fases 0 (Cimientos), 1 (Eventos y navegación por rol) y 2 (Reservas) ya están construidas y con sus pruebas — verificado: existen `Login.razor`, `Registro.razor`, `ListaEventos.razor`, `MisActividades.razor`, `MisReservas.razor`, `InscritosDeSesion.razor`, y pruebas de `EstadoDeCarga`, `ZonaHorariaService`, `JwtTokenParser`, ausencia de campos de priorización (RN-020), AMB-014, navegación por rol y pertenencia. Faltan las Fases 3, 4, 5 y 6.

---

Vas a continuar la implementación de fitnessfrontend desde donde quedó. Las Fases 0, 1 y 2 ya están hechas — **no las reescribas, no las "mejores" por tu cuenta.** Si crees que algo de esas fases tiene un defecto real, dímelo antes de tocarlo.

## Antes de continuar, vuelve a leer (aunque ya lo hayas leído antes)

1. `AGENTS.md` — las ocho reglas no negociables.
2. `docs/requisitos/01-ambiguedades-ui.md` — en particular AMB-014, ya implementada en la Fase 2; las fases que siguen tienen sus propias ambigüedades (Bloque 2, pregunta 9 para el check-in; AMB-013 para la configuración).
3. `docs/arquitectura/08-guia-de-implementacion.md` — este es tu plan exacto para lo que falta: la tabla de cobertura RF → fase → pantalla, y la Definición de Hecho completa de las Fases 3, 4, 5 y 6.
4. `docs/arquitectura/06-flujo-checkin-qr.md` — lectura obligatoria completa antes de tocar la Fase 3; es el flujo más delicado de todo el repositorio.
5. `../fitnessbackend/docs/arquitectura/07-contrato-api.md` — confirma los endpoints de Asistencia y Priorización contra el `/openapi/v1.json` real del backend en ejecución, no de memoria.

## Lo que NO debes tocar

Válido y no se reescribe: `Paginas/Identidad/`, `Paginas/Eventos/ListaEventos.razor` y `MisActividades.razor`, `Paginas/Reservas/`, `Servicios/Identidad/`, `Servicios/Eventos/`, `Servicios/Reservas/`, `Servicios/Infraestructura/` (incluye `EstadoDeCarga`, `IZonaHorariaService`, `ManejadorDeAutenticacionHttp`, `ILocalStorageService`), `Componentes/Compartidos/`, y todas las pruebas ya existentes en `tests/FitnessFrontend.Tests/`. Si necesitas extender alguno de estos (por ejemplo, `IReservasClienteService` para que `Asistencia` pueda consultarlo — ver Patrón 1 del documento 04), hazlo por composición, no reescribiendo lo que ya funciona.

## Tarea 1 — Fase 3: Asistencia y check-in por QR

Implementa **RF-005, RF-006, RF-012, RF-021** completos, siguiendo `docs/arquitectura/06-flujo-checkin-qr.md` al pie de la letra:

- `Paginas/Asistencia/CheckIn.razor` (ruta `/checkin/{Token}`) — implementa los **tres casos exactos** del documento 06: (A) con sesión iniciada, completa el check-in; (B) sin sesión iniciada, guarda el `tokenSesion` y redirige a login con `returnUrl`, volviendo automáticamente a `/checkin/{token}` tras autenticarse **sin que el usuario vuelva a escanear**; (C) token inválido o ventana cerrada, muestra el error del backend y **nunca** una pantalla de login.
- `Paginas/Asistencia/Ingreso.razor` (ruta `/ingreso/{IdPuntoAcceso}`) — RF-006, respeta el rechazo por intervalo mínimo (RN-030) mostrando el mensaje del backend, sin inventar una cuenta regresiva propia.
- `Paginas/Asistencia/QrDeSesion.razor` (Entrenador) — RF-012, construye la URL completa `https://<dominio-frontend>/checkin/{token}` y la pasa a un componente puro nuevo `Componentes/Compartidos/CodigoQr.razor` (Patrón 6: sabe dibujar un QR a partir de un string, nada de asistencia). Muestra la ventana de validez convertida con `IZonaHorariaService`.
- `Paginas/Asistencia/MiHistorial.razor` (Usuario) — RF-021. **Verifica por inspección del tipo `HistorialAsistenciaDto` generado** que no existe ningún campo de priorización antes de darlo por cerrado.
- Nuevo `Servicios/Asistencia/IAsistenciaClienteService.cs` / `AsistenciaClienteService.cs` (Patrón 1) y `Servicios/Asistencia/IFlujoCheckInService.cs` / `FlujoCheckInService.cs` (Patrón 6 — la lógica de decisión de los tres casos, que debe ser pura y comprobable sin UI).

**Ambigüedad Bloque 2, pregunta 9** (qué mostrar si el usuario no tiene reserva confirmada): el backend responderá con su propio `Resultado.Fallo`; muéstralo tal cual, **no ofrezcas una acción de "reservar ahora"** desde esta pantalla — sería asumir que el problema es recuperable cuando podría no serlo.

**Pruebas obligatorias de esta fase** (ver skill `pruebas-obligatorias-fitnessfrontend`, sección Fase 3): los tres casos A/B/C como pruebas de componente (bUnit) separadas. La prueba del Caso B debe verificar la redirección automática de vuelta a `/checkin/{token}` tras login, no solo que el login funciona.

## Tarea 2 — Fase 4: Administración de eventos

Implementa **RF-007, RF-008** (Must Have) y **RF-009** (Should Have — primer candidato a diferir si el tiempo aprieta):

- `Paginas/Eventos/Administrar/CrearEvento.razor` — RF-007, exige explícitamente la **fecha de apertura de reservas** (RN-028) en el formulario, sin calcularla ni precargarla con un valor propio.
- Configuración de recurrencia (RF-008) **dentro del mismo formulario**: trata el patrón de periodicidad como **dato que el backend define**, no como una lista de opciones fija en el marcado — la enumeración completa de patrones sigue pendiente (Bloque 2), así que el formulario debe reflejar lo que el backend soporte hoy sin anticipar patrones no implementados.
- `Paginas/Eventos/Administrar/EditarEvento.razor` — RF-009. La confirmación de eliminar/editar **no promete ningún comportamiento sobre reservas existentes** (ver `docs/requisitos/01-ambiguedades-ui.md`); refleja después lo que el backend haya hecho.
- Extiende `Servicios/Eventos/IEventosClienteService.cs` con los métodos de creación/edición — no crees un servicio paralelo.

## Tarea 3 — Fase 5: Priorización (exclusivo Administrador)

Implementa **RF-016, RF-018**:

- `Paginas/Priorizacion/IndicadorUsuario.razor` y `Paginas/Priorizacion/ConfiguracionSistema.razor`, con `[Authorize(Roles = "Administrador")]` explícito en cada componente además del layout por rol.
- `ConfiguracionSistema.razor` muestra los valores que devuelve el backend **sin precargar valores por defecto inventados** (AMB-013).
- Nuevo `Servicios/Priorizacion/IPriorizacionClienteService.cs` / `PriorizacionClienteService.cs`.
- Aplica el color de módulo distintivo del documento 07 (morado, tomado de la Facultad de Artes y Humanidades) para reforzar visualmente que es una sección restringida.

**Prueba obligatoria:** un usuario sin rol Administrador que navega **escribiendo la URL directamente** a `/priorizacion/...` recibe el `403` del backend y la UI lo maneja con un mensaje — no se queda en blanco ni asume que ocultar el enlace del menú basta.

## Tarea 4 — Fase 6: Verificación transversal

No es funcionalidad nueva — es la pasada final sobre **todo** el repositorio (fases 0 a 5):

1. `grep` explícito de `NivelPriorizacion`, `IndicadorConfiabilidad` fuera de `Paginas/Priorizacion/` y sus servicios — cualquier aparición en Usuario/Entrenador es bloqueante.
2. Recorre la tabla de cobertura RF → fase → pantalla del documento 08 completa: cada `Must Have` con pantalla, y confirma que RF-014, RF-015, RF-017, RF-019, RF-022 **siguen sin tener ninguna pantalla** (son procesos del backend).
3. Confirma que todo `PENDIENTE` de `docs/requisitos/01-ambiguedades-ui.md` que aplica a las fases 3-5 está presente en el código donde corresponde.
4. `grep` de literales hex (`#[0-9a-fA-F]{3,8}`) y nombres de fuente fuera de `wwwroot/css/itm-tema.css`.
5. Verifica que el celeste de acento (`--itm-celeste-acento`) no se usó como color de texto sobre fondo blanco en ninguna pantalla nueva (no cumple AA: 2.5:1).
6. `dotnet build` sin advertencias nuevas y `dotnet test` en verde para **todo** el proyecto, no solo las fases nuevas.
7. Prueba manual, en un teléfono real si es posible, de CU-01 a CU-06 completos contra el backend desplegado — en particular CU-03 (check-in) y CU-04 (ingreso general), que son las que se agregan en esta ronda.

## Flujo de trabajo (igual que en las fases anteriores)

Al cerrar cada fase (3, 4, 5, y luego la 6 al final): `dotnet build` + `dotnet test` tú mismo, después invoca a `revisor-fitnessfrontend` con el número de fase y los archivos tocados. No avances con hallazgos bloqueantes abiertos. Si el revisor solo reporta sugerencias, puedes avanzar dejando constancia de por qué no se aplicaron.

## Las tres cosas que más fácil vas a romper aquí

1. **Confundir el Caso B con el Caso C del check-in.** Ambos son "no funcionó", pero uno se arregla iniciando sesión y el otro no. Es la prueba obligatoria más importante de esta ronda.
2. **"Mejorar" AMB-014 o la pregunta del Bloque 2 con un mensaje más explicativo.** No lo hagas — ya está resuelto de la forma menos comprometedora posible; un mensaje más informativo es exactamente la violación de RN-020 que se evitó.
3. **Poner la recurrencia como enum cerrado en el marcado de `CrearEvento.razor`.** El backend puede añadir patrones nuevos; si la UI los codifica como opciones fijas, vas a tener que volver a tocar el frontend cada vez.

Cuando termines, dime qué fases quedaron cerradas, qué reportó el revisor en cada una, qué pruebas corren, y qué `PENDIENTE` quedaron marcados en el código (deberían ser al menos: AMB-014 ya existente de la Fase 2, el de Bloque 2/pregunta 9 de la Fase 3, y el de las reservas existentes al editar/eliminar evento de la Fase 4).
