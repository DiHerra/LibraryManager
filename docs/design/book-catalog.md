# Libros, editoriales, distribuidoras y catálogo (L03)

## Modelo

- **Distribuidora** (`Distributor`): plazo de entrega en días, pedido mínimo y plazo de devolución de novedades. Las usará L04.
- **Editorial** (`Publisher`): tamaño (pequeña, mediana o grande), géneros en los que se centra, distribuidora y descuento al librero sobre el PVP (en puntos básicos). Grandes en torno al 30 %, medianas al 33 % y pequeñas al 35 %, ±2 puntos.
- **Libro** (`Book`): un título del mundo, no un ejemplar. Título, autor, editorial, género, encuadernación, PVP con IVA, fecha de publicación, popularidad base (1–100), semivida de la novedad y suelo de fondo.

## Fase e interés

- `PhaseOn(fecha)`: *próximamente* antes de salir, *novedad* durante los 90 días siguientes a la publicación y *fondo* después.
- `PopularityOn(fecha)` = popularidad base × (suelo + (1 − suelo) × 0,5^(edad / semivida)). Es máxima el día que sale, la parte de novedad se reduce a la mitad cada semivida y queda el suelo de fondo. Antes de salir es 0.
- Semivida entre 20 y 180 días (los textos escolares, 120). Suelo entre el 5 y el 30 % en general; un 10 % de la ficción se convierte en clásico del fondo (40–70 %); infantil, poesía, ensayo y cocina conservan más (25–60 %); los textos escolares, el 60 %.

La demanda (L06) partirá de este interés, multiplicado por la temporada (L02) y por la colocación en la tienda (L05).

## Generador

`CatalogGenerator.Generate(semilla, fechaDeReferencia)` crea un mundo reproducible: misma semilla y misma fecha, mismo catálogo.

- 4 distribuidoras y 20 editoriales con nombres ficticios (`CatalogNameBank`).
- 260 autores ficticios con un género principal y una fama muy sesgada (pocos autores muy conocidos).
- 640 títulos. La editorial se elige con peso por tamaño (grande 4, mediana 2, pequeña 1); el 85 % de las veces el género es uno de los de la editorial y el 70 % de las veces el autor es especialista en ese género.
- Fechas: el 15 % sale en los próximos 12 meses, el 40 % salió en los dos últimos años y el resto en los ocho anteriores. Los textos escolares salen en junio o julio.
- Popularidad = 1 + 70 × suerte³ + 29 × fama del autor: la mayoría de los títulos son modestos y unos pocos son superventas.
- Precio según encuadernación (bolsillo 8–13 €, rústica 15–23 €, tapa dura 19–32 €), ajustado por género (infantil y poesía más baratos, cocina y viajes más caros, textos escolares 24–42 €), terminado en ,95 o ,90.
- Títulos únicos generados con plantillas por género; si una combinación se repite, se prueba otra y, como último recurso, se numera.

## Guardado

La partida guarda el catálogo completo (versión 3), no la semilla. Así, mejorar el generador solo cambia las partidas nuevas. Una partida de la versión 2 recibe un catálogo generado con una semilla derivada del estado de su generador, sin consumirlo, y con su fecha actual como referencia.

## Pantalla

«Ver catálogo» en la tienda muestra los 40 títulos más populares del día, filtrables por género, y los que salen en los próximos 30 días. Es de solo consulta hasta que existan los pedidos (L04).
