using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Purchasing;
using LibraryManager.Domain.Stock;
using LibraryManager.Domain.Time;
using LibraryManager.Domain.World;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class PurchasingTests
    {
        private WorldState _world;

        [SetUp]
        public void SetUp()
        {
            _world = WorldState.CreateNew("Libros", 2026);
        }

        private Distributor Distributor => _world.Catalog.Distributors[0];

        private Book[] ReleasedBooksOf(Distributor distributor) =>
            _world.Catalog.Books
                .Where(b => b.IsReleased(_world.Date) && _world.Catalog.DistributorOf(b).Id == distributor.Id)
                .OrderBy(b => b.Id)
                .ToArray();

        private OrderRequestLine[] EnoughFor(Distributor distributor, int titles = 3, int quantity = 10) =>
            ReleasedBooksOf(distributor).Take(titles).Select(b => new OrderRequestLine(b.Id, quantity)).ToArray();

        [Test]
        public void NewWorld_StartsWithTheInitialStock()
        {
            Assert.AreEqual(150, _world.Inventory.TitleCount);
            Assert.AreEqual(15 * 6 + 35 * 4 + 100 * 2, _world.Inventory.TotalUnits);
            Assert.IsTrue(_world.Inventory.Lots.All(lot => lot.ReturnDeadline == null));
        }

        [Test]
        public void UnitCost_IsThePriceMinusThePublisherDiscount()
        {
            Book book = _world.Catalog.Books[0];
            Publisher publisher = _world.Catalog.GetPublisher(book.PublisherId);

            Money cost = PurchasingRules.UnitCost(book, publisher);

            Assert.AreEqual(book.Price - book.Price.Percentage(publisher.TradeDiscountBasisPoints), cost);
            Assert.That(cost, Is.LessThan(book.Price));
        }

        [Test]
        public void PlaceOrder_SchedulesADeliveryThatPausesAndIsPaidOnArrival()
        {
            Money cashBefore = _world.Bookstore.Cash;
            PurchaseOrder order = _world.PlaceOrder(Distributor.Id, EnoughFor(Distributor));

            Assert.AreEqual(cashBefore, _world.Bookstore.Cash, "No se paga al pedir.");
            Assert.That(order.ExpectedDate, Is.GreaterThanOrEqualTo(_world.Date.AddDays(Distributor.LeadTimeDays)));
            Assert.IsTrue(_world.Schedule.IsOpen(order.ExpectedDate));

            AdvanceResult result = _world.AdvanceDays(30);

            Assert.AreEqual(order.ExpectedDate, _world.Date, "El avance se detiene el día que llega el pedido.");
            Assert.AreEqual(PurchaseOrderStatus.Received, order.Status);
            Assert.AreEqual(cashBefore - order.Total + result.Revenue, _world.Bookstore.Cash, "Se paga al recibir; el resto del cambio son las ventas.");
            Assert.IsTrue(result.StoppedEarly);
            foreach (OrderLine line in order.Lines)
            {
                Assert.That(_world.Inventory.Quantity(line.BookId), Is.GreaterThanOrEqualTo(line.Quantity));
            }
        }

        [Test]
        public void ReceivedNewReleases_CanBeReturnedWithinTheWindow()
        {
            Book novelty = _world.Catalog.Books
                // Salido hace poco, para que siga siendo novedad cuando llegue el pedido.
                .Where(b => b.PhaseOn(_world.Date) == BookPhase.NewRelease && b.PublicationDate.DaysUntil(_world.Date) <= 30)
                .OrderBy(b => b.Id)
                .First();
            Distributor distributor = _world.Catalog.DistributorOf(novelty);
            var request = new[] { new OrderRequestLine(novelty.Id, 100) };
            PurchaseOrder order = _world.PlaceOrder(distributor.Id, request);
            _world.AdvanceDays(30);
            int before = _world.Inventory.Quantity(novelty.Id);
            Money cashBefore = _world.Bookstore.Cash;

            (int quantity, Money refund) = _world.ReturnNewReleases(novelty.Id);

            Assert.AreEqual(100, quantity);
            Assert.AreEqual(order.Lines[0].Total, refund);
            Assert.AreEqual(cashBefore + refund, _world.Bookstore.Cash);
            Assert.AreEqual(before - 100, _world.Inventory.Quantity(novelty.Id));
            Assert.Throws<PurchasingException>(() => _world.ReturnNewReleases(novelty.Id));
        }

        [Test]
        public void Liquidate_PaysTenPercentOfThePrice()
        {
            StockLot lot = _world.Inventory.Lots[0];
            Book book = _world.Catalog.GetBook(lot.BookId);
            int before = _world.Inventory.Quantity(book.Id);
            Money cashBefore = _world.Bookstore.Cash;

            Money income = _world.Liquidate(book.Id, 1);

            Assert.AreEqual(book.Price.Percentage(1000), income);
            Assert.AreEqual(cashBefore + income, _world.Bookstore.Cash);
            Assert.AreEqual(before - 1, _world.Inventory.Quantity(book.Id));
            Assert.Throws<PurchasingException>(() => _world.Liquidate(book.Id, 1000));
        }

        [Test]
        public void InvalidOrders_AreRejected()
        {
            Book[] books = ReleasedBooksOf(Distributor);
            Book other = _world.Catalog.Books.First(b => b.IsReleased(_world.Date) && _world.Catalog.DistributorOf(b).Id != Distributor.Id);
            Book upcoming = _world.Catalog.Books.First(b => !b.IsReleased(_world.Date));

            Assert.Throws<PurchasingException>(() => _world.PlaceOrder(Distributor.Id, new OrderRequestLine[0]));
            Assert.Throws<PurchasingException>(() => _world.PlaceOrder(Distributor.Id, new[] { new OrderRequestLine(books[0].Id, 1) }), "Por debajo del mínimo.");
            Assert.Throws<PurchasingException>(() => _world.PlaceOrder(Distributor.Id, new[] { new OrderRequestLine(other.Id, 50) }), "De otra distribuidora.");
            Assert.Throws<PurchasingException>(() => _world.PlaceOrder(_world.Catalog.DistributorOf(upcoming).Id, new[] { new OrderRequestLine(upcoming.Id, 50) }), "Aún no ha salido.");
            Assert.Throws<PurchasingException>(() => _world.PlaceOrder(Distributor.Id, new[] { new OrderRequestLine(books[0].Id, 101) }), "Demasiados por título.");
            Assert.Throws<PurchasingException>(() => _world.PlaceOrder(99, EnoughFor(Distributor)));
            Assert.AreEqual(0, _world.Orders.All.Count);
        }

        [Test]
        public void DeliveryDate_SkipsDaysTheStoreIsClosed()
        {
            var distributor = new Distributor(1, "Cauce", 4, Money.Zero, 90);
            // Miércoles 2 + 4 días = domingo 6 → lunes 7.
            Assert.AreEqual(GameDate.Create(2026, 9, 7), PurchasingRules.DeliveryDate(GameDate.Create(2026, 9, 2), distributor, StoreSchedule.CreateDefault()));
        }
    }
}
