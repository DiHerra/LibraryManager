# Stock, pedidos y devoluciones (L04)

## Inventario

`Inventory` guarda las existencias por **lotes** (`StockLot`): título, cantidad, coste unitario, fecha de llegada y, si se puede devolver, fecha límite de devolución. Las salidas son FIFO (primero lo más antiguo), así que el coste de lo que se venda en L07 sale del lote correcto.

**Fondo inicial:** una partida nueva empieza con los 150 títulos más populares del día: 6 ejemplares de los 15 primeros, 4 de los 35 siguientes y 2 del resto (490 ejemplares). Ya son de la librería: no se pagan ni se pueden devolver.

## Coste

Coste unitario = PVP − descuento de la editorial (28–37 %). El IVA se tratará en L08.

## Pedidos

- Un pedido va a **una distribuidora** y solo puede llevar títulos ya publicados de editoriales que esa distribuidora reparte.
- Límites: de 1 a 100 ejemplares por título y 500 por pedido; el total a coste debe cubrir el **pedido mínimo** de la distribuidora (60–200 €).
- Llega el primer día de apertura después del **plazo de entrega** (2–6 días). La llegada es un evento del mundo que **pausa el avance** (L02).
- Se **paga al recibirlo**. Los plazos de pago a 30–60 días quedan para L08.
- El jugador prepara una **cesta** (`OrderDraft`, en Application, no se guarda) desde el catálogo y la envía por distribuidora desde la pantalla de pedidos.

## Devoluciones y saldos

- Un lote que llega mientras el título es **novedad** se puede devolver hasta `llegada + plazo de devolución` de la distribuidora (90 o 120 días). Se devuelven todos los ejemplares en plazo y se recupera su coste.
- El fondo no se devuelve: se puede **saldar** a un saldista por el 10 % del PVP por ejemplar.

## Alertas

- **Superventas sin stock:** títulos entre los 30 más populares del día sin ejemplares ni pedido en camino.
- **Devoluciones a punto de vencer:** novedades devolvibles cuyo plazo acaba en 14 días o menos.
- **Libros parados:** títulos cuyo último lote llegó hace más de 180 días. Cobrará más sentido con las ventas (L07).

## Guardado

La versión 4 guarda los lotes, los pedidos (también los recibidos, como historial) y el número del siguiente pedido, y añade a los eventos la referencia al pedido. Una partida de la versión 3 recibe el fondo inicial con su fecha actual y ningún pedido.
