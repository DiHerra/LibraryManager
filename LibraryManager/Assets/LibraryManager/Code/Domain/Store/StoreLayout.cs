using System;
using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Stock;

namespace LibraryManager.Domain.Store
{
    /// <summary>Ejemplares de un título expuestos en un mueble.</summary>
    public readonly struct Placement
    {
        public Placement(int fixtureId, int bookId, int quantity)
        {
            FixtureId = fixtureId;
            BookId = bookId;
            Quantity = quantity;
        }

        public int FixtureId { get; }
        public int BookId { get; }
        public int Quantity { get; }
    }

    /// <summary>
    /// Muebles de la tienda y qué hay expuesto en cada uno. Lo que no está expuesto está
    /// en el almacén: solo se vende si un cliente lo pide (L06/L07).
    /// </summary>
    public sealed class StoreLayout
    {
        /// <summary>Multiplicadores de visibilidad en puntos básicos (10000 = estantería de su sección).</summary>
        public const int ShopWindowVisibility = 25000;
        public const int NewReleaseTableVisibility = 18000;
        public const int CounterDisplayVisibility = 15000;
        public const int ShelfVisibility = 10000;
        public const int WrongSectionVisibility = 6000;
        public const int BackroomVisibility = 2000;

        private readonly List<Fixture> _fixtures;
        private readonly Dictionary<(int FixtureId, int BookId), int> _placements = new Dictionary<(int, int), int>();

        public StoreLayout(IEnumerable<Fixture> fixtures, IEnumerable<Placement> placements = null)
        {
            _fixtures = (fixtures ?? throw new ArgumentNullException(nameof(fixtures))).OrderBy(f => f.Id).ToList();
            if (_fixtures.Count == 0)
            {
                throw new ArgumentException("La tienda necesita al menos un mueble.", nameof(fixtures));
            }

            if (_fixtures.Select(f => f.Id).Distinct().Count() != _fixtures.Count)
            {
                throw new ArgumentException("Hay muebles con el mismo identificador.", nameof(fixtures));
            }

            foreach (Placement placement in placements ?? Enumerable.Empty<Placement>())
            {
                Fixture fixture = GetFixture(placement.FixtureId);
                if (placement.Quantity <= 0)
                {
                    throw new ArgumentException("Una colocación guardada tiene cantidad no positiva.");
                }

                if (_placements.ContainsKey((fixture.Id, placement.BookId)))
                {
                    throw new ArgumentException("Hay una colocación repetida.");
                }

                _placements[(fixture.Id, placement.BookId)] = placement.Quantity;
                if (UsedCapacity(fixture.Id) > fixture.Capacity)
                {
                    throw new ArgumentException($"El mueble {fixture.Name} supera su capacidad.");
                }
            }
        }

        public IReadOnlyList<Fixture> Fixtures => _fixtures;

        public IEnumerable<Placement> Placements =>
            _placements.OrderBy(p => p.Key.FixtureId).ThenBy(p => p.Key.BookId).Select(p => new Placement(p.Key.FixtureId, p.Key.BookId, p.Value));

        public Fixture GetFixture(int id) =>
            _fixtures.FirstOrDefault(f => f.Id == id) ?? throw new KeyNotFoundException($"No existe el mueble {id}.");

        public int UsedCapacity(int fixtureId) => _placements.Where(p => p.Key.FixtureId == fixtureId).Sum(p => p.Value);

        public int FreeCapacity(int fixtureId) => GetFixture(fixtureId).Capacity - UsedCapacity(fixtureId);

        public int DisplayedQuantity(int bookId) => _placements.Where(p => p.Key.BookId == bookId).Sum(p => p.Value);

        public int DisplayedOn(int fixtureId, int bookId) => _placements.TryGetValue((fixtureId, bookId), out int quantity) ? quantity : 0;

        public IEnumerable<Placement> PlacementsOf(int bookId) => Placements.Where(p => p.BookId == bookId);

        public IEnumerable<Placement> PlacementsOn(int fixtureId) => Placements.Where(p => p.FixtureId == fixtureId);

        /// <summary>Visibilidad de un mueble para un libro de un género.</summary>
        public static int VisibilityOf(Fixture fixture, Genre genre)
        {
            switch (fixture.Kind)
            {
                case FixtureKind.ShopWindow: return ShopWindowVisibility;
                case FixtureKind.NewReleaseTable: return NewReleaseTableVisibility;
                case FixtureKind.CounterDisplay: return CounterDisplayVisibility;
                default: return fixture.Section == genre ? ShelfVisibility : WrongSectionVisibility;
            }
        }

        /// <summary>
        /// Visibilidad de un título: la del mejor mueble donde tiene ejemplares; si solo está
        /// en el almacén, la del almacén; si no hay ejemplares, 0.
        /// </summary>
        public int VisibilityOf(Book book, Inventory inventory)
        {
            if (inventory.Quantity(book.Id) == 0)
            {
                return 0;
            }

            int best = BackroomVisibility;
            foreach (Placement placement in PlacementsOf(book.Id))
            {
                best = Math.Max(best, VisibilityOf(GetFixture(placement.FixtureId), book.Genre));
            }

            return best;
        }

        /// <summary>Expone ejemplares en un mueble, sacándolos del almacén.</summary>
        public void Place(int fixtureId, int bookId, int quantity, Inventory inventory)
        {
            if (quantity <= 0)
            {
                throw new StoreLayoutException("La cantidad debe ser positiva.");
            }

            Fixture fixture = GetFixture(fixtureId);
            if (FreeCapacity(fixtureId) < quantity)
            {
                throw new StoreLayoutException($"En {fixture.Name} solo caben {FreeCapacity(fixtureId)} ejemplares más.");
            }

            int inBackroom = inventory.Quantity(bookId) - DisplayedQuantity(bookId);
            if (inBackroom < quantity)
            {
                throw new StoreLayoutException($"Solo hay {Math.Max(0, inBackroom)} ejemplares en el almacén.");
            }

            _placements[(fixtureId, bookId)] = DisplayedOn(fixtureId, bookId) + quantity;
        }

        /// <summary>Retira ejemplares de un mueble y los devuelve al almacén.</summary>
        public void Remove(int fixtureId, int bookId, int quantity)
        {
            int current = DisplayedOn(fixtureId, bookId);
            if (quantity <= 0 || quantity > current)
            {
                throw new StoreLayoutException("No hay tantos ejemplares en ese mueble.");
            }

            if (current == quantity)
            {
                _placements.Remove((fixtureId, bookId));
            }
            else
            {
                _placements[(fixtureId, bookId)] = current - quantity;
            }
        }

        public void ClearFixture(int fixtureId)
        {
            GetFixture(fixtureId);
            foreach ((int FixtureId, int BookId) key in _placements.Keys.Where(k => k.FixtureId == fixtureId).ToList())
            {
                _placements.Remove(key);
            }
        }

        /// <summary>
        /// Ajusta lo expuesto a las existencias después de una salida (venta, devolución,
        /// saldo): si se expone más de lo que hay, se quita primero de los muebles menos visibles.
        /// </summary>
        public void Reconcile(Inventory inventory, BookCatalog catalog)
        {
            foreach (int bookId in _placements.Keys.Select(k => k.BookId).Distinct().ToList())
            {
                int excess = DisplayedQuantity(bookId) - inventory.Quantity(bookId);
                if (excess <= 0)
                {
                    continue;
                }

                Genre genre = catalog.GetBook(bookId).Genre;
                foreach (Placement placement in PlacementsOf(bookId)
                             .OrderBy(p => VisibilityOf(GetFixture(p.FixtureId), genre))
                             .ThenBy(p => p.FixtureId)
                             .ToList())
                {
                    int taken = Math.Min(excess, placement.Quantity);
                    Remove(placement.FixtureId, bookId, taken);
                    excess -= taken;
                    if (excess == 0)
                    {
                        break;
                    }
                }
            }
        }
    }

    public sealed class StoreLayoutException : InvalidOperationException
    {
        public StoreLayoutException(string message) : base(message)
        {
        }
    }
}
