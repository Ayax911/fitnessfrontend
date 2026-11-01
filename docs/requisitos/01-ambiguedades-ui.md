# 01. Ambigüedades abiertas que se resuelven en la interfaz

> Contraparte de [fitnessbackend/docs/requisitos/13-ambiguedades-inconsistencias.md](../../../fitnessbackend/docs/requisitos/13-ambiguedades-inconsistencias.md). Este documento existe porque **varias ambigüedades todavía abiertas del levantamiento son, por su naturaleza, decisiones de interfaz** — el backend las deja explícitamente sin resolver y las hereda este repositorio.
>
> La regla de comportamiento del proyecto aplica igual aquí: **no se resuelve unilateralmente ninguna de estas.** Se implementa el comportamiento menos comprometedor y se deja un `@* PENDIENTE (...) *@` o `// PENDIENTE (...)` que cite el ID. Nunca se implementa una interpretación en silencio como si estuviera confirmada.

## AMB-014 — la más importante de este repositorio

| Campo | Contenido |
|---|---|
| **Origen** | RF-002 / RF-017 — experiencia del usuario cuya ventana de reserva aún no abre |
| **El conflicto** | El indicador de confiabilidad y el nivel de priorización **no** deben ser visibles para el usuario (RN-020), pero el usuario **sí** va a notar que no puede reservar todavía una sesión que ve en el listado |
| **La pregunta sin responder** | ¿El usuario ve un mensaje genérico (p. ej. "aún no puedes reservar este evento"), o simplemente no se le muestra la opción de reservar, sin ninguna explicación? |
| **Por qué importa** | Sin definirlo, la interfaz puede confundir al usuario o generar quejas sin explicación posible — y cualquier mensaje demasiado específico ("tu nivel es penalizado", "podrás reservar en 3 días porque…") filtraría exactamente lo que RN-020 prohíbe |

**Comportamiento menos comprometedor a implementar mientras siga abierta:** mostrar la sesión en el listado con la acción de reservar **deshabilitada** y un texto genérico y neutro que no revele ni el nivel ni el motivo (p. ej. "Reservas aún no disponibles para esta sesión"), sin fecha estimada ni explicación del criterio. Es lo menos comprometedor porque: (a) no oculta información que el usuario ya puede ver (la sesión existe), (b) no revela nada del sistema de priorización, y (c) es reversible hacia cualquiera de las dos opciones que el stakeholder decida.

**Marcar en el código así:**
```razor
@* PENDIENTE (AMB-014): mensaje genérico neutro mientras el stakeholder no defina
   si debe mostrarse un texto explicativo o simplemente ocultarse la acción.
   No añadir motivo, nivel ni fecha estimada — violaría RN-020. *@
```

## Otras ambigüedades abiertas con impacto directo en la UI

| ID / origen | Qué está sin definir | Impacto en la interfaz | Comportamiento menos comprometedor |
|---|---|---|---|
| **Bloque 2, pregunta 9** (RF-005) | Qué ocurre si un usuario **sin reserva confirmada** escanea el QR de una sesión | La pantalla de check-in debe mostrar algo cuando el backend rechace por esta causa | Mostrar el `mensaje` que devuelva el backend tal cual, sin ofrecer una acción de "reservar ahora" que asumiría que el check-in sin reserva es recuperable |
| **Bloque 2, pregunta 25** (RF-011) | Si el Entrenador también debe ver la **lista de espera** de su sesión, además de los confirmados | Qué se renderiza en `InscritosDeSesion.razor` | Renderizar únicamente lo que el endpoint devuelva (por defecto solo `Confirmada`, según el propio `// PENDIENTE` del backend) — **no** añadir una segunda pestaña de lista de espera por iniciativa propia |
| **RN-011 pendiente** (RF-004) | Si existe un **plazo límite** antes de la sesión para poder cancelar | Si el botón "Cancelar reserva" debe deshabilitarse cerca de la hora de la sesión | Mantener la acción siempre habilitada y dejar que el backend acepte o rechace — la UI no inventa un plazo que ninguna regla define |
| **RF-009, información faltante** | Qué ocurre con reservas/asistencias ya registradas al editar o eliminar un evento (p. ej. reducir el cupo por debajo de las reservas confirmadas) | Qué advertencia muestra la pantalla de administración antes de confirmar | Mostrar una confirmación explícita de la acción sin prometer ningún comportamiento concreto sobre las reservas existentes; reflejar después lo que el backend haya hecho |
| **AMB-013** (RF-018) | Valores por defecto de los parámetros configurables | Qué muestra el formulario de configuración al abrirlo | Mostrar los valores que devuelva el backend, sin precargar valores "razonables" propios en el formulario |

## Qué NO es una ambigüedad de este repositorio

- **Cualquier regla de negocio.** Si la duda es "¿debería permitirse X?", la respuesta no se decide en la UI: se consulta al backend y se muestra su resultado. Ver la regla de comportamiento 1 de [../arquitectura/08-guia-de-implementacion.md](../arquitectura/08-guia-de-implementacion.md).
- **Los textos de error de negocio.** El backend devuelve `{ codigoError, mensaje }`; el frontend muestra ese `mensaje`. Solo se redacta un texto propio cuando el mensaje del backend sea genuinamente insuficiente para el usuario final, y en ese caso se documenta en el componente por qué se sustituyó.
