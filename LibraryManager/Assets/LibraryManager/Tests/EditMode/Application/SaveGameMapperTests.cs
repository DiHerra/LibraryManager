using System.Linq;
using LibraryManager.Application.Saves;
using LibraryManager.Domain.World;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Application
{
    public sealed class SaveGameMapperTests
    {
        [Test]
        public void RoundTrip_KeepsTheWholeWorld()
        {
            WorldState world = WorldState.CreateNew("Libros del Faro", 1234);
            world.AdvanceDay();
            world.Bookstore.Pay(LibraryManager.Domain.Economy.Money.FromEuros(99.95m));
            world.Random.NextULong();

            WorldState loaded = SaveGameMapper.ToWorld(SaveGameMapper.ToData(world, new FixedClock().UtcNow));

            Assert.AreEqual(world.Bookstore.Name, loaded.Bookstore.Name);
            Assert.AreEqual(world.Bookstore.Cash, loaded.Bookstore.Cash);
            Assert.AreEqual(world.Date, loaded.Date);
            Assert.AreEqual(world.Random.NextULong(), loaded.Random.NextULong());
            Assert.AreEqual(world.Events.NextId, loaded.Events.NextId);
            Assert.AreEqual(world.Events.Pending.Count, loaded.Events.Pending.Count);
            Assert.AreEqual(world.Schedule.OpenDays, loaded.Schedule.OpenDays);
            Assert.AreEqual(world.Catalog.Books.Count, loaded.Catalog.Books.Count);
        }

        [Test]
        public void RoundTrip_KeepsTheCatalog()
        {
            WorldState world = WorldState.CreateNew("Libros", 31);
            WorldState loaded = SaveGameMapper.ToWorld(SaveGameMapper.ToData(world, new FixedClock().UtcNow));

            for (int i = 0; i < world.Catalog.Books.Count; i += 37)
            {
                var original = world.Catalog.Books[i];
                var copy = loaded.Catalog.GetBook(original.Id);
                Assert.AreEqual(original.Title, copy.Title);
                Assert.AreEqual(original.Author, copy.Author);
                Assert.AreEqual(original.Price, copy.Price);
                Assert.AreEqual(original.PublicationDate, copy.PublicationDate);
                Assert.AreEqual(original.PopularityOn(world.Date), copy.PopularityOn(world.Date));
            }

            var publisher = world.Catalog.Publishers[3];
            Assert.AreEqual(publisher.FocusGenres, loaded.Catalog.GetPublisher(publisher.Id).FocusGenres);
            Assert.AreEqual(world.Catalog.Distributors[0].LeadTimeDays, loaded.Catalog.Distributors[0].LeadTimeDays);
        }

        [Test]
        public void VersionTwo_IsMigratedWithAGeneratedCatalog()
        {
            SaveGameData data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 5), new FixedClock().UtcNow);
            data.schemaVersion = 2;
            data.world.catalog = null;
            string randomState = data.world.randomState;

            WorldState world = SaveGameMapper.ToWorld(data);

            Assert.AreEqual(SaveGameSchema.CurrentVersion, data.schemaVersion);
            Assert.That(world.Catalog.Books.Count, Is.GreaterThanOrEqualTo(600));
            Assert.AreEqual(randomState, world.Random.State.ToString(System.Globalization.CultureInfo.InvariantCulture), "La migración no consume el generador de la partida.");
        }

        [Test]
        public void RoundTrip_KeepsStockOrdersAndDeliveries()
        {
            WorldState world = WorldState.CreateNew("Libros", 12);
            var book = world.Catalog.Books.First(b => b.PhaseOn(world.Date) == LibraryManager.Domain.Catalog.BookPhase.NewRelease);
            var distributor = world.Catalog.DistributorOf(book);
            world.PlaceOrder(distributor.Id, new[] { new LibraryManager.Domain.Purchasing.OrderRequestLine(book.Id, 100) });
            world.Liquidate(world.Inventory.Lots[0].BookId, 1);

            WorldState loaded = SaveGameMapper.ToWorld(SaveGameMapper.ToData(world, new FixedClock().UtcNow));

            Assert.AreEqual(world.Inventory.TotalUnits, loaded.Inventory.TotalUnits);
            Assert.AreEqual(world.Inventory.ValueAtCost, loaded.Inventory.ValueAtCost);
            Assert.AreEqual(world.Orders.NextId, loaded.Orders.NextId);
            Assert.AreEqual(100, loaded.Orders.PendingQuantity(book.Id));

            // El pedido guardado llega igual en la partida cargada.
            loaded.AdvanceDays(30);
            world.AdvanceDays(30);
            Assert.AreEqual(world.Inventory.Quantity(book.Id), loaded.Inventory.Quantity(book.Id));
            Assert.AreEqual(world.Inventory.ReturnableQuantity(book.Id, world.Date), loaded.Inventory.ReturnableQuantity(book.Id, loaded.Date));
            Assert.AreEqual(world.Bookstore.Cash, loaded.Bookstore.Cash);
        }

        [Test]
        public void VersionThree_IsMigratedWithTheInitialStock()
        {
            SaveGameData data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 5), new FixedClock().UtcNow);
            data.schemaVersion = 3;
            data.world.stock = null;
            data.world.orders = null;

            WorldState world = SaveGameMapper.ToWorld(data);

            Assert.AreEqual(SaveGameSchema.CurrentVersion, data.schemaVersion);
            Assert.AreEqual(150, world.Inventory.TitleCount);
            Assert.AreEqual(0, world.Orders.All.Count);
        }

        [Test]
        public void RoundTrip_KeepsTheStoreLayout()
        {
            WorldState world = WorldState.CreateNew("Libros", 21);
            var window = world.Layout.Fixtures.First(f => f.Kind == LibraryManager.Domain.Store.FixtureKind.ShopWindow);
            world.Layout.ClearFixture(window.Id);

            WorldState loaded = SaveGameMapper.ToWorld(SaveGameMapper.ToData(world, new FixedClock().UtcNow));

            CollectionAssert.AreEqual(
                world.Layout.Placements.Select(p => $"{p.FixtureId}:{p.BookId}:{p.Quantity}").ToList(),
                loaded.Layout.Placements.Select(p => $"{p.FixtureId}:{p.BookId}:{p.Quantity}").ToList());
            Assert.AreEqual(world.Layout.Fixtures.Count, loaded.Layout.Fixtures.Count);
            Assert.AreEqual(world.Layout.Fixtures.Last().Section, loaded.Layout.Fixtures.Last().Section);
        }

        [Test]
        public void VersionFour_IsMigratedWithTheDefaultLayoutArranged()
        {
            SaveGameData data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 5), new FixedClock().UtcNow);
            data.schemaVersion = 4;
            data.world.fixtures = null;
            data.world.placements = null;

            WorldState world = SaveGameMapper.ToWorld(data);

            Assert.AreEqual(SaveGameSchema.CurrentVersion, data.schemaVersion);
            Assert.AreEqual(18, world.Layout.Fixtures.Count);
            Assert.That(world.Layout.Placements.Sum(p => p.Quantity), Is.GreaterThan(0));
        }

        [Test]
        public void RoundTrip_KeepsSalesSpecialOrdersAndReputation()
        {
            WorldState world = WorldState.CreateNew("Libros", 44);
            world.SetLoyaltyDiscount(300);
            world.AdvanceDays(4);
            var book = world.Catalog.Books.First(b => b.IsReleased(world.Date) && world.Inventory.Quantity(b.Id) == 0);
            world.Sales.AddSpecialOrder(book.Id, world.Date);

            WorldState loaded = SaveGameMapper.ToWorld(SaveGameMapper.ToData(world, new FixedClock().UtcNow));

            Assert.AreEqual(world.Sales.Reputation, loaded.Sales.Reputation);
            Assert.AreEqual(300, loaded.Sales.LoyaltyDiscountBasisPoints);
            Assert.AreEqual(world.Sales.NextSpecialOrderId, loaded.Sales.NextSpecialOrderId);
            CollectionAssert.AreEqual(
                world.Sales.SpecialOrders.Select(o => $"{o.Id}:{o.BookId}:{o.RequestedOn.DayNumber}:{o.ExpiresOn.DayNumber}").ToList(),
                loaded.Sales.SpecialOrders.Select(o => $"{o.Id}:{o.BookId}:{o.RequestedOn.DayNumber}:{o.ExpiresOn.DayNumber}").ToList());
            Assert.AreEqual(world.Sales.Ledger.Days.Count, loaded.Sales.Ledger.Days.Count);
            var original = world.Sales.Ledger.Latest;
            var copy = loaded.Sales.Ledger.Latest;
            Assert.AreEqual(original.Date, copy.Date);
            Assert.AreEqual(original.Units, copy.Units);
            Assert.AreEqual(original.Revenue, copy.Revenue);
            Assert.AreEqual(original.Cost, copy.Cost);
            Assert.AreEqual(original.SatisfactionPercent, copy.SatisfactionPercent);
            CollectionAssert.AreEqual(original.UnitsByHour, copy.UnitsByHour);
            CollectionAssert.AreEqual(original.UnitsByGenre, copy.UnitsByGenre);
            CollectionAssert.AreEqual(
                world.Sales.Ledger.Books.Select(t => $"{t.BookId}:{t.Units}:{t.Revenue.Cents}:{t.LastSoldOn.DayNumber}").ToList(),
                loaded.Sales.Ledger.Books.Select(t => $"{t.BookId}:{t.Units}:{t.Revenue.Cents}:{t.LastSoldOn.DayNumber}").ToList());

            // La partida cargada sigue vendiendo exactamente igual.
            world.AdvanceDays(10);
            loaded.AdvanceDays(10);
            Assert.AreEqual(world.Bookstore.Cash, loaded.Bookstore.Cash);
            Assert.AreEqual(world.Sales.Reputation, loaded.Sales.Reputation);
            Assert.AreEqual(world.Inventory.TotalUnits, loaded.Inventory.TotalUnits);
        }

        [Test]
        public void VersionFive_IsMigratedWithDefaultSales()
        {
            SaveGameData data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 5), new FixedClock().UtcNow);
            data.schemaVersion = 5;
            data.world.sales = null;

            WorldState world = SaveGameMapper.ToWorld(data);

            Assert.AreEqual(SaveGameSchema.CurrentVersion, data.schemaVersion);
            Assert.AreEqual(LibraryManager.Domain.Sales.SalesState.DefaultReputation, world.Sales.Reputation);
            Assert.AreEqual(0, world.Sales.LoyaltyDiscountBasisPoints);
            Assert.IsEmpty(world.Sales.SpecialOrders);
            Assert.IsEmpty(world.Sales.Ledger.Days);
        }

        [Test]
        public void InvalidSales_AreRejected()
        {
            SaveGameData data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 5), new FixedClock().UtcNow);
            data.world.sales.loyaltyDiscountBasisPoints = 900;
            Assert.Throws<SaveGameException>(() => SaveGameMapper.ToWorld(data));

            data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 5), new FixedClock().UtcNow);
            data.world.sales.specialOrders.Add(new SpecialOrderData { id = 1, bookId = 99999, requestedDayNumber = 1, expiresDayNumber = 31 });
            Assert.Throws<SaveGameException>(() => SaveGameMapper.ToWorld(data));

            data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 5), new FixedClock().UtcNow);
            data.world.sales = null;
            Assert.Throws<SaveGameException>(() => SaveGameMapper.ToWorld(data));
        }

        [Test]
        public void MoreDisplayedThanInStock_IsRejected()
        {
            SaveGameData data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 5), new FixedClock().UtcNow);
            data.world.placements[0].quantity += 1000;
            Assert.Throws<SaveGameException>(() => SaveGameMapper.ToWorld(data));
        }

        [Test]
        public void CorruptCatalog_IsRejected()
        {
            SaveGameData data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 5), new FixedClock().UtcNow);
            data.world.catalog.books[0].publisherId = 999;
            Assert.Throws<SaveGameException>(() => SaveGameMapper.ToWorld(data));

            data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 5), new FixedClock().UtcNow);
            data.world.catalog.books[0].genre = 99;
            Assert.Throws<SaveGameException>(() => SaveGameMapper.ToWorld(data));

            data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 5), new FixedClock().UtcNow);
            data.world.stock[0].bookId = 99999;
            Assert.Throws<SaveGameException>(() => SaveGameMapper.ToWorld(data));
        }

        [Test]
        public void RoundTrip_KeepsEventDetails()
        {
            WorldState world = WorldState.CreateNew("Libros", 1);
            world.Events.Schedule(
                LibraryManager.Domain.Time.GameDate.Create(2027, 1, 31),
                LibraryManager.Domain.Time.WorldEventKind.Payment,
                "Cuota",
                LibraryManager.Domain.Economy.Money.FromEuros(12.5m),
                LibraryManager.Domain.Time.EventRecurrence.Monthly,
                pausesAdvance: true);

            WorldState loaded = SaveGameMapper.ToWorld(SaveGameMapper.ToData(world, new FixedClock().UtcNow));

            var original = world.Events.Pending[1];
            var copy = loaded.Events.Pending[1];
            Assert.AreEqual(original.Id, copy.Id);
            Assert.AreEqual(original.Date, copy.Date);
            Assert.AreEqual(original.Kind, copy.Kind);
            Assert.AreEqual(original.Title, copy.Title);
            Assert.AreEqual(original.Amount, copy.Amount);
            Assert.AreEqual(original.Recurrence, copy.Recurrence);
            Assert.AreEqual(original.PausesAdvance, copy.PausesAdvance);
            Assert.AreEqual(original.AnchorDay, copy.AnchorDay);
        }

        [Test]
        public void VersionOne_IsMigratedWithDefaultScheduleAndRent()
        {
            var data = new SaveGameData
            {
                schemaVersion = 1,
                savedAtUtc = "2026-11-19T18:00:00.0000000Z",
                world = new WorldData
                {
                    dateDayNumber = LibraryManager.Domain.Time.GameDate.Create(2026, 9, 10).DayNumber,
                    randomState = "123",
                    bookstore = new BookstoreData { name = "Antigua", cashCents = 500000 },
                    schedule = null,
                    events = null
                }
            };

            WorldState world = SaveGameMapper.ToWorld(data);

            Assert.AreEqual(SaveGameSchema.CurrentVersion, data.schemaVersion);
            Assert.AreEqual(6, world.Schedule.OpenDays.Count);
            Assert.AreEqual(LibraryManager.Domain.Time.GameDate.Create(2026, 10, 1), world.Events.Pending[0].Date);
            Assert.AreEqual(WorldState.RentTitle, world.Events.Pending[0].Title);
            Assert.AreEqual(500000, world.Bookstore.Cash.Cents);
        }

        [Test]
        public void ToData_WritesCurrentVersion()
        {
            SaveGameData data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 1), new FixedClock().UtcNow);

            Assert.AreEqual(SaveGameSchema.CurrentVersion, data.schemaVersion);
            Assert.AreEqual("2026-11-19T18:00:00.0000000Z", data.savedAtUtc);
        }

        [Test]
        public void FutureVersion_IsRejected()
        {
            SaveGameData data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 1), new FixedClock().UtcNow);
            data.schemaVersion = SaveGameSchema.CurrentVersion + 1;

            Assert.Throws<SaveGameException>(() => SaveGameMapper.ToWorld(data));
        }

        [Test]
        public void InvalidData_IsRejectedWithSaveGameException()
        {
            SaveGameData data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 1), new FixedClock().UtcNow);
            data.world.bookstore.name = "";
            Assert.Throws<SaveGameException>(() => SaveGameMapper.ToWorld(data));

            data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 1), new FixedClock().UtcNow);
            data.world.randomState = "no es un número";
            Assert.Throws<SaveGameException>(() => SaveGameMapper.ToWorld(data));

            data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 1), new FixedClock().UtcNow);
            data.world.events[0].kind = 99;
            Assert.Throws<SaveGameException>(() => SaveGameMapper.ToWorld(data));

            data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 1), new FixedClock().UtcNow);
            data.world.schedule.openDays.Clear();
            Assert.Throws<SaveGameException>(() => SaveGameMapper.ToWorld(data));
        }
    }
}
