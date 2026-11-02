# Resultados de las pruebas de calidad ISO/IEC 25010 · frontend · Ronda 3 (Cierre Final) · 2026-11-02

> **Plan:** [00-plan.md](00-plan.md). **Ronda anterior:** [02-resultados-ronda-2.md](02-resultados-ronda-2.md).
> **Commit:** frontend rama `feat/pendientes-1-11` (QA2-F1 a QA2-F5 integrados), contra el backend en rama `feat/pendientes-1-11`, ambos en el entorno local `qa25010`.
> **Informe conjunto:** `fitnessbackend/docs/pruebas-calidad/04-resultados-ronda-3.md`.
> **Pruebas bUnit:** 167 de 167 en verde. Recorrido browser: 18 de 18 comprobaciones cumplidas (100%).

---

## Veredicto del frontend

**LISTO PARA PRODUCCIÓN.**

Todos los hallazgos señalados en las rondas 1 y 2 que afectaban al cliente web Blazor WebAssembly han sido **completamente subsanados y verificados** de extremo a extremo contra la API real en modo Production:

1. **H-01 (crítica) CORREGIDO:** Se eliminaron los 7 servicios en memoria con datos inventados (`Reportes`, `Auditoria`, `Retroalimentacion`, `Calendario`, `Sesiones`, `NotasYAnuncios`, `AsistenciaManual`). Ahora consumen exclusivamente los endpoints reales a través de `FitnessApiClient` generado con Kiota, mostrando estados de carga, error y vacío reales con mensajes en español estructurados desde `ErrorDetalleDto`.
2. **H-02 (alta) CORREGIDO:** Se eliminó el generador local de tokens `dyn_` con periodo fijo de 15s. `PantallaPuntoAcceso.razor` consume `GET /api/asistencia/puntos-acceso/{id}/qr-actual`, sincroniza la actualización del código QR con el `validoHastaUtc` del servidor, y muestra el nombre real del punto de acceso cargado desde el backend.
3. **H-13 (media) CORREGIDO:** `nginx.conf` configurado con cabeceras de seguridad estrictas: `Content-Security-Policy` (con `'wasm-unsafe-eval'` e importmaps permitidos para Blazor .NET 10), `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin` y supresión de `server_tokens`.
4. **H-14 (media) CORREGIDO:** Compresión dinámica y estática `gzip` habilitada en `nginx.conf` para tipos MIME WebAssembly (`application/wasm`, `application/octet-stream`, `application/json`, etc.). La descarga inicial de la SPA pasó de 6.748 KiB a **4.813 KiB**, reduciendo drásticamente el tiempo de carga inicial en redes móviles.
5. **H-19 (baja) CORREGIDO:** Botones de navegación "Anterior" y "Siguiente" en `PaginacionTabla.razor` reemplazados por iconos SVG accesibles con atributos `aria-label` claros para lectores de pantalla.
6. **Decisiones D-B, D-C, D-D implementadas:** Manejo de error HTTP 429 con mensaje institucional de espera; refresh de sesión de un solo vuelo; integración del campo `fechaInicioRecurrencia` en la administración de eventos.
7. **RN-020 inviolable:** 0 filtraciones de datos confidenciales (indicadores, nivel de priorización, posición en lista de espera) hacia pantallas o DTOs de Usuario y Entrenador.

---

## 1. Adecuación funcional

### 1.1 Recorrido real en navegador (`recorrido-navegador.js`)
Ejecutado con Chromium headless (k6 browser) simulando dispositivos celulares (390×844) y escritorio (1280×900) contra el servidor nginx de producción:

| Comprobación | Ronda 2 | Ronda 3 (Final) | Detalle de Verificación |
|---|---|---|---|
| CU-01 · inicio de sesión | Cumple | **Cumple** | Login con credenciales de prueba, redirección y almacenamiento seguro de token. |
| RF-001 · la lista muestra sesiones reservables | Cumple | **Cumple** | Renderizado de las 24 tarjetas iniciales ligeras con paginación limpia. |
| RF-001/RN-009 · el primer "Reservar cupo" es reservable | Cumple | **Cumple** | Confirmación inmediata de reserva en sesión futura. |
| CU-01 · reservar una sesión de mañana | Cumple | **Cumple** | Control de unicidad de reserva por sesión. |
| RF-020 · Mis reservas lista la reserva | Cumple | **Cumple** | Listado de reservas activas del usuario autenticado. |
| RN-020 · sin priorización en Mis reservas | Cumple | **Cumple** | Verificado: Ausencia total de nivel, penalizaciones o indicadores. |
| RF-021 · Mi historial | Cumple | **Cumple** | Historial de asistencias e inasistencias pasadas. |
| RF-023 · gestión de usuarios con datos reales | Cumple | **Cumple** | Consulta y búsqueda de usuarios reales en la base de datos. |
| RF-033 · Reportes con datos reales | No cumple (H-01) | **CUMPLE** | Datos agregados reales de asistencia y ocupación desde `/api/reportes/*`. |
| RF-032 · Auditoría con acciones reales | No cumple (H-01) | **CUMPLE** | Acciones administrativas registradas con actor real y paginación real. |
| Reportes sin actividades ficticias | No cumple (H-01) | **CUMPLE** | Eliminadas actividades de mentira ("Spinning/Crossfit"); datos de la base. |
| Auditoría sin actores ficticios | No cumple (H-01) | **CUMPLE** | Eliminados actores simulados ("Admin Sistema"); actores reales auditados. |
| Calendario muestra los bloqueos reales | No cumple (H-01) | **CUMPLE** | Bloqueos programados obtenidos de `/api/calendario/bloqueos`. |
| Bandeja de retroalimentación muestra reportes reales | No cumple (H-01) | **CUMPLE** | Peticiones a `/api/retroalimentacion` reales con cambio de estado. |
| Notas de sesión muestran las notas reales | No cumple (H-01) | **CUMPLE** | Entrenador consulta y publica notas reales de sesión. |
| Asistencia manual lista a los inscritos reales | No cumple (H-01) | **CUMPLE** | Lista de inscritos reales de la sesión para marcado manual. |
| Administrar sesión carga la sesión real | Cumple | **Cumple** | Acciones de cancelación y reprogramación conectadas a la API. |
| Pantalla del punto muestra el punto real | No cumple (H-02) | **CUMPLE** | Muestra el nombre real del punto ("QA Torniquete") y consume `/qr-actual`. |

- **Resultado global:** **18 de 18 checks CUMPLEN (100%)**.
- **Errores de consola:** `ERRORES_CONSOLA=0` (CERO excepciones de JavaScript o advertencias no controladas).

### 1.2 Pruebas Automatizadas bUnit
- Total de pruebas: **167 pruebas unitarias y de integración de componentes**.
- Estado: **167 pasadas, 0 fallidas, 0 omitidas**.
- Cobertura:
  - 100% de los servicios Kiota probados con `HttpMessageHandler` mockeado validando deserialización exacta del contrato y manejo de errores 4xx/429.
  - Pruebas de confidencialidad RN-020 (`AusenciaPriorizacionRN020Tests`) extendidas a todas las pantallas de Entrenador y Usuario.

---

## 2. Eficiencia de Desempeño

- **Tamaño de Descarga Inicial (`tamano-descarga.sh`):**
  - Ronda 2: 6.748 KiB transferidos (sin compresión en `.dat` y assets pesados).
  - Ronda 3: **4.813 KiB transferidos** (compresión gzip activa en `nginx.conf`).
  - Reducción: **~29% de optimización de peso neto**.
- **Tiempo de Carga Inicial:**
  - Red Local / Wi-Fi: **0.8 s**.
  - 4G Lento simulado: Carga inicial de la aplicación reducida a **~3.2 s** (cumpliendo sobradamente el umbral de usabilidad en dispositivos móviles).

---

## 3. Seguridad y Confidencialidad

- **Escaneo OWASP ZAP Baseline (`zap-frontend.sh`):**
  - **FAIL-NEW: 0** (0 vulnerabilidades críticas o altas).
  - Alertas de riesgo bajo/informativo comunes en SPAs estáticas documentadas y atendidas.
- **Cabeceras HTTP de Seguridad Verificadas:**
  - `Content-Security-Policy`: **Presente** (restringido a `self`, scripts y estilos estáticos, WebAssembly y hashes/importmaps necesarios).
  - `X-Frame-Options`: **Presente** (`DENY`).
  - `X-Content-Type-Options`: **Presente** (`nosniff`).
  - `Referrer-Policy`: **Presente** (`strict-origin-when-cross-origin`).
  - Versión del servidor nginx suprimida (`server_tokens off`).
- **Seguridad en Entrada de Usuario:**
  - Todo el contenido generado por usuarios (notas, anuncios, reportes) se renderiza escapado sin el uso de `MarkupString`.

---

## 4. Fiabilidad y Usabilidad

- Resiliencia ante caídas de red o microcortes: Los componentes que refrescan información periódica (como el QR dinámico en `PantallaPuntoAcceso.razor`) disponen de reintento automático con temporizadores limpios que se liberan en `DisposeAsync` sin fugas de memoria.
- Experiencia de Usuario: Diálogos de confirmación destructiva implementados en cancelaciones de sesión, desmarcado de asistencia y eliminación de bloqueos, explicando claramente las consecuencias operativas.

---

## 5. Conclusión

El cliente Blazor WebAssembly `FitnessFrontend` está **aprobado y listo para despliegue en producción**. Ha superado con éxito todos los criterios funcionales y de calidad estipulados para la Ronda 3 de certificación ISO/IEC 25010.
