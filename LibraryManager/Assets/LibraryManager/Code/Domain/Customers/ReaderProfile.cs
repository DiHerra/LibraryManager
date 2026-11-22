using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.Customers
{
    public enum ReaderKind
    {
        /// <summary>Lector ocasional: novedades conocidas y poco gasto.</summary>
        Casual = 0,
        /// <summary>Gran lector: busca títulos concretos y ojea mucho.</summary>
        Avid = 1,
        /// <summary>Familia: infantil, juvenil, cómic y libros de texto.</summary>
        Family = 2,
        /// <summary>Busca un regalo: más en Navidad y en el Día del Libro.</summary>
        Gift = 3,
        /// <summary>Aficionado a la fantasía, la ciencia ficción y el cómic.</summary>
        Fan = 4
    }

    /// <summary>Tipo de lector: gustos, presupuesto y forma de comprar.</summary>
    public sealed class ReaderProfile
    {
        public const int PreferredGenreWeight = 3;
        public const int OtherGenreWeight = 1;
        public const int BackToSchoolSchoolMultiplier = 4;

        public ReaderProfile(
            ReaderKind kind,
            int sharePercent,
            IEnumerable<Genre> preferredGenres,
            Money budget,
            double searchProbability,
            int browseAttempts,
            double impulseProbability)
        {
            Kind = kind;
            SharePercent = sharePercent;
            PreferredGenres = preferredGenres.ToList();
            Budget = budget;
            SearchProbability = searchProbability;
            BrowseAttempts = browseAttempts;
            ImpulseProbability = impulseProbability;
        }

        public ReaderKind Kind { get; }

        /// <summary>Parte de los visitantes de un día normal (los cinco suman 100).</summary>
        public int SharePercent { get; }

        public IReadOnlyList<Genre> PreferredGenres { get; }

        /// <summary>Presupuesto medio de la visita; cada visitante lleva entre el 60 % y el 140 %.</summary>
        public Money Budget { get; }

        /// <summary>Probabilidad de entrar buscando un título concreto.</summary>
        public double SearchProbability { get; }

        /// <summary>Veces que ojea las mesas y estanterías.</summary>
        public int BrowseAttempts { get; }

        /// <summary>Probabilidad de mirar el expositor de caja.</summary>
        public double ImpulseProbability { get; }

        public bool Prefers(Genre genre) => PreferredGenres.Contains(genre);

        /// <summary>Peso del género para este lector en esa temporada (3 si le gusta, 1 si no; texto ×4 en vuelta al cole).</summary>
        public int GenreWeight(Genre genre, CommercialSeason season)
        {
            int weight = Prefers(genre) ? PreferredGenreWeight : OtherGenreWeight;
            if (genre == Genre.School && season == CommercialSeason.BackToSchool)
            {
                weight *= BackToSchoolSchoolMultiplier;
            }

            return weight;
        }

        /// <summary>Parte de visitantes en esa temporada: los que buscan regalo se duplican en Navidad y el Día del Libro.</summary>
        public int ShareIn(CommercialSeason season)
        {
            bool giftSeason = season == CommercialSeason.Christmas || season == CommercialSeason.BookDay;
            return Kind == ReaderKind.Gift && giftSeason ? SharePercent * 2 : SharePercent;
        }
    }

    /// <summary>Los cinco tipos de lector del mundo.</summary>
    public static class ReaderProfiles
    {
        public static readonly IReadOnlyList<ReaderProfile> All = new[]
        {
            new ReaderProfile(
                ReaderKind.Casual, 35,
                new[] { Genre.Fiction, Genre.Crime, Genre.Romance, Genre.SelfHelp, Genre.Cooking },
                Money.FromEuros(25m), 0.3, 2, 0.15),
            new ReaderProfile(
                ReaderKind.Avid, 20,
                new[] { Genre.Fiction, Genre.Crime, Genre.Historical, Genre.Essay, Genre.Poetry, Genre.Fantasy, Genre.ScienceFiction },
                Money.FromEuros(45m), 0.5, 3, 0.1),
            new ReaderProfile(
                ReaderKind.Family, 20,
                new[] { Genre.Children, Genre.YoungAdult, Genre.School, Genre.Comics },
                Money.FromEuros(40m), 0.5, 2, 0.2),
            new ReaderProfile(
                ReaderKind.Gift, 15,
                new[] { Genre.Fiction, Genre.Cooking, Genre.Travel, Genre.Children, Genre.Essay },
                Money.FromEuros(35m), 0.4, 2, 0.25),
            new ReaderProfile(
                ReaderKind.Fan, 10,
                new[] { Genre.Fantasy, Genre.ScienceFiction, Genre.Comics, Genre.YoungAdult },
                Money.FromEuros(40m), 0.6, 2, 0.1)
        };

        public static ReaderProfile Get(ReaderKind kind) => All.First(profile => profile.Kind == kind);
    }
}
