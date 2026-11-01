# Prompt para Antigravity — arranque del repositorio de frontend

> Pegar este prompt en Antigravity tras abrir el workspace en `fitnessfrontend/` y seleccionar el agente `constructor-fitnessfrontend` con `/agents`.
> Contexto: este repositorio está vacío de código — solo existe documentación de diseño en `docs/`. El backend ya está especificado (y en implementación) en el repositorio hermano `fitnessbackend/`.

---

Vas a arrancar el repositorio de frontend desde cero. **Todo lo que necesitas está especificado en `docs/` — léelo antes de escribir nada.** No hay código previo que conservar, pero sí hay un diseño cerrado que no debes reinterpretar.

## Antes de tocar nada

Lee en este orden, completo:

1. `AGENTS.md` (raíz) — las ocho reglas no negociables.
2. `docs/requisitos/00-indice.md` — qué hereda este repositorio del backend y qué decide por su cuenta.
3. `docs/requisitos/01-ambiguedades-ui.md` — **AMB-014 y las otras cuatro ambigüedades abiertas que son decisiones de interfaz.** Esto es lo que más fácil se viola sin darse cuenta.
4. `../fitnessbackend/docs/arquitectura/07-contrato-api.md` — el contrato exacto que vas a consumir.
5. `docs/arquitectura/00-indice.md` y todos los documentos que enumera (01 a 08).

El documento `docs/arquitectura/08-guia-de-implementacion.md` es tu plan de trabajo: tiene la tabla de cobertura RF → fase → pantalla y las seis fases con su Definición de Hecho.

## Qué es este repositorio

Un cliente **Blazor WebAssembly standalone sobre .NET 10 LTS**, publicado como archivos estáticos. Consume por HTTP la API de `fitnessbackend` con bearer tokens. No tiene base de datos, no tiene proceso de servidor, y **no contiene ni una sola regla de negocio**: el backend decide, este repositorio muestra.

## Dependencia de arranque (resuélvela primero)

La Fase 0 necesita el `/openapi/v1.json` del backend en ejecución para generar el cliente HTTP con Kiota. Antes de nada:

1. Verifica si hay una instancia del backend accesible (su propio `docker compose` en `../fitnessbackend/` es suficiente).
2. Si la hay, genera el cliente en `src/FitnessFrontend/ApiCliente/` con Kiota y sigue.
3. **Si no la hay, detente y dímelo.** No escribas llamadas `HttpClient` a mano "mientras tanto" ni inventes los DTOs a partir del Markdown: el contrato exacto es el OpenAPI, y un cliente escrito a mano se desincroniza sin que nada falle visiblemente.

## Trabajo a realizar

Ejecuta las **Fases 0, 1 y 2** del documento 08, en orden, con su Definición de Hecho completa:

- **Fase 0 — Cimientos:** proyecto Blazor WASM, tema visual institucional (`itm-tema.css`), cliente Kiota, autenticación con bearer token persistido, `EstadoDeCarga<T>`, `IZonaHorariaService`, `ManejadorDeAutenticacionHttp`. Cubre RF-013.
- **Fase 1 — Eventos y navegación por rol:** RF-001 y RF-010, con layout por rol.
- **Fase 2 — Reservas:** RF-002, RF-003, RF-004, RF-011, RF-020.

Al cerrar **cada** fase: ejecuta `dotnet build` y `dotnet test`, y luego invoca a `revisor-fitnessfrontend` con el número de fase y los archivos tocados. No avances con hallazgos bloqueantes abiertos.

## Las cinco cosas que más fácil vas a romper (préstales atención extra)

1. **AMB-014.** Cuando el backend rechace una reserva con `VentanaNoAbierta`, el motivo real es el nivel de priorización del usuario — que él **no debe conocer** (RN-020). Implementa el comportamiento menos comprometedor de `docs/requisitos/01-ambiguedades-ui.md` (acción deshabilitada + texto genérico neutro) con su marcador `@* PENDIENTE (AMB-014) *@`. **No escribas un mensaje más explicativo pensando que mejoras la experiencia** — eso es exactamente la violación.
2. **RN-020 como barrera de tipo.** Los componentes de Usuario y Entrenador consumen solo los DTO generados de esos roles. Nada de `JsonElement`, `dynamic` ni `Dictionary<string, object>`.
3. **Zona horaria.** El backend entrega todo en UTC. Este repositorio es el único lugar del sistema que convierte a `America/Bogota`, y lo hace **solo** a través de `IZonaHorariaService`. Ningún `.ToLocalTime()` suelto: el sistema entero está gobernado por ventanas temporales y un error aquí rompe reglas de negocio en silencio.
4. **Nada de lógica de negocio en el cliente.** Si necesitas saber si algo está permitido, pregúntaselo al backend. No calcules cupos, ni compares fechas contra ventanas de reserva, ni deduzcas niveles.
5. **Identidad visual.** Solo los tokens de `docs/arquitectura/07-identidad-visual-y-sistema-de-diseno.md`. Tres valores del Manual de Imagen ITM están en disputa y documentados como pendientes — **no elijas uno por tu cuenta**. Y el celeste de acento **no cumple contraste AA como texto sobre blanco** (2.5:1): úsalo solo como decoración, borde, o texto sobre el azul primario.

## Sobre la plantilla base

`../kinetic/` es una plantilla de gimnasio (Bootstrap 5 + JS vanilla) de la que se reutiliza **la estructura y el comportamiento** (grid, tarjetas, tabs del horario semanal, accesibilidad: skip link, ARIA, `:focus-visible`, `prefers-reduced-motion`), **no su paleta ni su tipografía** — que se reemplazan por las institucionales del ITM. El mapeo componente a componente está en el documento 07.

## Reglas vigentes durante todo el trabajo

- No resuelvas una ambigüedad abierta por tu cuenta: comportamiento menos comprometedor + `PENDIENTE` que cite el ID.
- Toda página enrutable lleva el ID de su RF de origen en un comentario.
- `ApiCliente/` se regenera con Kiota, nunca se edita a mano.
- No edites los documentos de `docs/` (ni los de este repositorio ni los del backend) para que encajen con tu código. Si crees que el diseño tiene un error, repórtalo.

Cuando termines, dime qué fases quedaron cerradas, qué pruebas corren, qué reportó el revisor, y qué `PENDIENTE` quedaron marcados en el código.
