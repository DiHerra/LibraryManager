# ADR-0001: arquitectura por capas

- Estado: aceptada
- Fecha: 2026-11-19
- Alcance: todo el código

## Contexto

Library Manager sigue el método de Volley Manager. En Volley la separación por assemblies funcionó, pero dos clases de presentación crecieron hasta miles de líneas y concentraron casos de uso.

## Decisión

- `LibraryManager.Domain`: reglas del juego en C# puro (`noEngineReferences`). Sin Unity, sin archivos, sin hora real.
- `LibraryManager.Application`: casos de uso (`GameSession`), formato de guardado y migraciones. Depende solo del dominio.
- `LibraryManager.Infrastructure`: Unity, disco y JSON (`JsonSaveGameRepository`).
- `LibraryManager.Presentation`: pantallas. Un presenter pequeño por pantalla o sección; las pantallas llaman a casos de uso de Application y no modifican el dominio directamente.
- `LibraryManager.Editor`: herramientas del editor.
- Pruebas: `Tests.EditMode` para reglas y casos de uso, `Tests.PlayMode` para integración con escenas, `Tests.Statistical` para equilibrio con muchas semillas.
- Todo azar pasa por `DeterministicRandom`, cuyo estado se guarda. Misma semilla y mismas decisiones, mismo resultado.
- El dinero se guarda en céntimos (`Money`) y las fechas como número de día (`GameDate`).

## Consecuencias

- Las reglas se prueban sin abrir escenas.
- Cada cambio del guardado sube `SaveGameSchema.CurrentVersion` y añade una migración con prueba.
- Si una clase de presentación pasa de unas 400 líneas, se extrae una sección o un caso de uso antes de seguir.
