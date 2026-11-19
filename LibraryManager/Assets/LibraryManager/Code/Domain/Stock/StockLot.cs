using System;
using LibraryManager.Domain.Common;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.Stock
{
    /// <summary>
    /// Ejemplares de un mismo título que entraron juntos, al mismo coste. Si eran
    /// novedad al llegar, se pueden devolver a la distribuidora hasta <see cref="ReturnDeadline"/>.
    /// </summary>
    public sealed class StockLot
    {
        public StockLot(int bookId, int quantity, Money unitCost, GameDate receivedDate, GameDate? returnDeadline)
        {
            BookId = bookId;
            Quantity = quantity > 0 ? quantity : throw new ArgumentOutOfRangeException(nameof(quantity), "Un lote necesita al menos un ejemplar.");
            Guard.NotNegative(unitCost.Cents, nameof(unitCost));
            UnitCost = unitCost;
            ReceivedDate = receivedDate;
            if (returnDeadline.HasValue && returnDeadline.Value < receivedDate)
            {
                throw new ArgumentOutOfRangeException(nameof(returnDeadline), "El plazo de devolución no puede acabar antes de la llegada.");
            }

            ReturnDeadline = returnDeadline;
        }

        public int BookId { get; }
        public int Quantity { get; private set; }
        public Money UnitCost { get; }
        public GameDate ReceivedDate { get; }
        public GameDate? ReturnDeadline { get; }

        public Money TotalCost => UnitCost * Quantity;

        public bool IsReturnableOn(GameDate date) => ReturnDeadline.HasValue && date <= ReturnDeadline.Value;

        internal int Take(int quantity)
        {
            int taken = Math.Min(quantity, Quantity);
            Quantity -= taken;
            return taken;
        }
    }
}
