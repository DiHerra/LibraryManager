using System;
using System.Collections.Generic;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Common;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Store;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.World
{
    /// <summary>Estado completo de una partida. Todo lo que se guarda cuelga de aquí.</summary>
    public sealed class WorldState
    {
        public static readonly GameDate DefaultStartDate = GameDate.Create(2026, 9, 1);
        public static readonly Money DefaultStartingCash = Money.FromEuros(30000m);
        public static readonly Money DefaultMonthlyRent = Money.FromEuros(1800m);
        public const string RentTitle = "Alquiler del local";

        /// <summary>Tope de seguridad para un avance de varios días.</summary>
        public const int MaxDaysPerAdvance = 366;

        public WorldState(
            Bookstore bookstore,
            GameDate date,
            DeterministicRandom random,
            StoreSchedule schedule,
            WorldEventQueue events,
            BookCatalog catalog)
        {
            Bookstore = Guard.NotNull(bookstore, nameof(bookstore));
            Date = date;
            Random = Guard.NotNull(random, nameof(random));
            Schedule = Guard.NotNull(schedule, nameof(schedule));
            Events = Guard.NotNull(events, nameof(events));
            Catalog = Guard.NotNull(catalog, nameof(catalog));
        }

        public Bookstore Bookstore { get; }

        public GameDate Date { get; private set; }

        public DeterministicRandom Random { get; }

        public StoreSchedule Schedule { get; }

        public WorldEventQueue Events { get; }

        /// <summary>Títulos, editoriales y distribuidoras del mundo.</summary>
        public BookCatalog Catalog { get; }

        public bool IsOpenToday => Schedule.IsOpen(Date);

        public CommercialSeason Season => CommercialCalendar.GetSeason(Date);

        public static WorldState CreateNew(string bookstoreName, ulong seed)
        {
            var events = new WorldEventQueue();
            ScheduleDefaultRent(events, DefaultStartDate);

            var random = new DeterministicRandom(seed);
            BookCatalog catalog = CatalogGenerator.Generate(random.NextULong(), DefaultStartDate);

            return new WorldState(
                new Bookstore(bookstoreName, DefaultStartingCash),
                DefaultStartDate,
                random,
                StoreSchedule.CreateDefault(),
                events,
                catalog);
        }

        /// <summary>Programa el alquiler mensual el día 1 del mes siguiente a <paramref name="from"/>.</summary>
        public static void ScheduleDefaultRent(WorldEventQueue events, GameDate from)
        {
            DateTime nextMonth = from.ToDateTime().AddMonths(1);
            events.Schedule(
                GameDate.Create(nextMonth.Year, nextMonth.Month, 1),
                WorldEventKind.Payment,
                RentTitle,
                DefaultMonthlyRent,
                EventRecurrence.Monthly);
        }

        /// <summary>Pasa al día siguiente y resuelve sus eventos.</summary>
        public DayReport AdvanceDay()
        {
            CommercialSeason previousSeason = Season;
            Money cashBefore = Bookstore.Cash;

            Date = Date.AddDays(1);

            IReadOnlyList<WorldEvent> due = Events.TakeDue(Date);
            var pauses = new List<PauseReason>();

            foreach (WorldEvent worldEvent in due)
            {
                if (worldEvent.Kind == WorldEventKind.Payment)
                {
                    Bookstore.Pay(worldEvent.Amount);
                }

                if (worldEvent.PausesAdvance)
                {
                    pauses.Add(new PauseReason(PauseReasonKind.EventNeedsAttention, worldEvent.Title));
                }
            }

            CommercialSeason season = Season;
            if (season != previousSeason && season != CommercialSeason.Normal)
            {
                pauses.Add(new PauseReason(PauseReasonKind.SeasonStarted, season.ToString()));
            }

            if (!cashBefore.IsNegative && Bookstore.Cash.IsNegative)
            {
                pauses.Add(new PauseReason(PauseReasonKind.CashBecameNegative, string.Empty));
            }

            return new DayReport(Date, IsOpenToday, season, due, cashBefore, Bookstore.Cash, pauses);
        }

        /// <summary>
        /// Avanza hasta <paramref name="days"/> días y se detiene antes si un día trae
        /// un motivo de pausa. Siempre avanza al menos un día.
        /// </summary>
        public AdvanceResult AdvanceDays(int days)
        {
            Guard.InRange(days, 1, MaxDaysPerAdvance, nameof(days));

            var reports = new List<DayReport>();
            for (int i = 0; i < days; i++)
            {
                DayReport report = AdvanceDay();
                reports.Add(report);
                if (report.ShouldPause)
                {
                    return new AdvanceResult(reports) { StoppedEarly = i < days - 1 };
                }
            }

            return new AdvanceResult(reports);
        }

        /// <summary>Avanza hasta el próximo lunes (fin de la semana en curso), con las mismas pausas.</summary>
        public AdvanceResult AdvanceToNextWeek()
        {
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)Date.DayOfWeek + 7) % 7;
            return AdvanceDays(daysUntilMonday == 0 ? 7 : daysUntilMonday);
        }
    }
}
