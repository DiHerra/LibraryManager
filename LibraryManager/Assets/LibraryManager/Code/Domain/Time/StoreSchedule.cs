using System;
using System.Collections.Generic;
using System.Linq;

namespace LibraryManager.Domain.Time
{
    /// <summary>Días y horario de apertura de la librería.</summary>
    public sealed class StoreSchedule
    {
        public const int DefaultOpeningHour = 10;
        public const int DefaultClosingHour = 20;

        private readonly bool[] _openDays = new bool[7];

        public StoreSchedule(IEnumerable<DayOfWeek> openDays, int openingHour, int closingHour)
        {
            if (openDays == null)
            {
                throw new ArgumentNullException(nameof(openDays));
            }

            foreach (DayOfWeek day in openDays)
            {
                _openDays[(int)day] = true;
            }

            if (!_openDays.Any(open => open))
            {
                throw new ArgumentException("La librería tiene que abrir al menos un día.", nameof(openDays));
            }

            if (openingHour < 0 || closingHour > 24 || closingHour <= openingHour)
            {
                throw new ArgumentOutOfRangeException(nameof(closingHour), "El horario debe estar entre 0 y 24 y cerrar después de abrir.");
            }

            OpeningHour = openingHour;
            ClosingHour = closingHour;
        }

        /// <summary>De lunes a sábado, de 10 a 20.</summary>
        public static StoreSchedule CreateDefault()
        {
            return new StoreSchedule(
                new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
                DefaultOpeningHour,
                DefaultClosingHour);
        }

        public int OpeningHour { get; }

        public int ClosingHour { get; }

        public int HoursOpen => ClosingHour - OpeningHour;

        public IReadOnlyList<DayOfWeek> OpenDays =>
            Enumerable.Range(0, 7).Where(day => _openDays[day]).Select(day => (DayOfWeek)day).ToList();

        public bool OpensOn(DayOfWeek day)
        {
            return _openDays[(int)day];
        }

        /// <summary>Abre ese día: es un día de apertura y no es festivo.</summary>
        public bool IsOpen(GameDate date)
        {
            return OpensOn(date.DayOfWeek) && !PublicHolidays.IsHoliday(date);
        }
    }
}
