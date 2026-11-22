# Lectores, demanda y ventas (L06-L07)

Estado: implementado el 2026-11-22, pendiente de validación en Unity. Los números son provisionales hasta el equilibrio de L08.

## Cuándo se vende

Al pulsar «Avanzar día», si la tienda abre ese día se simula su jornada completa (`SalesSimulator`) antes de pasar al día siguiente: se cobra lo vendido, se saca del inventario (FIFO) y se reajusta lo expuesto. Después llega el día nuevo con sus eventos (alquiler, pedidos). Los días cerrados (domingos y festivos) no hay ventas. Todo el azar sale de `DeterministicRandom`: misma semilla y mismas decisiones, mismas ventas.

## Lectores

| Tipo | Parte | Géneros preferidos | Presupuesto medio | Busca un título | Veces que ojea | Mira la caja |
|---|---|---|---|---|---|---|
| Ocasional | 35 % | narrativa, negra, romántica, autoayuda, cocina | 25 € | 30 % | 2 | 15 % |
| Gran lector | 20 % | narrativa, negra, histórica, ensayo, poesía, fantasía, ciencia ficción | 45 € | 50 % | 3 | 10 % |
| Familia | 20 % | infantil, juvenil, texto, cómic | 40 € | 50 % | 2 | 20 % |
| Regalo | 15 % | narrativa, cocina, viajes, infantil, ensayo | 35 € | 40 % | 2 | 25 % |
| Aficionado | 10 % | fantasía, ciencia ficción, cómic, juvenil | 40 € | 60 % | 2 | 10 % |

- Cada visitante lleva entre el 60 % y el 140 % del presupuesto medio de su tipo.
- Un género preferido pesa 3 y el resto 1. En la vuelta al cole los libros de texto pesan ×4.
- En Navidad y en el Día del Libro los que buscan regalo son el doble.
- El 40 % de los visitantes tiene la tarjeta de lector.

## Afluencia

Visitantes del día = 55 × día de la semana × temporada × reputación × azar (±15 %).

- Día: lunes ×0,8; martes a jueves ×0,9; viernes ×1,1; sábado ×1,5 (domingo ×1,2 si algún día se abre).
- Temporada: la de `CommercialCalendar` (vuelta al cole ×1,3, Navidad ×1,8, Día del Libro ×2,5, verano ×0,8).
- Reputación (0–100): de ×0,6 a ×1,4, ×1 con 50.
- Hora de llegada: pesos 7 antes de las 12, 10 de 12 a 14, 7 de 14 a 17, 12 de 17 a 19 y 9 después.

## Una visita

1. **Búsqueda** (según el tipo): elige un título publicado con peso interés^1,5 × gusto por el género.
   - Si hay ejemplares (también en el almacén) y le llega el presupuesto, lo compra.
   - Si no hay, el librero propone con un 35 % el título expuesto más popular del mismo género; el cliente lo acepta con un 50 % (más con descuento de tarjeta).
   - Si no, encarga el libro con un 40 % (máximo 80 encargos pendientes). Si tampoco, la búsqueda se pierde.
2. **Ojear**: elige entre lo expuesto con peso (interés + 1) × visibilidad × gusto. Lo compra con probabilidad 0,05 + 0,006 × interés × visibilidad, con un tope del 85 %. Lo expuesto se repone del almacén durante el día.
3. **Caja**: con su probabilidad mira el expositor de caja y compra uno con un 50 %.

No compra dos veces el mismo título ni pasa de su presupuesto.

## Encargos

Un encargo es un ejemplar para un cliente. Al empezar cada jornada, los encargos con ejemplares en la tienda se recogen y se cobran (como cliente con tarjeta). El cliente espera 30 días; después lo da por perdido. La pantalla de la tienda lista los encargos por título con un botón para añadirlos al borrador de pedido.

## Precio, descuento e IVA

- Se cobra el PVP. Con tarjeta se aplica el descuento elegido, de 0 a 5 % (el máximo legal en España para el libro). El descuento multiplica la disposición a comprar de quien tiene tarjeta por 1 + descuento / 25 (×1,2 con el 5 %).
- El IVA superreducido del 4 % va incluido en el PVP: IVA = importe × 4 / 104. Se registra pero aún no se liquida (L08).
- Margen bruto de la jornada = ingresos − IVA − coste de lo vendido.

## Satisfacción y reputación

Satisfacción de cada visita (0–1):

| Resultado | Satisfacción |
|---|---|
| Encontró y compró lo que buscaba, o compró sin buscar | 1 |
| Aceptó la recomendación del librero | 0,8 |
| Dejó un encargo | 0,7 |
| Buscaba algo, no salió bien, pero se llevó otro libro | al menos 0,6 |
| Ojeó y no compró | 0,5 |
| Lo encontró pero no le llegaba el presupuesto | 0,4 |
| No lo encontró y se fue sin nada | 0 |
| Recogió su encargo / el encargo caducó | 1 / 0 |

La reputación (0–100, empieza en 50) recorre cada jornada un 2 % de la distancia hasta la satisfacción media del día. Una tienda vacía baja; una bien surtida sube despacio.

## Registro de ventas

`SalesLedger` guarda las últimas 400 jornadas (unidades, compradores, visitantes, ingresos, coste, descuentos, búsquedas, encontradas, perdidas, recomendaciones, encargos, satisfacción, unidades por hora y por género) y, de toda la partida, los totales por título con la fecha de la última venta. Las alertas de libros parados cuentan como movimiento la última venta o la última llegada.

## Guardado

La versión 6 añade `sales`: reputación, descuento, encargos y el registro. Una partida de la versión 5 empieza con reputación 50, sin descuento, sin encargos y sin ventas registradas.

## Pendiente para otros bloques

- L08: equilibrio de afluencia, presupuestos y fondo inicial (ahora el fondo inicial se agota en unas dos semanas si no se repone), liquidación del IVA y quiebra.
- L09: la recomendación dependerá del librero de turno y de su conocimiento del género.
- L10: eventos que suben la reputación y las visitas.
- L11: la competencia restará búsquedas de novedades y superventas.
