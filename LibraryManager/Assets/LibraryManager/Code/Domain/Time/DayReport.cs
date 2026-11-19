using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Economy;

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
            IReadOnlyList<PauseReason> pauseReasons)
        {
            Date = date;
            IsOpen = isOpen;
            Season = season;
            Events = events;
            CashBefore = cashBefore;
            CashAfter = cashAfter;
            PauseReasons = pauseReasons;
        }

        public GameDate Date { get; }
        public bool IsOpen { get; }
        public CommercialSeason Season { get; }
        public IReadOnlyList<WorldEvent> Events { get; }
        public Money CashBefore { get; }
        public Money CashAfter { get; }
        public IReadOnlyList<PauseReason> PauseReasons { get; }

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
    }
}
