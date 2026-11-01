# 09 · Rediseño visual (sistema de diseño aplicado)

Complementa [07-identidad-visual-y-sistema-de-diseno.md](07-identidad-visual-y-sistema-de-diseno.md) sin reemplazarlo: documenta lo que el rediseño añadió. La fuente de los tokens sigue siendo `src/FitnessFrontend/wwwroot/css/itm-tema.css`.

## Qué cambió
- **Logo oficial** (`wwwroot/img/itm-horizontal-*.png`, recortado de `logos2026/LOGOS`): sin modificar y sin texto al lado. Alto mínimo 28 px y margen de 8 px son valores **provisionales** (el manual no da medida): PENDIENTE con Comunicaciones.
- **Navegación responsive** (celular < 720 px · tablet 720–1023 · escritorio ≥ 1024): barra superior con enlaces desde 720 px; el Usuario en celular usa barra inferior de 3 destinos (`NavTabsUsuario`); Administrador y Entrenador, que tienen más destinos, usan un menú desplegable.
- **Insignias de estado** (`InsigniaEstado`, dirección A "tinta suave"): fondo claro + texto oscuro del mismo color + icono + palabra. Seis estados: `ok`, `espera`, `asistida`, `no`, `cancelada`, `ingreso`. Los tonos `--estado-*` son **derivados** de los colores del manual (contraste texto/fondo entre 5.8:1 y 13:1): PENDIENTE de confirmar.
- **Carga con esqueletos** (`EsqueletoTarjetas`, `EsqueletoLista`, esqueleto genérico en `EstadoDeCarga`): copian la forma del contenido; se anuncian con texto (`role="status"`, `aria-busy`) y con movimiento reducido el brillo se detiene.
- **Cupo sin puntos**: se muestra el número ("N cupos") y una barra de ocupación solo como refuerzo; los eventos suelen superar 30 personas.
- **Estados de interacción** en botones, campos, chips y navegación (reposo, hover, pulsado, foco, deshabilitado, cargando).
- **Accesibilidad**: foco azul de 3 px (blanco sobre la barra azul); texto **negro** sobre rellenos verde, turquesa y naranja (el blanco no pasa AA); bordes de control `#767676`; objetivos táctiles de 48 px.
- Las pantallas de administración conservan su estructura Bootstrap y heredan el nuevo aspecto por los estilos de `.btn`, `.form-control`, `.alert`, `.table` y `.card-itm`.

## Lo que no cambia
RN-020 y AMB-014: ninguna pantalla de Usuario o Entrenador muestra priorización ni la fecha de apertura de reservas.

## Dónde está cada cosa
| Pieza | Ubicación |
|---|---|
| Tokens, botones, campos, alertas, insignias, esqueletos, responsive | `src/FitnessFrontend/wwwroot/css/itm-tema.css` |
| Logos | `src/FitnessFrontend/wwwroot/img/` |
| Barra superior, menú desplegable y barra inferior | `Layout/MainLayout.razor`, `Layout/NavMenu*.razor`, `Layout/NavTabsUsuario.razor` |
| Insignia de estado | `Componentes/Compartidos/InsigniaEstado.razor` |
| Esqueletos | `Componentes/Compartidos/EsqueletoTarjetas.razor`, `EsqueletoLista.razor`, `EstadoDeCarga.razor` |
| Pantallas rediseñadas | `Paginas/Eventos/ListaEventos`, `Paginas/Reservas/MisReservas`, `Paginas/Asistencia/{MiHistorial,CheckIn,Ingreso}`, `Paginas/Identidad/{Login,Registro}` |
| Pruebas | `tests/FitnessFrontend.Tests/Componentes/InsigniaYEsqueletosTests.cs` |

## Comportamientos a tener en cuenta
- **Selector de día** (Eventos): filtra en el cliente solo para presentar; "Todos" es la opción por defecto. No aplica ninguna regla de negocio.
- **Barra de cupo**: su ancho sale de `CupoMaximo` y `CuposOcupados` que informa el backend; el cliente no decide si hay cupo.
- La insignia de una reserva confirmada dice "Confirmada".
- Pendiente de revisión visual: Registro y pantallas de administración a ancho de celular, y Check-in/Ingreso en pantalla.

---

## Anexo: Componentes nuevos para el plan de despliegue (2026-10-31)

### 1. `ConfirmacionConsecuencia.razor` (Protección contra errores — ISO/IEC 25010)
- **Propósito:** diálogo modal accesible para acciones destructivas o masivas (cancelar sesión, retirar reserva de tercero, desmarcar asistencia manual, eliminar bloqueo de calendario).
- **Diseño y comportamiento:**
  - Explica en lenguaje del usuario la **consecuencia concreta** (p. ej., *"Se cancelarán N reservas y se liberará el cupo. Los usuarios verán que la sesión fue cancelada. No se les penalizará."*).
  - El botón de acción destructiva usa clase de peligro (`btn-itm-peligro`) y el botón secundario y por defecto de escape es *"Volver"* (`btn-itm-secundario`).
  - No permite confirmación con tecla Enter accidental sobre el formulario. Soporta cierre con tecla Esc y gestión accesible de foco con `aria-modal="true"` y `role="dialog"`.

### 2. `TablaAdaptable.razor` (Responsive en tres cortes)
- **Propósito:** presentación de colecciones tabulares administrativas (bloqueos de calendario, reservas de sesión, reportes, auditoría).
- **Diseño y comportamiento:**
  - En anchos de tablet y escritorio (≥ 720 px), se presenta como tabla HTML semántica con cabeceras claras (`<th>`, `aria-label`).
  - En celulares (< 720 px), conmuta automáticamente mediante CSS a una disposición de **tarjetas apiladas**, donde cada celda incluye su etiqueta descriptiva visible sin producir desbordamiento ni scroll horizontal.

### 3. `AnilloCuentaRegresiva.razor` (Indicador visual de rotación QR)
- **Propósito:** cuenta regresiva visual para el ciclo de vida del token dinámico de ingreso en `PantallaPuntoAcceso.razor`.
- **Diseño y comportamiento:**
  - Anillo circular trazado en SVG con cálculo dinámico del `stroke-dashoffset`.
  - Color azul institucional sobre fondo neutro, visible a distancia en pantallas de acceso físico.
  - **Accesibilidad y movimiento reducido:** si el usuario tiene habilitado `prefers-reduced-motion`, la animación continua se sustituye por actualización discreta de progreso.
  - **Lector de pantalla:** el atributo `aria-live` se mantiene **desactivado** para no saturar al lector de pantalla verbalizando cada segundo transcurrido.

