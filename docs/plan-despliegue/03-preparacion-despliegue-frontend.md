# Preparación de despliegue del frontend

> Complementa el paquete F9 y el B9 del backend. El frontend es un conjunto de **archivos estáticos** (Blazor WebAssembly) servidos por nginx sin privilegios (`Dockerfile`, `nginx.conf` ya existentes). Aquí se deja *listo para desplegar* y se prepara la **parte de cliente** de la auditoría de eficiencia de desempeño (OPS-04, backend `04-auditoria-eficiencia-desempeno.md`).

## 1. Publicación y configuración

- [ ] `UrlBaseApi` se sustituye al arrancar el contenedor (`docker/40-url-api.sh`); la aplicación **falla con un mensaje claro** si queda el marcador. Verificar con una imagen limpia.
- [ ] Un solo origen del frontend en `Cors:AllowedOrigins` del backend (sin comodines); documentar el par frontend/backend por entorno en el runbook.
- [ ] `docker build` desde cero y `docker run` con las variables documentadas: la app carga, inicia sesión y llega a `/health` del backend.
- [ ] Fallback de rutas SPA (`404.html`/`try_files`) comprobado con rutas profundas (`/ingreso/{token}`, `/checkin/{token}`, `/admin/reportes`) **recargando la página**: el QR abre directamente una ruta profunda, así que esto es crítico.
- [ ] El enlace del QR usa `{BaseUri}`: confirmar que en producción el `<base href>` y el dominio son los correctos (un QR con el origen equivocado no sirve).

## 2. Cabeceras y caché (nginx)

| Recurso | Política |
|---|---|
| `index.html` | `Cache-Control: no-cache` (siempre revalidar) |
| `_framework/*` con *fingerprint* | `public, max-age=31536000, immutable` |
| `css/`, `js/`, `img/` | Versionados por nombre o con caché moderada y revalidación |
| `service-worker*.js` y `manifest.webmanifest` (si F8) | `no-cache` |
| Compresión | Brotli y gzip para `.wasm`, `.dll`, `.js`, `.css`, `.json` (los `.br` precomprimidos que genera la publicación) |
| Seguridad | `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin`, `X-Frame-Options: DENY` (salvo la pantalla del punto de acceso si se incrusta), **CSP** propuesta abajo, HSTS en el proxy HTTPS |

**CSP de partida** (ajustar tras probar; Blazor WASM necesita `'wasm-unsafe-eval'`):
`default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self' https://fonts.googleapis.com 'unsafe-inline'; font-src https://fonts.gstatic.com; img-src 'self' data:; connect-src 'self' <URL de la API>; frame-ancestors 'none'`. Quitar `'unsafe-inline'` de estilos si el *build* lo permite.
**Tipografías:** hoy se cargan desde Google Fonts. Decisión pendiente con TI: **autoalojar Montserrat y Lato** (mejor privacidad, caché y rendimiento, y sin depender de un tercero); si se autoalojan, ajustar la CSP.

## 3. Parte de cliente de la auditoría de desempeño

| Métrica | Herramienta | Hipótesis (`PENDIENTE`) |
|---|---|---|
| Descarga inicial (WASM + dlls) con compresión | Lighthouse / pestaña Red | Medir y fijar un presupuesto; recortar con *trimming* y revisando dependencias |
| Tiempo hasta interactivo en teléfono de gama baja y 4G | Lighthouse móvil + T8 | Medir; el esqueleto y la pantalla de arranque deben aparecer rápido |
| Memoria del cliente tras 2 h en la pantalla del punto de acceso | Herramientas del navegador (T6) | Sin crecimiento sostenido |
| Peticiones por pantalla | Red del navegador | Sin llamadas duplicadas ni sondeos innecesarios (el sondeo del QR es 1 por periodo) |
| Frecuencia del sondeo del QR | Revisión de F6 | Ajustar con el periodo definitivo (AMB-D06) |

Las posibles mejoras (autoalojar fuentes, *lazy loading* de ensamblados de administración, *trimming* más agresivo, AOT si el tamaño de descarga lo justifica) se evalúan **con las mediciones**, no antes.

## 4. Integración futura con credenciales institucionales (D1)

Para que el cambio sea acotado:
- Toda la lógica de sesión está en `Servicios/Identidad/` detrás de `IAutenticacionService`; las pantallas `Login` y `Registro` son las únicas que cambiarían (o desaparecerían si el SSO hace el registro).
- El frontend ya depende de `GET /api/usuarios/yo` para el perfil y los roles; mantenerlo así.
- El renovado de sesión (F1) se apoya en el contrato de refresh: si el SSO emite otro tipo de token, se sustituye en el manejador, no en las pantallas.
Documentar en `README.md` una sección "Cambio a credenciales institucionales" con estos puntos de contacto.

## 5. Lista de verificación antes de entregar a TI (QA2-F5 / QA3-F1)

- [x] `dotnet publish -c Release` sin advertencias nuevas y la imagen Docker arranca como usuario sin privilegios (`USER 101` en `Dockerfile`).
- [x] Rutas profundas recargables; `try_files $uri $uri/ /index.html` en `nginx.conf` probado con `curl -I http://localhost:5081/admin/calendario` (200 OK).
- [x] Cabeceras de seguridad y caché comprobadas con `curl -I`: `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`, `server_tokens off`, `Cache-Control: no-store` para `/appsettings.json` y `Cache-Control: public, max-age=31536000, immutable` para `_framework/`.
- [x] CSP configurada sin `'unsafe-inline'` ni `'unsafe-eval'` en `script-src` usando el hash sha256 del script inline del importmap; `connect-src` restringido a `'self' ${URL_BASE_API}` sin comodines. OWASP ZAP sin alertas críticas ni medias.
- [x] Sin secretos ni URLs privadas en el repositorio; `appsettings.json` solo con el marcador `REEMPLAZAR_EN_DESPLIEGUE`.
- [ ] `docs/checklist-e2e.md` ejecutado contra el entorno desplegado de pruebas (a cargo de TI / usuario en entorno final).
- [ ] Informe de Lighthouse (rendimiento, accesibilidad, buenas prácticas) archivado en `docs/despliegue/` (a cargo de TI en servidor desplegado).

