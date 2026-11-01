# fitnessfrontend

Cliente web (Blazor WebAssembly) de la plataforma de eventos y asistencia del gimnasio universitario del ITM. Consume la API HTTP del repositorio [`fitnessbackend`](../fitnessbackend/) — este repositorio no contiene lógica de negocio propia.

**Estado actual (2026-10-31):** implementado y verificado de punta a punta contra el backend real; ver [docs/checklist-e2e.md](docs/checklist-e2e.md) (incluye lo que aún falta: check-in en teléfono real y revisión de accesibilidad).

## Documentación

| Carpeta | Contenido | Punto de entrada |
|---|---|---|
| [`docs/requisitos/`](docs/requisitos/) | Lo que es responsabilidad exclusiva del cliente, los casos de uso que pasan por esta interfaz, y las ambigüedades abiertas que son decisiones de UI (encabezadas por AMB-014). Los requisitos de negocio viven en `fitnessbackend/docs/requisitos/` | [00-indice.md](docs/requisitos/00-indice.md) |
| [`docs/arquitectura/`](docs/arquitectura/) | Stack y hosting, modelo de estado del cliente, patrones de UI, organización de carpetas, flujo de check-in por QR, sistema de diseño institucional, y la guía de implementación por fases | [00-indice.md](docs/arquitectura/00-indice.md) |

## Agentes

Dos agentes en [`.agents/agents/`](.agents/agents/), pensados para usarse en pareja: `constructor-fitnessfrontend` implementa cada fase, `revisor-fitnessfrontend` la aprueba — uno nunca aprueba su propio trabajo. El prompt de arranque está en [`.agents/prompt-frontend.md`](.agents/prompt-frontend.md).

## Identidad visual

La UI adapta la plantilla base [`../kinetic/`](../kinetic/) a la paleta y tipografía del Manual de Imagen ITM 2025 (`../recursos-diseno/Manual-ITM-V-2025.pdf`) — ver el detalle en [`docs/arquitectura/07-identidad-visual-y-sistema-de-diseno.md`](docs/arquitectura/07-identidad-visual-y-sistema-de-diseno.md).

## Ejecutar en desarrollo

Requisitos: .NET 10 SDK y el backend corriendo (`../fitnessbackend`, ver su README).

```bash
dotnet run --project src/FitnessFrontend --launch-profile http   # http://localhost:5081
dotnet test                                                      # pruebas bUnit y de servicios
```

`wwwroot/appsettings.Development.json` apunta la API a `http://localhost:5224`. El origen del frontend debe figurar en `Cors:AllowedOrigins` del backend.

> Si reconstruye el proyecto (`dotnet build`/`dotnet test`) mientras `dotnet run` está activo, reinicie `dotnet run`: de lo contrario la app se queda en "Cargando…".

## Regenerar el cliente de la API (Kiota)

El cliente de `src/FitnessFrontend/ApiCliente/` **no se edita a mano**. Con el backend en desarrollo (expone `/openapi/v1.json` solo en ese entorno):

```bash
dotnet tool install --global Microsoft.OpenApi.Kiota   # una sola vez
cd src/FitnessFrontend && kiota update -o ./ApiCliente
```

## Despliegue

`wwwroot/appsettings.json` trae el marcador `REEMPLAZAR_EN_DESPLIEGUE` en `UrlBaseApi`; la app **falla al arrancar con un mensaje claro** si no se sustituye.

```bash
docker build -t fitnessfrontend .
docker run -p 8081:8080 -e URL_BASE_API=https://api.ejemplo.edu.co fitnessfrontend
```

La imagen (nginx sin privilegios) genera `appsettings.json` con `URL_BASE_API` al arrancar y devuelve `index.html` para las rutas del cliente. En hostings estáticos (Netlify, etc.) sustituya el marcador en el paso de publicación; `wwwroot/_redirects` ya trae el fallback de rutas. HTTPS es obligatorio (el check-in por QR lo requiere). Registre el origen resultante en `Cors__AllowedOrigins__0` del backend.

## Roles en la interfaz

El bearer token de la API es opaco: los roles se obtienen de `GET /api/usuarios/yo` al iniciar sesión y se guardan junto al token. Si el rol de una persona cambia, lo verá tras volver a iniciar sesión (igual que la API, que autoriza con los roles del token).
