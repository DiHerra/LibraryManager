using System.Linq;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Purchasing;
using LibraryManager.Domain.Stock;
using LibraryManager.Domain.Time;
using LibraryManager.Domain.World;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class InventoryAlertsTests
    {
        [Test]
        public void NewWorld_HasNoMissingBestsellers()
        {
            WorldState world = WorldState.CreateNew("Libros", 3);
            Assert.IsEmpty(InventoryAlerts.MissingBestsellers(world.Catalog, world.Inventory, world.Orders, world.Date));
        }

        [Test]
        public void SoldOutBestseller_IsReported_UntilItIsOrdered()
        {
            WorldState world = WorldState.CreateNew("Libros", 3);
            var top = world.Catalog.MostPopular(world.Date, 1).Single();
            world.Liquidate(top.Id, world.Inventory.Quantity(top.Id));

            Assert.AreEqual(top.Id, InventoryAlerts.MissingBestsellers(world.Catalog, world.Inventory, world.Orders, world.Date).Single().Id);

            var distributor = world.Catalog.DistributorOf(top);
            world.PlaceOrder(distributor.Id, new[] { new OrderRequestLine(top.Id, 100) });
            Assert.IsEmpty(InventoryAlerts.MissingBestsellers(world.Catalog, world.Inventory, world.Orders, world.Date));
        }

        [Test]
        public void ExpiringReturns_AndStaleTitles()
        {
            var day = GameDate.Create(2026, 9, 1);
            var inventory = new Inventory();
            inventory.Add(new StockLot(1, 2, Money.FromEuros(10m), day, day.AddDays(10)));
            inventory.Add(new StockLot(2, 2, Money.FromEuros(10m), day, day.AddDays(60)));
            inventory.Add(new StockLot(3, 2, Money.FromEuros(10m), day.AddDays(-200), null));

            var expiring = InventoryAlerts.ExpiringReturns(inventory, day);
            Assert.AreEqual(1, expiring.Single().BookId);
            Assert.AreEqual(day.AddDays(10), expiring.Single().Deadline);

            CollectionAssert.AreEqual(new[] { 3 }, InventoryAlerts.StaleTitles(inventory, day).ToArray());
        }
    }
}
