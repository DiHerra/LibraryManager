using System;
using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Stock;
using LibraryManager.Domain.Store;
using LibraryManager.Domain.Time;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class StoreLayoutTests
    {
        private static readonly GameDate Today = GameDate.Create(2026, 9, 1);

        private static StoreLayout SmallLayout() => new StoreLayout(new[]
        {
            new Fixture(1, FixtureKind.ShopWindow, "Escaparate", 5, null, 0, 0, 2, 1),
            new Fixture(2, FixtureKind.Shelf, "Estantería", 10, Genre.Crime, 0, 2, 2, 1)
        });

        private static Inventory InventoryWith(int bookId, int quantity)
        {
            var inventory = new Inventory();
            inventory.Add(new StockLot(bookId, quantity, Money.FromEuros(10m), Today, null));
            return inventory;
        }

        private static Book CreateBook(int id, Genre genre) =>
            new Book(id, "Libro " + id, "Autor", 1, genre, BookFormat.Paperback, Money.FromEuros(18.95m), Today, 50, 60, 20);

        [Test]
        public void Place_RespectsCapacityAndBackroomStock()
        {
            StoreLayout layout = SmallLayout();
            Inventory inventory = InventoryWith(7, 8);

            layout.Place(1, 7, 5, inventory);

            Assert.AreEqual(0, layout.FreeCapacity(1));
            Assert.Throws<StoreLayoutException>(() => layout.Place(1, 7, 1, inventory), "Escaparate lleno.");
            Assert.Throws<StoreLayoutException>(() => layout.Place(2, 7, 4, inventory), "Solo quedan 3 en el almacén.");
            layout.Place(2, 7, 3, inventory);
            Assert.AreEqual(8, layout.DisplayedQuantity(7));
        }

        [Test]
        public void Remove_SendsCopiesBackToTheBackroom()
        {
            StoreLayout layout = SmallLayout();
            Inventory inventory = InventoryWith(7, 4);
            layout.Place(2, 7, 4, inventory);

            layout.Remove(2, 7, 3);

            Assert.AreEqual(1, layout.DisplayedOn(2, 7));
            Assert.Throws<StoreLayoutException>(() => layout.Remove(2, 7, 2));
            layout.Remove(2, 7, 1);
            Assert.IsEmpty(layout.Placements);
        }

        [Test]
        public void Visibility_DependsOnTheBestFixture()
        {
            StoreLayout layout = SmallLayout();
            Book crime = CreateBook(7, Genre.Crime);
            Book poetry = CreateBook(8, Genre.Poetry);
            var inventory = new Inventory();
            inventory.Add(new StockLot(7, 4, Money.FromEuros(10m), Today, null));
            inventory.Add(new StockLot(8, 4, Money.FromEuros(10m), Today, null));

            Assert.AreEqual(StoreLayout.BackroomVisibility, layout.VisibilityOf(crime, inventory));
            Assert.AreEqual(0, layout.VisibilityOf(CreateBook(9, Genre.Crime), inventory), "Sin ejemplares.");

            layout.Place(2, 7, 2, inventory);
            layout.Place(2, 8, 2, inventory);
            Assert.AreEqual(StoreLayout.ShelfVisibility, layout.VisibilityOf(crime, inventory));
            Assert.AreEqual(StoreLayout.WrongSectionVisibility, layout.VisibilityOf(poetry, inventory));

            layout.Place(1, 7, 1, inventory);
            Assert.AreEqual(StoreLayout.ShopWindowVisibility, layout.VisibilityOf(crime, inventory));
        }

        [Test]
        public void Reconcile_RemovesFromTheLeastVisibleFixtureFirst()
        {
            StoreLayout layout = SmallLayout();
            Inventory inventory = InventoryWith(7, 6);
            var catalog = new BookCatalog(
                new[] { new Distributor(1, "Cauce", 3, Money.Zero, 90) },
                new[] { new Publisher(1, "Trébol", PublisherSize.Small, new[] { Genre.Crime }, 1, 3500) },
                new[] { CreateBook(7, Genre.Crime) });
            layout.Place(1, 7, 2, inventory);
            layout.Place(2, 7, 4, inventory);

            inventory.RemoveOldestFirst(7, 5);
            layout.Reconcile(inventory, catalog);

            Assert.AreEqual(1, layout.DisplayedOn(1, 7), "El escaparate conserva lo que puede.");
            Assert.AreEqual(0, layout.DisplayedOn(2, 7));
        }

        [Test]
        public void InvalidLayouts_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new StoreLayout(new Fixture[0]));
            Assert.Throws<ArgumentException>(() => new Fixture(1, FixtureKind.Shelf, "Sin sección", 10, null, 0, 0, 1, 1));
            Assert.Throws<ArgumentException>(() => new StoreLayout(SmallLayout().Fixtures, new[] { new Placement(1, 7, 6) }), "Supera la capacidad.");
        }

        [Test]
        public void DefaultLayout_HasAShelfPerGenreInsideTheGrid()
        {
            StoreLayout layout = DefaultStoreLayout.Create();

            foreach (Genre genre in Enum.GetValues(typeof(Genre)))
            {
                Assert.AreEqual(1, layout.Fixtures.Count(f => f.Kind == FixtureKind.Shelf && f.Section == genre), genre.ToString());
            }

            foreach (Fixture fixture in layout.Fixtures)
            {
                Assert.That(fixture.X + fixture.Width, Is.LessThanOrEqualTo(DefaultStoreLayout.GridWidth), fixture.Name);
                Assert.That(fixture.Y + fixture.Height, Is.LessThanOrEqualTo(DefaultStoreLayout.GridHeight), fixture.Name);
            }

            // Ningún mueble se solapa con otro.
            var fixtures = layout.Fixtures.ToList();
            for (int i = 0; i < fixtures.Count; i++)
            {
                for (int j = i + 1; j < fixtures.Count; j++)
                {
                    Fixture a = fixtures[i];
                    Fixture b = fixtures[j];
                    bool overlap = a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
                    Assert.IsFalse(overlap, $"{a.Id} y {b.Id} se solapan.");
                }
            }
        }
    }
}
