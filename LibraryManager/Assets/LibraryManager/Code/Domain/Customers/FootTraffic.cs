using System;
using LibraryManager.Domain.Common;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.Customers
{
    /// <summary>Cuántos lectores entran en la tienda un día y a qué hora.</summary>
    public static class FootTraffic
    {
        /// <summary>Visitantes de un martes normal con reputación 50.</summary>
        public const int BaseVisitors = 55;

        /// <summary>Factor del día de la semana en puntos básicos (10000 = ×1).</summary>
        public static int WeekdayBasisPoints(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Monday: return 8000;
                case DayOfWeek.Friday: return 11000;
                case DayOfWeek.Saturday: return 15000;
                case DayOfWeek.Sunday: return 12000;
                default: return 9000;
            }
        }

        /// <summary>Factor de la reputación (0–100): de ×0,6 a ×1,4, ×1 con 50.</summary>
        public static double ReputationFactor(double reputation)
        {
            return 0.6 + 0.8 * Math.Max(0d, Math.Min(100d, reputation)) / 100d;
        }

        /// <summary>
        /// Visitantes esperados sin azar: base × día × temporada × reputación.
        /// </summary>
        public static double ExpectedVisitors(GameDate date, double reputation)
        {
            double season = CommercialCalendar.DemandBasisPoints(CommercialCalendar.GetSeason(date)) / 10000d;
            double weekday = WeekdayBasisPoints(date.DayOfWeek) / 10000d;
            return BaseVisitors * weekday * season * ReputationFactor(reputation);
        }

        /// <summary>Visitantes del día, con una variación de ±15 %.</summary>
        public static int Visitors(GameDate date, double reputation, DeterministicRandom random)
        {
            double noise = 0.85 + 0.3 * random.NextDouble();
            return (int)Math.Round(ExpectedVisitors(date, reputation) * noise, MidpointRounding.AwayFromZero);
        }

        /// <summary>Peso relativo de una hora del día: más gente a mediodía y a última hora de la tarde.</summary>
        public static int HourWeight(int hour)
        {
            if (hour < 12) return 7;
            if (hour < 14) return 10;
            if (hour < 17) return 7;
            if (hour < 19) return 12;
            return 9;
        }

        /// <summary>Hora de llegada de un visitante entre la apertura y el cierre.</summary>
        public static int ArrivalHour(StoreSchedule schedule, DeterministicRandom random)
        {
            int total = 0;
            for (int hour = schedule.OpeningHour; hour < schedule.ClosingHour; hour++)
            {
                total += HourWeight(hour);
            }

            int roll = random.NextInt(0, total);
            for (int hour = schedule.OpeningHour; hour < schedule.ClosingHour; hour++)
            {
                roll -= HourWeight(hour);
                if (roll < 0)
                {
                    return hour;
                }
            }

            return schedule.ClosingHour - 1;
        }
    }
}
