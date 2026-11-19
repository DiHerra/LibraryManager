# Reloj del mundo, calendario y eventos (L02)

## Fecha y avance

La partida empieza el martes 1 de septiembre de 2026. `WorldState.AdvanceDay` pasa al día siguiente y, al empezar ese día, resuelve sus eventos. Devuelve un `DayReport` con la fecha, si la tienda abre, la temporada, los eventos resueltos, la caja antes y después, y los motivos de pausa.

`AdvanceDays(n)` avanza hasta `n` días (máximo 366) y se detiene al final del primer día que trae un motivo de pausa. `AdvanceToNextWeek` avanza hasta el lunes siguiente (7 días si ya es lunes) con las mismas pausas. Ambos devuelven un `AdvanceResult` con los días, los días de apertura, el cambio de caja y si se detuvo antes de tiempo.

## Horario

`StoreSchedule` guarda los días de apertura y las horas. Por defecto abre de lunes a sábado, de 10 a 20. La tienda no abre los festivos nacionales de fecha fija (`PublicHolidays`: 1/1, 6/1, 1/5, 15/8, 12/10, 1/11, 6/12, 8/12 y 25/12). Los festivos locales y móviles quedan fuera de la 1.0. El horario se guarda en la partida para que más adelante el jugador pueda cambiarlo.

## Temporadas comerciales

| Temporada | Fechas | Demanda base |
|---|---|---|
| Normal | resto del año | ×1,00 |
| Vuelta al cole | 25 de agosto – 30 de septiembre | ×1,30 |
| Navidad | 1 de diciembre – 6 de enero | ×1,80 |
| Día del Libro | 16 – 23 de abril | ×2,50 |
| Verano | 1 de julio – 24 de agosto | ×0,80 |

Los multiplicadores (`CommercialCalendar.DemandBasisPoints`, en puntos básicos) son provisionales: la demanda los usará en L06 y se calibrarán en L08.

## Eventos

`WorldEventQueue` guarda los eventos pendientes ordenados por fecha y, a igual fecha, por identificador, así que el orden siempre es el mismo. Cada evento tiene tipo (`Reminder` o `Payment`), título, importe, repetición (ninguna, semanal o mensual) y si pausa el avance. Al resolverse, un evento recurrente programa su siguiente repetición. Un evento mensual conserva su día original (`AnchorDay`): uno del 31 pasa al 28 o 29 en febrero y vuelve al 31 en marzo.

Toda partida nueva programa el **alquiler del local**: 1.800 € el día 1 de cada mes, empezando el 1 de octubre. Es la primera salida fija de caja; el resto de gastos llega en L08.

## Motivos de pausa

- Un evento marcado como `PausesAdvance` (por ejemplo, un pedido que llega en L04).
- Empieza una temporada comercial distinta de la normal.
- La caja pasa de positiva a negativa (una sola vez, no cada día en negativo).

## Guardado

La versión 2 añade el horario, los eventos y el siguiente identificador de evento. Una partida de la versión 1 se migra con el horario por defecto y el alquiler a partir del mes siguiente a su fecha.
