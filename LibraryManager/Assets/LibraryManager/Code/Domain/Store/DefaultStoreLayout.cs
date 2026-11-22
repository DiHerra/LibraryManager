using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Stock;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.Store
{
    /// <summary>
    /// Local inicial de 20 × 14 casillas (la entrada abajo): escaparate, mesa de novedades,
    /// expositor de caja y una estantería por género. Y colocación automática de existencias.
    /// </summary>
    public static class DefaultStoreLayout
    {
        public const int GridWidth = 20;
        public const int GridHeight = 14;
        public const int ShelfCapacity = 40;

        public static StoreLayout Create()
        {
            var fixtures = new List<Fixture>
            {
                new Fixture(1, FixtureKind.ShopWindow, "Escaparate", 12, null, 2, 13, 6, 1),
                new Fixture(2, FixtureKind.NewReleaseTable, "Mesa de novedades", 40, null, 8, 9, 4, 2),
                new Fixture(3, FixtureKind.CounterDisplay, "Expositor de caja", 15, null, 15, 10, 3, 1)
            };

            // (x, y, ancho, alto) de las 15 estanterías: pared del fondo, laterales e islas.
            (int X, int Y, int W, int H)[] shelves =
            {
                (1, 0, 3, 1), (4, 0, 3, 1), (7, 0, 3, 1), (10, 0, 3, 1), (13, 0, 3, 1), (16, 0, 3, 1),
                (0, 2, 1, 3), (0, 5, 1, 3), (0, 8, 1, 3),
                (19, 2, 1, 3), (19, 5, 1, 3), (19, 8, 1, 3),
                (5, 4, 4, 1), (11, 4, 4, 1), (8, 6, 4, 1)
            };

            Genre[] genres = (Genre[])System.Enum.GetValues(typeof(Genre));
            for (int i = 0; i < genres.Length && i < shelves.Length; i++)
            {
                (int x, int y, int w, int h) = shelves[i];
                fixtures.Add(new Fixture(10 + i, FixtureKind.Shelf, "Estantería", ShelfCapacity, genres[i], x, y, w, h));
            }

            return new StoreLayout(fixtures);
        }

        /// <summary>
        /// Recoloca todo: escaparate con los más populares, mesa con las novedades, expositor
        /// con libros baratos y populares, y el resto en la estantería de su género hasta llenarla.
        /// </summary>
        public static void AutoArrange(StoreLayout layout, Inventory inventory, BookCatalog catalog, GameDate date)
        {
            foreach (Fixture fixture in layout.Fixtures)
            {
                layout.ClearFixture(fixture.Id);
            }

            List<Book> inStock = inventory.BookIds
                .Select(catalog.GetBook)
                .OrderByDescending(book => book.PopularityOn(date))
                .ThenBy(book => book.Id)
                .ToList();

            foreach (Fixture fixture in layout.Fixtures.Where(f => f.Kind == FixtureKind.ShopWindow))
            {
                Fill(layout, inventory, fixture, inStock, 3);
            }

            foreach (Fixture fixture in layout.Fixtures.Where(f => f.Kind == FixtureKind.NewReleaseTable))
            {
                Fill(layout, inventory, fixture, inStock.Where(b => b.PhaseOn(date) == BookPhase.NewRelease), 4);
            }

            foreach (Fixture fixture in layout.Fixtures.Where(f => f.Kind == FixtureKind.CounterDisplay))
            {
                Fill(layout, inventory, fixture, inStock.Where(b => b.Price.Cents < 1500), 2);
            }

            foreach (Book book in inStock)
            {
                ShelveInSection(layout, inventory, book);
            }
        }

        /// <summary>Pone en la estantería de su sección los ejemplares que estén en el almacén, si caben.</summary>
        public static void ShelveInSection(StoreLayout layout, Inventory inventory, Book book)
        {
            foreach (Fixture shelf in layout.Fixtures.Where(f => f.Kind == FixtureKind.Shelf && f.Section == book.Genre))
            {
                int inBackroom = inventory.Quantity(book.Id) - layout.DisplayedQuantity(book.Id);
                int quantity = System.Math.Min(inBackroom, layout.FreeCapacity(shelf.Id));
                if (quantity > 0)
                {
                    layout.Place(shelf.Id, book.Id, quantity, inventory);
                }
            }
        }

        private static void Fill(StoreLayout layout, Inventory inventory, Fixture fixture, IEnumerable<Book> books, int perTitle)
        {
            foreach (Book book in books)
            {
                int inBackroom = inventory.Quantity(book.Id) - layout.DisplayedQuantity(book.Id);
                int quantity = System.Math.Min(System.Math.Min(perTitle, inBackroom), layout.FreeCapacity(fixture.Id));
                if (quantity > 0)
                {
                    layout.Place(fixture.Id, book.Id, quantity, inventory);
                }

                if (layout.FreeCapacity(fixture.Id) == 0)
                {
                    return;
                }
            }
        }
    }
}
