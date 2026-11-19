using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Common;

namespace LibraryManager.Domain.Catalog
{
    /// <summary>Editorial. El descuento es el margen que deja al librero sobre el PVP.</summary>
    public sealed class Publisher
    {
        public Publisher(int id, string name, PublisherSize size, IEnumerable<Genre> focusGenres, int distributorId, int tradeDiscountBasisPoints)
        {
            Id = id > 0 ? id : throw new System.ArgumentOutOfRangeException(nameof(id));
            Name = Guard.NotBlank(name, nameof(name));
            Size = size;
            FocusGenres = Guard.NotNull(focusGenres, nameof(focusGenres)).Distinct().ToList();
            if (FocusGenres.Count == 0)
            {
                throw new System.ArgumentException("La editorial necesita al menos un género.", nameof(focusGenres));
            }

            DistributorId = distributorId;
            TradeDiscountBasisPoints = Guard.InRange(tradeDiscountBasisPoints, 1000, 6000, nameof(tradeDiscountBasisPoints));
        }

        public int Id { get; }
        public string Name { get; }
        public PublisherSize Size { get; }
        public IReadOnlyList<Genre> FocusGenres { get; }
        public int DistributorId { get; }

        /// <summary>Descuento sobre el PVP en puntos básicos (3000 = 30 %).</summary>
        public int TradeDiscountBasisPoints { get; }
    }
}
