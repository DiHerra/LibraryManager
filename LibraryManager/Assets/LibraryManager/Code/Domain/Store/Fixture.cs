using System;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Common;

namespace LibraryManager.Domain.Store
{
    /// <summary>Tipo de mueble. Los valores se guardan en la partida: no reordenar.</summary>
    public enum FixtureKind
    {
        /// <summary>Estantería de una sección (un género).</summary>
        Shelf = 0,
        /// <summary>Mesa de novedades junto a la entrada.</summary>
        NewReleaseTable = 1,
        /// <summary>Escaparate: lo ve quien pasa por la calle.</summary>
        ShopWindow = 2,
        /// <summary>Expositor junto a la caja: compras por impulso.</summary>
        CounterDisplay = 3
    }

    /// <summary>
    /// Un mueble de la tienda: tipo, capacidad en ejemplares y, si es estantería, su
    /// sección. La posición (en casillas) es para dibujar el plano.
    /// </summary>
    public sealed class Fixture
    {
        public Fixture(int id, FixtureKind kind, string name, int capacity, Genre? section, int x, int y, int width, int height)
        {
            Id = id > 0 ? id : throw new ArgumentOutOfRangeException(nameof(id));
            Kind = kind;
            Name = Guard.NotBlank(name, nameof(name));
            Capacity = Guard.InRange(capacity, 1, 1000, nameof(capacity));
            if (kind == FixtureKind.Shelf && section == null)
            {
                throw new ArgumentException("Una estantería necesita una sección.", nameof(section));
            }

            Section = kind == FixtureKind.Shelf ? section : null;
            X = Guard.InRange(x, 0, 100, nameof(x));
            Y = Guard.InRange(y, 0, 100, nameof(y));
            Width = Guard.InRange(width, 1, 100, nameof(width));
            Height = Guard.InRange(height, 1, 100, nameof(height));
        }

        public int Id { get; }
        public FixtureKind Kind { get; }
        public string Name { get; }
        public int Capacity { get; }
        public Genre? Section { get; }
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
    }
}
