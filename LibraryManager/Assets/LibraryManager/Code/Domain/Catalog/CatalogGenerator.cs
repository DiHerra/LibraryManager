using System;
using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Common;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.Catalog
{
    /// <summary>
    /// Genera el catálogo del mundo a partir de una semilla. Misma semilla y misma fecha,
    /// mismo catálogo. La partida guarda el catálogo generado, así que cambiar este
    /// generador no altera partidas existentes.
    /// </summary>
    public static class CatalogGenerator
    {
        public const int DefaultBookCount = 640;
        public const int AuthorCount = 260;

        private static readonly Genre[] AllGenres = (Genre[])Enum.GetValues(typeof(Genre));

        public static BookCatalog Generate(ulong seed, GameDate referenceDate, int bookCount = DefaultBookCount)
        {
            Guard.InRange(bookCount, 1, 100000, nameof(bookCount));
            var random = new DeterministicRandom(seed);

            List<Distributor> distributors = CreateDistributors(random);
            List<Publisher> publishers = CreatePublishers(random, distributors);
            List<Author> authors = CreateAuthors(random);
            List<Book> books = CreateBooks(random, referenceDate, bookCount, publishers, authors);

            return new BookCatalog(distributors, publishers, books);
        }

        private static List<Distributor> CreateDistributors(DeterministicRandom random)
        {
            int[] leadTimes = { 2, 3, 4, 6 };
            int[] minimumEuros = { 150, 100, 200, 60 };
            var result = new List<Distributor>();
            for (int i = 0; i < CatalogNameBank.Distributors.Length; i++)
            {
                result.Add(new Distributor(
                    i + 1,
                    CatalogNameBank.Distributors[i],
                    leadTimes[i],
                    Money.FromEuros(minimumEuros[i]),
                    random.Chance(0.5) ? 90 : 120));
            }

            return result;
        }

        private static List<Publisher> CreatePublishers(DeterministicRandom random, List<Distributor> distributors)
        {
            var result = new List<Publisher>();
            for (int i = 0; i < CatalogNameBank.Publishers.Length; i++)
            {
                PublisherSize size = i < 4 ? PublisherSize.Large : i < 12 ? PublisherSize.Medium : PublisherSize.Small;
                int genreCount = size == PublisherSize.Large ? 5 : size == PublisherSize.Medium ? 3 : 2;
                List<Genre> genres = Shuffle(AllGenres.ToList(), random).Take(genreCount).ToList();

                int baseDiscount = size == PublisherSize.Large ? 3000 : size == PublisherSize.Medium ? 3300 : 3500;
                int discount = baseDiscount + random.NextInt(-2, 3) * 100;
                int distributorId = distributors[random.NextInt(0, distributors.Count)].Id;

                result.Add(new Publisher(i + 1, CatalogNameBank.Publishers[i], size, genres, distributorId, discount));
            }

            return result;
        }

        private sealed class Author
        {
            public string Name;
            public Genre MainGenre;
            /// <summary>Fama del autor: sube la popularidad de todos sus libros.</summary>
            public double Fame;
        }

        private static List<Author> CreateAuthors(DeterministicRandom random)
        {
            var names = new HashSet<string>();
            var result = new List<Author>();
            while (result.Count < AuthorCount)
            {
                string name = Pick(CatalogNameBank.FirstNames, random) + " " + Pick(CatalogNameBank.LastNames, random);
                if (!names.Add(name))
                {
                    name += " " + Pick(CatalogNameBank.LastNames, random);
                    if (!names.Add(name))
                    {
                        continue;
                    }
                }

                result.Add(new Author
                {
                    Name = name,
                    MainGenre = AllGenres[random.NextInt(0, AllGenres.Length)],
                    Fame = Math.Pow(random.NextDouble(), 4)
                });
            }

            return result;
        }

        private static List<Book> CreateBooks(
            DeterministicRandom random,
            GameDate referenceDate,
            int bookCount,
            List<Publisher> publishers,
            List<Author> authors)
        {
            var publisherWeights = publishers.Select(p => p.Size == PublisherSize.Large ? 4 : p.Size == PublisherSize.Medium ? 2 : 1).ToList();
            int totalWeight = publisherWeights.Sum();
            var titles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var books = new List<Book>(bookCount);

            for (int id = 1; id <= bookCount; id++)
            {
                Publisher publisher = PickWeighted(publishers, publisherWeights, totalWeight, random);
                Genre genre = random.Chance(0.85)
                    ? publisher.FocusGenres[random.NextInt(0, publisher.FocusGenres.Count)]
                    : AllGenres[random.NextInt(0, AllGenres.Length)];

                Author author = PickAuthor(authors, genre, random);
                GameDate publication = PublicationDate(referenceDate, genre, random);
                BookFormat format = PickFormat(genre, referenceDate, publication, random);
                string title = UniqueTitle(genre, random, titles);

                double luck = Math.Pow(random.NextDouble(), 3);
                int popularity = Clamp((int)Math.Round(1 + 70 * luck + 29 * author.Fame), 1, Book.MaxPopularity);
                int halfLife = genre == Genre.School ? 120 : random.NextInt(20, 181);
                int floor = FloorPercent(genre, random);

                books.Add(new Book(
                    id,
                    title,
                    genre == Genre.School ? "Equipo editorial " + publisher.Name : author.Name,
                    publisher.Id,
                    genre,
                    format,
                    Price(genre, format, random),
                    publication,
                    popularity,
                    halfLife,
                    floor));
            }

            return books;
        }

        private static Author PickAuthor(List<Author> authors, Genre genre, DeterministicRandom random)
        {
            if (random.Chance(0.7))
            {
                List<Author> specialists = authors.Where(a => a.MainGenre == genre).ToList();
                if (specialists.Count > 0)
                {
                    return specialists[random.NextInt(0, specialists.Count)];
                }
            }

            return authors[random.NextInt(0, authors.Count)];
        }

        /// <summary>
        /// 15 % sale en los próximos 12 meses, 40 % en los dos últimos años y el resto en
        /// los ocho anteriores. Los libros de texto salen en junio o julio.
        /// </summary>
        private static GameDate PublicationDate(GameDate referenceDate, Genre genre, DeterministicRandom random)
        {
            double roll = random.NextDouble();
            int offset = roll < 0.15
                ? random.NextInt(1, 366)
                : roll < 0.55
                    ? -random.NextInt(0, 730)
                    : -random.NextInt(730, 3650);

            GameDate date = referenceDate.AddDays(offset);
            if (genre == Genre.School)
            {
                int year = date.Year;
                if (GameDate.Create(year, 6, 1) > referenceDate.AddDays(365))
                {
                    year--;
                }

                date = GameDate.Create(year, 6, 1).AddDays(random.NextInt(0, 61));
            }

            return date;
        }

        private static BookFormat PickFormat(Genre genre, GameDate referenceDate, GameDate publication, DeterministicRandom random)
        {
            if (genre == Genre.School)
            {
                return BookFormat.Paperback;
            }

            bool old = publication.DaysUntil(referenceDate) > 730;
            if (old && genre != Genre.Children && random.Chance(0.4))
            {
                return BookFormat.Pocket;
            }

            double hardcoverChance = genre == Genre.Children || genre == Genre.Comics || genre == Genre.Cooking ? 0.5 : 0.2;
            return random.Chance(hardcoverChance) ? BookFormat.Hardcover : BookFormat.Paperback;
        }

        /// <summary>PVP en euros terminado en ,95 o ,90, según encuadernación y género.</summary>
        private static Money Price(Genre genre, BookFormat format, DeterministicRandom random)
        {
            int minimum;
            int maximum;
            switch (format)
            {
                case BookFormat.Pocket: minimum = 8; maximum = 13; break;
                case BookFormat.Hardcover: minimum = 19; maximum = 32; break;
                default: minimum = 15; maximum = 23; break;
            }

            if (genre == Genre.Children) { minimum -= 4; maximum -= 6; }
            if (genre == Genre.Poetry) { minimum -= 2; maximum -= 6; }
            if (genre == Genre.Cooking || genre == Genre.Travel) { minimum += 3; maximum += 5; }
            if (genre == Genre.School) { minimum = 24; maximum = 42; }

            int euros = random.NextInt(minimum, Math.Max(minimum + 1, maximum + 1));
            int cents = random.Chance(0.7) ? 95 : 90;
            return new Money(euros * 100L + cents);
        }

        private static int FloorPercent(Genre genre, DeterministicRandom random)
        {
            switch (genre)
            {
                case Genre.Children:
                case Genre.Poetry:
                case Genre.Essay:
                case Genre.Cooking:
                    return random.NextInt(25, 61);
                case Genre.School:
                    return 60;
                default:
                    // Algunos títulos se convierten en clásicos del fondo.
                    return random.Chance(0.1) ? random.NextInt(40, 71) : random.NextInt(5, 31);
            }
        }

        private static string UniqueTitle(Genre genre, DeterministicRandom random, HashSet<string> used)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                string candidate = Title(genre, random);
                if (used.Add(candidate))
                {
                    return candidate;
                }
            }

            for (int volume = 2; ; volume++)
            {
                string candidate = $"{Title(genre, random)} {volume}";
                if (used.Add(candidate))
                {
                    return candidate;
                }
            }
        }

        private static string Title(Genre genre, DeterministicRandom random)
        {
            switch (genre)
            {
                case Genre.Crime: return Format(CatalogNameBank.CrimeTitles, CatalogNameBank.Places, random);
                case Genre.Fantasy: return Format(CatalogNameBank.FantasyTitles, CatalogNameBank.FantasyNames, random);
                case Genre.ScienceFiction: return Format(CatalogNameBank.SciFiTitles, CatalogNameBank.SciFiNames, random);
                case Genre.Romance: return Format(CatalogNameBank.RomanceTitles, CatalogNameBank.RomancePlaces, random);
                case Genre.Children: return Format(CatalogNameBank.ChildrenTitles, CatalogNameBank.ChildrenCharacters, random);
                case Genre.YoungAdult: return Pick(CatalogNameBank.YoungAdultTitles, random);
                case Genre.Comics: return Format(CatalogNameBank.ComicTitles, CatalogNameBank.ComicNames, random);
                case Genre.Essay: return Format(CatalogNameBank.EssayTitles, CatalogNameBank.EssayTopics, random);
                case Genre.SelfHelp: return Format(CatalogNameBank.SelfHelpTitles, CatalogNameBank.SelfHelpTopics, random);
                case Genre.Cooking: return Format(CatalogNameBank.CookingTitles, CatalogNameBank.CookingTopics, random);
                case Genre.Travel: return Format(CatalogNameBank.TravelTitles, CatalogNameBank.TravelPlaces, random);
                case Genre.Poetry: return Format(CatalogNameBank.PoetryTitles, CatalogNameBank.EssayTopics, random);
                case Genre.School: return Pick(CatalogNameBank.SchoolSubjects, random) + " " + Pick(CatalogNameBank.SchoolCourses, random);
                case Genre.Historical:
                    return Pick(CatalogNameBank.Nouns, random) + " " + Pick(CatalogNameBank.Complements, random) + " (" + random.NextInt(1492, 1940) + ")";
                default:
                    return Pick(CatalogNameBank.Nouns, random) + " " + Pick(CatalogNameBank.Complements, random);
            }
        }

        private static string Format(string[] templates, string[] values, DeterministicRandom random)
        {
            string text = string.Format(Pick(templates, random), Pick(values, random));
            return char.ToUpperInvariant(text[0]) + text.Substring(1);
        }

        private static T Pick<T>(IReadOnlyList<T> items, DeterministicRandom random)
        {
            return items[random.NextInt(0, items.Count)];
        }

        private static T PickWeighted<T>(IReadOnlyList<T> items, IReadOnlyList<int> weights, int totalWeight, DeterministicRandom random)
        {
            int roll = random.NextInt(0, totalWeight);
            for (int i = 0; i < items.Count; i++)
            {
                roll -= weights[i];
                if (roll < 0)
                {
                    return items[i];
                }
            }

            return items[items.Count - 1];
        }

        private static List<T> Shuffle<T>(List<T> items, DeterministicRandom random)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = random.NextInt(0, i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }

            return items;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return value < minimum ? minimum : value > maximum ? maximum : value;
        }
    }
}
