using System;
using System.Collections.Generic;
using System.Linq;

namespace LibraryManager.Domain.Purchasing
{
    /// <summary>Pedidos pendientes y el historial de recibidos.</summary>
    public sealed class PurchaseOrderBook
    {
        private readonly List<PurchaseOrder> _orders = new List<PurchaseOrder>();

        public PurchaseOrderBook()
        {
            NextId = 1;
        }

        public PurchaseOrderBook(IEnumerable<PurchaseOrder> orders, int nextId)
        {
            _orders.AddRange(orders ?? throw new ArgumentNullException(nameof(orders)));
            if (_orders.Select(o => o.Id).Distinct().Count() != _orders.Count)
            {
                throw new ArgumentException("Hay pedidos con el mismo número.", nameof(orders));
            }

            int minimum = _orders.Count == 0 ? 1 : _orders.Max(o => o.Id) + 1;
            NextId = nextId >= minimum ? nextId : throw new ArgumentOutOfRangeException(nameof(nextId));
        }

        public int NextId { get; private set; }

        public IReadOnlyList<PurchaseOrder> All => _orders;

        public IEnumerable<PurchaseOrder> Pending => _orders.Where(o => o.Status == PurchaseOrderStatus.Pending);

        public PurchaseOrder Get(int id) => _orders.FirstOrDefault(o => o.Id == id);

        public int PendingQuantity(int bookId) =>
            Pending.SelectMany(o => o.Lines).Where(line => line.BookId == bookId).Sum(line => line.Quantity);

        internal int ReserveId() => NextId++;

        internal void Add(PurchaseOrder order) => _orders.Add(order);
    }
}
