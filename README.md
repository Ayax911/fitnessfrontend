# fitnessfrontend

Cliente web (Blazor WebAssembly) de la plataforma de eventos y asistencia del gimnasio universitario del ITM. Consume la API HTTP del repositorio [`fitnessbackend`](../fitnessbackend/) — este repositorio no contiene lógica de negocio propia.

**Estado actual: fase de diseño cerrada, sin código todavía.** La implementación la realiza un agente de codificación (Antigravity) a partir de la documentación en [`docs/`](docs/) — ver [AGENTS.md](AGENTS.md) para el punto de entrada.

## Documentación

| Carpeta | Contenido | Punto de entrada |
|---|---|---|
| [`docs/requisitos/`](docs/requisitos/) | Lo que es responsabilidad exclusiva del cliente, los casos de uso que pasan por esta interfaz, y las ambigüedades abiertas que son decisiones de UI (encabezadas por AMB-014). Los requisitos de negocio viven en `fitnessbackend/docs/requisitos/` | [00-indice.md](docs/requisitos/00-indice.md) |
| [`docs/arquitectura/`](docs/arquitectura/) | Stack y hosting, modelo de estado del cliente, patrones de UI, organización de carpetas, flujo de check-in por QR, sistema de diseño institucional, y la guía de implementación por fases | [00-indice.md](docs/arquitectura/00-indice.md) |

## Agentes

Dos agentes en [`.agents/agents/`](.agents/agents/), pensados para usarse en pareja: `constructor-fitnessfrontend` implementa cada fase, `revisor-fitnessfrontend` la aprueba — uno nunca aprueba su propio trabajo. El prompt de arranque está en [`.agents/prompt-frontend.md`](.agents/prompt-frontend.md).

## Identidad visual

La UI adapta la plantilla base [`../kinetic/`](../kinetic/) a la paleta y tipografía del Manual de Imagen ITM 2025 (`../recursos-diseno/Manual-ITM-V-2025.pdf`) — ver el detalle en [`docs/arquitectura/07-identidad-visual-y-sistema-de-diseno.md`](docs/arquitectura/07-identidad-visual-y-sistema-de-diseno.md).

## Estado

Diseño de arquitectura completo. Pendiente: implementación (ver fases en [`docs/arquitectura/08-guia-de-implementacion.md`](docs/arquitectura/08-guia-de-implementacion.md)).
