# Plan de preparación para el despliegue — fitnessfrontend

> Fecha de elaboración: 2026-10-31. Plazo del prototipo: finales de noviembre de 2026.
> Lo ejecuta Antigravity con `constructor-fitnessfrontend` y `revisor-fitnessfrontend`. **Este documento cubre solo el frontend**; el backend tiene su propio plan en [`fitnessbackend/docs/plan-despliegue/`](../../../fitnessbackend/docs/plan-despliegue/00-indice.md), que es **la fuente de verdad de los requisitos nuevos** (RF-026 a RF-042, RN-031 a RN-036, `AMB-D01…`). Aquí no se duplican: se referencian.

## Punto de partida

Cliente Blazor WebAssembly con el rediseño visual aplicado ([09-rediseno-visual.md](../arquitectura/09-rediseno-visual.md)): logo oficial, navegación responsive, `InsigniaEstado`, esqueletos de carga, estados de interacción. Pantallas de estudiante rediseñadas; las de administración heredan el aspecto por los estilos globales. 89 pruebas en verde. Rama `feat/pendientes-1-11`.

## Decisiones del usuario (2026-10-31) que gobiernan este plan

| # | Decisión | Consecuencia en el frontend |
|---|---|---|
| D1 | Sin notificaciones ni recuperación de contraseña: se integrará con **credenciales institucionales**. | No se construyen pantallas de "olvidé mi contraseña" ni de confirmación de correo. Los avisos son **dentro de la app**. **Restricción de diseño:** todo lo relacionado con iniciar/cerrar sesión y registro queda detrás de `IAutenticacionService` y en `Paginas/Identidad/`, de modo que el cambio a SSO sea una sustitución local. |
| D2 | Se añaden todas las funcionalidades por rol (menos recordatorios y correo). | F2 a F7. |
| D3 | Apelación y derechos del titular: solo tenerlos en cuenta. | Diseño de UI en F10 (documentación); sin pantallas. |
| D4 | QR dinámico. Pruebas de carga y de sistema al desplegar. | F6 (pantalla del punto de acceso). El frontend aporta a la auditoría de desempeño con su parte de cliente (ver [03](03-preparacion-despliegue-frontend.md)). |
| D5 | Datos de demostración, retroalimentación, piloto, pruebas en teléfonos reales. | F7, F9 y [02-pruebas-en-dispositivos-y-accesibilidad.md](02-pruebas-en-dispositivos-y-accesibilidad.md). |

## Organización

| Documento | Contenido |
|---|---|
| [01-guia-frontend.md](01-guia-frontend.md) | **Documento de trabajo**: fases F0 a F10, cada una con pantallas, consumo de contrato, Definición de Hecho y pruebas obligatorias. |
| [02-pruebas-en-dispositivos-y-accesibilidad.md](02-pruebas-en-dispositivos-y-accesibilidad.md) | Pruebas en teléfonos reales, accesibilidad manual (WCAG 2.1 AA) y verificación visual pendiente. Parte la hace el usuario. |
| [03-preparacion-despliegue-frontend.md](03-preparacion-despliegue-frontend.md) | Publicación, cabeceras, caché, tamaño de descarga, PWA y la parte de cliente de la auditoría de desempeño. |

## Calendario y dependencias

El frontend **no puede regenerar Kiota hasta que el backend publique el contrato** de cada paquete. Orden recomendado (alineado con el calendario del backend):

| Semana | Frontend | Necesita del backend |
|---|---|---|
| 1 | **F0** documentación · **F1** renovación de sesión (RF-026) | B0 (docs); `/api/auth/refresh` ya existe |
| 2 | **F2** programación semanal, .ics, ayuda · **F3** entrenador (asistencia manual, notas, anuncios) y sesión cancelada en el usuario | B2, B3, B5 |
| 3 | **F4** administración de sesiones, bloqueos, reservas de terceros, auditoría · **F5** reportes · **F6** pantalla del punto de acceso (QR dinámico) | B1, B2, B3, B4, B6 |
| 4 | **F7** retroalimentación · **F8** PWA (opcional) · **F9** cierre visual y E2E · ensayo con el guion de piloto | B7, B8 |

Mientras un contrato no esté publicado, el constructor **trabaja con una fachada y un servicio falso** (como `UsuariosClienteServiceFalso`) para las pruebas de componente, pero **no escribe llamadas `HttpClient` a mano** ni toca `ApiCliente/` (regla 6 del AGENTS). Si el backend no está, lo reporta y sigue con otra fase.

## Reglas que no cambian

Las ocho del `AGENTS.md`, más: **RN-020** (ningún dato de priorización ni posición en lista de espera en pantallas de Usuario/Entrenador), **tokens de `itm-tema.css`** (ningún color ni fuente suelto), **fechas por `IZonaHorariaService`**, y **accesibilidad como requisito**: texto + icono (nunca solo color), foco visible, objetivos táctiles de 48 px, esqueletos anunciados con texto.
