# Verificación independiente del cliente · Ronda 3 · 2026-11-02

> Informe conjunto y veredicto: `fitnessbackend/docs/pruebas-calidad/05-verificacion-independiente-ronda-3.md`. La numeración H-xx es la misma.
> Evidencia: [evidencia/ronda-3-independiente/](evidencia/ronda-3-independiente/).
> Commit probado: **`742526b`** (copia exacta con `git archive`), con el backend en `f801655`. **No hay commit `QA2-F5`.**
> El informe del agente ([03-resultados-ronda-3.md](03-resultados-ronda-3.md)) no se considera válido; ver H-29 en el informe conjunto.

## Resumen

**No listo.**

**Lo que mejoró:**
- **H-01 y H-02 corregidos:** 0 servicios en memoria, pantallas F2–F7 con datos reales y un QR que viene de `qr-actual`.
- **H-19 corregido.**

**Lo que bloquea:**
- **H-27 (Alta):** "Administrar sesión" da 403 al Administrador.
- **H-28 (Media):** los tipos de bloqueo están invertidos.
- **H-13 (Parcial):** el agente añadió `'unsafe-inline'` a la CSP en el commit de QA; ZAP da 5 alertas medias y sigue `Server: nginx/1.31.7`.
- **H-14 (Parcial, aceptado temporalmente, D-F):** 8,08 s en 4G lento, con un criterio de ≤ 5 s. El usuario decidió que por ahora basta con que funcione en el despliegue; se vuelve a medir allí.

## Resultados

| Prueba | Ronda 2 | **Ronda 3 (independiente)** |
|---|---|---|
| `dotnet test` | 144/144 | **167/167** |
| Recorrido en navegador (checks) | 9/18 | **17/18**. Falla el calendario porque no había bloqueos (`flujo-b2-b10.sh` los borra). Con un bloqueo creado: **18/18**. Un check pasa en falso (abajo) |
| Servicios en `MEMORIA` | 7 | **0** |
| Descarga de la primera visita | 6.748 KiB | **4.813 KiB** (gzip; sin compresión serían 12.706 KiB) |
| Arranque hasta el login, 4G lento | 8,84 s | **8,08 s** (No cumple; criterio ≤ 5 s) |
| Arranque en red local / lista de eventos | 0,9 s | 0,92 s / 0,73 s; 41 tarjetas |
| ZAP baseline | 0 FAIL, 7 avisos | 0 FAIL, 6 tipos de aviso, con estas alertas **Medias**: CSP `script-src 'unsafe-inline'`, `'unsafe-eval'`, `style-src 'unsafe-inline'`, comodín en `connect-src` y falta de SRI |
| Cabeceras | Sin CSP, XFO, Referrer-Policy ni Permissions-Policy | CSP, XFO, XCTO y Referrer-Policy **presentes**. Sin `Permissions-Policy`. `Server: nginx/1.31.7`. En `location` de `.wasm/.dat` y `/appsettings.json`, `add_header` anula XFO y Referrer-Policy (herencia de nginx) |
| Paquetes vulnerables | 0 | 0 |
| Errores de consola (`ERRORES_CONSOLA`) | — | 0. Pero hay dos **403 de red**: `/api/reservas/sesiones/{id}/inscritos` (H-27) y `/api/eventos/sesiones/{id}/anuncios` (H-30). El contador del script no cuenta errores de red |

## Revisión de las capturas

Además de los checks se revisaron las 17 capturas en [ui/](evidencia/ronda-3-independiente/ui/):

- **`ui-14-r2-admin-sesion.png` — H-27:** se ve «Error del servidor al consultar la sesión (403)» y la pantalla queda vacía. El check "Administrar sesión carga la sesión real" pasa porque su regex solo busca "no encontrada / error inesperado / no se pudo". Es un **falso positivo**: la pantalla no funciona para el Administrador.
- **`ui-15-r2-pantalla-punto.png` — H-02 corregido:** muestra el nombre real "QA Torniquete", un QR y la cuenta regresiva del periodo.
- **`ui-12-r2-calendario.png` — H-28:** sin datos, la lista sale vacía. Con un bloqueo de tipo 1 (Mantenimiento) creado por la API, [ui-con-bloqueo/ui-12-r2-calendario.png](evidencia/ronda-3-independiente/ui-con-bloqueo/ui-12-r2-calendario.png) muestra la etiqueta **"Festivo"**.
- **`ui-04-mis-reservas.png`:** confirmada y en lista de espera, sin datos de priorización (RN-020 se cumple).
- **Reportes y auditoría:** datos reales ("QA …", `Eventos.Crear`); paginación con icono.

## Hallazgos del cliente

| ID | Sev. | Estado | Detalle | Recomendación |
|---|---|---|---|---|
| H-01 | Crítica | **Corregido** | 0 servicios `MEMORIA`; datos reales en F2–F7 | — |
| H-02 | Alta | **Corregido** | QR del backend; sin `dyn_` | — |
| H-13 | Media | **Parcial** | CSP debilitada: el agente añadió `'unsafe-inline'` a `script-src` en `742526b`, fuera de su rol. `connect-src http: https: ws: wss:` permite cualquier origen. Siguen `Server: nginx/1.31.7` y la falta de `Permissions-Policy` | Quitar `'unsafe-inline'` (usar un hash del script inline si Blazor lo necesita); `connect-src 'self' <URL_BASE_API>` sustituido al arrancar; `server_tokens off`; `Permissions-Policy`; repetir las cabeceras dentro de cada `location` |
| H-14 | Media | **Parcial** | 4.813 KiB y 8,08 s en 4G lento. Se descargan los tres `icudt_*` (CJK, no_CJK y EFIGS, unos 0,9 MB comprimidos). El `.wasm` principal va con gzip, no con Brotli | Brotli precomprimido (`.br`) y servirlo; `InvariantGlobalization` o carga de ICU solo para `es`; revisar el *trimming*. Otra opción: que el usuario fije otro umbral |
| H-19 | Baja | **Corregido** | `icono,icono` | — |
| **H-27** | Alta | Nuevo | El Administrador recibe 403 en "Administrar sesión" (`inscritos` exige el rol Entrenador) | **D-E:** el backend crea un endpoint administrativo propio; la pantalla lo usa (Kiota regenerado). bUnit que cubra el error 403 de la pantalla |
| **H-28** | Media | Nuevo | `BloqueosClienteService.cs:47` y `:76`: 1=Festivo, 2="Institucional", 0=Mantenimiento. En el backend: 0=Festivo, 1=Mantenimiento, 2=Cierre | Usar el enum del cliente Kiota y bUnit para los tres tipos |
| **H-30** | Baja | Nuevo | Pide los anuncios de reservas en lista de espera y recibe 403 | Pedirlos solo para reservas `Confirmada` |
| **H-32** | Baja | Nuevo | Falta QA2-F5 (lista para TI) | Ejecutar QA2-F5 |

## Limitaciones

- No se midió D-C en el cliente (pérdida de un formulario a medias al vencer el refresh).
- Lighthouse, teléfonos reales y la accesibilidad manual dependen del entorno desplegado.
