# Library Manager

Juego de gestión de una librería independiente para PC, hecho en Unity 6000.3.19f1 con UI Toolkit.

- `ROADMAP.md`: roadmap maestro con fases, bloques (L01, L02…) y avances validados.
- `docs/product/scope-1.0.md`: alcance cerrado de la 1.0.
- `docs/decisions`: decisiones (ADR). `docs/design`: un documento por sistema.
- `LibraryManager/`: proyecto Unity. Código en `Assets/LibraryManager/Code` (Domain, Application, Infrastructure, Presentation) y pruebas en `Assets/LibraryManager/Tests`.
- `scripts/Verify.ps1`: compilación y suite EditMode; la CI usa el mismo script.

## Primera puesta en marcha

1. Abre `LibraryManager/` con Unity 6000.3.19f1.
2. Menú **Library Manager > Configurar escenas base**: crea las escenas `MainMenu` y `Store` y las añade a la build.
3. **Window > General > Test Runner > EditMode > Run All**.
4. Abre `MainMenu` y pulsa Play.
