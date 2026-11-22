using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Sales;

namespace LibraryManager.Domain.Time
{
    /// <summary>Lo que pasó al empezar un día: apertura, temporada, eventos y caja.</summary>
    public sealed class DayReport
    {
        public DayReport(
            GameDate date,
            bool isOpen,
            CommercialSeason season,
            IReadOnlyList<WorldEvent> events,
            Money cashBefore,
            Money cashAfter,
            IReadOnlyList<PauseReason> pauseReasons,
            DailySales sales = null)
        {
            Date = date;
            IsOpen = isOpen;
            Season = season;
            Events = events;
            CashBefore = cashBefore;
            CashAfter = cashAfter;
            PauseReasons = pauseReasons;
            Sales = sales;
        }

        public GameDate Date { get; }
        public bool IsOpen { get; }
        public CommercialSeason Season { get; }
        public IReadOnlyList<WorldEvent> Events { get; }
        public Money CashBefore { get; }
        public Money CashAfter { get; }
        public IReadOnlyList<PauseReason> PauseReasons { get; }

        /// <summary>Ventas de la jornada que se cerró al avanzar (el día anterior a <see cref="Date"/>); null si estaba cerrado.</summary>
        public DailySales Sales { get; }

        public Money CashChange => CashAfter - CashBefore;
        public bool ShouldPause => PauseReasons.Count > 0;
    }

    /// <summary>Resultado de avanzar uno o varios días.</summary>
    public sealed class AdvanceResult
    {
        public AdvanceResult(IReadOnlyList<DayReport> days)
        {
            Days = days;
        }

        public IReadOnlyList<DayReport> Days { get; }

        public DayReport LastDay => Days[Days.Count - 1];

        /// <summary>Se detuvo antes de llegar al final pedido.</summary>
        public bool StoppedEarly { get; internal set; }

        public IReadOnlyList<PauseReason> PauseReasons => LastDay.PauseReasons;

        public int DaysOpen => Days.Count(day => day.IsOpen);

        public Money CashChange => Days.Aggregate(Money.Zero, (total, day) => total + day.CashChange);

        public IEnumerable<WorldEvent> Events => Days.SelectMany(day => day.Events);

        /// <summary>Jornadas de venta del avance.</summary>
        public IEnumerable<DailySales> Sales => Days.Where(day => day.Sales != null).Select(day => day.Sales);

        public int UnitsSold => Sales.Sum(day => day.Units);

        public Money Revenue => Sales.Aggregate(Money.Zero, (total, day) => total + day.Revenue);

        public int Visitors => Sales.Sum(day => day.Visitors);

        public int LostSearches => Sales.Sum(day => day.LostSearches);

        public int SpecialOrdersTaken => Sales.Sum(day => day.SpecialOrdersTaken);

        /// <summary>Títulos más vendidos en el avance.</summary>
        public IReadOnlyList<SaleLine> TopTitles(int count) =>
            Sales.SelectMany(day => day.Lines)
                .GroupBy(line => line.BookId)
                .Select(group => new SaleLine(group.Key, group.Sum(line => line.Units), group.Aggregate(Money.Zero, (total, line) => total + line.Revenue)))
                .OrderByDescending(line => line.Units)
                .ThenByDescending(line => line.Revenue)
                .ThenBy(line => line.BookId)
                .Take(count)
                .ToList();
    }
}
