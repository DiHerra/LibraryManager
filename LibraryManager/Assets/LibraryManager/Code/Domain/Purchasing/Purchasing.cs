using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Stock;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.Purchasing
{
    /// <summary>Línea pedida por el jugador: qué libro y cuántos ejemplares.</summary>
    public readonly struct OrderRequestLine
    {
        public OrderRequestLine(int bookId, int quantity)
        {
            BookId = bookId;
            Quantity = quantity;
        }

        public int BookId { get; }
        public int Quantity { get; }
    }

    /// <summary>Reglas de compra, recepción, devolución y saldo.</summary>
    public static class PurchasingRules
    {
        public const int MaxUnitsPerOrder = 500;
        public const int MaxUnitsPerLine = 100;

        /// <summary>Lo que paga un saldista por un libro de fondo: 10 % del PVP.</summary>
        public const int LiquidationBasisPoints = 1000;

        public static Money UnitCost(Book book, Publisher publisher)
        {
            return book.Price - book.Price.Percentage(publisher.TradeDiscountBasisPoints);
        }

        public static Money LiquidationValue(Book book)
        {
            return book.Price.Percentage(LiquidationBasisPoints);
        }

        /// <summary>Primer día de apertura a partir de la fecha prevista (la distribuidora no reparte en cierre).</summary>
        public static GameDate DeliveryDate(GameDate orderDate, Distributor distributor, StoreSchedule schedule)
        {
            GameDate date = orderDate.AddDays(distributor.LeadTimeDays);
            for (int i = 0; i < 14 && !schedule.IsOpen(date); i++)
            {
                date = date.AddDays(1);
            }

            return date;
        }

        /// <summary>Comprueba y prepara las líneas de un pedido a una distribuidora.</summary>
        public static List<OrderLine> BuildLines(BookCatalog catalog, int distributorId, GameDate today, IEnumerable<OrderRequestLine> request)
        {
            Distributor distributor = catalog.Distributors.FirstOrDefault(d => d.Id == distributorId)
                ?? throw new PurchasingException("Esa distribuidora no existe.");

            List<OrderRequestLine> requested = (request ?? Enumerable.Empty<OrderRequestLine>()).ToList();
            if (requested.Count == 0)
            {
                throw new PurchasingException("El pedido está vacío.");
            }

            var lines = new List<OrderLine>();
            foreach (IGrouping<int, OrderRequestLine> group in requested.GroupBy(line => line.BookId))
            {
                int quantity = group.Sum(line => line.Quantity);
                if (quantity <= 0 || quantity > MaxUnitsPerLine)
                {
                    throw new PurchasingException($"Cada título admite entre 1 y {MaxUnitsPerLine} ejemplares por pedido.");
                }

                Book book = catalog.Books.FirstOrDefault(b => b.Id == group.Key)
                    ?? throw new PurchasingException("Uno de los libros no existe.");
                if (!book.IsReleased(today))
                {
                    throw new PurchasingException($"«{book.Title}» todavía no ha salido.");
                }

                Publisher publisher = catalog.GetPublisher(book.PublisherId);
                if (publisher.DistributorId != distributor.Id)
                {
                    throw new PurchasingException($"«{book.Title}» no lo distribuye {distributor.Name}.");
                }

                lines.Add(new OrderLine(book.Id, quantity, UnitCost(book, publisher)));
            }

            int units = lines.Sum(line => line.Quantity);
            if (units > MaxUnitsPerOrder)
            {
                throw new PurchasingException($"Un pedido admite como mucho {MaxUnitsPerOrder} ejemplares.");
            }

            Money total = lines.Aggregate(Money.Zero, (sum, line) => sum + line.Total);
            if (total < distributor.MinimumOrder)
            {
                throw new PurchasingException($"{distributor.Name} pide un mínimo de {distributor.MinimumOrder.ToEuros():0} € por pedido.");
            }

            return lines;
        }

        /// <summary>Crea los lotes de un pedido recibido. Las novedades se pueden devolver dentro del plazo de la distribuidora.</summary>
        public static IEnumerable<StockLot> LotsFor(PurchaseOrder order, BookCatalog catalog, GameDate receivedDate)
        {
            Distributor distributor = catalog.GetDistributor(order.DistributorId);
            foreach (OrderLine line in order.Lines)
            {
                Book book = catalog.GetBook(line.BookId);
                GameDate? deadline = book.PhaseOn(receivedDate) == BookPhase.NewRelease && distributor.ReturnWindowDays > 0
                    ? receivedDate.AddDays(distributor.ReturnWindowDays)
                    : (GameDate?)null;
                yield return new StockLot(line.BookId, line.Quantity, line.UnitCost, receivedDate, deadline);
            }
        }

        /// <summary>
        /// Fondo con el que empieza la librería: los 150 títulos más populares, con más
        /// ejemplares cuanto más arriba. Ya es de la librería (no se paga ni se devuelve).
        /// </summary>
        public static IEnumerable<StockLot> InitialStock(BookCatalog catalog, GameDate date)
        {
            IReadOnlyList<Book> top = catalog.MostPopular(date, 150);
            for (int rank = 0; rank < top.Count; rank++)
            {
                Book book = top[rank];
                int quantity = rank < 15 ? 6 : rank < 50 ? 4 : 2;
                yield return new StockLot(book.Id, quantity, UnitCost(book, catalog.GetPublisher(book.PublisherId)), date, null);
            }
        }
    }
}
