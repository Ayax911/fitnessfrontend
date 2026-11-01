# 08. Guía de implementación y verificación

> Punto de entrada para el agente que va a programar este repositorio. Simétrico a [fitnessbackend/docs/arquitectura/06-guia-para-antigravity.md](../../../fitnessbackend/docs/arquitectura/06-guia-para-antigravity.md): dice **en qué orden construir**, **qué significa "terminado"** en cada paso, y **qué reglas de comportamiento** seguir para no divergir del diseño ya cerrado.

## Orden de lectura obligatorio

1. [docs/requisitos/00-indice.md](../requisitos/00-indice.md) — qué es responsabilidad del frontend y qué casos de uso pasan por esta interfaz.
2. [docs/requisitos/01-ambiguedades-ui.md](../requisitos/01-ambiguedades-ui.md) — **AMB-014 y las demás preguntas abiertas que son de interfaz.** No se diseña ninguna pantalla de reserva ni de check-in sin haber leído esto.
3. [fitnessbackend/docs/arquitectura/07-contrato-api.md](../../../fitnessbackend/docs/arquitectura/07-contrato-api.md) — el contrato exacto a consumir (endpoints, roles, mapeo `Resultado` → HTTP).
4. [02-stack-y-hosting.md](02-stack-y-hosting.md) y [05-organizacion-directorios.md](05-organizacion-directorios.md) — con qué y dónde se construye.
5. [04-patrones-de-diseno.md](04-patrones-de-diseno.md) y [06-flujo-checkin-qr.md](06-flujo-checkin-qr.md) — cómo se escribe el código, y el flujo más delicado en detalle.
6. [07-identidad-visual-y-sistema-de-diseno.md](07-identidad-visual-y-sistema-de-diseno.md) — antes de escribir cualquier CSS o marcado visual.
7. Este documento.

Cuando surja una duda sobre un comportamiento concreto, el orden de consulta es: **la ficha del RF en [05-requisitos-funcionales.md](../../../fitnessbackend/docs/requisitos/05-requisitos-funcionales.md) del backend → su criterio de aceptación en [12-criterios-aceptacion.md](../../../fitnessbackend/docs/requisitos/12-criterios-aceptacion.md) → si sigue sin estar claro, [01-ambiguedades-ui.md](../requisitos/01-ambiguedades-ui.md) de este repositorio y [13-ambiguedades-inconsistencias.md](../../../fitnessbackend/docs/requisitos/13-ambiguedades-inconsistencias.md) del backend** para confirmar si ya es una ambigüedad conocida.

## Reglas de comportamiento

1. **Nunca reimplementar una regla de negocio en el cliente.** Si una pantalla necesita saber si algo "está permitido", se lo pregunta al backend y muestra su respuesta — jamás replica RN-001/RN-025/RN-028 con una condición local, que se desincronizaría del backend sin que nada falle visiblemente.
2. **No resolver unilateralmente una ambigüedad abierta.** Implementar el comportamiento menos comprometedor definido en [01-ambiguedades-ui.md](../requisitos/01-ambiguedades-ui.md) y marcarlo con `@* PENDIENTE (AMB-014) *@` o `// PENDIENTE (Bloque 2, pregunta 9)`. Nunca implementar una interpretación en silencio como si estuviera confirmada.
3. **Toda pantalla lleva el ID de su RF de origen en un comentario** al inicio del componente (`@* RF-020: consultar sus propias reservas *@`), igual que el backend exige en sus servicios. Es lo que hace verificable la cobertura de la tabla de abajo.
4. **Ningún componente de Usuario o Entrenador puede renderizar nivel de priorización ni indicador de confiabilidad** (RN-020). Es un hallazgo bloqueante de seguridad, no una sugerencia de estilo.
5. **Ningún color ni fuente fuera de los tokens** de [07-identidad-visual-y-sistema-de-diseno.md](07-identidad-visual-y-sistema-de-diseno.md). Si hace falta un valor nuevo, se verifica primero contra el Manual de Imagen ITM antes de introducirlo — y si el manual no lo define, se trata como pendiente, no se inventa.
6. **El cliente HTTP (`ApiCliente/`) se regenera, no se edita.** Si el contrato del backend cambia, se corre Kiota de nuevo.
7. **Toda fecha se muestra convertida por `IZonaHorariaService`** (Patrón 3). Ningún `.ToLocalTime()` disperso: el backend entrega todo en UTC y este repositorio es el único lugar del sistema donde se convierte a `America/Bogota`.
8. **No editar los documentos de `docs/`** (ni los de este repositorio ni los del backend) para que encajen con el código ya escrito. Si el diseño tiene un error, reportarlo.

---

## Cobertura de requisitos funcionales — a qué fase pertenece cada RF

Esta tabla existe porque la revisión del backend del 2026-09-26 descubrió que tres RF `Must Have` (RF-010, RF-011, RF-012) no estaban asignados a ningún módulo ni fase y por eso no se construyeron. **La verificación equivalente en este repositorio es esta tabla**, y se recorre completa antes de dar el prototipo por cerrado.

| RF | Prioridad | Actor | Fase | Pantalla / componente |
|---|---|---|---|---|
| RF-013 | Must | Todos | **Fase 0** | `Login.razor`, `Registro.razor` |
| RF-001 | Must | Todos | **Fase 1** | `ListaEventos.razor` |
| RF-010 | Must | Entrenador | **Fase 1** | `MisActividades.razor` |
| RF-002 | Must | Usuario | **Fase 2** | acción de reservar en `ListaEventos.razor` — sujeta a AMB-014 |
| RF-003 | Must | Usuario | **Fase 2** | resultado de lista de espera de la misma acción |
| RF-004 | Must | Usuario | **Fase 2** | `MisReservas.razor` |
| RF-020 | Must | Usuario | **Fase 2** | `MisReservas.razor` |
| RF-011 | Must | Entrenador | **Fase 2** | `InscritosDeSesion.razor` |
| RF-005 | Must | Usuario | **Fase 3** | `CheckIn.razor` |
| RF-006 | Must | Usuario | **Fase 3** | `Ingreso.razor` |
| RF-012 | Must | Entrenador | **Fase 3** | `QrDeSesion.razor` |
| RF-021 | Must | Usuario | **Fase 3** | `MiHistorial.razor` |
| **RF-007** | **Must** | **Administrador** | **Fase 4** | **`Paginas/Eventos/Administrar/CrearEvento.razor`** |
| **RF-008** | **Must** | **Administrador** | **Fase 4** | **configuración de recurrencia dentro del formulario de RF-007** |
| **RF-009** | **Should** | **Administrador** | **Fase 4** | **`Paginas/Eventos/Administrar/EditarEvento.razor`** |
| RF-025 | Must | Usuario | **Fase 0** (ampliación 2026-10-31) | `Registro.razor` — nombre, tipo de usuario y autorización de tratamiento de datos |
| RF-023 | Must | Administrador | **Fase 4** (ampliación 2026-10-31) | `Paginas/Usuarios/GestionUsuarios.razor`; selector de entrenador en `CrearEvento.razor`/`EditarEvento.razor`; búsqueda en `IndicadorUsuario.razor` |
| RF-024 | Must | Administrador | **Fase 4** (ampliación 2026-10-31) | `Paginas/Asistencia/Administrar/PuntosAcceso.razor` (lista, alta, activar/desactivar, QR imprimible) |
| RF-016 | Must | Administrador | **Fase 5** | `IndicadorUsuario.razor` |
| RF-018 | Must | Administrador | **Fase 5** | `ConfiguracionSistema.razor` |
| RF-026 | Must | Todos | **F1** | `ManejadorDeAutenticacionHttp.cs`, `AutenticacionService` (renovación de sesión de un solo vuelo) |
| RF-027 | Must | Entrenador, Administrador | **F3** | `Paginas/Asistencia/AsistenciaManual.razor` |
| RF-028 | Must | Administrador | **F4** | `Paginas/Eventos/Administrar/AdministrarSesion.razor` (cancelar/reprogramar) y `MisReservas.razor` (lado usuario) |
| RF-029 | Should | Entrenador, Administrador | **F3** (lado usuario) / **F4** (admin/entrenador) | Anuncios en `AdministrarSesion.razor` y consulta en `MisReservas.razor`/detalle |
| RF-030 | Should | Administrador | **F4** | `Paginas/Eventos/Administrar/AdministrarSesion.razor` (reservas de terceros) |
| RF-031 | Should | Administrador / Todos | **F4** (gestión) / **F2** (consulta) | `Paginas/Eventos/Administrar/BloqueosCalendario.razor` y franjas en `Programacion.razor` |
| RF-032 | Must | Administrador | **F4** | `Paginas/Auditoria/RegistroAuditoria.razor` |
| RF-033 | Must | Administrador | **F5** | `Paginas/Reportes/Reportes.razor` |
| RF-034 | Could | Entrenador, Administrador | **F3** | `Paginas/Eventos/NotasDeSesion.razor` |
| RF-035 | Should | Autenticados | **F2** | `Paginas/Eventos/Programacion.razor` |
| RF-036 | Could | Usuario | **F2** | Botón .ics en `MisReservas.razor` (`Servicios/Calendario/GeneradorIcs.cs`) |
| RF-037 | Could | Usuario | **F2** | Contador agregado en `ListaEventos.razor` y `Programacion.razor` (sin posición personal) |
| RF-038 | Should | Todos / Administrador | **F2** (ayuda) / **F7** (reportar) | `Paginas/Ayuda/Ayuda.razor`, `Paginas/Retroalimentacion/Reportar.razor`, `MisReportes.razor`, `BandejaAdministrador.razor` |
| RF-039 | Must | Administrador (pantalla), Usuario (escaneo) | **F6** | `Paginas/Asistencia/Administrar/PantallaPuntoAcceso.razor`, `Ingreso.razor` (QrVencido), `PuntosAcceso.razor` |

### RF que deliberadamente NO tienen interfaz en este repositorio

Verificar que estén ausentes es tan parte de la cobertura como verificar que los demás estén presentes — si alguno de estos aparece como pantalla, es un defecto (lógica de negocio migrada al cliente, violando la regla de comportamiento 1):

| RF | Por qué no tiene UI |
|---|---|
| RF-014 | Notificación por correo — la envía el backend; el usuario la recibe fuera de la aplicación |
| RF-015 | Cálculo del indicador de confiabilidad — proceso interno, y su resultado es invisible para Usuario/Entrenador por RN-020 |
| RF-017 | Aplicación de la ventana escalonada — es una **validación del backend**; la UI solo refleja su rechazo (y ahí entra AMB-014) |
| RF-019 | Promoción automática desde lista de espera — proceso del backend (CU-07); la UI solo muestra el estado ya cambiado |
| RF-022 | Consolidación de la sesión — `BackgroundService` del backend (CU-08) |
| RF-041 | Solicitud de revisión de priorización — diferido a diseño en F10; sin pantallas en este prototipo |
| RF-042 | Derechos del titular de datos (Ley 1581) — diferido a diseño en F10; sin pantallas en este prototipo |

---

## Fases de implementación

### Fase 0 — Cimientos

**Alcance:** proyecto Blazor WASM standalone, tema visual institucional (`itm-tema.css` con los tokens del documento 07 sobre la estructura de Kinetic), cliente Kiota generado contra el backend en desarrollo, `AuthenticationStateProvider` + `ManejadorDeAutenticacionHttp` (Patrón 7), `EstadoDeCarga<T>` (Patrón 2), `IZonaHorariaService` (Patrón 3). Cubre **RF-013**.

**Dependencia de arranque:** requiere una instancia del backend corriendo en desarrollo que exponga `/openapi/v1.json` (basta el `docker compose` del propio backend). Ver [02-stack-y-hosting.md](02-stack-y-hosting.md).

**DoD:**
- `Registro` seguido de `Login` (RF-013) obtiene un bearer token y lo persiste; al recargar la página la sesión sigue activa.
- Una llamada autenticada que devuelve `401` limpia el token y redirige a login, sin dejar la app en un estado "a medias" donde unas llamadas funcionan y otras no.
- La pantalla de login ya usa la paleta y tipografía institucionales, no las de Kinetic original — verificación visual directa contra el documento 07.
- `EstadoDeCarga<T>` e `IZonaHorariaService` existen y tienen al menos una prueba unitaria cada uno.

### Fase 1 — Eventos y navegación por rol

**Alcance:** **RF-001, RF-010.** Layout y menú por rol (Patrón 4).

**DoD:**
- Un Usuario ve el listado de eventos con horario, entrenador y cupos disponibles (criterio de aceptación de RF-001), con las fechas ya convertidas a `America/Bogota`.
- **Prueba obligatoria de pertenencia:** con dos entrenadores de prueba, cada uno ve en `MisActividades.razor` únicamente sus propias actividades (RF-010, RN-007) — espeja la prueba equivalente de la Fase 1 del backend.
- Ningún enlace a una pantalla de otro rol es visible ni alcanzable desde el menú del rol actual.

### Fase 2 — Reservas

**Alcance:** **RF-002, RF-003, RF-004, RF-011, RF-020.**

**DoD:**
- Reservar una sesión con cupo confirma la reserva; sin cupo, la UI comunica la ubicación en lista de espera con el mensaje del backend (RF-002/RF-003) — no con un texto inventado.
- `MisReservas.razor` muestra únicamente las reservas propias, con su estado, y permite cancelar (RF-004, RF-020).
- `InscritosDeSesion.razor` renderiza `ReservaInscritoDto` **sin** ningún campo de priorización — verificado por **inspección del tipo consumido**, no solo mirando la pantalla (mismo criterio que la Fase 6 del backend).
- **Verificación obligatoria de AMB-014:** una sesión cuya ventana de reserva aún no abre para el usuario de prueba muestra el comportamiento menos comprometedor definido en [01-ambiguedades-ui.md](../requisitos/01-ambiguedades-ui.md), con su marcador `PENDIENTE` presente en el código, y **sin** mencionar nivel, motivo ni fecha estimada.
- CU-01 y CU-02 ejecutables de punta a punta contra el backend real.

### Fase 3 — Asistencia y check-in por QR

**Alcance:** **RF-005, RF-006, RF-012, RF-021.** Implementación completa de [06-flujo-checkin-qr.md](06-flujo-checkin-qr.md).

**DoD (esta fase es el equivalente frontend de la criticidad que RF-022 tiene en el backend):**
- **Prueba obligatoria de los tres casos del documento 06:** (A) con sesión iniciada el check-in se completa; (B) sin sesión iniciada, redirige a login y **vuelve automáticamente** a `/checkin/{token}` completando el check-in, sin que el usuario tenga que volver a escanear; (C) con token inválido o ventana cerrada, muestra el error del backend y **no** una pantalla de login — distinguir B de C es el punto que más fácil se rompe.
- `QrDeSesion.razor` genera un QR que, escaneado con la cámara nativa de un teléfono real, abre la URL correcta de este frontend (RF-012). La ventana de validez se muestra en `America/Bogota`.
- `Ingreso.razor` registra el ingreso general (RF-006); si el backend lo rechaza por el intervalo mínimo (RN-030), se muestra ese rechazo, no un éxito silencioso.
- `MiHistorial.razor` no incluye ningún dato derivado de priorización (RF-021, RN-020) — verificado por inspección de tipo.
- CU-03 y CU-04 ejecutables de punta a punta, **probados en un teléfono real**, no solo en el emulador del navegador de escritorio.

### Fase 4 — Administración de eventos

> **Esta fase se añadió al corregir la revisión de esta documentación:** RF-007, RF-008 y RF-009 tenían pantalla prevista en el árbol de directorios pero no estaban asignados a ninguna fase — exactamente el mismo tipo de hueco silencioso que la revisión del backend detectó con RF-010/011/012.

**Alcance:** **RF-007** (crear evento, incluida la fecha de apertura de reservas), **RF-008** (configurar recurrencia), **RF-009** (editar/eliminar — `Should Have`).

**DoD:**
- El Administrador crea un evento de ocasión única y uno recurrente; tras crearlos, sus sesiones aparecen en el listado de RF-001 (criterio de aceptación de RF-007).
- El formulario exige explícitamente la **fecha de apertura de reservas** (RN-028, BLK-05) — no la calcula ni la precarga con un valor propio.
- La configuración de recurrencia trata el patrón como **dato extensible**, no como una lista cerrada de opciones fijas en el marcado: la enumeración de patrones sigue pendiente (Bloque 2), así que la UI refleja lo que el backend soporte, sin anticipar patrones no implementados.
- **RF-009 es `Should Have`:** si el plazo aprieta, es el primer candidato a diferir en este repositorio (ver más abajo). La confirmación de eliminación no promete ningún comportamiento concreto sobre reservas existentes (ver [01-ambiguedades-ui.md](../requisitos/01-ambiguedades-ui.md)).
- CU-05 ejecutable de punta a punta.

### Fase 5 — Priorización (exclusivo Administrador)

**Alcance:** **RF-016, RF-018.**

**DoD:**
- Solo el rol Administrador alcanza estas pantallas; verificado también **escribiendo la URL directamente** con otro rol — el backend responde `403` y la UI lo maneja con un mensaje, sin quedarse en blanco.
- `ConfiguracionSistema.razor` muestra los valores que devuelve el backend, **sin precargar valores por defecto inventados** (AMB-013).
- El color de módulo distintivo del documento 07 está aplicado, reforzando visualmente que es una sección restringida.
- CU-06 ejecutable de punta a punta (junto con lo construido en las fases 1 y 2).

### Fase 6 — Verificación transversal de confidencialidad, accesibilidad e identidad

**Alcance:** no es funcionalidad nueva — es la pasada de verificación equivalente a la Fase 6 del backend, sobre todo lo construido.

**DoD:**
- **RN-020:** para cada pantalla de Usuario y Entrenador, confirmar **por inspección del tipo consumido** (no de una respuesta de ejemplo) que no existe ningún campo de nivel de priorización ni de indicador de confiabilidad. Búsqueda explícita de esos nombres fuera de las pantallas de Administrador.
- **Cobertura de RF:** recorrer la tabla de cobertura de arriba completa y confirmar que cada RF `Must Have` tiene su pantalla con su comentario de origen, y que ninguno de los RF "sin interfaz" apareció como pantalla.
- **Casos de uso:** CU-01 a CU-06 ejecutados manualmente de punta a punta contra el backend desplegado, siguiendo su flujo principal y al menos un flujo de excepción de cada uno (mismo criterio que la estrategia de pruebas del backend).
- **Ambigüedades marcadas:** todo `PENDIENTE` de [01-ambiguedades-ui.md](../requisitos/01-ambiguedades-ui.md) existe en el código donde corresponde; ninguna de esas ambigüedades quedó resuelta en silencio.
- **Identidad visual:** ningún color hex ni familia tipográfica hardcodeada fuera de `itm-tema.css`. Contraste WCAG AA verificado sobre los componentes finales, no solo sobre la paleta teórica — con atención especial al celeste de acento, que **no cumple AA como texto sobre blanco** (ver documento 07).
- **Accesibilidad heredada:** navegación por teclado, `:focus-visible` y `prefers-reduced-motion` siguen funcionando igual que en la plantilla Kinetic original después de todos los cambios de tema.

---

## Estrategia de pruebas

| Tipo | Qué cubre | Herramienta |
|---|---|---|
| Unitarias, sin red | `EstadoDeCarga<T>`, `IZonaHorariaService`, `FlujoCheckInService` (la lógica de decisión del documento 06, que es pura) | xUnit |
| De componente | Pantallas con lógica de renderizado condicional: `CheckIn.razor` (casos A/B/C), la acción de reservar bajo AMB-014, y los DTO de Usuario/Entrenador bajo RN-020 | **bUnit** |
| Manual, checklist end-to-end | CU-01 a CU-06 contra el backend real, con el check-in probado en un teléfono físico | — |

**Regla de cobertura mínima:** toda verificación marcada como *obligatoria* en el DoD de una fase debe tener una prueba automatizada que falle si alguien revierte esa lógica — no basta con haberla comprobado a mano una vez. Aplica en particular a los tres casos del check-in (Fase 3) y a la ausencia de campos de priorización (Fase 2).

---

## Qué hacer si el plazo de noviembre de 2026 se pone en riesgo

Orden de recorte, alineado con la priorización MoSCoW del backend ([16-priorizacion.md](../../../fitnessbackend/docs/requisitos/16-priorizacion.md)):

1. **RF-009** (editar/eliminar evento, `Should Have`) — primer candidato a diferir, igual que en el backend.
2. **Pulido visual de la Fase 6** — se puede reducir a verificar contraste y RN-020, difiriendo refinamientos estéticos.
3. **Nunca se recorta el flujo de check-in por QR (Fase 3)**, ni la verificación de RN-020. El primero es la razón de existir del sistema desde la perspectiva del estudiante; el segundo es una regla de confidencialidad, no una funcionalidad.

## Checklist de cierre del prototipo

- [ ] Fases 0 a 5 completas con su DoD verificado.
- [ ] Tabla de cobertura de RF recorrida completa: cada `Must Have` con pantalla, cada "sin interfaz" ausente.
- [ ] Fase 6 ejecutada sobre el sistema completo, no pantalla por pantalla.
- [ ] CU-01 a CU-06 verificados manualmente contra el backend desplegado.
- [ ] `docs/` de este repositorio sigue reflejando el sistema real — cualquier desviación tomada durante la implementación quedó documentada aquí.

---

## Anexo: Plan de preparación para el despliegue (F0 a F10, 2026-10-31)

Con las fases 0 a 6 y el rediseño visual concluidos, el trabajo vigente pasa a regirse por [docs/plan-despliegue/00-indice.md](../plan-despliegue/00-indice.md) y [docs/plan-despliegue/01-guia-frontend.md](../plan-despliegue/01-guia-frontend.md).
Las nuevas fases F0 a F10 cubren:
- **F0**: Documentación previa y actualización de coberturas (este anexo).
- **F1**: Renovación de sesión de un solo vuelo (`ManejadorDeAutenticacionHttp.cs`, RF-026).
- **F2**: Programación semanal, `.ics`, lista de espera agregada y ayuda (`RF-035`, `RF-036`, `RF-037`, `RF-038`).
- **F3**: Entrenador (asistencia manual, notas, anuncios) y visualización de sesión cancelada en el usuario (`RF-027`, `RF-029`, `RF-034`, `RF-028`).
- **F4**: Administración de sesiones, bloqueos de calendario, reservas de terceros y registro de auditoría (`RF-028`, `RF-029`, `RF-030`, `RF-031`, `RF-032`).
- **F5**: Reportes administrativos y exportación CSV (`RF-033`).
- **F6**: Pantalla del punto de acceso con QR dinámico (`RF-039`).
- **F7**: Retroalimentación y reporte de problemas (`RF-038`).
- **F8**: PWA instalable (opcional).
- **F9**: Cierre visual, accesibilidad WCAG 2.1 AA y E2E.
- **F10**: Diseños diferidos en documentación (`RF-041`, `RF-042`).

