# Instrucciones para agentes

- Responde y documenta en español.
- `ROADMAP.md` es la fuente de verdad. Trabaja por bloques (L01, L02…): al cerrar uno, marca las casillas y añade una nota «Avance AAAA-MM-DD (Lxx)» con lo hecho, las pruebas nuevas por área y cómo se validó. Lo que Diego no ha probado en Unity queda como «pendiente de validación en Unity».
- Las decisiones de diseño o de alcance de Diego se anotan con fecha («decisión de Diego, AAAA-MM-DD») y, si cambian el rumbo, como ADR en `docs/decisions`.
- Cada sistema nuevo tiene su documento en `docs/design/<sistema>.md`.
- Arquitectura (ADR-0001): reglas en `Domain` y casos de uso en `Application`, ambos sin UnityEngine. Pantallas pequeñas en `Presentation`; nada de controladores de miles de líneas.
- Todo azar pasa por `DeterministicRandom`. El dinero es `Money` (céntimos). Las fechas son `GameDate`.
- Cada cambio del guardado sube `SaveGameSchema.CurrentVersion`, añade la migración en `SaveGameMigrator` y una prueba que carga la versión anterior.
- Cada regla nueva lleva pruebas EditMode. Verificación: `scripts/Verify.ps1` con Unity cerrado.
- Dentro de los namespaces `LibraryManager.*`, usa `UnityEngine.Application` con el nombre completo: `Application` a secas es el namespace `LibraryManager.Application`.
