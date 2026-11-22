using System;
using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Common;
using LibraryManager.Domain.Customers;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Stock;
using LibraryManager.Domain.Store;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.Sales
{
    /// <summary>
    /// Simula una jornada de apertura: recogidas de encargos, llegada de lectores, búsquedas,
    /// recomendaciones, encargos nuevos, compras al ojear y compras por impulso en caja.
    /// Saca del inventario lo vendido, registra la jornada y ajusta la reputación.
    /// El cobro en caja y el ajuste de lo expuesto los hace quien llama.
    /// </summary>
    public static class SalesSimulator
    {
        public const double ImpulseBuyProbability = 0.5;

        public static DailySales SimulateDay(
            GameDate date,
            StoreSchedule schedule,
            BookCatalog catalog,
            Inventory inventory,
            StoreLayout layout,
            SalesState sales,
            DeterministicRandom random)
        {
            Guard.NotNull(schedule, nameof(schedule));
            Guard.NotNull(catalog, nameof(catalog));
            Guard.NotNull(inventory, nameof(inventory));
            Guard.NotNull(layout, nameof(layout));
            Guard.NotNull(sales, nameof(sales));
            Guard.NotNull(random, nameof(random));

            var day = new Day(date, schedule, catalog, inventory, layout, sales, random);
            day.Run();
            return day.Result;
        }

        /// <summary>Estado de trabajo de una jornada: existencias, pesos de elección y totales.</summary>
        private sealed class Day
        {
            private readonly GameDate _date;
            private readonly StoreSchedule _schedule;
            private readonly BookCatalog _catalog;
            private readonly Inventory _inventory;
            private readonly SalesState _sales;
            private readonly DeterministicRandom _random;
            private readonly CommercialSeason _season;
            private readonly int _discount;

            private readonly Dictionary<int, int> _stock = new Dictionary<int, int>();
            private readonly Dictionary<int, int> _displayed = new Dictionary<int, int>();
            private readonly Dictionary<int, int> _visibility = new Dictionary<int, int>();
            private readonly Dictionary<int, double> _popularity = new Dictionary<int, double>();
            private readonly Dictionary<ReaderKind, WeightedBooks> _searchTargets = new Dictionary<ReaderKind, WeightedBooks>();
            private readonly Dictionary<ReaderKind, WeightedBooks> _browsable = new Dictionary<ReaderKind, WeightedBooks>();
            private readonly Dictionary<Genre, List<Book>> _recommendable = new Dictionary<Genre, List<Book>>();
            private readonly WeightedBooks _counter;
            private readonly double[] _profileShares;
            private readonly Dictionary<int, (int Units, Money Revenue)> _lines = new Dictionary<int, (int Units, Money Revenue)>();

            private double _satisfactionSum;
            private int _satisfactionSamples;

            public Day(GameDate date, StoreSchedule schedule, BookCatalog catalog, Inventory inventory, StoreLayout layout, SalesState sales, DeterministicRandom random)
            {
                _date = date;
                _schedule = schedule;
                _catalog = catalog;
                _inventory = inventory;
                _sales = sales;
                _random = random;
                _season = CommercialCalendar.GetSeason(date);
                _discount = sales.LoyaltyDiscountBasisPoints;
                Result = new DailySales(date, schedule.OpeningHour, schedule.HoursOpen);

                foreach (StockLot lot in inventory.Lots)
                {
                    _stock[lot.BookId] = Stock(lot.BookId) + lot.Quantity;
                }

                foreach (Placement placement in layout.Placements)
                {
                    _displayed[placement.BookId] = (_displayed.TryGetValue(placement.BookId, out int shown) ? shown : 0) + placement.Quantity;
                }

                List<Book> released = catalog.ReleasedBy(date).ToList();
                foreach (Book book in released)
                {
                    _popularity[book.Id] = book.PopularityOn(date);
                }

                List<Book> displayedBooks = _displayed.Keys
                    .Where(id => Stock(id) > 0)
                    .OrderBy(id => id)
                    .Select(id => catalog.GetBook(id))
                    .ToList();
                foreach (Book book in displayedBooks)
                {
                    _visibility[book.Id] = layout.VisibilityOf(book, inventory);
                }

                foreach (ReaderProfile profile in ReaderProfiles.All)
                {
                    _searchTargets[profile.Kind] = new WeightedBooks(
                        released,
                        book => Math.Pow(Popularity(book), 1.5) * profile.GenreWeight(book.Genre, _season));
                    _browsable[profile.Kind] = new WeightedBooks(
                        displayedBooks,
                        book => (Popularity(book) + 1d) * _visibility[book.Id] / 10000d * profile.GenreWeight(book.Genre, _season));
                }

                foreach (IGrouping<Genre, Book> group in displayedBooks.GroupBy(book => book.Genre))
                {
                    _recommendable[group.Key] = group.OrderByDescending(book => Popularity(book)).ThenBy(book => book.Id).ToList();
                }

                List<Book> counterBooks = layout.Fixtures
                    .Where(fixture => fixture.Kind == FixtureKind.CounterDisplay)
                    .SelectMany(fixture => layout.PlacementsOn(fixture.Id))
                    .Select(placement => placement.BookId)
                    .Distinct()
                    .Where(id => Stock(id) > 0)
                    .OrderBy(id => id)
                    .Select(id => catalog.GetBook(id))
                    .ToList();
                _counter = new WeightedBooks(counterBooks, book => Popularity(book) + 1d);

                _profileShares = ReaderProfiles.All.Select(profile => (double)profile.ShareIn(_season)).ToArray();
            }

            public DailySales Result { get; }

            public void Run()
            {
                CollectSpecialOrders();

                int visitors = FootTraffic.Visitors(_date, _sales.ReputationScore, _random);
                Result.Visitors = visitors;
                for (int i = 0; i < visitors; i++)
                {
                    Visit();
                }

                Result.SetLines(_lines.Select(pair => new SaleLine(pair.Key, pair.Value.Units, pair.Value.Revenue)));
                if (_satisfactionSamples > 0)
                {
                    double average = _satisfactionSum / _satisfactionSamples * 100d;
                    Result.SatisfactionPercent = (int)Math.Round(average, MidpointRounding.AwayFromZero);
                    _sales.ApplySatisfaction(average);
                }

                _sales.Ledger.Record(Result);
            }

            private int Stock(int bookId) => _stock.TryGetValue(bookId, out int quantity) ? quantity : 0;

            /// <summary>Lo expuesto se repone del almacén durante el día: hay a la vista lo expuesto mientras quede stock.</summary>
            private int Displayed(int bookId) => Math.Min(_displayed.TryGetValue(bookId, out int shown) ? shown : 0, Stock(bookId));

            private double Popularity(Book book) => _popularity.TryGetValue(book.Id, out double value) ? value : 0d;

            private void CollectSpecialOrders()
            {
                foreach (SpecialOrder order in _sales.SpecialOrders.ToList())
                {
                    if (order.IsExpiredOn(_date))
                    {
                        _sales.RemoveSpecialOrder(order);
                        Result.SpecialOrdersExpired++;
                        AddSatisfaction(0d);
                    }
                    else if (Stock(order.BookId) > 0)
                    {
                        Sell(_catalog.GetBook(order.BookId), cardholder: true, FootTraffic.ArrivalHour(_schedule, _random));
                        _sales.RemoveSpecialOrder(order);
                        Result.SpecialOrdersDelivered++;
                        Result.Buyers++;
                        AddSatisfaction(1d);
                    }
                }
            }

            private void Visit()
            {
                ReaderProfile profile = PickProfile();
                bool cardholder = _random.Chance(SalesRules.CardholderShare);
                long budget = (long)Math.Round(profile.Budget.Cents * (0.6 + 0.8 * _random.NextDouble()));
                int hour = FootTraffic.ArrivalHour(_schedule, _random);
                var bought = new HashSet<int>();
                double? searchOutcome = null;

                bool TryBuy(Book book)
                {
                    Money price = SalesRules.PriceFor(book.Price, _discount, cardholder);
                    if (bought.Contains(book.Id) || Stock(book.Id) == 0 || price.Cents > budget)
                    {
                        return false;
                    }

                    Sell(book, cardholder, hour);
                    budget -= price.Cents;
                    bought.Add(book.Id);
                    return true;
                }

                if (_random.Chance(profile.SearchProbability))
                {
                    searchOutcome = Search(profile, cardholder, TryBuy);
                }

                for (int attempt = 0; attempt < profile.BrowseAttempts; attempt++)
                {
                    Book book = _browsable[profile.Kind].Pick(_random);
                    if (book == null)
                    {
                        break;
                    }

                    if (Displayed(book.Id) > 0
                        && !bought.Contains(book.Id)
                        && _random.Chance(SalesRules.BrowseBuyProbability(Popularity(book), _visibility[book.Id], _discount, cardholder)))
                    {
                        TryBuy(book);
                    }
                }

                if (_random.Chance(profile.ImpulseProbability))
                {
                    Book book = _counter.Pick(_random);
                    if (book != null && Displayed(book.Id) > 0 && _random.Chance(ImpulseBuyProbability * SalesRules.DiscountBoost(_discount, cardholder)))
                    {
                        TryBuy(book);
                    }
                }

                if (bought.Count > 0)
                {
                    Result.Buyers++;
                }

                double satisfaction = searchOutcome.HasValue
                    ? (bought.Count > 0 ? Math.Max(searchOutcome.Value, 0.6) : searchOutcome.Value)
                    : (bought.Count > 0 ? 1d : 0.5);
                AddSatisfaction(satisfaction);
            }

            /// <summary>Busca un título concreto. Devuelve la satisfacción de la búsqueda.</summary>
            private double Search(ReaderProfile profile, bool cardholder, Func<Book, bool> tryBuy)
            {
                Book target = _searchTargets[profile.Kind].Pick(_random);
                if (target == null)
                {
                    return 0.5;
                }

                Result.Searches++;
                if (Stock(target.Id) > 0)
                {
                    Result.SearchesFound++;
                    return tryBuy(target) ? 1d : 0.4;
                }

                if (_random.Chance(SalesRules.RecommendationChance))
                {
                    Book alternative = Recommend(target);
                    if (alternative != null)
                    {
                        Result.Recommendations++;
                        if (_random.Chance(SalesRules.RecommendationAcceptance * SalesRules.DiscountBoost(_discount, cardholder)) && tryBuy(alternative))
                        {
                            Result.RecommendationsAccepted++;
                            return 0.8;
                        }
                    }
                }

                if (_sales.SpecialOrders.Count < SalesRules.MaxPendingSpecialOrders && _random.Chance(SalesRules.SpecialOrderChance))
                {
                    _sales.AddSpecialOrder(target.Id, _date);
                    Result.SpecialOrdersTaken++;
                    return 0.7;
                }

                Result.LostSearches++;
                return 0d;
            }

            /// <summary>El librero propone el título expuesto más popular del mismo género que quede.</summary>
            private Book Recommend(Book target)
            {
                if (!_recommendable.TryGetValue(target.Genre, out List<Book> candidates))
                {
                    return null;
                }

                return candidates.FirstOrDefault(book => book.Id != target.Id && Displayed(book.Id) > 0);
            }

            private ReaderProfile PickProfile()
            {
                double total = _profileShares.Sum();
                double roll = _random.NextDouble() * total;
                for (int i = 0; i < _profileShares.Length; i++)
                {
                    roll -= _profileShares[i];
                    if (roll < 0)
                    {
                        return ReaderProfiles.All[i];
                    }
                }

                return ReaderProfiles.All[ReaderProfiles.All.Count - 1];
            }

            private void Sell(Book book, bool cardholder, int hour)
            {
                Money price = SalesRules.PriceFor(book.Price, _discount, cardholder);
                Money cost = _inventory.RemoveOldestFirst(book.Id, 1);
                _stock[book.Id] = Stock(book.Id) - 1;

                Result.Units++;
                Result.Revenue += price;
                Result.Cost += cost;
                Result.Discounts += book.Price - price;
                int hourIndex = hour - Result.OpeningHour;
                if (hourIndex >= 0 && hourIndex < Result.UnitsByHour.Length)
                {
                    Result.UnitsByHour[hourIndex]++;
                }

                Result.UnitsByGenre[(int)book.Genre]++;
                (int units, Money revenue) = _lines.TryGetValue(book.Id, out (int Units, Money Revenue) line) ? line : (0, Money.Zero);
                _lines[book.Id] = (units + 1, revenue + price);
            }

            private void AddSatisfaction(double value)
            {
                _satisfactionSum += value;
                _satisfactionSamples++;
            }
        }

        /// <summary>Elección ponderada de libros con pesos acumulados y búsqueda binaria.</summary>
        private sealed class WeightedBooks
        {
            private readonly List<Book> _books = new List<Book>();
            private readonly List<double> _cumulative = new List<double>();

            public WeightedBooks(IEnumerable<Book> books, Func<Book, double> weight)
            {
                double total = 0d;
                foreach (Book book in books)
                {
                    double value = weight(book);
                    if (value <= 0d)
                    {
                        continue;
                    }

                    total += value;
                    _books.Add(book);
                    _cumulative.Add(total);
                }
            }

            public Book Pick(DeterministicRandom random)
            {
                if (_books.Count == 0)
                {
                    return null;
                }

                double roll = random.NextDouble() * _cumulative[_cumulative.Count - 1];
                int index = _cumulative.BinarySearch(roll);
                index = index < 0 ? ~index : index + 1;
                return _books[Math.Min(index, _books.Count - 1)];
            }
        }
    }
}
