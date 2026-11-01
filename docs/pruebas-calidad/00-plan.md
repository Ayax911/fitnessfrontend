# Plan de pruebas de calidad ISO/IEC 25010 · frontend (preparación para el despliegue)

> **Fecha:** 2026-11-01, ejecución programada a las 02:01 (America/Bogota).
> **Commit probado:** `46e0dee`.
> **Plan conjunto:** el plan completo (decisiones del usuario, severidades y criterios del backend) está en `fitnessbackend/docs/pruebas-calidad/00-plan.md`. Este documento detalla solo lo que se mide en el cliente.
> **Resultados:** [01-resultados.md](01-resultados.md).

## Reglas

Son las mismas que en el backend:

- Solo entorno local: la imagen Docker del repositorio (`Dockerfile` + `nginx.conf`) en `http://localhost:5081`, conectada a la API local `qa25010` en `http://127.0.0.1:18080`.
- Si algo falla, solo se documenta; no se modifica el código.
- Los entregables se suben con commit en `feat/pendientes-1-11`.

## Criterios por subcaracterística (cliente)

| Característica | Subcaracterística | Criterio (provisional · `PENDIENTE` de acuerdo) | Cómo se mide |
|---|---|---|---|
| Adecuación funcional | Completitud | Cada pantalla de un RF llama al backend real. No hay datos inventados en la interfaz. | Clasificación de servicios en API real o en memoria (`docs/pruebas-calidad/evidencia/servicios-api-vs-memoria.txt`) y matriz del backend |
| | Corrección | `dotnet test` (bUnit) 100 % en verde. Recorrido real: iniciar sesión, reservar, Mis reservas, Mi historial y administración. | `dotnet test`, `pruebas/funcional/recorrido-navegador.js` (Chromium headless, k6 browser) |
| | Pertinencia | No se ofrecen acciones imposibles (p. ej. reservar una sesión pasada). | Recorrido en navegador |
| Eficiencia de desempeño | Comportamiento temporal | Arranque en frío hasta el formulario de inicio de sesión ≤ 5 s en 4G lento (150 ms, 5 Mbps). LCP ≤ 2,5 s y CLS ≤ 0,1 en red local (referencias de Web Vitals). | Recorrido con `throttleNetwork` y métricas `browser_web_vital_*` |
| | Utilización de recursos | Descarga de la primera visita ≤ 3 MB comprimidos (provisional). DOM de la lista de actividades acotado. | `pruebas/desempeno/tamano-descarga.sh`, conteo de nodos y altura en el recorrido |
| Seguridad | Confidencialidad e integridad | Cabeceras CSP, `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy` y `Permissions-Policy` en el sitio. Sin alertas altas ni medias de ZAP. RN-020 en pantalla. | `pruebas/seguridad/zap-frontend.sh`, recorrido |
| | Paquetes | `dotnet list package --vulnerable` sin hallazgos. | CLI |
| Fiabilidad | Madurez | 0 errores de consola de la aplicación en el recorrido. | Recorrido (eventos de consola) |
| | Recuperabilidad | La sesión sobrevive a un reinicio de la API (los tokens siguen válidos). | Prueba `fiabilidad/02` del backend |

## Cómo repetir

Ver [`pruebas/README.md`](../../pruebas/README.md).
