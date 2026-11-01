# AGENTS.md — Punto de partida para el agente de codificación (Antigravity)

Este archivo es el punto de entrada para cualquier agente de codificación que vaya a trabajar en este repositorio. No repite el contenido de `docs/` — lo señala en el orden correcto y resume las reglas que no se pueden pasar por alto.

## Qué es este proyecto

Cliente web de la plataforma de eventos y asistencia del gimnasio universitario del ITM (área de Bienestar Universitario): permite consultar actividades, reservar cupo, registrar asistencia por QR, y —según el rol— administrar eventos y el sistema de priorización.

**Este repositorio es exclusivamente el frontend (Blazor WebAssembly).** No contiene lógica de negocio ni base de datos. El backend vive en el repositorio hermano [`../fitnessbackend/`](../fitnessbackend/) y expone la API que este cliente consume — ver [su contrato de API](../fitnessbackend/docs/arquitectura/07-contrato-api.md), que es el punto de contacto entre ambos repositorios.

## Antes de escribir una sola línea de código

Lee, en este orden exacto:

1. [docs/requisitos/00-indice.md](docs/requisitos/00-indice.md) — qué hereda este repositorio del backend y qué decide por su cuenta, más los casos de uso que pasan por esta interfaz.
2. [docs/requisitos/01-ambiguedades-ui.md](docs/requisitos/01-ambiguedades-ui.md) — **AMB-014 y las demás preguntas abiertas que son decisiones de interfaz.** No diseñes ninguna pantalla de reserva o de check-in sin haber leído esto.
3. [../fitnessbackend/docs/arquitectura/07-contrato-api.md](../fitnessbackend/docs/arquitectura/07-contrato-api.md) — el contrato exacto: endpoints, roles, y la tabla de mapeo `Resultado` → HTTP.
4. [docs/arquitectura/00-indice.md](docs/arquitectura/00-indice.md) — diseño técnico completo (stack, estado del cliente, patrones, directorios, flujo de QR, identidad visual).
5. [docs/arquitectura/08-guia-de-implementacion.md](docs/arquitectura/08-guia-de-implementacion.md) — **este es tu documento de trabajo día a día**: reglas de comportamiento, tabla de cobertura RF → fase → pantalla, fases 0 a 6 con su Definición de Hecho, estrategia de pruebas.

Si necesitas contexto de negocio (qué significa una regla RN-xxx, por qué existe un RF), la fuente de verdad es [`../fitnessbackend/docs/requisitos/`](../fitnessbackend/docs/requisitos/00-indice.md). **No la dupliques en este repositorio.**

## Las ocho reglas que no se negocian

1. **Nunca reimplementes una regla de negocio en el cliente.** Si una pantalla necesita saber si algo "está permitido", se lo pregunta al backend y muestra su respuesta. Replicar RN-001/RN-025/RN-028 con una condición local es el error más caro posible aquí: se desincroniza del backend sin que nada falle visiblemente.
2. **No resuelvas unilateralmente una ambigüedad abierta.** Implementa el comportamiento menos comprometedor definido en [docs/requisitos/01-ambiguedades-ui.md](docs/requisitos/01-ambiguedades-ui.md) y márcalo con `@* PENDIENTE (AMB-014) *@`. Nunca implementes una interpretación en silencio.
3. **Toda pantalla lleva el ID de su RF de origen** en un comentario al inicio del componente (`@* RF-020: consultar sus propias reservas *@`). Es lo que hace verificable la tabla de cobertura.
4. **Ningún componente de Usuario o Entrenador renderiza nivel de priorización ni indicador de confiabilidad** (RN-020). Es un hallazgo bloqueante de seguridad, no una preferencia de estilo.
5. **Ningún color ni fuente fuera de los tokens** de [docs/arquitectura/07-identidad-visual-y-sistema-de-diseno.md](docs/arquitectura/07-identidad-visual-y-sistema-de-diseno.md). Si el Manual de Imagen ITM no define un valor, es un pendiente — no lo inventes. En particular: **el celeste de acento no cumple contraste AA como texto sobre blanco** y no puede usarse así.
6. **El cliente HTTP de `ApiCliente/` se regenera con Kiota, no se edita a mano.** Si un tipo generado está mal, el problema está en el contrato del backend o en el generador.
7. **Toda fecha se muestra a través de `IZonaHorariaService`.** El backend entrega todo en UTC y este repositorio es el único lugar del sistema donde se convierte a `America/Bogota` — un error aquí rompe ventanas de negocio en silencio.
8. **No edites los documentos de `docs/`** (ni los de este repositorio ni los del backend) para que encajen con el código que ya escribiste. Si crees que el diseño tiene un error, repórtalo.

## Trabajo vigente: preparación para el despliegue (desde 2026-10-31)

Las fases 0 a 6 y el rediseño visual están hechos. El trabajo actual está en **[docs/plan-despliegue/](docs/plan-despliegue/00-indice.md)**: fases **F0 a F10** de [01-guia-frontend.md](docs/plan-despliegue/01-guia-frontend.md); los requisitos nuevos viven en [`../fitnessbackend/docs/plan-despliegue/01-requisitos-nuevos.md`](../fitnessbackend/docs/plan-despliegue/01-requisitos-nuevos.md). Prompt de arranque: [.agents/prompt-despliegue-frontend.md](.agents/prompt-despliegue-frontend.md). Decisiones que lo gobiernan: **sin recuperación de contraseña ni correo** (credenciales institucionales; la sesión queda detrás de `IAutenticacionService`), **QR de ingreso dinámico con pantalla de punto de acceso**, **RN-020 sin posición en lista de espera**, y **pruebas en teléfonos reales y accesibilidad manual** a cargo del usuario.

## Cómo empezar

Ve directo a la **Fase 0** de [docs/arquitectura/08-guia-de-implementacion.md](docs/arquitectura/08-guia-de-implementacion.md) y sigue las fases en orden. Cada una tiene su propia Definición de Hecho ligada a los criterios de aceptación del backend.

**Dependencia de arranque:** la Fase 0 necesita una instancia del backend corriendo en desarrollo que exponga `/openapi/v1.json` (basta su `docker compose`). Sin eso no puedes generar el cliente Kiota. Si no está disponible, dilo en vez de escribir llamadas `HttpClient` a mano "mientras tanto".

## Agentes personalizados de este repositorio

Este workspace define dos agentes en [.agents/agents/](.agents/agents/), pensados para usarse en pareja — uno nunca aprueba su propio trabajo:

- **`constructor-fitnessfrontend`** — implementa cada fase de la guía.
- **`revisor-fitnessfrontend`** — revisa el cierre de cada fase (Definición de Hecho, patrones, cobertura RF, confidencialidad RN-020, identidad visual y accesibilidad) y solo reporta hallazgos; nunca edita código.

Ambos comparten dos skills en [.agents/skills/](.agents/skills/): `pruebas-obligatorias-fitnessfrontend` (qué pruebas no pueden faltar en cada fase) y `verificacion-cobertura-ui` (que toda RF tenga pantalla y toda pantalla tenga RF). Selecciona `constructor-fitnessfrontend` para empezar a programar — él mismo invoca al revisor al cerrar cada fase.

## Si algo no está claro

Este repositorio no tiene código todavía — todo lo que existe es especificación y diseño en `docs/`. Si una decisión de negocio genuina bloquea el avance y no está documentada ni siquiera como pendiente, no la inventes: márcala como bloqueo explícito (`// PENDIENTE`) y sigue con lo que sí está definido. Si la duda es sobre el **contrato de la API**, la respuesta está en el `/openapi/v1.json` del backend en ejecución, que es más autoritativo que cualquier Markdown.
