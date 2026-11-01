# RF-041 y RF-042 · Diseño UI diferido: apelación y derechos del titular

> Fecha: 2026-10-31.
> Fase: **F10** del plan de despliegue frontend (`docs/plan-despliegue/01-guia-frontend.md`).
> Estado: **Solo documentación de diseño y bocetos en texto**. Sin implementación de código en este prototipo (conforme a la decisión de usuario D3).
> Alineado con el documento de arquitectura backend: `fitnessbackend/docs/plan-despliegue/06-diseno-apelacion-y-derechos-titular.md`.

---

## 1. Verificación de seguridad y confidencialidad previa (RN-020)

Antes de proponer cualquier interfaz para el titular de los datos:

| Regla | Compromiso en la interfaz de Usuario | Verificación de diseño |
|---|---|---|
| **RN-020** | Ninguna pantalla de Usuario o Entrenador renderiza nivel de priorización, indicador de confiabilidad, penalizaciones activas, fecha de apertura ni posición en lista de espera. | Las pantallas propuestas no muestran si el usuario está o no penalizado. La acción de apelación se rotula neutralmente como *"Solicitar revisión de mis reservas"* y no revela el estado del indicador. |
| **D1** | Sin notificaciones externas por correo. | El estado de la solicitud y las respuestas institucionales se consultan exclusivamente dentro de la aplicación. |
| **Tokens ITM** | Uso exclusivo de tipografías y colores de `itm-tema.css`. | Interfaces basadas en tarjetas `card-itm`, botones accesibles de 48 px y componentes `InsigniaEstado`. |

---

## 2. RF-041: Boceto de UI para "Solicitar revisión de reservas" (Apelación)

### 2.1. Punto de entrada (Lado Usuario)
Ubicación propuesta: Enlace accesible en `/mis-reservas` (o dentro de `/ayuda`):
*"¿Tuviste un inconveniente o inasistencia de fuerza mayor con una reserva? Solicita una revisión al equipo de Bienestar Universitario."*

### 2.2. Boceto de Pantalla: `/reservas/solicitar-revision`

```text
+-----------------------------------------------------------------------------------+
|  [Logo ITM]                      Gimnasio ITM                     [Usuario] [Menú]|
+-----------------------------------------------------------------------------------+
|  < Volver a Mis Reservas                                                          |
|                                                                                   |
|  Solicitud de revisión de reservas y asistencia                                   |
|  Si presentaste una inasistencia justificada por causas de fuerza mayor o fallas  |
|  técnicas comprobadas, puedes remitir una solicitud de revisión administrativa.   |
|                                                                                   |
|  +-----------------------------------------------------------------------------+  |
|  | FORMULARIO DE REVISIÓN                                                      |  |
|  |                                                                             |  |
|  | Seleccione la reserva o fecha objeto de revisión (opcional):                |  |
|  | [ Desplegable: Últimas 5 reservas / actividades recientes ]                 |  |
|  |                                                                             |  |
|  | Motivo y justificación detallada:                                           |  |
|  | +-------------------------------------------------------------------------+ |  |
|  | | Explique las circunstancias de tiempo, modo y lugar...                  | |  |
|  | |                                                                         | |  |
|  | +-------------------------------------------------------------------------+ |  |
|  | Contador: 0 / 1000 caracteres                                               |  |
|  |                                                                             |  |
|  | [ ] Adjuntar soporte o constancia médica / académica (futuro)               |  |
|  |                                                                             |  |
|  | [ Cancelar ]                             [ Enviar solicitud de revisión ]  |  |
|  +-----------------------------------------------------------------------------+  |
|                                                                                   |
|  * Su solicitud será evaluada de acuerdo con el reglamento de Bienestar.          |
|    Recibirá la resolución directamente en esta sección.                           |
+-----------------------------------------------------------------------------------+
```

### 2.3. Boceto de Bandeja Administrativa: `/admin/solicitudes-revision`

- Accesible exclusivamente para rol `Administrador`.
- Muestra el historial consolidado de inasistencias y asistencias del estudiante clasificado por origen (`Reserva.OrigenCancelacion`, RN-031).
- Acciones:
  - **Mantener estado:** Confirma con motivo explicativo.
  - **Restablecer inasistencia:** Descuenta la inasistencia justificada recalculando el motor de confiabilidad sin editar datos directos. Auditoría automática e inmutable (RF-032).

---

## 3. RF-042: Boceto de UI para "Mis datos" (Ley 1581 de 2012)

### 3.1. Punto de entrada
Menú de usuario (barra superior o pie de página): *"Mis datos y privacidad"*.

### 3.2. Boceto de Pantalla: `/perfil/mis-datos`

```text
+-----------------------------------------------------------------------------------+
|  [Logo ITM]                      Gimnasio ITM                     [Usuario] [Menú]|
+-----------------------------------------------------------------------------------+
|  Derechos del Titular de Datos Personales                                          |
|  En cumplimiento de la Ley Estatutaria 1581 de 2012 y la política institucional   |
|  de tratamiento de datos de la Institución Universitaria ITM.                     |
|                                                                                   |
|  +-----------------------------------------------------------------------------+  |
|  | 1. IDENTIFICACIÓN Y PERFIL                                                  |  |
|  | Nombre: Juan Pérez Gómez                                                    |  |
|  | Correo institucional: juan.perez@itm.edu.co                                 |  |
|  | Vinculación: Estudiante                                                     |  |
|  | Autorización de tratamiento otorgada el: 15/09/2026 10:30 a. m.             |  |
|  | Versión del aviso legal: v1.0 (2026)                                        |  |
|  +-----------------------------------------------------------------------------+  |
|                                                                                   |
|  +-----------------------------------------------------------------------------+  |
|  | 2. COPIA PORTABLE DE MIS DATOS                                              |  |
|  | Descargue una copia completa de su historial de reservas, asistencias y     |  |
|  | solicitudes registradas en la plataforma.                                   |  |
|  | [ Descargar mi historial (formato CSV / JSON) ]                             |  |
|  | (Nota de diseño: El archivo exportado excluye estrictamente campos RN-020) |  |
|  +-----------------------------------------------------------------------------+  |
|                                                                                   |
|  +-----------------------------------------------------------------------------+  |
|  | 3. RECTIFICACIÓN Y SUPRESIÓN                                                |  |
|  | Para actualizar sus datos personales institucionales o revocar la          |  |
|  | autorización de uso de las instalaciones deportivas:                        |  |
|  |                                                                             |  |
|  | [ Solicitar actualización de datos ]   [ Solicitar revocación / supresión ]|  |
|  +-----------------------------------------------------------------------------+  |
+-----------------------------------------------------------------------------------+
```

---

## 4. Garantías de confidencialidad y conclusiones de diseño

1. **Blindaje de RN-020:** Ninguno de los dos bocetos introduce campos de nivel, penalización, umbral o posición en cola. La descarga de datos personales exporta asistencias y reservas sin columnas relativas al algoritmo interno de confiabilidad.
2. **Reversibilidad y compatibilidad institucional:** Las acciones de rectificación y supresión se canalizan mediante solicitudes auditadas, protegiendo la integridad de los reportes históricos y la rendición de cuentas de la Universidad.
3. **Cierre de F10:** El diseño conceptual queda documentado para habilitar la futura implementación en cuanto el área jurídica y Bienestar Universitario aprueben las definiciones normativas pendientes.
