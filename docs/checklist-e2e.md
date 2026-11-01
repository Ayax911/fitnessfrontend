# Checklist de verificación E2E

> Recorrido manual con el sistema real (API + PostgreSQL + este frontend en un navegador), 2026-10-31.
> Entorno: base de datos **vacía** creada por las migraciones al arrancar (`Database__AplicarMigracionesAlIniciar=true`) y administrador inicial designado por `Bootstrap__AdminEmail`.
> Leyenda: ✅ ejecutado y correcto · ⬜ no ejecutado en este recorrido (se indica por qué).

## Despliegue y arranque

| Verificación | Resultado |
|---|---|
| Base vacía: las 3 migraciones aplican al arrancar la API | ✅ |
| `GET /health` responde 200 (también en contenedor Docker) | ✅ |
| Administrador inicial: registrar la cuenta, reiniciar la API → rol Administrador | ✅ |
| Imagen Docker de la API: usuario sin privilegios, token válido tras recrear el contenedor (claves persistentes) | ✅ |
| Imagen Docker del frontend: `URL_BASE_API` se inyecta al arrancar; rutas profundas devuelven la app | ✅ |

## Casos de uso

| CU / RF | Flujo | Resultado |
|---|---|---|
| RF-025 | Registro sin marcar la autorización: no envía y muestra el motivo | ✅ |
| RF-025 | Registro con correo ya existente: muestra el mensaje del backend | ✅ |
| RF-013 | Inicio de sesión: el menú corresponde al rol (Administrador / Entrenador / Usuario) | ✅ (tras corregir D-1) |
| RF-023 | Administrador asigna el rol Entrenador desde Usuarios y roles | ✅ |
| RF-023 | Administrador intenta quitarse su propio rol: rechazo con el mensaje del backend | ✅ |
| RF-024 | Crear punto de acceso; el QR codifica `/ingreso/{token}` (no el GUID) | ✅ |
| RF-024 | Impresión del QR (`window.print`) | ⬜ no probada |
| CU-04 / RF-006 | Escaneo sin sesión → login → vuelve solo al ingreso → "Ingreso Autorizado" | ✅ |
| CU-04 / RN-030 | Segundo escaneo inmediato: rechazo con el mensaje del backend | ✅ |
| CU-05 / RF-007 | Crear evento único eligiendo el entrenador de la lista; hora local de Bogotá guardada en UTC | ✅ |
| CU-05 / RF-008 | Crear evento recurrente | ⬜ solo pruebas automáticas |
| RF-001 | Listado con horario en `America/Bogota`, entrenador por nombre y cupos | ✅ |
| CU-01 / RF-002 | Reservar con cupo: confirmada y el cupo baja (5 → 4) | ✅ |
| CU-01 / RF-003 | Reservar sin cupo (lista de espera) | ⬜ solo pruebas automáticas |
| CU-02 / RF-004 | Cancelar reserva | ⬜ solo pruebas automáticas |
| RF-010 | Entrenador ve únicamente sus actividades (1/5 inscritos) | ✅ |
| RF-012 | Entrenador ve el QR de su sesión con la ventana de validez en hora local | ✅ |
| RF-011 | Entrenador ve la lista de inscritos | ⬜ no abierta en este recorrido |
| CU-03 / RF-005 caso B | Sin sesión → login → vuelve solo a `/checkin/{token}` → "Asistencia confirmada" | ✅ |
| CU-03 / RF-005 caso C | Token inválido con sesión: error del backend, **no** pantalla de login | ✅ |
| CU-03 / RF-005 caso A | Check-in con la sesión ya iniciada | ⬜ cubierto por prueba bUnit |
| RF-021 | Historial muestra el ingreso general y la asistencia a la sesión | ✅ (tras corregir D-4) |
| CU-06 / RF-016 | Administrador busca al estudiante **por nombre** y consulta su indicador | ✅ |
| CU-07 | Promoción desde lista de espera | ⬜ solo pruebas de integración con PostgreSQL real |
| CU-08 / RF-022 | Consolidación al cerrar la ventana | ⬜ solo pruebas de integración (incluida la atomicidad) |

## Pendiente que requiere a una persona

- [ ] **Check-in por QR probado en un teléfono real** (cámara nativa, HTTPS): el flujo se verificó en el navegador de escritorio.
- [ ] Contraste WCAG AA y navegación por teclado sobre las pantallas nuevas (`/admin/usuarios`, `/admin/puntos-acceso`, registro ampliado).
- [ ] Texto y enlace oficial del aviso de privacidad (RNF-011, validación jurídica): hoy la casilla de autorización usa un texto mínimo.

## Defectos hallados durante este recorrido (todos corregidos)

| ID | Defecto | Corrección |
|---|---|---|
| D-1 | **Todos los usuarios aparecían como "Usuario"**: el frontend leía los roles del bearer token como si fuera un JWT, pero el token de Identity es opaco. Entrenador y Administrador no podían usar sus pantallas. | `GET /api/usuarios/yo` entrega el perfil y los roles; el frontend lo guarda al iniciar sesión. Una sesión sin claims legibles se trata como anónima. |
| D-2 | El banner rojo "Ha ocurrido un error inesperado" se veía **siempre**: ninguna regla CSS lo ocultaba. | Regla en `itm-tema.css` (oculto por defecto; Blazor lo muestra ante una excepción real). |
| D-3 | Tras iniciar sesión con perfil fallido podía quedar una sesión a medias (token sin perfil). | Si `/yo` falla se limpia la sesión y se avisa. |
| D-4 | El historial rotulaba como "Ingreso General" las asistencias a sesiones (el backend enviaba `Tipo="Evento"`; el contrato es `"Sesion"`). | Backend corregido y prueba que fija el contrato. |
| D-5 | La pantalla de ingreso mostraba el token del QR como "Punto de acceso". | Muestra el nombre del punto. |

## Nota para quien repita el recorrido

Si reconstruye el frontend (`dotnet build`/`dotnet test`) mientras `dotnet run` está activo, el servidor de desarrollo queda con los archivos de arranque antiguos y la app se queda en "Cargando…": reinicie `dotnet run`.
