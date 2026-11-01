# Pruebas en dispositivos reales, accesibilidad y verificación visual

> Complementa F9 de [01-guia-frontend.md](01-guia-frontend.md). **Lo que requiere una persona con un teléfono o un lector de pantalla lo hace el usuario**; el agente prepara la guía, el entorno y registra los resultados. Los resultados se anotan en `docs/checklist-e2e.md`.

## 1. Preparación del entorno (la hace el agente)

1. Levantar backend y frontend accesibles desde la red local (no `localhost`): backend con `Cors:AllowedOrigins` que incluya el origen del frontend por IP o nombre de la LAN; frontend publicado con `UrlBaseApi` apuntando al backend. Un teléfono real **necesita HTTPS** para cámara y *Wake Lock* en la mayoría de navegadores; usar un túnel HTTPS temporal o un certificado local de desarrollo y documentarlo en el runbook (sin dejar credenciales ni URLs privadas en el repositorio).
2. Sembrar los datos de demostración (B8) y dejar a la vista el usuario, la sesión y el punto de acceso para cada prueba.
3. Imprimir o mostrar la hoja de tareas de abajo.

## 2. Pruebas en teléfonos reales (las hace el usuario)

Matriz y tareas completas en el guion de piloto (`fitnessbackend/docs/plan-despliegue/05-datos-demostracion-y-piloto.md`). Mínimo antes del piloto:

| # | Prueba | Dispositivo | Resultado esperado |
|---|---|---|---|
| T1 | Iniciar sesión y reservar una actividad | Android y iPhone | Sin desbordes; barra inferior usable con una mano |
| T2 | **Escanear el QR dinámico de la pantalla del punto de acceso con la cámara nativa** | Android y iPhone | Abre el enlace, registra el ingreso o pide iniciar sesión y vuelve (`returnUrl`) |
| T3 | Escanear el QR y esperar > 2 periodos antes de abrir el enlace | Cualquiera | Mensaje `QrVencido` claro; escanear de nuevo funciona |
| T4 | Check-in del QR de una sesión del entrenador | Android y iPhone | Confirmación visible de inmediato |
| T5 | **Sesión vencida durante un check-in** (esperar a que expire el token o forzarlo) | Cualquiera | Se renueva en silencio y se completa (F1) |
| T6 | Pantalla del punto de acceso 2 horas en un monitor/tableta | Chrome en quiosco | El QR sigue cambiando, la pantalla no se apaga, sin fugas visibles |
| T7 | Pantalla del punto de acceso con la red cortada 1 minuto | Monitor | Mantiene el QR hasta vencer y muestra "Sin conexión"; se recupera sola |
| T8 | Carga inicial en datos móviles (3G/4G) con un teléfono de gama baja | Android gama baja | Se ve el esqueleto/pantalla de arranque y la app es usable; anotar segundos |
| T9 | Girar el teléfono y usar el zoom del sistema (texto grande) | Cualquiera | Nada se corta ni se solapa |
| T10 | Teclado en pantalla sobre formularios (Registro, Reportar) | Android y iPhone | El campo activo no queda oculto; el botón principal es alcanzable |

Registrar por prueba: dispositivo y versión, navegador, resultado (✓/✗), captura si falla y segundos cuando aplique.

## 3. Verificación visual pendiente del rediseño (el agente, en el navegador)

Cosas que quedaron **sin ver en pantalla** al aplicar el rediseño:
- [ ] Registro a 390 px (formulario largo: foco, errores, casilla de autorización).
- [ ] Check-in e Ingreso: resultado correcto, resultado de error, esqueleto.
- [ ] Esqueletos de Eventos, Mis reservas y Mi asistencia (frenar la red del navegador para verlos).
- [ ] Pantallas de administración a 390 px: `GestionUsuarios`, `PuntosAcceso`, `CrearEvento`, `EditarEvento`, `MisActividades`, `InscritosDeSesion`, `QrDeSesion`, `ConfiguracionSistema`, `IndicadorUsuario`.
- [ ] Menú desplegable de Administrador y Entrenador en celular (abre, cierra al navegar, foco, `aria-expanded`).
- [ ] 720–1023 px (tablet): barra superior sin desbordar con los 6 destinos del Administrador.
Para ver a ancho de celular sin redimensionar la ventana, abrir la ruta dentro de un `iframe` de 390 px en una página del mismo origen.

## 4. Accesibilidad manual (WCAG 2.1 AA — ISO/IEC 25010 "Accesibilidad")

Lista de verificación por pantalla (la herramienta automática no basta):

| Criterio | Cómo se comprueba |
|---|---|
| Teclado | Recorrer toda la pantalla solo con Tab/Shift+Tab/Enter/Espacio/Esc: orden lógico, nada atrapado, menú y diálogos cierran con Esc |
| Foco visible | Anillo de 3 px visible sobre cada superficie (azul sobre claro, blanco sobre la barra azul) |
| Nombres accesibles | Cada botón de icono, enlace del logo, `NavLink` y campo tiene nombre; `aria-current`/`aria-pressed`/`aria-expanded` correctos |
| Contraste | Texto ≥ 4,5:1 y componentes ≥ 3:1 con los tokens (comprobar insignias, alertas, chips, estados deshabilitados y la barra inferior) |
| No solo color | Cada estado tiene icono y texto (insignias, alertas, resultado de QR, disponibilidad) |
| Objetivos táctiles | ≥ 48 px (el chip, los botones y las filas de acción) |
| Zoom y reflujo | 200 % de zoom y ancho de 320 px sin scroll horizontal ni pérdida de contenido |
| Movimiento | Con "reducir movimiento" el brillo de los esqueletos y el anillo de cuenta regresiva se detienen o se simplifican |
| Lector de pantalla | NVDA (Windows), TalkBack (Android) y VoiceOver (iOS): el esqueleto anuncia "Cargando…"; los errores de formulario se anuncian; los resultados de check-in e ingreso se anuncian al aparecer (`role="status"`/`alert`); la cuenta regresiva del QR **no** se anuncia cada segundo |
| Formularios | Etiqueta visible, ayuda asociada (`aria-describedby`), error enlazado y resumen al enviar |
| Idioma | `lang="es"`; textos en español claro, sin jerga |
| Tiempo | Ningún límite de tiempo que el usuario no pueda extender (el QR dinámico lo gestiona el sistema; el mensaje es claro) |

**Herramientas sugeridas:** Lighthouse (accesibilidad) y axe DevTools en el navegador; contraste con la herramienta de los tokens del sistema de diseño. Documentar el resultado en `docs/auditoria-accesibilidad.md` (nuevo) con hallazgos priorizados.

## 5. Qué se considera "aprobado"

- T1–T10 sin ✗ bloqueantes (un ✗ en T2, T4 o T5 es bloqueante).
- Verificación visual sin desbordes ni textos cortados en 390/768/1280 px.
- Lista de accesibilidad sin incumplimientos AA abiertos; los pendientes quedan documentados con su impacto.
