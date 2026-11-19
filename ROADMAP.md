# Library Manager — Roadmap maestro

Fecha de referencia: 19 de noviembre de 2026. Estado: borrador pendiente de lectura de Diego. Aprobado el enfoque «gestión primero, historia después» (2026-11-19).

Este documento es la referencia permanente para desarrollar el juego, con el mismo método que Volley Manager: bloques con ID estable, criterios de aceptación y una nota de avance con la validación al cerrar cada bloque.

## 1. Objetivo y alcance

Crear un juego de gestión para PC en el que el usuario dirige una librería independiente en una ciudad ficticia durante varios años: elige el catálogo, compra a distribuidoras, coloca los libros, atiende a los lectores, contrata libreros, organiza eventos y compite con una cadena y con la venta online. Las decisiones deben tener consecuencias comprensibles y el mundo debe seguir reglas coherentes.

Decisión de enfoque (Diego, 2026-11-19): **gestión primero, historia después**. La 1.0 es el modo de gestión libre. El modo historia es un bloque posterior que reutiliza los sistemas ya probados.

`docs/product/scope-1.0.md` fija la línea base, la política de compatibilidad y la definición de terminado.

## 2. Cómo interpretar los estados

- **Pendiente:** no hay implementación.
- **Parcial:** existe una parte, pero faltan comportamiento, integración o validación.
- **Cerrado:** se cumplen los criterios de aceptación y la validación queda registrada (pruebas EditMode verdes y, si hay pantalla, prueba de Diego en Unity).
- `[x]` capacidad terminada; `[ ]` trabajo futuro.

## 3. Base técnica (heredada de Volley Manager)

| Elemento | Decisión |
|---|---|
| Motor | Unity 6000.3.19f1, C#, UI Toolkit |
| Assemblies | `LibraryManager.Domain` y `LibraryManager.Application` (sin referencias a UnityEngine), `Infrastructure`, `Presentation`, `Editor`, `Tests.EditMode`, `Tests.PlayMode`, `Tests.Statistical` |
| Datos | ScriptableObjects en `Assets/LibraryManager/Data`, convertidos a entidades de dominio |
| Guardado | JSON con versión de esquema y migraciones probadas desde la versión 1 |
| Aleatoriedad | Semillas deterministas; misma semilla, mismo resultado |
| Verificación | `scripts/Verify.ps1` (compilación y EditMode) y CI con el runner Windows propio |
| Lección aprendida | Una pantalla = un presenter pequeño por sección; ningún controlador central de miles de líneas |

## 4. Orden de desarrollo

| Fase | Bloques | Resultado esperado |
|---|---|---|
| A. Fundamentos | L01–L03 | Proyecto verificable, reloj del mundo y modelo de libros |
| B. Núcleo de tienda | L04–L08 | Un día de librería jugable: stock, clientes, ventas y caja |
| C. Gestión | L09–L13 | Personal, eventos, competencia y economía mensual sostenida |
| D. Producto | L14–L16 | Interfaz final, partidas robustas y contenido de lanzamiento |
| E. Cierre | L17–L18 | Campaña larga estable y versión 1.0 |
| F. Expansiones | L19+ | Modo historia y ampliaciones |

## 5. Bloques de la versión 1.0

### L01. Proyecto verificable y arquitectura

Estado: cerrado el 2026-11-19. Prioridad: crítica. Dependencias: ninguna.

- [x] Proyecto Unity 6000.3.19f1 con URP 2D, UI Toolkit e Input System (plantilla Universal 2D).
- [x] Assemblies `Domain` y `Application` sin UnityEngine, más `Infrastructure`, `Presentation`, `Editor`, `Tests.EditMode`, `Tests.PlayMode` y `Tests.Statistical`.
- [x] Base del dominio: `Money` en céntimos, `GameDate`, `DeterministicRandom` con estado guardable, `Bookstore` y `WorldState`.
- [x] Casos de uso en `GameSession` (nueva partida, cargar, guardar, avanzar día) con `OperationGate` contra reentradas.
- [x] Guardado JSON versión 1 (`SaveGameData`, `SaveGameMapper`, `SaveGameMigrator`, `JsonSaveGameRepository` con escritura atómica y copia `.bak`).
- [x] Menú principal y pantalla provisional de la tienda (UI Toolkit desde código).
- [x] Menú del editor «Library Manager > Configurar escenas base» que crea `MainMenu`, `Store`, el PanelSettings y la lista de escenas de la build.
- [x] `scripts/Verify.ps1` y workflow de CI EditMode para el runner Windows propio.
- [x] ADR-0001 de arquitectura, ADR-0002 de alcance 1.0 y ADR-0003 de enfoque.
- [x] Validación en Unity: compila, Test Runner EditMode sin fallos y recorrido nueva partida → avanzar día → guardar → continuar.

Avance 2026-11-19 (L01): estructura creada con pruebas EditMode de dinero (4), fechas (4), generador (4), mundo y librería (4), compuerta (3), conversión de guardado (4), sesión (3) y repositorio JSON (3). Validado en Unity por Diego: Test Runner sin fallos y recorrido del menú y la tienda correcto. Corrección durante la validación: la escena Store se guardaba sin PanelSettings (Unity lo descargaba al abrir la segunda escena) y se veía la pantalla azul; el menú del editor carga el asset después de abrir cada escena.

### L02. Reloj del mundo y calendario

Estado: cerrado el 2026-11-19. Dependencias: L01. Diseño: `docs/design/world-clock.md`.

- [x] Fecha del mundo, días de apertura y horario de la tienda (`StoreSchedule`, festivos nacionales fijos).
- [x] Avance por día con resumen (`DayReport`) y avance hasta el lunes siguiente con resumen de la semana (`AdvanceResult`).
- [x] Cola de eventos fechados con repetición semanal y mensual (`WorldEventQueue`); alquiler mensual de 1.800 € desde el 1 de octubre.
- [x] Temporadas comerciales: vuelta al cole, Navidad, Día del Libro, verano, con multiplicador de demanda provisional.
- [x] Motivos de pausa del avance: evento que pide atención, inicio de temporada y caja en negativo.
- [x] Guardado versión 2 (horario y eventos) con migración desde la versión 1.
- [x] Pantalla de la tienda: día, abierto o cerrado, temporada, caja, próximos eventos, «Avanzar día», «Avanzar semana» y resumen del último avance.
- [x] Validación en Unity.

Avance 2026-11-19 (L02): pruebas nuevas de horario (3), temporadas (11 casos), cola de eventos (5), reloj del mundo (9), guardado de eventos y migración de la versión 1 (2 más 2 casos de datos inválidos) y avance semanal de la sesión (1). Validado en Unity por Diego: Test Runner sin fallos, migración de la partida de L01, cobro del alquiler el 1 de octubre y pausa al empezar la Navidad.

### L03. Modelo de libros y catálogo del mundo

Estado: en curso, pendiente de validación en Unity. Dependencias: L02. Diseño: `docs/design/book-catalog.md`.

- [x] Libro: título, autor, editorial, género (15), encuadernación (3), PVP, fecha de publicación y fase (próximamente, novedad o fondo).
- [x] Popularidad base, semivida de la novedad y suelo de fondo; interés del día con `PopularityOn`.
- [x] Editoriales (tamaño, géneros, descuento) y distribuidoras (plazo, pedido mínimo, plazo de devolución).
- [x] Generador de catálogo reproducible por semilla: 640 títulos, 20 editoriales, 4 distribuidoras y 260 autores ficticios.
- [x] Guardado versión 3 con el catálogo completo y migración desde la versión 2.
- [x] Panel «Ver catálogo» en la tienda: más populares del día por género y próximas novedades.
- [ ] Validación en Unity.

Avance 2026-11-19 (L03): pruebas nuevas de libro (3), catálogo (3), generador (8) y guardado del catálogo (3). Decisión técnica: el generador vive en el dominio (C#) y no en un script Python con assets como en Volley, porque el catálogo se genera por partida; la partida guarda el catálogo completo para que los cambios del generador no alteren partidas existentes. Pendiente de compilar y ejecutar en Unity.

### L04. Stock, pedidos y devoluciones

- [ ] Inventario por título y ejemplar, con coste y fecha de entrada.
- [ ] Pedidos a distribuidoras con plazo, mínimo y descuento.
- [ ] Devolución de novedades dentro de plazo y saldos del fondo.
- [ ] Alertas de rotura de stock y de libros parados.

### L05. Tienda: secciones, estanterías, mesas y escaparate

- [ ] Plano de la tienda con muebles y capacidad.
- [ ] Colocar libros por sección, en mesa de novedades o en escaparate.
- [ ] La ubicación afecta la visibilidad y la probabilidad de venta.
- [ ] Vista 2D de la tienda.

### L06. Lectores y demanda

- [ ] Tipos de lector con gustos por género, presupuesto y frecuencia.
- [ ] Llegada de clientes por hora, día y temporada.
- [ ] Búsqueda de un libro concreto, compra por impulso y pedido por encargo.
- [ ] Satisfacción del cliente y fidelidad.

### L07. Venta y recomendación

- [ ] Simulación de una jornada: clientes, búsquedas, ventas y libros no encontrados.
- [ ] Recomendación del librero según su conocimiento del género.
- [ ] Tarjeta de fidelidad y descuentos permitidos.
- [ ] Registro de ventas por título, género y hora.

### L08. Caja y economía básica

- [ ] Caja diaria, IVA superreducido del 4 % y márgenes por editorial.
- [ ] Gastos fijos: alquiler, suministros y sueldos.
- [ ] Balance mensual y quiebra si la caja no aguanta.
- [ ] Prueba de equilibrio con semillas: una librería bien llevada sobrevive y una mal llevada no.

### L09. Personal

- [ ] Libreros con conocimiento por género, trato y rapidez.
- [ ] Contratación, sueldo, turnos y cansancio.
- [ ] Formación y mejora con el tiempo.

### L10. Eventos y comunidad

- [ ] Presentaciones de autor, firmas y club de lectura.
- [ ] Coste, preparación y efecto en ventas y reputación.
- [ ] Reputación del barrio.

### L11. Competencia

- [ ] Cadena grande y venta online con precios y surtido propios.
- [ ] Efecto en la demanda de novedades y de fondo.

### L12. Ampliaciones y carrera

- [ ] Reformas, más metros, cafetería o sección infantil.
- [ ] Préstamos y objetivos a largo plazo.
- [ ] Segunda librería (si cabe en el alcance tras L08).

### L13. Informes y análisis

- [ ] Informes de ventas, rotación, rentabilidad por sección y por editorial.
- [ ] Bandeja de avisos.

### L14. Interfaz final, navegación y accesibilidad
### L15. Partidas, ajustes y recuperación
### L16. Contenido, tutorial, localización (español e inglés) y presentación
### L17. Simulación de varios años, rendimiento y pruebas integrales
### L18. Beta, distribución y lanzamiento 1.0

## 6. Expansiones

### L19. Modo historia

Modo aparte de la gestión libre, con personajes, diálogos y guion, apoyado en los sistemas de L01–L13. Se diseñará cuando la gestión esté jugable.
