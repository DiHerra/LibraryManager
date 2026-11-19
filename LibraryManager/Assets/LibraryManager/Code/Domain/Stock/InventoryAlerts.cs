using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Purchasing;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.Stock
{
    public readonly struct ExpiringReturn
    {
        public ExpiringReturn(int bookId, int quantity, GameDate deadline)
        {
            BookId = bookId;
            Quantity = quantity;
            Deadline = deadline;
        }

        public int BookId { get; }
        public int Quantity { get; }
        public GameDate Deadline { get; }
    }

    /// <summary>Avisos del inventario para la pantalla de la tienda.</summary>
    public static class InventoryAlerts
    {
        public const int BestsellerRank = 30;
        public const int ExpiringReturnDays = 14;
        public const int StaleDays = 180;

        /// <summary>Títulos entre los más populares del día sin ejemplares ni pedido en camino.</summary>
        public static IReadOnlyList<Book> MissingBestsellers(BookCatalog catalog, Inventory inventory, PurchaseOrderBook orders, GameDate date)
        {
            return catalog.MostPopular(date, BestsellerRank)
                .Where(book => inventory.Quantity(book.Id) == 0 && orders.PendingQuantity(book.Id) == 0)
                .ToList();
        }

        /// <summary>Novedades cuyo plazo de devolución acaba en los próximos 14 días.</summary>
        public static IReadOnlyList<ExpiringReturn> ExpiringReturns(Inventory inventory, GameDate date)
        {
            GameDate limit = date.AddDays(ExpiringReturnDays);
            return inventory.Lots
                .Where(lot => lot.IsReturnableOn(date) && lot.ReturnDeadline.Value <= limit)
                .GroupBy(lot => lot.BookId)
                .Select(group => new ExpiringReturn(group.Key, group.Sum(lot => lot.Quantity), group.Min(lot => lot.ReturnDeadline.Value)))
                .OrderBy(item => item.Deadline)
                .ThenBy(item => item.BookId)
                .ToList();
        }

        /// <summary>Títulos cuyo lote más reciente lleva más de 180 días en la tienda.</summary>
        public static IReadOnlyList<int> StaleTitles(Inventory inventory, GameDate date)
        {
            return inventory.Lots
                .GroupBy(lot => lot.BookId)
                .Where(group => group.Max(lot => lot.ReceivedDate).DaysUntil(date) > StaleDays)
                .Select(group => group.Key)
                .OrderBy(id => id)
                .ToList();
        }
    }
}
