# 04. Patrones de diseño (frontend)

> Simétrico a [fitnessbackend/docs/arquitectura/04-patrones-de-diseno.md](../../../fitnessbackend/docs/arquitectura/04-patrones-de-diseno.md), pero para las decisiones propias de una UI Blazor WebAssembly. Cada patrón indica el problema que resuelve y por qué se prefirió sobre la alternativa.

## Patrón 1 — Servicio por módulo como fachada del cliente HTTP

**Problema:** los componentes `.razor` no deben llamar directamente al cliente Kiota generado ni construir URLs a mano.

**Decisión:** un servicio de aplicación por módulo de dominio (`IEventosClienteService`, `IReservasClienteService`, `IAsistenciaClienteService`, `IPriorizacionClienteService` — este último solo inyectado en las páginas de Administrador), que envuelve el cliente Kiota y traduce la respuesta a un tipo de resultado uniforme para la UI (ver Patrón 3). Mismo espíritu que el patrón Facade del backend (Patrón 1 de su documento 04), aplicado del lado del consumidor.

**Alternativa descartada:** inyectar `HttpClient`/el cliente Kiota directamente en cada componente. Se descarta porque duplicaría el manejo de errores y de tokens en cada `.razor`, violando Mantenibilidad (#1).

## Patrón 2 — `EstadoDeCarga<T>` (Result Object del lado de la UI)

**Problema:** cada pantalla necesita representar tres estados — cargando, éxito con datos, error — sin recurrir a `null` ambiguo ni a excepciones para flujo de control.

**Decisión:** un tipo `EstadoDeCarga<T>` con tres casos (`Cargando`, `Exito(T valor)`, `Error(string mensaje)`), consumido con un `switch` en el marcado Razor. Es el equivalente en el cliente del `Resultado`/`Resultado<T>` del backend (Patrón 3 de su documento 04) — incluso el nombre se eligió deliberadamente paralelo para que un desarrollador que conoce un repositorio reconozca el patrón en el otro.

**Alternativa descartada:** dos variables sueltas por componente (`bool cargando`, `string? error`). Se descarta porque permite estados imposibles (cargando y con error a la vez) que `EstadoDeCarga<T>` hace irrepresentables por construcción.

## Patrón 3 — Adapter de zona horaria (`IZonaHorariaService`)

**Problema:** el backend entrega todo en UTC (`DateTimeOffset`) por decisión explícita; algún punto del cliente debe convertir a `America/Bogota` sin que la conversión se disperse por todos los componentes.

**Decisión:** un único servicio `IZonaHorariaService` con un método `AHoraLocal(DateTimeOffset utc)`. Todo componente que muestra una fecha lo usa; ninguno llama `.ToLocalTime()` directamente. Es un Adapter en el mismo sentido que `INotificacionSender` lo es en el backend (Patrón 11 de su documento 04): aísla una dependencia externa (la zona horaria del entorno de ejecución del navegador, que no es confiable) detrás de una interfaz propia.

**Alternativa descartada:** confiar en `DateTimeOffset.ToLocalTime()` del navegador. Se descarta porque la zona horaria del dispositivo del usuario no está garantizada a ser `America/Bogota` (un estudiante de intercambio, un dispositivo mal configurado), y el sistema entero depende de ventanas temporales exactas (RN-025, RN-028 del backend) — mostrar la hora equivocada aquí puede hacer que alguien llegue tarde a su propia ventana de check-in.

## Patrón 4 — Strategy de layout por rol

**Problema:** Usuario, Entrenador y Administrador tienen navegación y pantallas disponibles distintas, y ninguna pantalla de un rol debe ser alcanzable, ni por URL directa, por otro rol.

**Decisión:** un `AuthorizeView`/layout por rol en el enrutador de Blazor, más una verificación de rol en cada página (`[Authorize(Roles = "...")]` sobre el componente), replicando del lado de UI la misma tabla de "Rol requerido" que ya existe en [07-contrato-api.md](../../../fitnessbackend/docs/arquitectura/07-contrato-api.md) del backend. El backend sigue siendo la autoridad real (rechaza con `403` igual si la UI fallara), pero la UI no debe ni mostrar el enlace a una pantalla que el rol actual no puede usar.

**Nota:** esto es una mejora de usabilidad, no un mecanismo de seguridad — la seguridad real vive en el backend (`RequireAuthorization(Roles.X)`), tal como aclara explícitamente el propio contrato de API.

## Patrón 5 — DTOs por rol como barrera adicional a RN-020

**Problema:** RN-020 (el Usuario y el Entrenador nunca ven nivel de priorización ni indicador de confiabilidad) ya se hace cumplir en el backend con DTOs distintos por rol (Decisión 8 del documento 02 del backend). Pero un frontend descuidado podría reintroducir el campo si alguien usa un tipo genérico.

**Decisión:** los componentes de Usuario y Entrenador consumen exclusivamente los tipos generados a partir de los DTOs específicos de esos roles (`ReservaResumenDto`, `ReservaInscritoDto`, `HistorialAsistenciaDto`, `ActividadAsignadaDto`) — nunca un `Dictionary<string, object>`, un `JsonElement` crudo, ni un tipo compartido con el DTO de Administrador. Si el tipo generado no tiene el campo, es sintácticamente imposible renderizarlo.

## Patrón 6 — Componente de QR desacoplado del flujo de negocio

**Problema:** la generación del código QR (a partir de `TokenQR`) es una preocupación de presentación; el flujo de qué hacer cuando el usuario no tiene sesión iniciada (documento 06) es una preocupación de navegación/autenticación. Mezclarlas en un solo componente las vuelve difíciles de probar por separado.

**Decisión:** un componente puro `<CodigoQr Valor="@url" />` que solo sabe dibujar un QR a partir de un string, sin saber nada de asistencia ni de sesiones — y, por separado, un servicio de flujo (`IFlujoCheckInService`) que decide si mostrar login o completar el check-in. Es el mismo principio de "núcleo puro vs. servicio impuro" que el backend aplica a sus clases `Calculador*`/`Evaluador*` (ver su documento 04).

## Patrón 7 — Interceptor único de autenticación en el cliente HTTP

**Problema:** cada llamada autenticada necesita el header `Authorization: Bearer <token>`, y cada `401` necesita disparar el mismo flujo de renovación/logout.

**Decisión:** un `DelegatingHandler` único registrado en el `HttpClient` que usa el cliente Kiota, que añade el header y intercepta `401` en un solo lugar. Ningún servicio de módulo (Patrón 1) maneja tokens por su cuenta.

**Alternativa descartada:** pasar el token como parámetro a cada método de cada servicio de módulo. Se descarta por la misma razón que el backend prefiere `IOptions<T>` sobre pasar configuración a mano por todos lados: centraliza un mecanismo transversal en un único punto de cambio.
