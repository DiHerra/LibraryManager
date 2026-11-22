using System;
using System.Collections.Generic;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Common;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Purchasing;
using LibraryManager.Domain.Sales;
using LibraryManager.Domain.Stock;
using LibraryManager.Domain.Store;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.World
{
    /// <summary>Estado completo de una partida. Todo lo que se guarda cuelga de aquí.</summary>
    public sealed class WorldState
    {
        public static readonly GameDate DefaultStartDate = GameDate.Create(2026, 9, 1);
        public static readonly Money DefaultStartingCash = Money.FromEuros(30000m);
        public static readonly Money DefaultMonthlyRent = Money.FromEuros(1800m);
        public const string RentTitle = "Alquiler del local";

        /// <summary>Tope de seguridad para un avance de varios días.</summary>
        public const int MaxDaysPerAdvance = 366;

        public WorldState(
            Bookstore bookstore,
            GameDate date,
            DeterministicRandom random,
            StoreSchedule schedule,
            WorldEventQueue events,
            BookCatalog catalog,
            Inventory inventory,
            PurchaseOrderBook orders,
            StoreLayout layout,
            SalesState sales = null)
        {
            Bookstore = Guard.NotNull(bookstore, nameof(bookstore));
            Date = date;
            Random = Guard.NotNull(random, nameof(random));
            Schedule = Guard.NotNull(schedule, nameof(schedule));
            Events = Guard.NotNull(events, nameof(events));
            Catalog = Guard.NotNull(catalog, nameof(catalog));
            Inventory = Guard.NotNull(inventory, nameof(inventory));
            Orders = Guard.NotNull(orders, nameof(orders));
            Layout = Guard.NotNull(layout, nameof(layout));
            Sales = sales ?? new SalesState();
        }

        public Bookstore Bookstore { get; }

        public GameDate Date { get; private set; }

        public DeterministicRandom Random { get; }

        public StoreSchedule Schedule { get; }

        public WorldEventQueue Events { get; }

        /// <summary>Títulos, editoriales y distribuidoras del mundo.</summary>
        public BookCatalog Catalog { get; }

        /// <summary>Ejemplares en la tienda.</summary>
        public Inventory Inventory { get; }

        /// <summary>Pedidos a distribuidoras.</summary>
        public PurchaseOrderBook Orders { get; }

        /// <summary>Muebles de la tienda y libros expuestos.</summary>
        public StoreLayout Layout { get; }

        /// <summary>Reputación, tarjeta de lector, encargos y registro de ventas.</summary>
        public SalesState Sales { get; }

        public bool IsOpenToday => Schedule.IsOpen(Date);

        public CommercialSeason Season => CommercialCalendar.GetSeason(Date);

        public static WorldState CreateNew(string bookstoreName, ulong seed)
        {
            var events = new WorldEventQueue();
            ScheduleDefaultRent(events, DefaultStartDate);

            var random = new DeterministicRandom(seed);
            BookCatalog catalog = CatalogGenerator.Generate(random.NextULong(), DefaultStartDate);
            var inventory = new Inventory(PurchasingRules.InitialStock(catalog, DefaultStartDate));
            StoreLayout layout = DefaultStoreLayout.Create();
            DefaultStoreLayout.AutoArrange(layout, inventory, catalog, DefaultStartDate);

            return new WorldState(
                new Bookstore(bookstoreName, DefaultStartingCash),
                DefaultStartDate,
                random,
                StoreSchedule.CreateDefault(),
                events,
                catalog,
                inventory,
                new PurchaseOrderBook(),
                layout);
        }

        /// <summary>Programa el alquiler mensual el día 1 del mes siguiente a <paramref name="from"/>.</summary>
        public static void ScheduleDefaultRent(WorldEventQueue events, GameDate from)
        {
            DateTime nextMonth = from.ToDateTime().AddMonths(1);
            events.Schedule(
                GameDate.Create(nextMonth.Year, nextMonth.Month, 1),
                WorldEventKind.Payment,
                RentTitle,
                DefaultMonthlyRent,
                EventRecurrence.Monthly);
        }

        /// <summary>
        /// Cierra el día en curso (si la tienda abre, se simula su jornada de venta y se cobra),
        /// pasa al día siguiente y resuelve sus eventos.
        /// </summary>
        public DayReport AdvanceDay()
        {
            CommercialSeason previousSeason = Season;
            Money cashBefore = Bookstore.Cash;

            DailySales sales = null;
            if (IsOpenToday)
            {
                sales = SalesSimulator.SimulateDay(Date, Schedule, Catalog, Inventory, Layout, Sales, Random);
                Layout.Reconcile(Inventory, Catalog);
                Bookstore.ReceivePayment(sales.Revenue);
            }

            Date = Date.AddDays(1);

            IReadOnlyList<WorldEvent> due = Events.TakeDue(Date);
            var pauses = new List<PauseReason>();

            foreach (WorldEvent worldEvent in due)
            {
                if (worldEvent.Kind == WorldEventKind.Payment)
                {
                    Bookstore.Pay(worldEvent.Amount);
                }
                else if (worldEvent.Kind == WorldEventKind.OrderDelivery)
                {
                    ReceiveOrder((int)worldEvent.ReferenceId);
                }

                if (worldEvent.PausesAdvance)
                {
                    pauses.Add(new PauseReason(PauseReasonKind.EventNeedsAttention, worldEvent.Title));
                }
            }

            CommercialSeason season = Season;
            if (season != previousSeason && season != CommercialSeason.Normal)
            {
                pauses.Add(new PauseReason(PauseReasonKind.SeasonStarted, season.ToString()));
            }

            if (!cashBefore.IsNegative && Bookstore.Cash.IsNegative)
            {
                pauses.Add(new PauseReason(PauseReasonKind.CashBecameNegative, string.Empty));
            }

            return new DayReport(Date, IsOpenToday, season, due, cashBefore, Bookstore.Cash, pauses, sales);
        }

        /// <summary>
        /// Hace un pedido a una distribuidora. Llega el primer día de apertura tras el plazo
        /// de entrega, como un evento que pausa el avance, y se paga al recibirlo.
        /// </summary>
        public PurchaseOrder PlaceOrder(int distributorId, IEnumerable<OrderRequestLine> request)
        {
            List<OrderLine> lines = PurchasingRules.BuildLines(Catalog, distributorId, Date, request);
            Distributor distributor = Catalog.GetDistributor(distributorId);
            GameDate delivery = PurchasingRules.DeliveryDate(Date, distributor, Schedule);

            var order = new PurchaseOrder(Orders.ReserveId(), distributorId, lines, Date, delivery, PurchaseOrderStatus.Pending);
            Orders.Add(order);
            Events.Schedule(
                delivery,
                WorldEventKind.OrderDelivery,
                $"Llega el pedido n.º {order.Id} de {distributor.Name}",
                order.Total,
                pausesAdvance: true,
                referenceId: order.Id);
            return order;
        }

        /// <summary>Devuelve a la distribuidora los ejemplares de novedad aún en plazo y recupera su coste.</summary>
        public (int Quantity, Money Refund) ReturnNewReleases(int bookId)
        {
            (int quantity, Money cost) = Inventory.RemoveReturnable(bookId, Date);
            if (quantity == 0)
            {
                throw new PurchasingException("No hay ejemplares de ese libro que se puedan devolver.");
            }

            Layout.Reconcile(Inventory, Catalog);
            Bookstore.ReceivePayment(cost);
            return (quantity, cost);
        }

        /// <summary>Vende ejemplares a un saldista por el 10 % del PVP (los más antiguos primero).</summary>
        public Money Liquidate(int bookId, int quantity)
        {
            if (quantity <= 0 || Inventory.Quantity(bookId) < quantity)
            {
                throw new PurchasingException("No hay tantos ejemplares de ese libro.");
            }

            Inventory.RemoveOldestFirst(bookId, quantity);
            Layout.Reconcile(Inventory, Catalog);
            Money income = PurchasingRules.LiquidationValue(Catalog.GetBook(bookId)) * quantity;
            Bookstore.ReceivePayment(income);
            return income;
        }

        private void ReceiveOrder(int orderId)
        {
            PurchaseOrder order = Orders.Get(orderId);
            if (order == null || order.Status != PurchaseOrderStatus.Pending)
            {
                return;
            }

            foreach (StockLot lot in PurchasingRules.LotsFor(order, Catalog, Date))
            {
                Inventory.Add(lot);
                DefaultStoreLayout.ShelveInSection(Layout, Inventory, Catalog.GetBook(lot.BookId));
            }

            order.MarkReceived();
            Bookstore.Pay(order.Total);
        }

        /// <summary>Fija el descuento de la tarjeta de lector (0–500 puntos básicos, máximo legal del 5 %).</summary>
        public void SetLoyaltyDiscount(int basisPoints)
        {
            Sales.SetLoyaltyDiscount(basisPoints);
        }

        /// <summary>Recoloca toda la tienda con la colocación automática.</summary>
        public void AutoArrangeStore()
        {
            DefaultStoreLayout.AutoArrange(Layout, Inventory, Catalog, Date);
        }

        /// <summary>
        /// Avanza hasta <paramref name="days"/> días y se detiene antes si un día trae
        /// un motivo de pausa. Siempre avanza al menos un día.
        /// </summary>
        public AdvanceResult AdvanceDays(int days)
        {
            Guard.InRange(days, 1, MaxDaysPerAdvance, nameof(days));

            var reports = new List<DayReport>();
            for (int i = 0; i < days; i++)
            {
                DayReport report = AdvanceDay();
                reports.Add(report);
                if (report.ShouldPause)
                {
                    return new AdvanceResult(reports) { StoppedEarly = i < days - 1 };
                }
            }

            return new AdvanceResult(reports);
        }

        /// <summary>Avanza hasta el próximo lunes (fin de la semana en curso), con las mismas pausas.</summary>
        public AdvanceResult AdvanceToNextWeek()
        {
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)Date.DayOfWeek + 7) % 7;
            return AdvanceDays(daysUntilMonday == 0 ? 7 : daysUntilMonday);
        }
    }
}
