using System;
using LibraryManager.Domain.Common;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.Catalog
{
    /// <summary>Un título del catálogo del mundo (no un ejemplar en la tienda).</summary>
    public sealed class Book
    {
        public const int NewReleaseDays = 90;
        public const int MaxPopularity = 100;

        public Book(
            int id,
            string title,
            string author,
            int publisherId,
            Genre genre,
            BookFormat format,
            Money price,
            GameDate publicationDate,
            int basePopularity,
            int halfLifeDays,
            int backlistFloorPercent)
        {
            Id = id > 0 ? id : throw new ArgumentOutOfRangeException(nameof(id));
            Title = Guard.NotBlank(title, nameof(title));
            Author = Guard.NotBlank(author, nameof(author));
            PublisherId = publisherId;
            Genre = genre;
            Format = format;
            if (price.Cents <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(price), "El precio debe ser positivo.");
            }

            Price = price;
            PublicationDate = publicationDate;
            BasePopularity = Guard.InRange(basePopularity, 1, MaxPopularity, nameof(basePopularity));
            HalfLifeDays = Guard.InRange(halfLifeDays, 7, 3650, nameof(halfLifeDays));
            BacklistFloorPercent = Guard.InRange(backlistFloorPercent, 0, 100, nameof(backlistFloorPercent));
        }

        public int Id { get; }
        public string Title { get; }
        public string Author { get; }
        public int PublisherId { get; }
        public Genre Genre { get; }
        public BookFormat Format { get; }

        /// <summary>Precio de venta al público, con IVA.</summary>
        public Money Price { get; }

        public GameDate PublicationDate { get; }

        /// <summary>Interés del público al salir (1–100). Pocos títulos son superventas.</summary>
        public int BasePopularity { get; }

        /// <summary>Días en que el interés de novedad se reduce a la mitad.</summary>
        public int HalfLifeDays { get; }

        /// <summary>Parte del interés inicial que conserva como fondo (los clásicos conservan mucho).</summary>
        public int BacklistFloorPercent { get; }

        public bool IsReleased(GameDate date) => PublicationDate <= date;

        public BookPhase PhaseOn(GameDate date)
        {
            if (!IsReleased(date))
            {
                return BookPhase.Upcoming;
            }

            return PublicationDate.DaysUntil(date) <= NewReleaseDays ? BookPhase.NewRelease : BookPhase.Backlist;
        }

        /// <summary>
        /// Interés del público ese día (0–100): máximo al salir, baja con la semivida
        /// hasta el suelo de fondo. Antes de salir es 0.
        /// </summary>
        public double PopularityOn(GameDate date)
        {
            if (!IsReleased(date))
            {
                return 0d;
            }

            int age = PublicationDate.DaysUntil(date);
            double floor = BacklistFloorPercent / 100d;
            double decay = Math.Pow(0.5d, age / (double)HalfLifeDays);
            return BasePopularity * (floor + (1d - floor) * decay);
        }
    }
}
