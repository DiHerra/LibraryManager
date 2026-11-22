using System;
using System.Linq;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Sales;
using LibraryManager.Domain.Stock;
using LibraryManager.Domain.Time;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class SalesLedgerTests
    {
        private static readonly GameDate Start = GameDate.Create(2026, 9, 1);

        private static DailySales Day(GameDate date, params SaleLine[] lines)
        {
            var day = new DailySales(date, 10, 10);
            day.SetLines(lines);
            day.Units = lines.Sum(line => line.Units);
            day.Revenue = lines.Aggregate(Money.Zero, (total, line) => total + line.Revenue);
            return day;
        }

        [Test]
        public void Ledger_KeepsDailySummariesAndTotalsPerTitle()
        {
            var ledger = new SalesLedger();
            ledger.Record(Day(Start, new SaleLine(1, 2, Money.FromEuros(40m)), new SaleLine(2, 1, Money.FromEuros(15m))));
            ledger.Record(Day(Start.AddDays(1), new SaleLine(1, 1, Money.FromEuros(20m))));

            Assert.AreEqual(2, ledger.Days.Count);
            Assert.AreEqual(3, ledger.TotalOf(1).Units);
            Assert.AreEqual(Money.FromEuros(60m), ledger.TotalOf(1).Revenue);
            Assert.AreEqual(Start.AddDays(1), ledger.LastSold(1));
            Assert.AreEqual(Start, ledger.LastSold(2));
            Assert.IsNull(ledger.LastSold(3));
            Assert.AreEqual(Money.FromEuros(75m), ledger.RevenueBetween(Start, Start.AddDays(1)));
            Assert.AreEqual(Start.AddDays(1), ledger.Latest.Date);
        }

        [Test]
        public void Ledger_KeepsOnlyTheLastFourHundredDays()
        {
            var ledger = new SalesLedger();
            for (int i = 0; i < SalesLedger.MaxDays + 20; i++)
            {
                ledger.Record(Day(Start.AddDays(i), new SaleLine(1, 1, Money.FromEuros(10m))));
            }

            Assert.AreEqual(SalesLedger.MaxDays, ledger.Days.Count);
            Assert.AreEqual(Start.AddDays(20), ledger.Days[0].Date);
            Assert.AreEqual(SalesLedger.MaxDays + 20, ledger.TotalOf(1).Units, "Los totales por título no se recortan.");
        }

        [Test]
        public void Ledger_RejectsDaysOutOfOrder()
        {
            var ledger = new SalesLedger();
            ledger.Record(Day(Start.AddDays(1)));

            Assert.Throws<ArgumentException>(() => ledger.Record(Day(Start.AddDays(1))));
            Assert.Throws<ArgumentException>(() => ledger.Record(Day(Start)));
        }

        [Test]
        public void LoyaltyDiscount_IsLimitedToTheLegalMaximum()
        {
            var sales = new SalesState();
            sales.SetLoyaltyDiscount(500);

            Assert.AreEqual(500, sales.LoyaltyDiscountBasisPoints);
            Assert.Throws<ArgumentOutOfRangeException>(() => sales.SetLoyaltyDiscount(501));
            Assert.Throws<ArgumentOutOfRangeException>(() => sales.SetLoyaltyDiscount(-1));
        }

        [Test]
        public void Reputation_MovesSlowlyTowardsSatisfaction()
        {
            var sales = new SalesState();

            sales.ApplySatisfaction(100);
            Assert.AreEqual(5100, sales.Reputation, "Un 2 % de la distancia de 50 a 100.");

            for (int i = 0; i < 500; i++)
            {
                sales.ApplySatisfaction(0);
            }

            Assert.That(sales.Reputation, Is.InRange(0, 30));
        }

        [Test]
        public void SpecialOrders_GetIncreasingIdsAndExpireAfterThirtyDays()
        {
            var sales = new SalesState();
            SpecialOrder first = sales.AddSpecialOrder(5, Start);
            SpecialOrder second = sales.AddSpecialOrder(5, Start);

            Assert.AreEqual(first.Id + 1, second.Id);
            Assert.AreEqual(2, sales.PendingSpecialOrders(5));
            Assert.AreEqual(Start.AddDays(SpecialOrder.PatienceDays), first.ExpiresOn);
            Assert.IsFalse(first.IsExpiredOn(Start.AddDays(30)));
            Assert.IsTrue(first.IsExpiredOn(Start.AddDays(31)));
            Assert.AreEqual(10, first.DaysLeft(Start.AddDays(20)));
        }

        [Test]
        public void StaleTitles_CountTheLastSaleAsMovement()
        {
            var inventory = new Inventory();
            inventory.Add(new StockLot(3, 2, Money.FromEuros(10m), Start, null));
            var ledger = new SalesLedger();
            ledger.Record(Day(Start.AddDays(100), new SaleLine(3, 1, Money.FromEuros(18m))));

            Assert.IsEmpty(InventoryAlerts.StaleTitles(inventory, Start.AddDays(250), ledger));
            CollectionAssert.AreEqual(new[] { 3 }, InventoryAlerts.StaleTitles(inventory, Start.AddDays(250)).ToArray());
            CollectionAssert.AreEqual(new[] { 3 }, InventoryAlerts.StaleTitles(inventory, Start.AddDays(281), ledger).ToArray());
        }
    }
}
