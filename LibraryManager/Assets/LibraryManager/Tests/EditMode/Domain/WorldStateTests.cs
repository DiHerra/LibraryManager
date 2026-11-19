using System;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Store;
using LibraryManager.Domain.World;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class WorldStateTests
    {
        [Test]
        public void CreateNew_StartsWithDefaults()
        {
            WorldState world = WorldState.CreateNew("  La Página en Blanco ", 5);

            Assert.AreEqual("La Página en Blanco", world.Bookstore.Name);
            Assert.AreEqual(WorldState.DefaultStartDate, world.Date);
            Assert.AreEqual(WorldState.DefaultStartingCash, world.Bookstore.Cash);
        }

        [Test]
        public void AdvanceDay_MovesOneDay()
        {
            WorldState world = WorldState.CreateNew("Libros", 5);
            world.AdvanceDay();

            Assert.AreEqual(WorldState.DefaultStartDate.AddDays(1), world.Date);
        }

        [Test]
        public void Bookstore_RejectsInvalidNames()
        {
            Assert.Throws<ArgumentException>(() => new Bookstore(" ", Money.Zero));
            Assert.Throws<ArgumentException>(() => new Bookstore(new string('x', Bookstore.MaxNameLength + 1), Money.Zero));
        }

        [Test]
        public void Bookstore_PaymentsMoveCash()
        {
            var bookstore = new Bookstore("Libros", Money.FromEuros(100m));
            bookstore.ReceivePayment(Money.FromEuros(20m));
            bookstore.Pay(Money.FromEuros(150m));

            Assert.AreEqual(Money.FromEuros(-30m), bookstore.Cash);
            Assert.Throws<ArgumentOutOfRangeException>(() => bookstore.Pay(Money.FromEuros(-1m)));
        }
    }
}
