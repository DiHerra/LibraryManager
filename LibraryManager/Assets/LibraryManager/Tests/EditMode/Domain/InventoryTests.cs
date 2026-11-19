using System;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Stock;
using LibraryManager.Domain.Time;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class InventoryTests
    {
        private static readonly GameDate Day = GameDate.Create(2026, 9, 1);

        [Test]
        public void RemoveOldestFirst_UsesFifoCosts()
        {
            var inventory = new Inventory();
            inventory.Add(new StockLot(1, 2, Money.FromEuros(10m), Day.AddDays(5), null));
            inventory.Add(new StockLot(1, 3, Money.FromEuros(8m), Day, null));

            Money cost = inventory.RemoveOldestFirst(1, 4);

            Assert.AreEqual(Money.FromEuros(3 * 8m + 10m), cost);
            Assert.AreEqual(1, inventory.Quantity(1));
            Assert.AreEqual(1, inventory.Lots.Count);
        }

        [Test]
        public void RemoveOldestFirst_RejectsMoreThanAvailable()
        {
            var inventory = new Inventory();
            inventory.Add(new StockLot(1, 2, Money.FromEuros(10m), Day, null));

            Assert.Throws<InvalidOperationException>(() => inventory.RemoveOldestFirst(1, 3));
            Assert.AreEqual(2, inventory.Quantity(1));
        }

        [Test]
        public void RemoveReturnable_OnlyTakesLotsInsideTheirDeadline()
        {
            var inventory = new Inventory();
            inventory.Add(new StockLot(1, 2, Money.FromEuros(10m), Day, Day.AddDays(90)));
            inventory.Add(new StockLot(1, 4, Money.FromEuros(10m), Day, null));
            inventory.Add(new StockLot(2, 5, Money.FromEuros(10m), Day, Day.AddDays(10)));

            Assert.AreEqual(2, inventory.ReturnableQuantity(1, Day.AddDays(90)));
            Assert.AreEqual(0, inventory.ReturnableQuantity(2, Day.AddDays(11)));

            (int quantity, Money cost) = inventory.RemoveReturnable(1, Day.AddDays(30));
            Assert.AreEqual(2, quantity);
            Assert.AreEqual(Money.FromEuros(20m), cost);
            Assert.AreEqual(4, inventory.Quantity(1));
        }

        [Test]
        public void Totals_AddUpAllLots()
        {
            var inventory = new Inventory();
            inventory.Add(new StockLot(1, 2, Money.FromEuros(10m), Day, null));
            inventory.Add(new StockLot(2, 3, Money.FromEuros(5m), Day, null));

            Assert.AreEqual(5, inventory.TotalUnits);
            Assert.AreEqual(2, inventory.TitleCount);
            Assert.AreEqual(Money.FromEuros(35m), inventory.ValueAtCost);
        }
    }
}
