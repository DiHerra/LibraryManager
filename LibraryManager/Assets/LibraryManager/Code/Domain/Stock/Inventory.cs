using System;
using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.Stock
{
    /// <summary>Existencias de la librería, por lotes. Las salidas son FIFO: primero lo más antiguo.</summary>
    public sealed class Inventory
    {
        private readonly List<StockLot> _lots = new List<StockLot>();

        public Inventory()
        {
        }

        public Inventory(IEnumerable<StockLot> lots)
        {
            foreach (StockLot lot in lots ?? throw new ArgumentNullException(nameof(lots)))
            {
                Add(lot);
            }
        }

        /// <summary>Lotes ordenados por fecha de llegada (y orden de entrada).</summary>
        public IReadOnlyList<StockLot> Lots => _lots;

        public int TotalUnits => _lots.Sum(lot => lot.Quantity);

        public int TitleCount => _lots.Select(lot => lot.BookId).Distinct().Count();

        public Money ValueAtCost => _lots.Aggregate(Money.Zero, (total, lot) => total + lot.TotalCost);

        public IEnumerable<int> BookIds => _lots.Select(lot => lot.BookId).Distinct();

        public void Add(StockLot lot)
        {
            if (lot == null)
            {
                throw new ArgumentNullException(nameof(lot));
            }

            int index = _lots.FindIndex(existing => existing.ReceivedDate > lot.ReceivedDate);
            _lots.Insert(index < 0 ? _lots.Count : index, lot);
        }

        public int Quantity(int bookId) => _lots.Where(lot => lot.BookId == bookId).Sum(lot => lot.Quantity);

        public IEnumerable<StockLot> LotsOf(int bookId) => _lots.Where(lot => lot.BookId == bookId);

        public int ReturnableQuantity(int bookId, GameDate date) =>
            LotsOf(bookId).Where(lot => lot.IsReturnableOn(date)).Sum(lot => lot.Quantity);

        /// <summary>Saca ejemplares, los más antiguos primero. Devuelve su coste.</summary>
        public Money RemoveOldestFirst(int bookId, int quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }

            if (Quantity(bookId) < quantity)
            {
                throw new InvalidOperationException($"No hay {quantity} ejemplares del libro {bookId}.");
            }

            return Remove(LotsOf(bookId).ToList(), quantity);
        }

        /// <summary>Saca todos los ejemplares todavía devolubles. Devuelve cuántos y su coste.</summary>
        public (int Quantity, Money Cost) RemoveReturnable(int bookId, GameDate date)
        {
            List<StockLot> lots = LotsOf(bookId).Where(lot => lot.IsReturnableOn(date)).ToList();
            int quantity = lots.Sum(lot => lot.Quantity);
            return quantity == 0 ? (0, Money.Zero) : (quantity, Remove(lots, quantity));
        }

        private Money Remove(List<StockLot> lots, int quantity)
        {
            Money cost = Money.Zero;
            int remaining = quantity;
            foreach (StockLot lot in lots)
            {
                int taken = lot.Take(remaining);
                cost += lot.UnitCost * taken;
                remaining -= taken;
                if (lot.Quantity == 0)
                {
                    _lots.Remove(lot);
                }

                if (remaining == 0)
                {
                    break;
                }
            }

            return cost;
        }
    }
}
