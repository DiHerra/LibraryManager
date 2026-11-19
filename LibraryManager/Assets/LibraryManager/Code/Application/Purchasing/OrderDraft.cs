using System;
using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Purchasing;

namespace LibraryManager.Application.Purchasing
{
    /// <summary>Borrador de un pedido a una distribuidora, con sus totales a precio de coste.</summary>
    public sealed class DraftGroup
    {
        public DraftGroup(Distributor distributor, IReadOnlyList<(Book Book, int Quantity, Money UnitCost)> lines)
        {
            Distributor = distributor;
            Lines = lines;
        }

        public Distributor Distributor { get; }
        public IReadOnlyList<(Book Book, int Quantity, Money UnitCost)> Lines { get; }
        public int Units => Lines.Sum(line => line.Quantity);
        public Money Total => Lines.Aggregate(Money.Zero, (total, line) => total + line.UnitCost * line.Quantity);
        public bool MeetsMinimum => Total >= Distributor.MinimumOrder;

        public IEnumerable<OrderRequestLine> ToRequest() => Lines.Select(line => new OrderRequestLine(line.Book.Id, line.Quantity));
    }

    /// <summary>
    /// Cesta de la compra del jugador mientras elige libros en el catálogo. No se guarda:
    /// solo existe hasta que se envían los pedidos.
    /// </summary>
    public sealed class OrderDraft
    {
        private readonly Dictionary<int, int> _quantities = new Dictionary<int, int>();

        public bool IsEmpty => _quantities.Count == 0;

        public int Quantity(int bookId) => _quantities.TryGetValue(bookId, out int quantity) ? quantity : 0;

        public void Add(int bookId, int quantity)
        {
            if (quantity == 0)
            {
                return;
            }

            int updated = Math.Max(0, Math.Min(PurchasingRules.MaxUnitsPerLine, Quantity(bookId) + quantity));
            if (updated == 0)
            {
                _quantities.Remove(bookId);
            }
            else
            {
                _quantities[bookId] = updated;
            }
        }

        public void Clear() => _quantities.Clear();

        public void ClearDistributor(BookCatalog catalog, int distributorId)
        {
            foreach (int bookId in _quantities.Keys.ToList())
            {
                if (catalog.DistributorOf(catalog.GetBook(bookId)).Id == distributorId)
                {
                    _quantities.Remove(bookId);
                }
            }
        }

        /// <summary>El borrador separado por distribuidora, porque cada una recibe su pedido.</summary>
        public IReadOnlyList<DraftGroup> ByDistributor(BookCatalog catalog)
        {
            return _quantities
                .Select(pair => catalog.GetBook(pair.Key))
                .GroupBy(book => catalog.DistributorOf(book).Id)
                .OrderBy(group => group.Key)
                .Select(group => new DraftGroup(
                    catalog.GetDistributor(group.Key),
                    group.OrderBy(book => book.Title, StringComparer.CurrentCulture)
                        .Select(book => (book, _quantities[book.Id], PurchasingRules.UnitCost(book, catalog.GetPublisher(book.PublisherId))))
                        .ToList()))
                .ToList();
        }
    }
}
