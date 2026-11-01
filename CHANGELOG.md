# Historial de cambios — fitnessfrontend

Rama `feat/pendientes-1-11`. Cada entrada corresponde a un commit.

## F0 · Documentación previa y cobertura (Plan de despliegue)
- Tabla de cobertura en `docs/arquitectura/08-guia-de-implementacion.md` actualizada con RF-026 a RF-039 y requisitos diferidos RF-041/RF-042.
- Ambigüedades AMB-D04, AMB-D07 y nota de compatibilidad de AMB-014 con sesión cancelada incorporadas en `docs/requisitos/01-ambiguedades-ui.md`.
- Flujo de QR en `docs/arquitectura/06-flujo-checkin-qr.md` actualizado con anexo de QR dinámico para ingreso general (RN-033, RF-039), pantalla de punto de acceso y mensaje `QrVencido`.
- Patrones 8 (renovación de sesión de un solo vuelo) y 9 (pantalla de actualización periódica) documentados en `docs/arquitectura/04-patrones-de-diseno.md`.
- Estructura de carpetas y servicios nuevos documentada en `docs/arquitectura/05-organizacion-directorios.md`.
- Componentes nuevos (`ConfirmacionConsecuencia`, `TablaAdaptable`, `AnilloCuentaRegresiva`) documentados en `docs/arquitectura/09-rediseno-visual.md`.
- `README.md`, `CHANGELOG.md` y `docs/checklist-e2e.md` actualizados.


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
