using LibraryManager.Domain.Economy;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class MoneyTests
    {
        [Test]
        public void FromEuros_RoundsToNearestCent()
        {
            Assert.AreEqual(1999, Money.FromEuros(19.99m).Cents);
            Assert.AreEqual(1000, Money.FromEuros(9.995m).Cents);
        }

        [Test]
        public void Arithmetic_KeepsCents()
        {
            Money total = Money.FromEuros(10.10m) + Money.FromEuros(0.20m) - Money.FromEuros(0.05m);
            Assert.AreEqual(1025, total.Cents);
            Assert.AreEqual(3075, (total * 3).Cents);
        }

        [Test]
        public void Percentage_UsesBasisPoints()
        {
            // IVA superreducido del 4 % sobre 20 €.
            Assert.AreEqual(80, Money.FromEuros(20m).Percentage(400).Cents);
        }

        [Test]
        public void Comparison_And_Equality()
        {
            Assert.IsTrue(Money.FromEuros(5m) > Money.FromEuros(4.99m));
            Assert.AreEqual(Money.FromEuros(1m), new Money(100));
            Assert.IsTrue((-Money.FromEuros(1m)).IsNegative);
        }
    }
}
