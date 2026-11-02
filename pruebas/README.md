# Pruebas de calidad del frontend (entorno local)

Plan: [docs/pruebas-calidad/00-plan.md](../docs/pruebas-calidad/00-plan.md). Resultados: [01-resultados.md](../docs/pruebas-calidad/01-resultados.md).

Requieren el entorno del backend levantado y sembrado (`fitnessbackend/pruebas/entorno/levantar.sh` y `datos/sembrar.sh`, red Docker `qa25010_default`).

```bash
pruebas/entorno/levantar-frontend.sh                 # imagen del repo (nginx) en http://localhost:5081 → API 127.0.0.1:18080
pruebas/desempeno/tamano-descarga.sh                 # bytes de la primera visita y estimación por red móvil
SALIDA=/ruta/salida pruebas/seguridad/zap-frontend.sh  # ZAP baseline + cabeceras
mkdir -p salida/ui && chmod 777 salida/ui
docker run --rm --network host -v "$PWD/pruebas:/guiones:ro" -v "$PWD/salida/ui:/salida" \
  -e WEB=http://localhost:5081 -e CLAVE='<clave de pruebas local de fitnessbackend/pruebas/entorno/comun.sh>' \
  -e USUARIO=qa.usuario0398@itm.test -e MANANA=$(date -d tomorrow +%d/%m/%Y) \
  -e PUNTO=<id de un punto de acceso> -e SESION=<id de una sesión de qa.entrenador1> \   # ronda 2: pantallas F2–F7
  grafana/k6:0.54.0-with-browser run /guiones/funcional/recorrido-navegador.js
pruebas/entorno/bajar-frontend.sh
dotnet test
```

**Notas:**
- **Red del host:** el recorrido usa `--network host` porque el navegador debe llegar tanto al sitio (5081) como a la API (18080) por `127.0.0.1`, igual que un usuario.
- **Capturas:** se guardan solo con la ventana visible. La lista de actividades sin paginar mide más de 600.000 px de alto, y una captura de página completa ocupa unos 18 MB.

- **Ronda 2:** el recorrido incluye las pantallas F2–F7 (Reportes, Auditoría, Calendario, Bandeja, Administrar sesión, Punto de acceso, Notas y Asistencia manual). Necesita `PUNTO` y `SESION` (ver `fitnessbackend/pruebas/seguridad/authz-b2-b10.sh`, que crea `QA2 Propia`, con anuncio y nota). Las capturas las crea k6 con otro usuario: `chmod -R a+rX salida/ui` para verlas.
- Resultados: [ronda 2](../docs/pruebas-calidad/02-resultados-ronda-2.md).
