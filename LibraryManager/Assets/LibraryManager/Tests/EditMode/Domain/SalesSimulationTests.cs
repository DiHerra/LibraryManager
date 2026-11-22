using System;
using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Sales;
using LibraryManager.Domain.Stock;
using LibraryManager.Domain.Store;
using LibraryManager.Domain.Time;
using LibraryManager.Domain.World;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class SalesSimulationTests
    {
        [Test]
        public void OpenDay_SellsBooksAndTheNumbersAddUp()
        {
            WorldState world = WorldState.CreateNew("Libros", 2026);
            Money cashBefore = world.Bookstore.Cash;
            int unitsBefore = world.Inventory.TotalUnits;
            Money stockCostBefore = world.Inventory.ValueAtCost;

            DailySales sales = world.AdvanceDay().Sales;

            Assert.IsNotNull(sales, "El 1 de septiembre (martes) la tienda abre.");
            Assert.AreEqual(WorldState.DefaultStartDate, sales.Date);
            Assert.That(sales.Visitors, Is.GreaterThan(30));
            Assert.That(sales.Units, Is.GreaterThan(0));
            Assert.AreEqual(unitsBefore - sales.Units, world.Inventory.TotalUnits);
            Assert.AreEqual(stockCostBefore - sales.Cost, world.Inventory.ValueAtCost);
            Assert.AreEqual(cashBefore + sales.Revenue, world.Bookstore.Cash);
            Assert.AreEqual(sales.Units, sales.Lines.Sum(line => line.Units));
            Assert.AreEqual(sales.Revenue, sales.Lines.Aggregate(Money.Zero, (total, line) => total + line.Revenue));
            Assert.AreEqual(sales.Units, sales.UnitsByHour.Sum());
            Assert.AreEqual(sales.Units, sales.UnitsByGenre.Sum());
            Assert.That(sales.Cost, Is.GreaterThan(Money.Zero));
            Assert.That(sales.GrossMargin, Is.GreaterThan(Money.Zero), "Se vende por encima del coste.");
            Assert.That(sales.Buyers, Is.InRange(1, sales.Visitors + sales.SpecialOrdersDelivered));
            Assert.That(sales.SearchesFound, Is.LessThanOrEqualTo(sales.Searches));
            Assert.That(sales.SatisfactionPercent, Is.InRange(0, 100));
            Assert.AreSame(sales, world.Sales.Ledger.Latest);
            foreach (int bookId in world.Layout.Placements.Select(p => p.BookId).Distinct())
            {
                Assert.That(world.Layout.DisplayedQuantity(bookId), Is.LessThanOrEqualTo(world.Inventory.Quantity(bookId)));
            }
        }

        [Test]
        public void ClosedDay_HasNoSales()
        {
            WorldState world = WorldState.CreateNew("Libros", 2026);
            while (world.Date.DayOfWeek != DayOfWeek.Sunday)
            {
                world.AdvanceDay();
            }

            int days = world.Sales.Ledger.Days.Count;
            DayReport report = world.AdvanceDay();

            Assert.IsNull(report.Sales);
            Assert.AreEqual(days, world.Sales.Ledger.Days.Count);
        }

        [Test]
        public void EmptyShop_LosesSearchesTakesSpecialOrdersAndLosesReputation()
        {
            WorldState world = WorldState.CreateNew("Libros", 2026);
            foreach (int bookId in world.Inventory.BookIds.ToList())
            {
                world.Liquidate(bookId, world.Inventory.Quantity(bookId));
            }

            AdvanceResult result = world.AdvanceDays(6);

            Assert.AreEqual(0, result.UnitsSold);
            Assert.That(result.LostSearches, Is.GreaterThan(0));
            Assert.That(result.SpecialOrdersTaken, Is.GreaterThan(0));
            Assert.AreEqual(result.SpecialOrdersTaken, world.Sales.SpecialOrders.Count);
            Assert.That(world.Sales.Reputation, Is.LessThan(SalesState.DefaultReputation));
        }

        [Test]
        public void SpecialOrder_IsCollectedWhenTheBookArrives()
        {
            WorldState world = WorldState.CreateNew("Libros", 2026);
            Book book = NotInStock(world);
            SpecialOrder order = world.Sales.AddSpecialOrder(book.Id, world.Date);
            world.Inventory.Add(new StockLot(book.Id, 1, Money.FromEuros(5m), world.Date, null));

            DailySales sales = world.AdvanceDay().Sales;

            Assert.That(sales.SpecialOrdersDelivered, Is.GreaterThanOrEqualTo(1));
            Assert.IsFalse(world.Sales.SpecialOrders.Any(o => o.Id == order.Id));
            Assert.That(world.Sales.Ledger.TotalOf(book.Id).Units, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void SpecialOrder_ExpiresIfTheBookNeverArrives()
        {
            WorldState world = WorldState.CreateNew("Libros", 2026);
            Book book = NotInStock(world);
            SpecialOrder order = world.Sales.AddSpecialOrder(book.Id, world.Date);
            int expired = 0;

            for (int i = 0; i < SpecialOrder.PatienceDays + 3; i++)
            {
                expired += world.AdvanceDay().Sales?.SpecialOrdersExpired ?? 0;
            }

            Assert.IsFalse(world.Sales.SpecialOrders.Any(o => o.Id == order.Id));
            Assert.That(expired, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void CardDiscount_IsWhatCustomersStopPaying()
        {
            WorldState world = WorldState.CreateNew("Libros", 2026);
            world.SetLoyaltyDiscount(500);

            AdvanceResult result = world.AdvanceDays(5);

            Money listValue = result.Sales.SelectMany(day => day.Lines)
                .Aggregate(Money.Zero, (total, line) => total + world.Catalog.GetBook(line.BookId).Price * line.Units);
            Money discounts = result.Sales.Aggregate(Money.Zero, (total, day) => total + day.Discounts);
            Assert.That(discounts, Is.GreaterThan(Money.Zero));
            Assert.AreEqual(listValue - result.Revenue, discounts);
            Assert.That(discounts.Cents, Is.LessThanOrEqualTo(listValue.Percentage(500).Cents + result.UnitsSold));
        }

        [Test]
        public void DisplayedBooks_SellMoreThanBooksInTheBackroom()
        {
            WorldState displayed = WorldState.CreateNew("Libros", 77);
            WorldState backroom = WorldState.CreateNew("Libros", 77);
            foreach (Fixture fixture in backroom.Layout.Fixtures)
            {
                backroom.Layout.ClearFixture(fixture.Id);
            }

            int shownUnits = displayed.AdvanceDays(5).UnitsSold;
            int hiddenUnits = backroom.AdvanceDays(5).UnitsSold;

            Assert.That(shownUnits, Is.GreaterThan(hiddenUnits * 1.5), $"Expuestos {shownUnits}, almacén {hiddenUnits}.");
            Assert.That(hiddenUnits, Is.GreaterThan(0), "Quien busca un título lo encuentra aunque esté en el almacén.");
        }

        [Test]
        public void SameSeed_GivesTheSameSales()
        {
            WorldState first = WorldState.CreateNew("Libros", 31);
            WorldState second = WorldState.CreateNew("Libros", 31);

            first.AdvanceDays(20);
            second.AdvanceDays(20);

            Assert.AreEqual(first.Bookstore.Cash, second.Bookstore.Cash);
            Assert.AreEqual(first.Sales.Reputation, second.Sales.Reputation);
            Assert.AreEqual(first.Inventory.TotalUnits, second.Inventory.TotalUnits);
            Assert.AreEqual(first.Sales.SpecialOrders.Count, second.Sales.SpecialOrders.Count);
            CollectionAssert.AreEqual(
                first.Sales.Ledger.Days.Select(day => day.Units).ToList(),
                second.Sales.Ledger.Days.Select(day => day.Units).ToList());
        }

        private static Book NotInStock(WorldState world)
        {
            return world.Catalog.Books
                .Where(b => b.IsReleased(world.Date) && world.Inventory.Quantity(b.Id) == 0)
                .OrderBy(b => b.Id)
                .First();
        }
    }
}
