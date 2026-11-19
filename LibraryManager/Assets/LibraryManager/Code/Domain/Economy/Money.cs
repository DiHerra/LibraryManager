using System;
using System.Globalization;

namespace LibraryManager.Domain.Economy
{
    /// <summary>Cantidad de dinero en céntimos de euro. Evita errores de redondeo.</summary>
    public readonly struct Money : IEquatable<Money>, IComparable<Money>
    {
        public static readonly Money Zero = new Money(0);

        public Money(long cents)
        {
            Cents = cents;
        }

        public long Cents { get; }

        public bool IsNegative => Cents < 0;

        public static Money FromEuros(decimal euros)
        {
            return new Money((long)decimal.Round(euros * 100m, 0, MidpointRounding.AwayFromZero));
        }

        public decimal ToEuros()
        {
            return Cents / 100m;
        }

        /// <summary>Aplica un porcentaje expresado en puntos básicos (2500 = 25 %), redondeando al céntimo.</summary>
        public Money Percentage(int basisPoints)
        {
            decimal result = Cents * (decimal)basisPoints / 10000m;
            return new Money((long)decimal.Round(result, 0, MidpointRounding.AwayFromZero));
        }

        public static Money operator +(Money left, Money right) => new Money(checked(left.Cents + right.Cents));
        public static Money operator -(Money left, Money right) => new Money(checked(left.Cents - right.Cents));
        public static Money operator *(Money money, int factor) => new Money(checked(money.Cents * factor));
        public static Money operator -(Money money) => new Money(-money.Cents);
        public static bool operator ==(Money left, Money right) => left.Cents == right.Cents;
        public static bool operator !=(Money left, Money right) => left.Cents != right.Cents;
        public static bool operator <(Money left, Money right) => left.Cents < right.Cents;
        public static bool operator >(Money left, Money right) => left.Cents > right.Cents;
        public static bool operator <=(Money left, Money right) => left.Cents <= right.Cents;
        public static bool operator >=(Money left, Money right) => left.Cents >= right.Cents;

        public bool Equals(Money other) => Cents == other.Cents;
        public override bool Equals(object obj) => obj is Money other && Equals(other);
        public override int GetHashCode() => Cents.GetHashCode();
        public int CompareTo(Money other) => Cents.CompareTo(other.Cents);

        public override string ToString()
        {
            return ToEuros().ToString("0.00", CultureInfo.InvariantCulture) + " EUR";
        }
    }
}
