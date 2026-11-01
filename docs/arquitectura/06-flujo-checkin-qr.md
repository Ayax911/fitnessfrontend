# 06. Flujo de check-in por QR

> Este es el único flujo de negocio que el backend delega explícitamente a este repositorio (ver [fitnessbackend/docs/arquitectura/02-stack-y-hosting.md](../../../fitnessbackend/docs/arquitectura/02-stack-y-hosting.md), Decisión 9 revisada, y su [07-contrato-api.md](../../../fitnessbackend/docs/arquitectura/07-contrato-api.md), sección "Qué NO define este documento"). Se documenta aparte porque tiene más superficie de decisión de UX que cualquier otra pantalla del sistema.

## Lo que ya decidió el backend (no se reabre aquí)

- El QR que muestra el entrenador **codifica una URL de este frontend**: `https://<dominio-frontend>/checkin/{tokenSesion}`.
- *(Reemplazado el 2026-10-31 por decisión D4 y RN-033 — ver Anexo)* ~El QR fijo de un punto de acceso codifica `https://<dominio-frontend>/ingreso/{idPuntoAcceso}`.~ Ahora el ingreso general usa un **QR dinámico rotativo** que codifica `https://<dominio-frontend>/ingreso/{tokenDinamico}`.
- La validación real (ventana de asistencia, reserva confirmada, validez del token) ocurre en el backend, en `POST /api/asistencia/checkin/{token}` y `POST /api/asistencia/ingreso/{token}`. Este frontend nunca decide si un check-in es válido — solo lo solicita y muestra el resultado.
- Sin un bearer token válido, esos endpoints devuelven `401`. Esa es la señal exacta que el frontend usa para decidir mostrar el login.

## Lo que decide este documento

### Caso A — el estudiante ya tiene sesión iniciada en el navegador

1. Escanea el QR con la cámara nativa del teléfono → se abre `/checkin/{token}` en el navegador.
2. `CheckIn.razor` lee `{Token}` de la ruta, confirma que hay un bearer token válido en `localStorage`.
3. Llama a `POST /api/asistencia/checkin/{token}` con el header `Authorization` ya adjunto (Patrón 7, `ManejadorDeAutenticacionHttp`).
4. Muestra el resultado (`EstadoDeCarga<T>`, Patrón 2): éxito → confirmación con el nombre de la sesión; `Resultado.Fallo` (ej. fuera de ventana, sin reserva confirmada) → el mensaje que devuelve el backend, sin reinterpretarlo.

### Caso B — el estudiante no tiene sesión iniciada (el caso que el backend documenta como "ahora responsabilidad del frontend")

1. `CheckIn.razor` detecta ausencia/expiración del token **antes** de llamar al backend (evita gastar una llamada que sabemos que devolverá `401`).
2. Guarda el token de la sesión (`tokenSesion`, no el bearer token — son cosas distintas y no deben confundirse) en el estado de navegación (query string de retorno o estado de sesión del navegador), y redirige a `Login.razor` con un `returnUrl` apuntando de vuelta a `/checkin/{token}`.
3. Tras un login exitoso, `AutenticacionService` respeta el `returnUrl` y navega de vuelta a `/checkin/{token}` — que entonces sigue el Caso A automáticamente, sin que el usuario tenga que volver a escanear nada.
4. Si el login falla o el usuario lo cancela, el `tokenSesion` guardado no se reutiliza indefinidamente: se descarta si no se completa el check-in en la misma visita (evita que un token de sesión viejo se dispare por accidente en una sesión de navegador mucho más tarde).

### Caso C — el token del QR es inválido o la sesión ya cerró su ventana

El backend responde con un `Resultado.Fallo` (`400`/`409`, no `401`). La pantalla debe distinguir claramente este caso del Caso B: aquí el problema no es la sesión del usuario, es el QR/la ventana — el mensaje debe decirlo así, nunca mostrar una pantalla de login para un problema que el login no soluciona.

## Diagrama de decisión

```
Escaneo del QR → abre /checkin/{token}
        │
        ▼
 ¿Hay bearer token válido en localStorage?
        │
   ┌────┴─────┐
   NO         SÍ
   │           │
   ▼           ▼
 Guardar     POST /api/asistencia/checkin/{token}
 returnUrl        │
   │         ┌────┴─────┐
   ▼         2xx        Fallo (400/409)
 Login            │           │
   │              ▼           ▼
   └────────▶ Confirmación  Mensaje de
     (tras       de          error
      login)     asistencia  (RN-022/RN-025)
```

## Generación del código QR (rol Entrenador, RF-012)

`GET /api/asistencia/sesiones/{idSesion}/qr` devuelve el `TokenQR` como texto plano, no una imagen (decisión explícita del backend). `QrDeSesion.razor` construye la URL completa (`https://<dominio-frontend>/checkin/{token}`) y la pasa al componente puro `<CodigoQr Valor="@url" />` (Patrón 6), que la renderiza como imagen SVG/canvas. La ventana de validez que acompaña al token se muestra junto al QR, convertida a `America/Bogota` (Patrón 3).

---

## Anexo: QR dinámico para ingreso general (RN-033, RF-039 — Plan de despliegue 2026-10-31)

### Contexto y cambio sobre el diseño original
En el diseño original, cada punto de acceso físico tenía un QR estático impreso que codificaba el identificador del punto. Por decisión D4 y regla RN-033, **el QR impreso queda reemplazado por un código QR dinámico** que rota periódicamente (provisionalmente cada 30 segundos, AMB-D06) para impedir que los usuarios guarden una foto y registren ingresos fraudulentos.

El flujo de check-in de una sesión con el entrenador (**RF-005, RF-012**) no cambia: ya utiliza un token único por instancia con ventana de tiempo restringida.

### Pantalla del punto de acceso (`/admin/puntos-acceso/{id}/pantalla`)
- **Destino físico:** pensada para operar de manera desatendida en un monitor, tableta o quiosco en la entrada de las instalaciones deportivas.
- **Identidad y diseño:** fondo claro institucional, logo oficial ITM azul, nombre claro del punto de acceso, código QR de gran escala y un anillo visual de cuenta regresiva (`AnilloCuentaRegresiva.razor`) sincronizado con la rotación del token.
- **Patrón de actualización periódica (Patrón 9):**
  1. El cliente consulta `GET /api/asistencia/puntos-acceso/{id}/qr-actual`.
  2. La respuesta incluye `{ token, validoHasta, periodoSegundos }`.
  3. El frontend genera el QR con la URL `{BaseUri}ingreso/{token}`.
  4. La próxima petición se programa automáticamente **2 segundos antes de `validoHasta`** basándose exclusivamente en el reloj del servidor entregado en el payload (no en el reloj local del cliente).
  5. En caso de fallo transitorio de red, el QR vigente se mantiene en pantalla hasta alcanzar su vencimiento real. Si al vencer no se ha podido renovar, la pantalla muestra un estado accesible *"Sin conexión con el servidor. Reintentando..."* con reintento progresivo, garantizando que nunca se exhiba un código ya expirado como si fuera válido.
  6. Se invoca la API nativa de *Screen Wake Lock* vía JSInterop para evitar que la pantalla del monitor o tableta se suspenda durante la jornada. Al abandonar el componente se liberan los temporizadores y el Wake Lock (`IDisposable` / `IAsyncDisposable`).

### Manejo de token vencido en el cliente (`Ingreso.razor`)
Cuando un usuario escanea un código QR cuyo periodo de rotación y tolerancia ya expiraron (RN-033):
- El backend responde `400` / `409` con el código de error `QrVencido` y mensaje explicativo del sistema.
- `Ingreso.razor` muestra el error con icono y texto claro: *"El código QR ha expirado. Por favor, escanee nuevamente el código mostrado en la pantalla de acceso"*.
- **Sin bucle de reintento:** la pantalla no ofrece reintentar con el mismo token vencido (lo que provocaría rechazos continuos), guiando al usuario a volver a enfocar la pantalla física.

