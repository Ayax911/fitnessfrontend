# Índice — Arquitectura y diseño técnico (frontend)

Insumo de entrada: [docs/requisitos/](../requisitos/00-indice.md) de este repositorio (lo que es responsabilidad exclusiva del cliente, incluidas las [ambigüedades abiertas que son de interfaz](../requisitos/01-ambiguedades-ui.md)) y, como fuente de verdad del negocio, [`fitnessbackend/docs/`](../../../fitnessbackend/docs/arquitectura/00-indice.md) — en particular su [07-contrato-api.md](../../../fitnessbackend/docs/arquitectura/07-contrato-api.md). Última actualización: 2026-09-27.

**Alcance de este conjunto de documentos: diseño puro.** No contienen código — son la especificación técnica a partir de la cual se programa y verifica el cliente Blazor WebAssembly. Espejan la estructura de `fitnessbackend/docs/arquitectura/` para que ambos repositorios se lean con el mismo mapa mental.

1. [Priorización de atributos de calidad (ISO/IEC 25010)](01-priorizacion-calidad-iso25010.md) — mismo orden que el backend (Mantenibilidad > Fiabilidad > Seguridad > Usabilidad > Rendimiento), reinterpretado para un cliente WASM.
2. [Stack tecnológico y hosting](02-stack-y-hosting.md) — Blazor WebAssembly (.NET 10) puro, sin servidor propio; hosting como archivos estáticos; generación de cliente HTTP con Kiota desde el OpenAPI del backend.
3. [Modelo de estado y datos del cliente](03-modelo-de-estado-cliente.md) — qué vive en memoria, qué en `localStorage`, cómo se cachean los DTOs del backend, manejo de tokens.
4. [Patrones de diseño de UI](04-patrones-de-diseno.md) — separación página/componente/servicio, patrón de manejo de `Resultado`/`Resultado<T>` en la UI, Adapter para el reloj/zona horaria, Strategy para roles.
5. [Organización de directorios](05-organizacion-directorios.md) — árbol de carpetas del proyecto Blazor, convención de nombres, mapeo con los módulos del backend.
6. [Flujo de check-in por QR](06-flujo-checkin-qr.md) — el único flujo de negocio que el backend delega explícitamente a este repositorio.
7. [Identidad visual y sistema de diseño](07-identidad-visual-y-sistema-de-diseno.md) — cómo se adapta la plantilla base (Kinetic) a la marca institucional del ITM: tokens de color (con las discrepancias del manual documentadas, no resueltas en silencio), tipografía, contraste WCAG verificado, y el mapeo componente a componente.
8. [Guía de implementación y verificación](08-guia-de-implementacion.md) — **documento de trabajo día a día:** orden de lectura, reglas de comportamiento, la tabla de cobertura RF → fase → pantalla, las seis fases con su Definición de Hecho, y la estrategia de pruebas.
9. [Rediseño visual](09-rediseno-visual.md) — lo que el rediseño añadió al sistema de diseño: logo oficial, navegación responsive, insignias de estado, esqueletos de carga, estados de interacción y los valores derivados que siguen pendientes de confirmar.

## Decisiones clave (resumen para orientarse rápido)

- **Este repositorio es solo frontend.** Consume la API HTTP de [`fitnessbackend`](../../../fitnessbackend/) — nunca implementa lógica de negocio (validación de cupos, ventanas de reserva, niveles de priorización); esa lógica vive y se verifica exclusivamente en el backend. El frontend solo refleja lo que la API le devuelve y le comunica al usuario los `Resultado.Fallo` en su idioma.
- **Stack:** Blazor WebAssembly standalone sobre .NET 10 LTS, publicado como archivos estáticos — sin `InteractiveServer`, sin SignalR, sin proceso siempre activo (a diferencia del backend, que sí lo necesita por RF-022).
- **Autenticación:** bearer tokens emitidos por `MapIdentityApi<ApplicationUser>()` del backend, guardados en memoria + `localStorage` para persistir la sesión entre recargas; nunca en cookies (cross-origin real).
- **Cliente HTTP generado, no escrito a mano:** Kiota contra `/openapi/v1.json` del backend, igual que recomienda el propio backend en su documento 07.
- **Base visual: plantilla Kinetic** (estructura HTML/CSS/JS de `../../kinetic/`), adaptada a la paleta y tipografía institucional del ITM (Manual de Imagen 2025, ver documento 07) — no se usa la paleta oscura original de Kinetic tal cual.
- **Un solo proyecto desplegable**, organizado por página/feature siguiendo los mismos módulos de dominio del backend (`Eventos`, `Reservas`, `Asistencia`, `Priorizacion` — este último solo para el rol Administrador —, `Identidad`), para que la trazabilidad entre repos sea directa.
- **RN-020 se refuerza también en el frontend:** ningún componente de Usuario o Entrenador importa ni renderiza un campo de nivel de priorización o indicador de confiabilidad, aunque el DTO llegara a incluirlo por error — es una segunda barrera, no la única.

## Estado

🟡 Diseño de arquitectura completo y listo para implementación, con dos condiciones explícitas:

1. **Dependencia de arranque:** requiere una instancia del backend en desarrollo exponiendo `/openapi/v1.json` para generar el cliente Kiota — ver [02-stack-y-hosting.md](02-stack-y-hosting.md).
2. **Ambigüedades abiertas:** AMB-014 y otras cuatro preguntas del levantamiento que son decisiones de interfaz siguen sin respuesta del stakeholder. **No bloquean el inicio** — cada una tiene definido su comportamiento menos comprometedor en [../requisitos/01-ambiguedades-ui.md](../requisitos/01-ambiguedades-ui.md) — pero deben quedar marcadas con `PENDIENTE` en el código, nunca resueltas en silencio.

### Revisión de consistencia de esta documentación (2026-09-27)

Se ejecutó sobre estos documentos la misma verificación bidireccional que la skill `verificacion-trazabilidad` aplica en el backend. Hallazgos corregidos:

| Hallazgo | Corrección |
|---|---|
| **RF-007, RF-008 y RF-009 no estaban asignados a ninguna fase** — tenían carpeta prevista en el árbol de directorios pero ninguna fase los construía. Es el mismo hueco silencioso que la revisión del backend detectó con RF-010/011/012 | Se añadió la **Fase 4 — Administración de eventos**, y una tabla de cobertura RF → fase → pantalla en el documento 08 que hace verificable la ausencia de huecos |
| **AMB-014 se estaba resolviendo en silencio** — el documento 03 indicaba mostrar el mensaje del backend tal cual, lo que para `VentanaNoAbierta` puede filtrar el motivo y violar RN-020 | Se creó [../requisitos/01-ambiguedades-ui.md](../requisitos/01-ambiguedades-ui.md) con AMB-014 y otras cuatro ambigüedades de interfaz, cada una con su comportamiento menos comprometedor y su marcador obligatorio |
| **El hex del celeste de acento estaba inventado** (`#29ABE2` no aparece en el manual), y el manual tiene dos inconsistencias propias (hex del azul malformado, hex del gris contradice su RGB) | Documentadas las tres en el documento 07 como preguntas abiertas para la Dirección de Comunicaciones; los tokens quedan centralizados para que corregirlas sea cambiar tres líneas |
| **El contraste WCAG se afirmaba sin calcular** | Calculado: el azul primario da 15.6:1 (✅ AAA); el celeste de acento da 2.5:1 sobre blanco (❌ **no cumple AA como texto**) — de ahí una regla dura nueva sobre dónde puede y no puede usarse |
| Faltaba el vínculo con los casos de uso | Añadida al índice de requisitos la tabla CU-01…CU-08 indicando cuáles se ejecutan por esta interfaz (seis) y cuáles son procesos internos del backend (dos) |
