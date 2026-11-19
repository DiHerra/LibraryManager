using System;
using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.Purchasing
{
    public enum PurchaseOrderStatus
    {
        Pending = 0,
        Received = 1
    }

    public sealed class OrderLine
    {
        public OrderLine(int bookId, int quantity, Money unitCost)
        {
            BookId = bookId;
            Quantity = quantity > 0 ? quantity : throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad debe ser positiva.");
            UnitCost = unitCost.Cents >= 0 ? unitCost : throw new ArgumentOutOfRangeException(nameof(unitCost));
        }

        public int BookId { get; }
        public int Quantity { get; }
        public Money UnitCost { get; }
        public Money Total => UnitCost * Quantity;
    }

    /// <summary>Pedido a una distribuidora. Se paga al recibirlo.</summary>
    public sealed class PurchaseOrder
    {
        public PurchaseOrder(int id, int distributorId, IEnumerable<OrderLine> lines, GameDate orderDate, GameDate expectedDate, PurchaseOrderStatus status)
        {
            Id = id > 0 ? id : throw new ArgumentOutOfRangeException(nameof(id));
            DistributorId = distributorId;
            Lines = (lines ?? throw new ArgumentNullException(nameof(lines))).ToList();
            if (Lines.Count == 0)
            {
                throw new ArgumentException("El pedido no tiene líneas.", nameof(lines));
            }

            if (Lines.Select(line => line.BookId).Distinct().Count() != Lines.Count)
            {
                throw new ArgumentException("El pedido repite un título.", nameof(lines));
            }

            OrderDate = orderDate;
            ExpectedDate = expectedDate >= orderDate ? expectedDate : throw new ArgumentOutOfRangeException(nameof(expectedDate));
            Status = status;
        }

        public int Id { get; }
        public int DistributorId { get; }
        public IReadOnlyList<OrderLine> Lines { get; }
        public GameDate OrderDate { get; }
        public GameDate ExpectedDate { get; }
        public PurchaseOrderStatus Status { get; private set; }

        public int TotalUnits => Lines.Sum(line => line.Quantity);
        public Money Total => Lines.Aggregate(Money.Zero, (total, line) => total + line.Total);

        internal void MarkReceived()
        {
            if (Status != PurchaseOrderStatus.Pending)
            {
                throw new InvalidOperationException($"El pedido {Id} ya se recibió.");
            }

            Status = PurchaseOrderStatus.Received;
        }
    }
}
