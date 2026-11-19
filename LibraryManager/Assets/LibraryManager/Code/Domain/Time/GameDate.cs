using System;
using System.Globalization;

namespace LibraryManager.Domain.Time
{
    /// <summary>Fecha del mundo del juego, sin hora. Se guarda como número de día.</summary>
    public readonly struct GameDate : IEquatable<GameDate>, IComparable<GameDate>
    {
        private static readonly DateTime Epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        private GameDate(int dayNumber)
        {
            if (dayNumber < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dayNumber), "La fecha no puede ser anterior al 1 de enero de 2000.");
            }

            DayNumber = dayNumber;
        }

        /// <summary>Días transcurridos desde el 1 de enero de 2000.</summary>
        public int DayNumber { get; }

        public int Year => ToDateTime().Year;
        public int Month => ToDateTime().Month;
        public int Day => ToDateTime().Day;
        public DayOfWeek DayOfWeek => ToDateTime().DayOfWeek;

        public static GameDate FromDayNumber(int dayNumber)
        {
            return new GameDate(dayNumber);
        }

        public static GameDate Create(int year, int month, int day)
        {
            var date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
            return new GameDate((int)(date - Epoch).TotalDays);
        }

        public GameDate AddDays(int days)
        {
            return new GameDate(checked(DayNumber + days));
        }

        public int DaysUntil(GameDate other)
        {
            return other.DayNumber - DayNumber;
        }

        public DateTime ToDateTime()
        {
            return Epoch.AddDays(DayNumber);
        }

        public static bool operator ==(GameDate left, GameDate right) => left.DayNumber == right.DayNumber;
        public static bool operator !=(GameDate left, GameDate right) => left.DayNumber != right.DayNumber;
        public static bool operator <(GameDate left, GameDate right) => left.DayNumber < right.DayNumber;
        public static bool operator >(GameDate left, GameDate right) => left.DayNumber > right.DayNumber;
        public static bool operator <=(GameDate left, GameDate right) => left.DayNumber <= right.DayNumber;
        public static bool operator >=(GameDate left, GameDate right) => left.DayNumber >= right.DayNumber;

        public bool Equals(GameDate other) => DayNumber == other.DayNumber;
        public override bool Equals(object obj) => obj is GameDate other && Equals(other);
        public override int GetHashCode() => DayNumber;
        public int CompareTo(GameDate other) => DayNumber.CompareTo(other.DayNumber);

        public override string ToString()
        {
            return ToDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}
