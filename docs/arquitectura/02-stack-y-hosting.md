# 02. Decisión de stack tecnológico y hosting (frontend)

> Documento de decisión arquitectónica (ADR), simétrico a [fitnessbackend/docs/arquitectura/02-stack-y-hosting.md](../../../fitnessbackend/docs/arquitectura/02-stack-y-hosting.md), Decisión 4 (revisada) y Decisión 9 (revisada). Fecha: 2026-09-27.

## Contexto

- El backend (`fitnessbackend`) ya decidió separarse en dos repositorios el 2026-09-26: expone una API HTTP pura, y este repositorio (`fitnessfrontend`) es el único consumidor conocido hasta ahora.
- Stack confirmado por experiencia previa del stakeholder: **.NET y Blazor** (mismo criterio que fijó el lenguaje del backend).
- Fecha objetivo del prototipo: finales de noviembre de 2026 — igual que el backend.
- Población: 30.000 estudiantes, mayoritariamente accediendo desde el teléfono para el check-in por QR.

## Decisión 1: Blazor WebAssembly standalone, no Blazor Server ni híbrido

| Opción | Por qué se descarta / se acepta |
|---|---|
| **Blazor WebAssembly standalone (elegida)** | Se publica como archivos estáticos (HTML/JS/WASM). No requiere proceso .NET propio, no requiere SignalR, no depende de que un host se mantenga siempre activo — a diferencia del backend (que sí lo necesita por RF-022). Encaja exactamente con la Decisión 4 revisada del backend: "un cliente separado que no comparte dominio ni sesión de cookies". |
| Blazor Server | Requeriría su propio proceso siempre activo y una conexión SignalR persistente — la misma razón por la que el backend descartó Blazor Server sirviendo HTML en su Decisión 4 original. No aporta nada aquí que WASM no resuelva más barato. |
| MAUI Blazor Hybrid / app nativa | Fuera de alcance: RF no exige app instalable; el QR ya está pensado para abrirse en el navegador del teléfono (Decisión 9 revisada del backend), no dentro de una app nativa. |

## Decisión 2: Runtime y versión

| Capa | Elección | Justificación |
|---|---|---|
| Runtime | **.NET 10 (LTS)** | Misma razón que el backend: .NET 8/9 llegan a fin de soporte el 10 de noviembre de 2026, justo antes de la entrega del prototipo. Ambos repositorios deben coincidir en versión de LTS para que el equipo no mantenga dos ciclos de vida distintos. |
| UI | **Blazor WebAssembly standalone** (`Microsoft.NET.Sdk.BlazorWebAssembly`) | Ver Decisión 1. |
| Cliente HTTP | **Kiota** generado contra `/openapi/v1.json` del backend | Recomendación explícita del backend en su [07-contrato-api.md](../../../fitnessbackend/docs/arquitectura/07-contrato-api.md) — soporte de primera clase para C#, mantenimiento activo, evita escribir `HttpClient` a mano. |
| Autenticación | Bearer token emitido por `MapIdentityApi<ApplicationUser>()` del backend, guardado con `ILocalStorageService` (o `ProtectedBrowserStorage` equivalente en WASM) | El backend no ofrece cookies cross-origin; el modelo de Identity para SPA/WASM es exactamente bearer token + almacenamiento en el cliente. |
| QR | Librería de generación de código QR **del lado del cliente** (ej. `QRCoder` compilado a WASM, o una librería JS vía interop) a partir del `TokenQR` que devuelve el backend | El backend documenta explícitamente que "generar y renderizar el código QR a partir de ese token es responsabilidad del repositorio de frontend" ([07-contrato-api.md](../../../fitnessbackend/docs/arquitectura/07-contrato-api.md)). |
| Estilos base | **Bootstrap 5** (heredado de la plantilla Kinetic, ver [07-identidad-visual-y-sistema-de-diseno.md](07-identidad-visual-y-sistema-de-diseno.md)) | Ya viene self-hosted en la plantilla; se reutiliza en vez de introducir Tailwind u otro sistema, priorizando Mantenibilidad (#1) sobre preferencia estética. |
| CI/CD | **GitHub Actions** | Build + publish de Blazor WASM a un hosting estático; simétrico a la elección del backend. |

**Lenguaje: C#.** Mismo razonamiento que el backend: con 9 semanas de plazo, introducir TypeScript/React solo para el frontend duplicaría el stack que el equipo debe dominar sin ninguna ventaja que un requisito exija.

## Decisión 3: Hosting — archivos estáticos

A diferencia del backend, este repositorio **no** está sujeto al veto de Vercel del documento 02 del backend (Decisión 1 de ese documento): Blazor WebAssembly publicado es únicamente HTML/CSS/JS/WASM servidos como archivos estáticos, sin ningún runtime de servidor.

| # | Opción | Costo | Notas |
|---|--------|-------|-------|
| 🥇 | **Vercel** o **Cloudflare Pages** | $0 | CDN global, despliegue automático desde GitHub en cada push, dominio HTTPS gratuito. Recomendada por simplicidad de integración con GitHub Actions. |
| 🥈 | **GitHub Pages** | $0 | Válida si no se necesita configuración de reescritura de rutas más allá de un `404.html` de fallback (necesario para el enrutamiento del lado del cliente de Blazor). |
| 🥉 | **Netlify** | $0 | Equivalente a Vercel; elegir cualquiera de las dos según preferencia del equipo, no hay un criterio técnico que decida entre ambas. |

**Requisito no negociable: HTTPS.** El flujo de check-in (documento 06) depende de que el navegador considere la página un contexto seguro; los tres hostings de la tabla lo dan por defecto.

**CORS:** el origen de este frontend (`http://localhost:<puerto>` en desarrollo, el dominio de producción cuando exista) debe registrarse explícitamente en la configuración de CORS del backend — ver la sección "Decisiones pendientes" de [fitnessbackend/docs/arquitectura/02-stack-y-hosting.md](../../../fitnessbackend/docs/arquitectura/02-stack-y-hosting.md). Este repositorio no puede desbloquearse a sí mismo esa restricción; es una coordinación entre los dos repositorios.

## Decisión 4: Zona horaria — toda conversión ocurre aquí, nunca en el backend

El backend almacena y transmite **todo** en UTC (`DateTimeOffset`) por decisión explícita (ver su documento 02, Decisión 5, regla transversal). Este repositorio es el **único** lugar del sistema donde una fecha se convierte a `America/Bogota` para mostrarse al usuario. Se centraliza en un único servicio (`IZonaHorariaService`, ver [04-patrones-de-diseno.md](04-patrones-de-diseno.md)) — ningún componente hace `.ToLocalTime()` ni aritmética de fechas por su cuenta.

## Dependencia de arranque

Este repositorio no puede generar su cliente Kiota ni empezar la Fase 0 de implementación (ver [08-guia-de-implementacion.md](08-guia-de-implementacion.md)) hasta que exista una instancia del backend corriendo en modo desarrollo exponiendo `/openapi/v1.json` (local, vía `docker compose` del propio backend, es suficiente). No es una dependencia de despliegue — es una dependencia del primer día de código.

## Topología de despliegue

```
                    Internet (HTTPS)
                          │
              ┌───────────┴───────────┐
              │                       │
      ┌───────▼────────┐     ┌────────▼────────┐
      │  Vercel/CF Pages │     │   Coolify        │  ← fitnessbackend
      │  (este repo,     │     │   (API .NET 10)  │     (repositorio separado)
      │  WASM estático)  │     │                  │
      └───────┬──────────┘     └────────┬─────────┘
              │      llamadas HTTP/JSON  │
              └──────────────────────────┘
                  Authorization: Bearer <token>
                  CORS explícito por origen
```

Sin servidor propio, sin proceso de fondo, sin base de datos en este repositorio — todo el estado persistente vive en el backend; este repositorio solo mantiene estado efímero (sesión del navegador) y de presentación.
