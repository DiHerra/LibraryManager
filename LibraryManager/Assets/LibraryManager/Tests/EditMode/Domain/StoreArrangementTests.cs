using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Purchasing;
using LibraryManager.Domain.Store;
using LibraryManager.Domain.World;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class StoreArrangementTests
    {
        [Test]
        public void NewWorld_ShowsTheBestsellersInTheWindowAndNoveltiesOnTheTable()
        {
            WorldState world = WorldState.CreateNew("Libros", 2026);
            StoreLayout layout = world.Layout;
            Fixture window = layout.Fixtures.Single(f => f.Kind == FixtureKind.ShopWindow);
            Fixture table = layout.Fixtures.Single(f => f.Kind == FixtureKind.NewReleaseTable);

            Book top = world.Catalog.MostPopular(world.Date, 1).Single();
            Assert.That(layout.DisplayedOn(window.Id, top.Id), Is.GreaterThan(0));
            Assert.AreEqual(window.Capacity, layout.UsedCapacity(window.Id));
            Assert.IsTrue(layout.PlacementsOn(table.Id).All(p => world.Catalog.GetBook(p.BookId).PhaseOn(world.Date) == BookPhase.NewRelease));

            foreach (int bookId in world.Inventory.BookIds)
            {
                Assert.That(layout.DisplayedQuantity(bookId), Is.LessThanOrEqualTo(world.Inventory.Quantity(bookId)));
            }
        }

        [Test]
        public void AutoArrange_IsDeterministic()
        {
            WorldState first = WorldState.CreateNew("Libros", 4);
            WorldState second = WorldState.CreateNew("Libros", 4);
            first.AutoArrangeStore();

            CollectionAssert.AreEqual(
                second.Layout.Placements.Select(p => $"{p.FixtureId}:{p.BookId}:{p.Quantity}").ToList(),
                first.Layout.Placements.Select(p => $"{p.FixtureId}:{p.BookId}:{p.Quantity}").ToList());
        }

        [Test]
        public void Liquidating_TrimsWhatIsDisplayed()
        {
            WorldState world = WorldState.CreateNew("Libros", 2026);
            Book top = world.Catalog.MostPopular(world.Date, 1).Single();

            world.Liquidate(top.Id, world.Inventory.Quantity(top.Id));

            Assert.AreEqual(0, world.Layout.DisplayedQuantity(top.Id));
        }

        [Test]
        public void ArrivingOrders_GoToTheirSectionShelf()
        {
            WorldState world = WorldState.CreateNew("Libros", 2026);
            Book book = world.Catalog.Books
                .Where(b => b.IsReleased(world.Date) && world.Inventory.Quantity(b.Id) == 0)
                .OrderBy(b => b.Id)
                .First();
            Fixture shelf = world.Layout.Fixtures.Single(f => f.Kind == FixtureKind.Shelf && f.Section == book.Genre);
            int free = world.Layout.FreeCapacity(shelf.Id);
            world.PlaceOrder(world.Catalog.DistributorOf(book).Id, new[] { new OrderRequestLine(book.Id, 100) });

            world.AdvanceDays(30);

            Assert.AreEqual(System.Math.Min(100, free), world.Layout.DisplayedOn(shelf.Id, book.Id));
        }
    }
}
