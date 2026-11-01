# Índice — Requisitos (frontend)

> Este repositorio (`fitnessfrontend`) **no vuelve a levantar los requisitos funcionales del negocio**: esos ya están cerrados y son la fuente de verdad en el repositorio del backend — [`../../fitnessbackend/docs/requisitos/`](../../../fitnessbackend/docs/requisitos/00-indice.md). Duplicarlos aquí violaría la regla de una sola fuente de verdad y los desincronizaría con el tiempo.

Lo que sí vive aquí es el **subconjunto de requisitos que son responsabilidad exclusiva del cliente (Blazor WebAssembly)** — decisiones que el backend delega explícitamente al frontend, según [`07-contrato-api.md`](../../../fitnessbackend/docs/arquitectura/07-contrato-api.md) del backend.

## Documentos de esta carpeta

1. Este índice — qué hereda el frontend y qué decide por su cuenta.
2. [Ambigüedades abiertas que se resuelven en la interfaz](01-ambiguedades-ui.md) — **AMB-014** y las demás preguntas del levantamiento que, por su naturaleza, son decisiones de UI y siguen sin respuesta del stakeholder. **Lectura obligatoria antes de diseñar cualquier pantalla de reserva o de check-in.**

## Cómo leer esto junto con el backend

1. Empieza por [docs/requisitos/00-indice.md](../../../fitnessbackend/docs/requisitos/00-indice.md) del backend — ahí está el problema, los actores, las reglas de negocio (RN-001 a RN-030) y los requisitos funcionales (RF-001 a RF-022).
2. Cada pantalla de este frontend implementa uno o más RF; la trazabilidad se mantiene citando el RF en el nombre del componente o en un comentario, igual que hace el backend.
3. Este documento (01 en adelante, ver índice de arquitectura) solo añade lo que el backend **no puede** decidir por sí mismo: navegación, estados de carga/error, responsive, accesibilidad, y el flujo de UI del check-in por QR (que el backend documenta como "responsabilidad del frontend" en su Decisión 9 revisada).

## Requisitos delegados explícitamente al frontend (citados desde el backend)

| Origen (backend) | Qué delega | Dónde se resuelve aquí |
|---|---|---|
| Decisión 9 revisada, [02-stack-y-hosting.md](../../../fitnessbackend/docs/arquitectura/02-stack-y-hosting.md) | Leer el token de la ruta `/checkin/{token}`, manejar el caso de sesión no iniciada (equivalente a `returnUrl`), reintentar el check-in tras login | [arquitectura/06-flujo-checkin-qr.md](../arquitectura/06-flujo-checkin-qr.md) |
| [07-contrato-api.md](../../../fitnessbackend/docs/arquitectura/07-contrato-api.md), "Qué NO define este documento" | Generar y renderizar la imagen QR a partir del `TokenQR` devuelto por `GET /api/asistencia/sesiones/{id}/qr` | [arquitectura/06-flujo-checkin-qr.md](../arquitectura/06-flujo-checkin-qr.md) |
| Decisión 5, [02-stack-y-hosting.md](../../../fitnessbackend/docs/arquitectura/02-stack-y-hosting.md) | Convertir toda fecha UTC recibida a `America/Bogota` para mostrarla — la API nunca hace esta conversión | [arquitectura/04-patrones-de-diseno.md](../arquitectura/04-patrones-de-diseno.md) |
| RN-020 (backend) | El frontend nunca debe intentar mostrar nivel de priorización/indicador de confiabilidad al Usuario o Entrenador — ni siquiera si un DTO cambiara de forma inesperada, la UI de esos roles no debe tener ningún componente capaz de renderizarlo | [arquitectura/01-priorizacion-calidad-iso25010.md](../arquitectura/01-priorizacion-calidad-iso25010.md) |
| **AMB-014**, y las preguntas abiertas de los Bloques 2/3 que son de interfaz | Qué ve el usuario cuando su ventana de reserva aún no abre, sin violar RN-020; qué se muestra al escanear sin reserva confirmada; si el Entrenador ve la lista de espera | [01-ambiguedades-ui.md](01-ambiguedades-ui.md) — **siguen abiertas, no se resuelven aquí** |

## Casos de uso que se ejecutan a través de esta interfaz

El backend define ocho casos de uso ([08-casos-uso.md](../../../fitnessbackend/docs/requisitos/08-casos-uso.md)) y su estrategia de pruebas incluye un recorrido manual de los ocho. **Seis de ellos solo pueden ejecutarse a través de este frontend** — son la lista de verificación end-to-end de este repositorio:

| Caso de uso | Actor | ¿Tiene UI en este repositorio? |
|---|---|---|
| CU-01 Reservar cupo en una sesión | Usuario | ✅ Sí |
| CU-02 Cancelar una reserva | Usuario | ✅ Sí (inicia con "el usuario consulta sus reservas activas" → RF-020) |
| CU-03 Registrar asistencia vía QR del entrenador | Usuario | ✅ Sí — ver [arquitectura/06-flujo-checkin-qr.md](../arquitectura/06-flujo-checkin-qr.md) |
| CU-04 Registrar ingreso general al gimnasio | Usuario | ✅ Sí |
| CU-05 Crear evento/actividad | Administrador | ✅ Sí |
| CU-06 Consultar actividades asignadas e inscritos | Entrenador | ✅ Sí |
| CU-07 Promoción automática desde lista de espera | Sistema | ❌ No — proceso interno del backend; la UI solo refleja el estado ya cambiado |
| CU-08 Consolidación de una sesión | Sistema | ❌ No — igual que CU-07 |

## Estado

✅ No hay requisitos de negocio pendientes de **levantar** para el frontend — heredan el estado "listo para arquitectura" del backend (ver su documento 20).

⚠️ Sí hay ambigüedades pendientes de **responder** por el stakeholder que afectan directamente a la interfaz — encabezadas por AMB-014. No bloquean el inicio (cada una tiene definido su comportamiento menos comprometedor), pero deben quedar marcadas en el código, no resueltas en silencio. Ver [01-ambiguedades-ui.md](01-ambiguedades-ui.md).
