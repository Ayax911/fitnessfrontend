# 03. Modelo de estado y datos del cliente

> Este repositorio no tiene modelo de datos propio — el modelo de datos real (entidades, relaciones, concurrencia) vive en [fitnessbackend/docs/arquitectura/03-modelo-de-datos.md](../../../fitnessbackend/docs/arquitectura/03-modelo-de-datos.md). Lo que este documento define es **qué información retiene el cliente, dónde, y por cuánto tiempo** — el equivalente frontend a un modelo de datos, pero de estado transitorio, no de persistencia.

## Qué vive en cada lugar

| Almacenamiento | Contenido | Vida |
|---|---|---|
| **Memoria (estado de la app, `AuthenticationStateProvider`)** | Bearer token activo, claims del usuario (id, rol), resultado de la última llamada por pantalla | Se pierde al recargar la página; se reconstruye desde `localStorage` en el arranque |
| **`localStorage`** | Bearer token, refresh token, hora de expiración | Persiste entre sesiones del navegador — es lo que evita pedir login en cada visita |
| **Estado de componente (`@code` de cada `.razor`)** | Datos de la pantalla actual (lista de eventos, DTO de una reserva, etc.), siempre obtenidos frescos del backend al entrar a la pantalla | Se descarta al navegar fuera de la página — **nunca** se cachea entre pantallas un DTO que refleje cupo o estado de reserva, porque quedaría obsoleto frente a la concurrencia real que el backend resuelve con `xmin` (ver Decisión 5 del backend) |
| **Query string de la ruta** | Únicamente identificadores no sensibles de navegación (`idEvento`, `idSesion`) y el `tokenSesion`/`idPuntoAcceso` del flujo de QR (documento 06) | No se coloca aquí nada del bearer token ni datos personales — ver regla de Privacidad en el documento 01 |

## Regla explícita: no hay caché de datos de negocio entre pantallas

A diferencia de una aplicación típica que cachea "eventos" o "cupos" para sentirse más rápida, este sistema tiene una razón concreta para no hacerlo: el cupo de una `SesionEvento` cambia por acción de otros usuarios en tiempo real (RN-001), y el nivel de priorización de un usuario nunca debe filtrarse a un componente que no sea de Administrador (RN-020). Cachear un DTO fuera del ciclo de vida de su propia pantalla reintroduce ambos riesgos. Cada pantalla vuelve a pedir su dato al backend al montarse (`OnInitializedAsync`); no hay un `StateContainer` global de "eventos" o "reservas".

**Única excepción explícita:** los datos del propio usuario autenticado (nombre, rol, id) obtenidos una vez tras login, porque son estables durante la sesión y no participan de ninguna condición de carrera.

## DTOs consumidos (generados, no definidos aquí)

Los tipos de datos exactos que el cliente ve son los generados por Kiota a partir de `/openapi/v1.json` del backend — este documento no los redefine para evitar que Markdown y código diverjan (la misma razón que el backend da para no repetir sus DTOs en `07-contrato-api.md`). Los agrupamos aquí solo por el rol que los consume, como mapa de lectura:

| Rol | DTOs que ve (según [07-contrato-api.md](../../../fitnessbackend/docs/arquitectura/07-contrato-api.md) del backend) | Nunca incluyen |
|---|---|---|
| Usuario | `EventoResumenDto`, `ReservaResumenDto`, `HistorialAsistenciaDto` | Nivel de priorización, indicador de confiabilidad (RN-020) |
| Entrenador | `ActividadAsignadaDto`, `ReservaInscritoDto`, token/ventana de QR | Igual que arriba |
| Administrador | Todo lo anterior + `IndicadorAdministradorDto`, `ConfiguracionPriorizacion` | — (único rol con acceso) |

## Manejo de `Resultado`/`Resultado<T>` en el cliente

El backend traduce `Resultado`/`Resultado<T>` a códigos HTTP según la tabla de su documento 07. El cliente debe deshacer esa traducción de forma consistente en un único punto (ver el patrón de manejo de resultados en [04-patrones-de-diseno.md](04-patrones-de-diseno.md)):

| Respuesta HTTP | Estado de UI resultante |
|---|---|
| `2xx` con cuerpo | Se renderiza el dato |
| `204 No Content` | Se muestra una confirmación (toast/mensaje), no una pantalla vacía |
| `400`/`409` con `{ codigoError, mensaje }` | Se muestra `mensaje` tal cual lo da el backend — el frontend no reinterpreta ni traduce `codigoError` a otro texto salvo que el mensaje del backend sea genuinamente insuficiente para el usuario final (a decidir por pantalla, documentado en el componente). **Excepción marcada: `VentanaNoAbierta`** — ver el recuadro de abajo |
| `401` | Se limpia el token local y se redirige a login, guardando la ruta actual para volver tras autenticarse (patrón `returnUrl` del lado del cliente — ver documento 06) |
| `403` | Se muestra "no autorizado", nunca se intenta reintentar con otro rol ni se oculta silenciosamente el error |
| Error de red (sin respuesta) | Se muestra un estado de "sin conexión", con opción de reintentar — nunca se interpreta como "sin resultados" |

### Caso especial: `VentanaNoAbierta` (AMB-014, sin resolver)

Este `codigoError` es el único donde mostrar el mensaje del backend tal cual **podría** violar RN-020, porque el motivo real por el que la ventana no está abierta para *este* usuario es su nivel de priorización — que el usuario no debe conocer. La ambigüedad AMB-014 (¿mensaje genérico, u ocultar la acción sin explicación?) **sigue abierta**: no se resuelve en este documento.

Comportamiento a implementar mientras tanto, y su marcador obligatorio en el código, están definidos en [../requisitos/01-ambiguedades-ui.md](../requisitos/01-ambiguedades-ui.md). Regla dura asociada: **ningún texto de esta pantalla menciona nivel, indicador, motivo ni fecha estimada de habilitación.**
