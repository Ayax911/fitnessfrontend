# 05. Organización de directorios

> Árbol concreto para el proyecto Blazor WebAssembly standalone decidido en [02-stack-y-hosting.md](02-stack-y-hosting.md), organizado por módulo de dominio para espejar [fitnessbackend/docs/arquitectura/05-organizacion-directorios.md](../../../fitnessbackend/docs/arquitectura/05-organizacion-directorios.md).

## Árbol completo

```
fitnessfrontend/
├── docs/
│   ├── requisitos/               ← lo que es responsabilidad exclusiva del cliente
│   └── arquitectura/             ← este conjunto de documentos
│
├── src/
│   └── FitnessFrontend/                         ← proyecto único, Microsoft.NET.Sdk.BlazorWebAssembly
│       ├── Program.cs                               (registro de HttpClient + Kiota, DI de servicios por módulo, AuthenticationStateProvider)
│       ├── wwwroot/
│       │   ├── appsettings.json                     (UrlBaseApi por entorno)
│       │   ├── appsettings.Development.json
│       │   ├── css/
│       │   │   ├── itm-tema.css                     (tokens de color/tipografía institucional — ver 07-identidad-visual-y-sistema-de-diseno.md)
│       │   │   └── bootstrap/                        (self-hosted, heredado de la plantilla Kinetic)
│       │   ├── js/
│       │   │   └── interop-qr.js                    (solo si la librería de QR elegida requiere JS interop)
│       │   └── index.html
│       │
│       ├── Layout/
│       │   ├── MainLayout.razor
│       │   ├── NavMenuUsuario.razor
│       │   ├── NavMenuEntrenador.razor
│       │   └── NavMenuAdministrador.razor            (Patrón 4 — Strategy de layout por rol)
│       │
│       ├── Paginas/
│       │   ├── Identidad/                        ← login, registro, confirmación de correo
│       │   │   ├── Login.razor
│       │   │   └── Registro.razor
│       │   │
│       │   ├── Eventos/                          ← RF-001, RF-007, RF-008, RF-009, RF-010
│       │   │   ├── ListaEventos.razor                (cualquier autenticado — RF-001; la acción de reservar vive aquí, sujeta a AMB-014)
│       │   │   ├── MisActividades.razor              (Entrenador — RF-010)
│       │   │   └── Administrar/                       (Administrador — Fase 4)
│       │   │       ├── CrearEvento.razor              (RF-007 + RF-008: la recurrencia es parte de este formulario)
│       │   │       └── EditarEvento.razor             (RF-009 — Should Have, primer candidato a diferir)
│       │   │
│       │   ├── Reservas/                         ← RF-002, RF-003, RF-004, RF-011, RF-020
│       │   │   ├── MisReservas.razor
│       │   │   └── InscritosDeSesion.razor           (Entrenador — RF-011)
│       │   │
│       │   ├── Asistencia/                       ← RF-005, RF-006, RF-012, RF-021
│       │   │   ├── CheckIn.razor                     (ruta /checkin/{token} — ver 06-flujo-checkin-qr.md)
│       │   │   ├── Ingreso.razor                     (ruta /ingreso/{idPuntoAcceso})
│       │   │   ├── MiHistorial.razor
│       │   │   └── QrDeSesion.razor                  (Entrenador — RF-012)
│       │   │
│       │   └── Priorizacion/                     ← RF-016, RF-018 — exclusivo Administrador
│       │       ├── IndicadorUsuario.razor
│       │       └── ConfiguracionSistema.razor
│       │
│       ├── Componentes/
│       │   ├── Compartidos/
│       │   │   ├── CodigoQr.razor                    (Patrón 6 — componente puro de presentación)
│       │   │   ├── EstadoDeCarga.razor               (renderiza los tres casos del Patrón 2)
│       │   │   └── BotonAccion.razor                 (botón institucional con estado de carga integrado)
│       │   └── Eventos/ Reservas/ Asistencia/         (componentes de presentación específicos de cada módulo, ej. tarjeta de sesión, pill de nivel — nunca de priorización para Usuario/Entrenador)
│       │
│       ├── Servicios/
│       │   ├── Identidad/
│       │   │   ├── IAutenticacionService.cs
│       │   │   └── AutenticacionService.cs           (login, logout, persistencia de token en localStorage)
│       │   ├── Eventos/
│       │   │   └── IEventosClienteService.cs / EventosClienteService.cs   (Patrón 1)
│       │   ├── Reservas/
│       │   │   └── IReservasClienteService.cs / ReservasClienteService.cs
│       │   ├── Asistencia/
│       │   │   ├── IAsistenciaClienteService.cs / AsistenciaClienteService.cs
│       │   │   └── IFlujoCheckInService.cs / FlujoCheckInService.cs        (Patrón 6)
│       │   ├── Priorizacion/
│       │   │   └── IPriorizacionClienteService.cs / PriorizacionClienteService.cs
│       │   └── Infraestructura/
│       │       ├── EstadoDeCarga.cs                  (Patrón 2, compartido por todos los servicios)
│       │       ├── IZonaHorariaService.cs / ZonaHorariaService.cs          (Patrón 3)
│       │       └── ManejadorDeAutenticacionHttp.cs   (DelegatingHandler, Patrón 7)
│       │
│       └── ApiCliente/                            ← generado por Kiota, NO editado a mano
│           └── (código generado — se regenera con `kiota generate` cuando el backend publica un contrato nuevo)
│
└── tests/
    └── FitnessFrontend.Tests/
        ├── Servicios/                             (pruebas de EstadoDeCarga, IZonaHorariaService, FlujoCheckInService — con bUnit para componentes)
        └── Componentes/
```

## Regla de dependencias

Espeja la regla del backend, adaptada al sentido único cliente → servidor:

1. **`Paginas/` y `Componentes/` nunca llaman directamente al cliente Kiota generado (`ApiCliente/`).** Siempre pasan por el servicio de módulo correspondiente (Patrón 1).
2. **Ningún servicio de módulo (`Servicios/<Modulo>/`) referencia el servicio de otro módulo directamente**, salvo a través de su interfaz pública — igual que la regla de dependencias entre módulos del backend. Ejemplo: `AsistenciaClienteService` puede depender de `IReservasClienteService` (para saber si hay una reserva confirmada antes de mostrar la pantalla de check-in), nunca de `ReservasClienteService` (la implementación concreta).
3. **`ApiCliente/` es código generado.** Ninguna clase fuera de esa carpeta se edita para "corregir" algo generado — si el tipo generado está mal, el problema está en el contrato del backend o en la versión del generador, no en el código del cliente.
4. **Los componentes de Usuario/Entrenador no importan ningún tipo del módulo `Priorizacion`.** Es la aplicación directa del Patrón 5.

## Convenciones de nombres

| Aspecto | Convención | Ejemplo |
|---|---|---|
| Idioma de páginas y dominio | **Español**, igual que el backend, para mantener la trazabilidad hacia los RF/RN | `MisReservas.razor`, `IReservasClienteService` |
| Idioma de infraestructura/framework | **Inglés** cuando lo exige la convención de Blazor/.NET | `Program.cs`, `MainLayout.razor`, `IHttpClientFactory` |
| Página enrutable | Sufijo implícito por ubicación en `Paginas/`, con `@page "/ruta"` explícita al inicio del archivo | `@page "/checkin/{Token}"` |
| Servicio de módulo (fachada) | `I<Modulo>ClienteService` / `<Modulo>ClienteService` | `IReservasClienteService` |
| Componente de presentación puro | Sin sufijo de "Service" ni lógica de red — solo `[Parameter]` de entrada | `CodigoQr.razor` |
| Tipo de resultado de UI | `EstadoDeCarga<T>` | — |
