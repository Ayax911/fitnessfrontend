---
name: pruebas-obligatorias-fitnessfrontend
description: Enumera las pruebas obligatorias de cada fase de implementación de fitnessfrontend y qué debe demostrar cada una. Usar al cerrar una fase (constructor) o al revisarla (revisor), antes de darla por terminada.
---

# Pruebas obligatorias por fase

Estas pruebas están marcadas como obligatorias en [08-guia-de-implementacion.md](../../../docs/arquitectura/08-guia-de-implementacion.md). No son la cobertura completa esperada — son el mínimo que no puede faltar, porque cada una verifica un riesgo donde el fallo es **silencioso**: no lanza excepción, no rompe el build, y solo se nota en producción frente a un usuario real.

## Fase 1 — Eventos: prueba de pertenencia del Entrenador

**Qué debe demostrar:** con dos entrenadores de prueba y una actividad asignada a cada uno, `MisActividades.razor` muestra a cada entrenador **únicamente** la suya (RF-010, RN-007).

**Por qué es obligatoria:** es el espejo en el cliente de la prueba de pertenencia de la Fase 1 del backend. Si la pantalla enviara un identificador tomado de un parámetro en vez de derivarlo del token, la prueba lo expone.

**Señal de prueba falsa-positiva:** si pasa con un solo entrenador sembrado, no está probando nada — el aislamiento solo se demuestra con dos.

## Fase 2 — Reservas: ausencia de campos de priorización (RN-020)

**Qué debe demostrar:** el tipo consumido por `InscritosDeSesion.razor` y por `MisReservas.razor` **no tiene** campos de nivel de priorización ni de indicador de confiabilidad.

**Cómo verificarla:** por **inspección del tipo** (reflexión sobre el DTO generado, o una prueba de compilación que falle si el campo existiera), no mirando una respuesta de ejemplo. Una respuesta de ejemplo solo prueba que ese usuario concreto no tenía el dato; el tipo prueba que ningún usuario puede tenerlo.

**Señal de prueba falsa-positiva:** si la prueba examina un JSON de muestra en vez del tipo, no cubre el caso de que el backend empiece a incluir el campo.

## Fase 2 — Reservas: comportamiento de AMB-014

**Qué debe demostrar:** ante un rechazo con `codigoError = VentanaNoAbierta`, la pantalla muestra el texto genérico neutro definido en [01-ambiguedades-ui.md](../../../docs/requisitos/01-ambiguedades-ui.md) y **no** menciona nivel, indicador, motivo ni fecha estimada.

**Cómo verificarla:** prueba de componente (bUnit) con el backend simulado devolviendo ese código de error, aseverando sobre el texto renderizado — incluyendo aserciones negativas explícitas (que el marcado **no** contenga "nivel", "penalizado", "prioridad", ni una fecha).

**Por qué es obligatoria:** es el único punto de la UI donde una decisión de redacción puede violar una regla de confidencialidad. Es también lo más probable que alguien "mejore" después sin entender por qué el mensaje era deliberadamente vago.

## Fase 3 — Check-in: los tres casos del flujo de QR

**Qué debe demostrar**, como tres pruebas separadas (ver [06-flujo-checkin-qr.md](../../../docs/arquitectura/06-flujo-checkin-qr.md)):

- **Caso A** — con sesión iniciada, `/checkin/{token}` completa el check-in y muestra confirmación.
- **Caso B** — sin sesión iniciada, redirige a login y, tras autenticarse, **vuelve automáticamente** a `/checkin/{token}` y completa el check-in **sin volver a escanear**.
- **Caso C** — con token inválido o ventana cerrada, muestra el error del backend y **no** una pantalla de login.

**Por qué es obligatoria:** distinguir B de C es el defecto más fácil de introducir en todo el repositorio — ambos son "no funcionó", pero uno se arregla iniciando sesión y el otro no. Confundirlos deja al estudiante en un bucle de login que nunca resuelve nada, parado frente al entrenador.

**Señal de prueba falsa-positiva:** si el Caso B se prueba navegando manualmente a `/checkin/{token}` después del login en vez de verificar la redirección automática, no está probando el `returnUrl`, que es justo lo que puede romperse.

## Fase 4 — Administración: recurrencia como dato, no como lista fija

**Qué debe demostrar:** el formulario de creación de evento refleja los patrones de recurrencia que el backend soporta, en vez de una lista de opciones fijada en el marcado.

**Por qué es obligatoria:** la enumeración de patrones de periodicidad sigue **pendiente** (Bloque 2). Una lista fija en el marcado se convierte en una decisión de negocio inventada, y además obliga a tocar el frontend cada vez que el backend soporte un patrón nuevo.

## Fase 5 — Priorización: aislamiento por rol

**Qué debe demostrar:** un usuario sin rol Administrador que navega **escribiendo la URL directamente** a una pantalla de `/priorizacion` no ve datos, y la UI maneja el `403` con un mensaje en vez de quedarse en blanco.

**Por qué es obligatoria:** ocultar el enlace del menú es usabilidad, no seguridad. Esta prueba verifica que la pantalla también se defiende cuando se la alcanza sin pasar por el menú.

## Regla general para cualquier fase no listada aquí

Antes de dar una fase por cerrada, pregúntate: **"¿qué regla de esta fase puede romperse sin que el build falle, sin que se lance una excepción, y sin que nadie lo note hasta que un usuario real lo sufra?"** — esa es la prueba obligatoria de la fase. En este repositorio esa categoría está dominada por tres cosas: campos que se filtran (RN-020), fechas mal convertidas (zona horaria), y flujos de navegación que se rompen solo en el caso sin sesión.
