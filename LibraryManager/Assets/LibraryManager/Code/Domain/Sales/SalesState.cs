using System;
using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Common;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.Sales
{
    /// <summary>
    /// Relación de la librería con sus lectores: reputación, descuento de la tarjeta,
    /// encargos pendientes y registro de ventas.
    /// </summary>
    public sealed class SalesState
    {
        /// <summary>Reputación en centésimas (0–10000 equivale a 0–100).</summary>
        public const int MaxReputation = 10000;
        public const int DefaultReputation = 5000;

        /// <summary>Cada jornada la reputación recorre un 2 % de la distancia hacia la satisfacción del día.</summary>
        public const double ReputationInertia = 0.02;

        private readonly List<SpecialOrder> _specialOrders = new List<SpecialOrder>();

        public SalesState()
            : this(DefaultReputation, 0, new SpecialOrder[0], 1, new SalesLedger())
        {
        }

        public SalesState(int reputation, int loyaltyDiscountBasisPoints, IEnumerable<SpecialOrder> specialOrders, int nextSpecialOrderId, SalesLedger ledger)
        {
            Reputation = Guard.InRange(reputation, 0, MaxReputation, nameof(reputation));
            LoyaltyDiscountBasisPoints = Guard.InRange(loyaltyDiscountBasisPoints, 0, SalesRules.MaxLoyaltyDiscountBasisPoints, nameof(loyaltyDiscountBasisPoints));
            Ledger = Guard.NotNull(ledger, nameof(ledger));
            foreach (SpecialOrder order in specialOrders ?? throw new ArgumentNullException(nameof(specialOrders)))
            {
                if (order == null || _specialOrders.Any(existing => existing.Id == order.Id))
                {
                    throw new ArgumentException("Hay un encargo vacío o repetido.");
                }

                _specialOrders.Add(order);
            }

            int minimumNext = _specialOrders.Count == 0 ? 1 : _specialOrders.Max(order => order.Id) + 1;
            NextSpecialOrderId = Math.Max(minimumNext, nextSpecialOrderId);
        }

        /// <summary>Reputación del barrio en centésimas (5000 = 50).</summary>
        public int Reputation { get; private set; }

        /// <summary>Reputación de 0 a 100.</summary>
        public double ReputationScore => Reputation / 100d;

        /// <summary>Descuento de la tarjeta de lector en puntos básicos (máximo 500, el 5 % legal).</summary>
        public int LoyaltyDiscountBasisPoints { get; private set; }

        /// <summary>Encargos pendientes, del más antiguo al más reciente.</summary>
        public IReadOnlyList<SpecialOrder> SpecialOrders => _specialOrders;

        public int NextSpecialOrderId { get; private set; }

        public SalesLedger Ledger { get; }

        public int PendingSpecialOrders(int bookId) => _specialOrders.Count(order => order.BookId == bookId);

        public void SetLoyaltyDiscount(int basisPoints)
        {
            LoyaltyDiscountBasisPoints = Guard.InRange(basisPoints, 0, SalesRules.MaxLoyaltyDiscountBasisPoints, nameof(basisPoints));
        }

        public SpecialOrder AddSpecialOrder(int bookId, GameDate date)
        {
            var order = new SpecialOrder(NextSpecialOrderId++, bookId, date, date.AddDays(SpecialOrder.PatienceDays));
            _specialOrders.Add(order);
            return order;
        }

        public void RemoveSpecialOrder(SpecialOrder order)
        {
            _specialOrders.Remove(order);
        }

        /// <summary>Acerca la reputación a la satisfacción del día (0–100).</summary>
        public void ApplySatisfaction(double satisfactionPercent)
        {
            double target = Math.Max(0d, Math.Min(100d, satisfactionPercent)) * 100d;
            int step = (int)Math.Round((target - Reputation) * ReputationInertia, MidpointRounding.AwayFromZero);
            Reputation = Math.Max(0, Math.Min(MaxReputation, Reputation + step));
        }
    }
}
