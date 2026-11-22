# Tienda: muebles, colocación y visibilidad (L05)

## Muebles

El local inicial (`DefaultStoreLayout`) mide 20 × 14 casillas, con la entrada abajo:

| Mueble | Capacidad | Visibilidad |
|---|---|---|
| Escaparate | 12 | ×2,5 |
| Mesa de novedades | 40 | ×1,8 |
| Expositor de caja | 15 | ×1,5 |
| Estantería de su sección (una por género, 15) | 40 cada una | ×1,0 |
| Estantería de otra sección | — | ×0,6 |
| Almacén (no expuesto) | sin límite | ×0,2 |

La visibilidad de un título es la del mejor mueble donde tiene al menos un ejemplar; si solo está en el almacén, ×0,2 (solo se vende si alguien lo pide); sin ejemplares, 0. La demanda (L06) la multiplicará por el interés del libro y por la temporada. Los valores son provisionales hasta calibrar en L08.

## Colocación

- Lo que no está expuesto está en el **almacén**. Colocar saca ejemplares del almacén; retirar los devuelve.
- Un mueble no admite más ejemplares que su capacidad, y no se puede exponer más de lo que hay en el inventario.
- Tras una salida de stock (devolución, saldo y, en L07, venta) se **reajusta** lo expuesto quitando primero de los muebles menos visibles.
- Los pedidos que llegan van a la **estantería de su sección** mientras quepan; el resto queda en el almacén.
- **Colocación automática:** escaparate con los títulos más populares (3 por título), mesa con las novedades (4), expositor con libros de menos de 15 € (2) y el resto en su estantería hasta llenarla. Es determinista.

## Vista 2D

Decisión de Diego (2026-11-22): **esquema ahora, pixel art después**. L05 usa un plano esquemático con UI Toolkit: cada mueble es un bloque en su posición, con un color por tipo que se oscurece al llenarse y su ocupación. Al pulsarlo se ve lo expuesto (con −1 y quitar) y lo que hay en el almacén (con +1 y todos), primero lo de su sección. El arte pixel del local se hará en L16.

## Guardado

La versión 5 guarda los muebles (tipo, nombre, capacidad, sección y posición) y lo expuesto en cada uno. Una partida de la versión 4 recibe el local por defecto con la colocación automática.
