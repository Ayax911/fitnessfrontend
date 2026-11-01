# Historial de cambios — fitnessfrontend

Rama `feat/pendientes-1-11`. Cada entrada corresponde a un commit.

## Plan de despliegue (2026-10-31)
- Plan de preparación para el despliegue en `docs/plan-despliegue/` (fases F0–F10), agentes y prompt actualizados (`.agents/prompt-despliegue-frontend.md`). Solo planificación: sin cambios de código.

## Rediseño visual (8f0c4ad)
- Sistema de diseño ITM aplicado en `itm-tema.css`; logo oficial; navegación responsive (barra superior, menú desplegable, barra inferior del Usuario en celular).
- `InsigniaEstado` (tinta suave, seis estados), esqueletos de carga, selector de día y cupo sin puntos en Eventos.
- Pantallas rediseñadas: Eventos, Mis reservas, Mi asistencia, Login, Registro, Check-in, Ingreso.
- Documentado en [docs/arquitectura/09-rediseno-visual.md](docs/arquitectura/09-rediseno-visual.md). Pruebas: 89.

## Cierre y verificación (276fcfb, 9d90229, 959d88d)
- Checklist E2E con los defectos hallados, README actualizado, I-10 cerrado.
- Roles desde `GET /api/usuarios/yo`, banner de error oculto por defecto, rótulos de historial e ingreso; el ingreso muestra el nombre del punto y no el token del QR.

## Despliegue (a17ce4d)
- Imagen Docker con nginx sin root, URL de la API por entorno y CI.

## Usuarios, roles y puntos de acceso (6d12047, b32cec4)
- Cliente Kiota regenerado.
- Pantallas de gestión de usuarios y roles (RF-023), puntos de acceso con QR imprimible (RF-024), selector de entrenador, búsqueda de usuario en Priorización y registro ampliado con autorización de datos (RF-025).

## Limpieza (729e9ed, f278492)
- Restos de plantilla eliminados, `Home` y `NotFound` movidos a `Paginas/`, línea base del repositorio.
