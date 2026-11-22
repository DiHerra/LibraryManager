using System;
using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.Sales
{
    /// <summary>Ventas acumuladas de un título desde el inicio de la partida.</summary>
    public sealed class BookSalesTotal
    {
        public BookSalesTotal(int bookId, int units, Money revenue, GameDate lastSoldOn)
        {
            BookId = bookId;
            Units = units;
            Revenue = revenue;
            LastSoldOn = lastSoldOn;
        }

        public int BookId { get; }
        public int Units { get; private set; }
        public Money Revenue { get; private set; }
        public GameDate LastSoldOn { get; private set; }

        internal void Add(int units, Money revenue, GameDate date)
        {
            Units += units;
            Revenue += revenue;
            if (date > LastSoldOn)
            {
                LastSoldOn = date;
            }
        }
    }

    /// <summary>
    /// Registro de ventas: el resumen de cada jornada (las últimas <see cref="MaxDays"/>)
    /// y los totales por título de toda la partida.
    /// </summary>
    public sealed class SalesLedger
    {
        public const int MaxDays = 400;

        private readonly List<DailySales> _days = new List<DailySales>();
        private readonly Dictionary<int, BookSalesTotal> _books = new Dictionary<int, BookSalesTotal>();

        public SalesLedger()
        {
        }

        public SalesLedger(IEnumerable<DailySales> days, IEnumerable<BookSalesTotal> books)
        {
            foreach (DailySales day in days ?? throw new ArgumentNullException(nameof(days)))
            {
                AddDay(day);
            }

            foreach (BookSalesTotal total in books ?? throw new ArgumentNullException(nameof(books)))
            {
                if (_books.ContainsKey(total.BookId))
                {
                    throw new ArgumentException($"Totales repetidos para el libro {total.BookId}.");
                }

                _books.Add(total.BookId, total);
            }
        }

        /// <summary>Jornadas registradas, de la más antigua a la más reciente.</summary>
        public IReadOnlyList<DailySales> Days => _days;

        public IEnumerable<BookSalesTotal> Books => _books.Values.OrderBy(total => total.BookId);

        public DailySales Latest => _days.Count == 0 ? null : _days[_days.Count - 1];

        public BookSalesTotal TotalOf(int bookId) => _books.TryGetValue(bookId, out BookSalesTotal total) ? total : null;

        public GameDate? LastSold(int bookId) => _books.TryGetValue(bookId, out BookSalesTotal total) ? total.LastSoldOn : (GameDate?)null;

        /// <summary>Jornadas entre <paramref name="from"/> y <paramref name="to"/>, ambas incluidas.</summary>
        public IEnumerable<DailySales> Between(GameDate from, GameDate to) => _days.Where(day => day.Date >= from && day.Date <= to);

        public Money RevenueBetween(GameDate from, GameDate to) => Between(from, to).Aggregate(Money.Zero, (total, day) => total + day.Revenue);

        /// <summary>Registra una jornada y suma sus líneas a los totales por título.</summary>
        public void Record(DailySales day)
        {
            RequireDay(day);
            AddDay(day);
            foreach (SaleLine line in day.Lines)
            {
                if (_books.TryGetValue(line.BookId, out BookSalesTotal total))
                {
                    total.Add(line.Units, line.Revenue, day.Date);
                }
                else
                {
                    _books.Add(line.BookId, new BookSalesTotal(line.BookId, line.Units, line.Revenue, day.Date));
                }
            }
        }

        private void AddDay(DailySales day)
        {
            RequireDay(day);
            if (_days.Count > 0 && day.Date <= _days[_days.Count - 1].Date)
            {
                throw new ArgumentException("Las jornadas se registran en orden de fecha y sin repetir.");
            }

            _days.Add(day);
            if (_days.Count > MaxDays)
            {
                _days.RemoveRange(0, _days.Count - MaxDays);
            }
        }

        private static void RequireDay(DailySales day)
        {
            if (day == null)
            {
                throw new ArgumentNullException(nameof(day));
            }
        }
    }
}
