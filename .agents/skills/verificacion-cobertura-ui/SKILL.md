---
name: verificacion-cobertura-ui
description: Verifica que toda pantalla de fitnessfrontend tenga su RF de origen y que toda RF que deba tener interfaz esté efectivamente construida, incluidas las ambigüedades que deben quedar marcadas y no resueltas. Usar antes de dar por cerrada cualquier fase, tanto al construir como al revisar.
---

# Verificación de cobertura RF ↔ pantalla

Esta skill opera en tres direcciones. Las tres son necesarias — cualquiera por separado deja pasar un defecto real que ya ocurrió en este proyecto.

## Dirección 1: pantalla → requisito (¿todo lo que se construyó está justificado?)

1. `grep_search` en los componentes nuevos/modificados buscando comentarios de origen (patrón `(RF|RN|AMB)-\d+`).
2. Para cada ID encontrado, confirmar que existe de verdad en [05-requisitos-funcionales.md](../../../../fitnessbackend/docs/requisitos/05-requisitos-funcionales.md), [04-reglas-negocio.md](../../../../fitnessbackend/docs/requisitos/04-reglas-negocio.md) o [13-ambiguedades-inconsistencias.md](../../../../fitnessbackend/docs/requisitos/13-ambiguedades-inconsistencias.md). Un ID inventado o mal escrito (ej. `RF-25` cuando el máximo es RF-022) es bloqueante — suele ser trazabilidad decorativa sobre una decisión inventada.
3. Toda página enrutable (`@page`) **sin** ningún comentario de RF es un hallazgo: o implementa un requisito que nadie documentó, o es alcance inventado.

## Dirección 2: requisito → pantalla (la que atrapa los huecos silenciosos)

**Esta es la dirección más fácil de omitir y la que más daño hace.** En la revisión del backend del 2026-09-26 se descubrió que RF-010, RF-011 y RF-012 (los tres del Entrenador, todos `Must Have`) no estaban asignados a ningún módulo ni fase. En la revisión de la documentación de *este* repositorio (2026-09-27) se encontró exactamente el mismo patrón: **RF-007, RF-008 y RF-009 tenían carpeta prevista pero ninguna fase los construía.** Un chequeo que solo mire del código hacia los requisitos nunca detecta esto, porque el código simplemente no existe.

1. Abrir la **tabla de cobertura RF → fase → pantalla** de [08-guia-de-implementacion.md](../../../docs/arquitectura/08-guia-de-implementacion.md) y listar las RF de la fase actual.
2. Para cada una, confirmar que existe la pantalla correspondiente **y** que tiene su comentario de origen. Una RF de la fase actual sin pantalla es bloqueante (fase incompleta).
3. **Contrachequeo contra la lista maestra:** recorrer la tabla resumen de [05-requisitos-funcionales.md](../../../../fitnessbackend/docs/requisitos/05-requisitos-funcionales.md) y confirmar que cada RF `Must Have` aparece en la tabla de cobertura del documento 08 — asignado a una fase, o listado explícitamente en la sección "RF que deliberadamente NO tienen interfaz". Una RF `Must Have` que no esté en ninguna de las dos listas es un hallazgo bloqueante **del diseño, no del código**: repórtalo como tal en vez de improvisar dónde ponerlo.
4. **Chequeo inverso de los "sin interfaz":** confirmar que RF-014, RF-015, RF-017, RF-019 y RF-022 **no** tienen pantalla. Si alguno aparece como pantalla, significa que se migró lógica de negocio al cliente — bloqueante.

## Dirección 3: ambigüedad → marcador (¿lo que no se sabe, se dejó sin resolver?)

1. Abrir [01-ambiguedades-ui.md](../../../docs/requisitos/01-ambiguedades-ui.md) y listar las entradas que tocan la fase actual.
2. Para cada una, `grep_search` de su marcador (`PENDIENTE (AMB-014)`, `PENDIENTE (Bloque 2, pregunta 9)`, etc.) y confirmar que existe donde corresponde.
3. Confirmar que el comportamiento implementado es el **menos comprometedor** descrito en ese documento, no una interpretación más "completa" o "amable" que el agente haya preferido. Un mensaje más explicativo de lo especificado en AMB-014 no es una mejora: es una violación de RN-020 disfrazada de buena UX.

## Qué NO es un hallazgo

- Un `PENDIENTE` bien formado no es un hallazgo — es exactamente el comportamiento esperado para una ambigüedad abierta.
- Componentes de presentación puros sin RF (un botón, un contenedor de carga, el componente `CodigoQr`) no necesitan comentario de origen: no implementan un requisito, lo sirven. Solo las **páginas enrutables** lo necesitan.
- Que una pantalla no cubra un flujo de excepción todavía no especificado, si está marcado como pendiente.
