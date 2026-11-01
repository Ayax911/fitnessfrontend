# 01. Priorización de atributos de calidad (ISO/IEC 25010)

> Hereda el orden ya confirmado por el stakeholder para todo el sistema (ver [fitnessbackend/docs/arquitectura/01-priorizacion-calidad-iso25010.md](../../../fitnessbackend/docs/arquitectura/01-priorizacion-calidad-iso25010.md)): **Mantenibilidad > Fiabilidad > Seguridad > Usabilidad > Rendimiento**. Este documento no reabre el orden — lo reinterpreta para las decisiones que son exclusivas de un cliente Blazor WebAssembly.

## 1. Mantenibilidad

- Un solo proyecto Blazor WASM, organizado por feature (espejo de los módulos del backend), sin arquitectura en capas especulativa (sin Clean Architecture de 4 proyectos para un frontend de este tamaño).
- El cliente HTTP se **genera** con Kiota a partir del contrato OpenAPI del backend — nunca se escribe a mano. Si el backend cambia un DTO, regenerar el cliente expone el error en tiempo de compilación, no en producción.
- Ningún componente de UI contiene lógica de negocio (cálculo de niveles, validación de ventanas de reserva): esa lógica ya existe en el backend y duplicarla en el cliente es la forma más común de que los dos repositorios diverjan silenciosamente.

## 2. Fiabilidad

- Todo error de red o `Resultado.Fallo` tiene una representación visual explícita — nunca una pantalla en blanco ni una excepción no capturada en la consola. Ver el patrón de manejo de resultados en [04-patrones-de-diseno.md](04-patrones-de-diseno.md).
- El estado de sesión (bearer token) sobrevive a un refresco de página (persistido en `localStorage`), pero **nunca** sobrevive a un `401` sin pasar por el flujo de refresh/login — un token expirado no debe producir una sesión "a medias" donde algunas llamadas funcionan y otras no.
- El flujo de check-in por QR (documento 06) es, dentro de este repositorio, el equivalente en importancia a RF-022 del backend: es la pantalla que un estudiante ve parado frente al entrenador, y debe degradar con gracia (mensaje claro) si el token es inválido o ya expiró, nunca quedarse cargando indefinidamente.

## 3. Seguridad

- El bearer token nunca se registra en logs de consola ni se expone en la URL (nunca como query string).
- CORS es responsabilidad del backend, pero el frontend nunca asume un origen de desarrollo fijo: la URL base de la API es configuración (`appsettings.json` de Blazor / variables de entorno de build), no una constante hardcodeada — así el mismo build no sirve para desarrollo y producción por accidente.
- Ningún componente de Usuario o Entrenador puede, ni siquiera en teoría, recibir o renderizar un campo de nivel de priorización / indicador de confiabilidad (RN-020 del backend) — se refuerza con DTOs generados que, para esos roles, simplemente no tienen esos campos (el backend ya los omite; el frontend no los reintroduce con un `dynamic` o un `Dictionary<string, object>` "por si acaso").

## 4. Usabilidad

- El flujo de escaneo de QR (Decisión 9 revisada del backend) es el punto de mayor prioridad de usabilidad: **cero pasos innecesarios** entre "escanear con la cámara nativa" y "asistencia registrada", incluyendo el caso de sesión no iniciada.
- Diseño mobile-first: la población objetivo (30.000 estudiantes) interactúa principalmente desde el teléfono al escanear el QR de entrada a una clase; el escritorio es secundario (más relevante para Administrador y Entrenador).
- Toda fecha/hora se muestra en `America/Bogota`, nunca en UTC crudo — ver la regla transversal del backend en su documento 02, Decisión 5.

## 5. Rendimiento

- Última prioridad, explícitamente: se acepta el costo de arranque en frío típico de Blazor WebAssembly (descarga del runtime de .NET) a cambio de mantenibilidad y de no depender de un servidor de renderizado. Si esto se vuelve un problema real, medido (no anticipado), la mitigación es AOT/trimming de la publicación, no un cambio de framework.
