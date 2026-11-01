# Prompt para Antigravity — plan de preparación para el despliegue (frontend)

> Pegar este prompt en Antigravity tras abrir el workspace en `fitnessfrontend/` y seleccionar el agente `constructor-fitnessfrontend` con `/agents`.
> Contexto: las fases 0 a 6, la verificación de punta a punta y el **rediseño visual** (sistema de diseño ITM, logo oficial, navegación responsive, insignias, esqueletos) ya están hechos. Este prompt abre el trabajo de preparación para el despliegue, decidido el 2026-10-31.

---

**Todo está especificado en `docs/plan-despliegue/` y en el plan del backend — léelo antes de escribir nada, en este orden:**

1. `AGENTS.md` (raíz) — las ocho reglas que no se negocian.
2. `docs/plan-despliegue/00-indice.md` — decisiones del usuario (D1 a D5), calendario y dependencias del backend.
3. `../fitnessbackend/docs/plan-despliegue/01-requisitos-nuevos.md` — **fuente de verdad** de RF-026 a RF-042, RN-031 a RN-036 y `AMB-D…`. No se duplica en este repositorio.
4. `docs/plan-despliegue/01-guia-frontend.md` — **tu documento de trabajo**: fases **F0 a F10**, con pantallas, Definición de Hecho y pruebas obligatorias.
5. `docs/plan-despliegue/02-pruebas-en-dispositivos-y-accesibilidad.md` y `03-preparacion-despliegue-frontend.md`.
6. `docs/arquitectura/09-rediseno-visual.md` — el sistema de diseño que debes usar en toda pantalla nueva.

## Qué cambia respecto al trabajo anterior

- **Fuera de alcance:** recuperación de contraseña, confirmación de correo y avisos por correo (se integrará con credenciales institucionales). Mantén sesión y registro detrás de `IAutenticacionService`. Los avisos son **dentro de la app**.
- **El QR de ingreso general es dinámico** (RF-039): ya no se imprime; hay una **pantalla del punto de acceso** que se actualiza sola (Patrón 9) y el mensaje `QrVencido`.
- **Se añaden:** renovación de sesión, vista semanal, `.ics`, ayuda, asistencia manual, notas, anuncios, administración de sesiones, bloqueos, reservas de terceros, auditoría, reportes con exportación, retroalimentación y (opcional) PWA.
- **Solo diseño** (sin pantallas): apelación y derechos del titular (F10).
- **RN-020 sigue siendo ley:** ninguna pantalla de Usuario/Entrenador muestra nivel, penalización, fecha de apertura ni **posición en lista de espera**.

## Cómo trabajar

1. **Empieza por F0** (documentación, solo añadiendo) y sigue **F1 → F10 en orden**.
2. **No regeneres Kiota hasta que el backend publique el contrato** del paquete correspondiente (tabla en `00-indice.md`). Mientras tanto, trabaja con una fachada y un servicio falso para las pruebas, y **nunca** con `HttpClient` a mano. Si el backend no está disponible, repórtalo y pasa a otra fase.
3. Por fase: implementa con sus **pruebas obligatorias**, ejecuta `dotnet build` y `dotnet test`, invoca a `revisor-fitnessfrontend` con el identificador de la fase, corrige los bloqueantes y solo entonces avanza.
4. **Un commit por fase** (`feat(F3): …`) en la rama que te indique el usuario, **nunca en `main`**. Actualiza `CHANGELOG.md` y `docs/checklist-e2e.md`.
5. **Toda pantalla nueva:** comentario de RF, esqueleto de carga, estado vacío, estado de error, confirmación con consecuencia en acciones destructivas, tablas que pasan a tarjetas en celular, tokens de `itm-tema.css` (ningún color ni fuente suelto) y foco visible.
6. Lo que requiere un teléfono real o un lector de pantalla **lo hace el usuario**: prepara el entorno y la hoja de pruebas de `02-…` y registra los resultados.

## Qué no se recorta nunca

F1 (renovación de sesión), F6 (pantalla del punto de acceso) y la verificación de RN-020. F8 (PWA) y F10 (documentación) son lo primero que se difiere.
