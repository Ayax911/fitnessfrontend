# 07. Identidad visual y sistema de diseño

> Fuentes de este documento: [`../../recursos-diseno/Manual-ITM-V-2025.pdf`](../../../recursos-diseno/Manual-ITM-V-2025.pdf) (Manual de Imagen Institucional ITM, versión 2025 — fuente de verdad de marca) y [`../../kinetic/`](../../../kinetic/) (plantilla base de estructura HTML/Bootstrap 5/JS, README en `kinetic/README.txt`). Ninguna paleta o tipografía de Kinetic se usa tal cual — se reemplaza por la del manual del ITM, conservando de Kinetic únicamente su estructura de maquetación, componentes y comportamiento (grid, tarjetas, tabs, animaciones, accesibilidad).

## Por qué Kinetic como base estructural

Kinetic ya resuelve, en HTML/CSS/JS listo para adaptar, varios problemas que este sistema necesita: un horario semanal interactivo que cambia por día (mapea directo a la vista de sesiones de un `Evento` por semana), tarjetas de "clase"/actividad (mapean a `SesionEvento`), contadores animados (útiles para cupos disponibles), y una tipografía de UI pensada para lectura rápida en tarjetas y tablas. Es mobile-first, con `:focus-visible`, ARIA en tabs y `prefers-reduced-motion` ya contemplados — alineado con Usabilidad (#4) sin partir de cero. Lo que **no** se conserva es su paleta oscura "gimnasio de barrio" ni su tipografía Anton/Barlow: ese lenguaje visual no es el del ITM.

## Paleta institucional (reemplaza la de Kinetic)

Tomada del Manual de Imagen ITM 2025, lámina "Colores" (tres colores base) y lámina "Colores secundarios para las facultades".

| Token | Uso | Valor | Procedencia |
|---|---|---|---|
| `--itm-azul-primario` | Color de marca principal — encabezados, navegación, botones primarios, el logo | `#011C5B` | **Derivado del RGB del manual** (R1 / G28 / B91 · Pantone 2758 · CMYK 99/100/41/11) — ver la nota de discrepancias |
| `--itm-negro` | Texto principal sobre fondo claro | `#000000` | Manual, Pantone Process Black (CMYK 0/0/0/100) |
| `--itm-gris` | Texto secundario, bordes, superficies neutras | `#C6C6C6` | **Derivado del RGB del manual** (R198 / G198 / B198 · Pantone Cool Gray 3 · CMYK K:50) — ver la nota de discrepancias |
| `--itm-blanco` | Fondo base, texto sobre `--itm-azul-primario` | `#FFFFFF` | — |
| `--itm-celeste-acento` | Elementos decorativos y de apoyo (círculos concéntricos, filetes, subrayados de sección) | ⚠️ **`#56ACDE` como marcador de posición** | **No está definido en la lámina de colores base del manual.** Ver la nota de abajo |

### ⚠️ Discrepancias del manual que NO se resuelven en silencio

Al transcribir la paleta se detectaron tres problemas en la fuente. Siguiendo la misma disciplina que el resto del proyecto, se documentan en vez de resolverse por cuenta propia:

1. **El hex del azul está malformado en el manual.** La lámina imprime `#01B1464` (siete dígitos, no es un hex válido). El RGB que acompaña — R1 / G28 / B91 — sí es inequívoco y corresponde a `#011C5B`. **Se usa el valor derivado del RGB**, que además es coherente con Pantone 2758.
2. **El hex del gris contradice su propio RGB.** La lámina imprime `Hexadecimal: #1D1D1B` (un casi-negro) junto a `R:198 G:198 B:198` (un gris claro) y `K:50`. Son dos colores distintos. **Se usa el valor derivado del RGB (`#C6C6C6`)**, coherente con Pantone Cool Gray 3 y con el uso visible en las propias piezas del manual. Queda como pregunta abierta para la Dirección de Comunicaciones: si `#1D1D1B` era el color pretendido, el token debe corregirse.
3. **El celeste de acento no tiene definición numérica.** Aparece profusamente en las láminas (títulos de sección, círculos concéntricos, filetes del pie "/// Libro de marca ///") pero **la lámina de colores base define solo tres colores**, y ninguno es ese celeste. `#56ACDE` se adopta como **marcador de posición** porque sí está definido en el manual — es el tercer color de la Facultad de Ingenierías — y es el tono más cercano al usado en las piezas.

**Consecuencia práctica:** estos tres valores son exactamente el tipo de dato que la regla de comportamiento 1 del proyecto prohíbe inventar. Están centralizados como tokens en un único archivo precisamente para que corregirlos, cuando Comunicaciones confirme, sea cambiar tres líneas y no auditar toda la interfaz. **Pregunta pendiente para el stakeholder:** confirmar el hex oficial del azul, del gris y del celeste de acento contra una pieza aprobada por la Dirección de Comunicaciones y Publicaciones.

### Contraste verificado (WCAG 2.1 AA)

Calculado sobre los valores de la tabla, no estimado a ojo:

| Combinación | Ratio | Veredicto |
|---|---|---|
| `#011C5B` sobre `#FFFFFF` | **15.6 : 1** | ✅ Cumple AA y AAA — es la combinación por defecto de texto |
| `#FFFFFF` sobre `#011C5B` | **15.6 : 1** | ✅ Cumple AA y AAA — texto sobre barras y botones primarios |
| `#56ACDE` sobre `#FFFFFF` | **2.5 : 1** | ❌ **NO cumple AA para texto** |
| `#56ACDE` sobre `#011C5B` | **6.2 : 1** | ✅ Cumple AA |

**Regla dura que se deriva de lo anterior:** el celeste de acento **nunca** se usa como color de texto sobre fondo blanco — ni para enlaces, ni para etiquetas, ni para texto de ayuda. Su uso legítimo es: elementos decorativos no informativos, bordes/filetes, e íconos acompañados de texto; o como texto **sobre el azul primario**, donde sí cumple. Los enlaces sobre fondo blanco usan `--itm-azul-primario` con subrayado.

**Regla del manual que aplica igual en digital:** "no cambiar los colores o fuentes" del logo — el isotipo ITM se usa únicamente en positivo (blanco) sobre `--itm-azul-primario`/fondos oscuros, o en negativo (azul/negro) sobre fondos claros, nunca en un color fuera de la paleta ni con gradientes propios. El manual también prohíbe explícitamente achatarlo, estirarlo, rotarlo, contornearlo o alterar su diagramación — lo que en la práctica web significa: el logo va como SVG con proporción bloqueada, nunca con `width` y `height` fijados de forma independiente.

### Color por módulo (opcional, inspirado en los colores de facultad del manual)

El manual asigna un color secundario a cada facultad para diferenciarlas manteniendo el azul institucional como base. Este sistema aplica la misma idea para diferenciar visualmente los módulos de la app **sin** introducir colores fuera del manual — se reutilizan los tonos ya definidos para las facultades, no se inventan nuevos:

| Módulo | Color de apoyo (tomado del manual) | Uso |
|---|---|---|
| Eventos | `#00A0B7` (Ingenierías, turquesa) | Iconos/bordes de tarjetas de evento |
| Reservas | `#009030` (Ciencias Exactas, verde) | Estados de cupo disponible |
| Asistencia | `#F19800` (Ciencias Económicas, naranja) | Estados de check-in / QR |
| Priorización (solo Administrador) | `#661081` (Artes y Humanidades, morado) | Exclusivo de la vista de Administrador — refuerza visualmente que es una sección distinta y restringida (RN-020) |

Esto es una convención de este sistema, no una regla del manual — puede ajustarse sin romper identidad de marca porque los cinco colores base siguen siendo los institucionales.

## Tipografía institucional (reemplaza Anton/Barlow de Kinetic)

| Rol | Tipografía (según el manual) | Uso en este sistema |
|---|---|---|
| Principal / obligatoria | **Montserrat** (pesos Thin a Black, con itálicas) | Toda la UI: navegación, botones, cuerpo de texto, tablas, formularios. Es la única obligatoria en "todos los medios digitales" según el manual. |
| Secundaria — verticalidad y énfasis | **Lato** | Texto largo donde Montserrat resulte pesado (descripciones de evento, ayudas de formulario) — el manual la destina a "cuerpos de texto extensos". |
| Secundaria — compatibilidad ofimática | **Arial** | Solo si se generan documentos/reportes exportables (ej. un PDF de historial de asistencia) fuera del navegador — no se usa en la interfaz web en sí. |
| Acento — uso puntual, nunca para texto funcional | **Charlotte** / **Regular Brush** (script) | Reservada, si acaso, a un elemento decorativo puntual en la pantalla de bienvenida/login (ej. una firma o llamado emocional corto) — el manual es explícito: "funciona mejor para un punto de énfasis, no para crear su mensaje completo". Nunca en botones, tablas, ni ningún texto que el usuario deba leer para operar el sistema (violaría Usabilidad y Accesibilidad). |

**Carga técnica:** Montserrat y Lato están disponibles en Google Fonts — se cargan igual que Kinetic ya carga sus fuentes (`<link>` a Google Fonts), solo cambiando la familia. Charlotte/Regular Brush son tipografías del manual sin garantía de licencia web gratuita — si se usan, deben auto-hospedarse con la licencia correspondiente, nunca enlazarse desde una fuente no verificada.

## Mapeo concreto Kinetic → ITM

| Elemento de Kinetic | Tratamiento original | Tratamiento en este sistema |
|---|---|---|
| Fondo de página (`#0d0b0a` obsidian ink) | Casi negro, tema oscuro | `--itm-blanco` como base; se reserva un tema oscuro opcional (`--itm-azul-primario` de fondo) únicamente para el layout de Administrador, si se decide diferenciarlo visualmente |
| Acento primario (`#ff4a1c` molten orange) | CTAs, estados activos | `--itm-azul-primario` para CTAs; `--itm-celeste-acento` para estados activos/hover, nunca naranja |
| Acento secundario (`#f7b32b` ember amber) | Stats, pill intermedio | Reemplazado por el color de módulo correspondiente (tabla de arriba) |
| Tipografía display (Anton) | Titulares en póster, mayúscula | Montserrat ExtraBold/Black en mayúscula sostenida para titulares de sección — mismo peso visual, sin cambiar de familia |
| Tipografía de cuerpo (Barlow) | Texto y UI | Montserrat Regular/Medium para UI; Lato para bloques largos |
| Horario semanal interactivo | Clases por día, coach, nivel | Sesiones de un `Evento` por semana — mismo componente de tabs por día, coach → nombre del Entrenador, "nivel" → cupo disponible (nunca el nivel de priorización, que es interno, RN-020) |
| Tarjetas de resultado de miembros (galería filtrable) | Fotos de transformación | No aplica al dominio (no hay galería de resultados físicos) — se descarta, no se fuerza un uso artificial |
| Contadores animados | Estadísticas del gimnasio | Cupos disponibles / inscritos de una sesión en tiempo de carga de la página (no en tiempo real vía WebSocket — no hay ese requisito) |
| Precios sin contrato | Tabla de precios | No aplica — el sistema no maneja pagos; se descarta |

## Accesibilidad heredada de Kinetic (se mantiene, no se reinventa)

Kinetic ya cumple, según su propio README: mobile-first 360–1440px, skip link, landmarks semánticos, ARIA en tabs y botones de icono, `:focus-visible`, y respeto de `prefers-reduced-motion`. Estas prácticas se conservan íntegramente al migrar la paleta y tipografía — cambiar colores y fuentes no debe tocar la estructura semántica ni los atributos ARIA ya presentes en el HTML base.

**Verificación obligatoria tras el cambio de paleta:** los ratios de la tabla de contraste de arriba. El azul primario pasa holgadamente en ambos sentidos; el celeste de acento **no** pasa como texto sobre blanco, y esa es la única restricción real que introduce el cambio de tema. Debe recalcularse si cualquiera de los tokens cambia de valor al resolverse las discrepancias del manual.

## Dónde vive esto en código

Los tokens de esta tabla se implementan como variables CSS en `wwwroot/css/itm-tema.css` (ver [05-organizacion-directorios.md](05-organizacion-directorios.md)), cargado después de Bootstrap para poder sobrescribir sus variables (`--bs-primary`, `--bs-body-font-family`, etc.) sin tocar el Bootstrap self-hosted heredado de Kinetic. Ningún componente usa un color o fuente hardcodeada fuera de estas variables — es la aplicación, en CSS, del mismo principio de "todo valor de marca es un token, nunca un literal repetido" que gobierna el resto de este sistema.
