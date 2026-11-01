# Prompt de corrección — auditoría final independiente (fitnessfrontend)

> Pegar en Antigravity con el workspace en `fitnessfrontend/`, agente `constructor-fitnessfrontend` seleccionado con `/agents`.
> Fuente: `docs/auditoria-final-independiente.md` (30 hallazgos verificados el 2026-09-28 por un modelo independiente). El hallazgo B-03 fue **verificado manualmente contra el código real** — confirmado exacto, se corrige sin re-cuestionar. Si al implementar cualquier otro descubres que la descripción no coincide con el código, detente y repórtalo.

---

Vas a corregir los hallazgos de la auditoría independiente que aplican a este repositorio (frontend). Es trabajo de **mantenimiento sobre código que ya funciona** — no reescribas nada que el hallazgo no señale explícitamente.

Corrige en **cuatro rondas**, en este orden. Al cerrar cada ronda: `dotnet build`, `dotnet test`, luego invoca a `revisor-fitnessfrontend` con la lista de hallazgos corregidos en esa ronda. No avances de ronda con hallazgos bloqueantes reportados sobre tu propia corrección.

## Ronda 1 — Bug bloqueante de negocio + fuga silenciosa de contexto (RN-020)

**Hallazgos: B-03, I-08**

1. **B-03 (enum de nivel de priorización desfasado):** en `Paginas/Priorizacion/IndicadorUsuario.razor`, el `switch` mapea mal el enum del backend (`Nuevo=0, Prioritario=1, Normal=2, Penalizado=3`). Corrígelo exactamente: `case 0 → Nuevo`, `case 1 → Prioritario`, `case 2 → Normal`, `case 3 → Penalizado`. Hoy un usuario `Nuevo` se muestra como "Prioritario" y un `Normal` como "Penalizado" — es la única pantalla donde el Administrador ve este dato, y hoy le muestra el nivel equivocado a todos los usuarios. Añade una prueba de componente que cubra los cuatro casos del enum explícitamente (no solo uno), para que un futuro desfase de índice se detecte de inmediato.

2. **I-08 (fecha de apertura visible, contradice el espíritu de RN-020/AMB-014):** en `Paginas/Eventos/ListaEventos.razor`, el mensaje genérico de AMB-014 ya está bien (no lo toques), pero (a) la fecha base de apertura de reservas se muestra públicamente y permite a un usuario penalizado deducir que su bloqueo es personal, y (b) el botón "Reservar" está habilitado inicialmente incluso cuando la ventana del usuario no está abierta. Corrige lo que se puede resolver **sin depender de un endpoint nuevo del backend** (que no vamos a pedir en esta ronda): deja de mostrar la fecha de apertura al usuario, y deshabilita preventivamente el botón "Reservar" cuando la respuesta indique que la ventana no está abierta, en vez de dejarlo habilitado hasta que el backend lo rechace. Esto no cambia el comportamiento ya definido para AMB-014 — lo refuerza.

## Ronda 2 — Flujos de navegación rotos

**Hallazgos: I-09, M-04, M-05**

1. **I-09 (ingreso general no autenticado pierde el registro):** en `Paginas/Asistencia/Ingreso.razor`, cuando un usuario sin sesión escanea el QR del torniquete, `App.razor` hoy renderiza `<Login />` inline sin `?returnUrl=...`, así que tras autenticarse va a `/eventos` y el ingreso nunca se registra. Implementa aquí el mismo patrón que ya usa `CheckIn.razor` (documento `docs/arquitectura/06-flujo-checkin-qr.md`, Caso B): redirige explícitamente a `/login?returnUrl={UriActual}` y completa el registro de ingreso automáticamente al volver, sin que el usuario tenga que volver a escanear.

2. **M-04 (ruta rota en menú de Administrador):** en `Layout/NavMenuAdministrador.razor`, el enlace apunta a `/admin/priorizacion` pero la ruta real es `/admin/priorizacion/usuarios`. Corrige el `href` para que coincida con la ruta que de verdad existe.

3. **M-05 (returnUrl se pierde entre Login y Registro):** en `Paginas/Identidad/Login.razor` y `Registro.razor`, cuando alguien llega a `/login?returnUrl=/checkin/{token}` y hace clic en "Registrarse", el parámetro no se propaga a `/registro`, y tras completar el registro vuelve a `/login` limpio — perdiendo el check-in pendiente. Propaga `returnUrl` en ambos enlaces (Login → Registro y de vuelta).

**Prueba obligatoria de esta ronda:** un flujo de componente que simule llegar a `/checkin/{token}` sin sesión, pasar por Registro (no solo Login), y confirmar que el check-in se completa igual al final — hoy solo está probado el camino de Login directo.

## Ronda 3 — Robustez de UI y honestidad de los tokens de diseño

**Hallazgos: I-11, I-13, M-07, M-08**

1. **I-11 (pantalla en blanco cuando `Valor` es `null`):** en `Componentes/Compartidos/EstadoDeCarga.razor`, si `EsExito == true` y `Valor == null`, hoy ninguna rama se activa y queda una pantalla completamente en blanco sin spinner ni mensaje. Cambia la condición a `else if (Estado.EsExito)` o agrega un bloque `else` explícito para el caso de valor nulo con un mensaje apropiado (ej. "No se encontraron datos").

2. **I-13 (colores en disputa adoptados sin marcarlo):** en `wwwroot/css/itm-tema.css`, el azul primario (`#011C5B`, inferido de RGB) y el gris (`#C6C6C6`, una de dos lecturas contradictorias del manual) están documentados como "en disputa" en `docs/arquitectura/07-identidad-visual-y-sistema-de-diseno.md`, pero solo el celeste de acento tiene el comentario `PENDIENTE` en el CSS. Agrega el mismo comentario `/* PENDIENTE: valor en disputa, ver 07-identidad-visual-y-sistema-de-diseno.md */` junto a las variables del azul y del gris — no cambies los valores, solo documenta su estado provisional en el mismo lugar donde ya vive el del celeste.

3. **M-07 (hex hardcodeado en hover):** en `wwwroot/css/itm-tema.css`, `.btn-itm-priorizacion:hover` usa `#520c68` directamente. Reemplázalo por `filter: brightness(0.85)` sobre el token existente, o declara una variable CSS derivada — no un literal nuevo.

4. **M-08 (estilos inline):** en `Layout/MainLayout.razor`, `Componentes/Compartidos/EstadoDeCarga.razor` y `App.razor`, hay tres `style=` inline para dimensiones. Extráelos a clases utilitarias en `itm-tema.css`.

## Ronda 4 — Zona horaria y regeneración del cliente API

**Hallazgo: I-03, y la parte frontend de I-10**

1. **I-03 (conversiones manuales fuera del servicio centralizado):** en `Paginas/Eventos/Administrar/CrearEvento.razor` y `EditarEvento.razor`, se usa `DateTime.Today` (reloj del navegador) y `TimeSpan.FromHours(-5)` hardcodeado en vez de pasar por `IZonaHorariaService` — que hoy no tiene un método para convertir de local a UTC. Extiende `IZonaHorariaService`/`ZonaHorariaService` con `AUtc(DateTime local)` y `ObtenerFechaHoyLocal()`, y reemplaza los usos sueltos en ambos formularios. Esto es exactamente la regla que ya rige el resto del repositorio (documento 04, Patrón 3) — este par de pantallas se saltó el punto centralizado.

2. **I-10, parte frontend:** el backend va a declarar (o ya declaró, confírmalo con el equipo/documentación de `fitnessbackend`) los códigos de error faltantes en `ReservaEndpoints.cs`/`AsistenciaEndpoints.cs` con `.Produces<ErrorDetalleDto>(...)`. Cuando eso esté hecho y el backend esté corriendo con el contrato actualizado, **regenera el cliente Kiota** en `src/FitnessFrontend/ApiCliente/` contra el `/openapi/v1.json` actualizado — no edites los archivos generados a mano. Verifica después de regenerar que los `catch (ErrorDetalleDto)` que hoy son código muerto en las pantallas de Reservas y Asistencia empiezan a manejar los errores reales (prueba forzando un `404`/`400` real contra el backend).

**No conviertas esta ronda en la primera si el backend todavía no terminó su Ronda 3** (que incluye I-10) — pregúntame si no estás seguro de si ya está lista la regeneración del cliente.

## Al terminar las cuatro rondas

1. `dotnet build` sin advertencias nuevas y `dotnet test` en verde para **todo** el proyecto.
2. Invoca a `revisor-fitnessfrontend` para una pasada completa contra la lista total de hallazgos de `docs/auditoria-final-independiente.md`, no solo el DoD de fase habitual.
3. Actualiza `docs/auditoria-final-independiente.md`: junto a cada hallazgo corregido, agrega `**Estado:** ✅ Corregido en [commit/ronda]`.
4. Prueba manual: el flujo de `Ingreso.razor` sin sesión (I-09) y el de `IndicadorUsuario.razor` con los cuatro niveles (B-03) en un navegador real, no solo en las pruebas automatizadas — son los dos hallazgos donde un error visual sutil es fácil de no notar aunque la prueba automatizada pase.

Cuando termines todo, dime qué rondas quedaron cerradas, qué reportó el revisor en la pasada final, y si algún hallazgo resultó ser distinto de lo que describía la auditoría al implementarlo.
