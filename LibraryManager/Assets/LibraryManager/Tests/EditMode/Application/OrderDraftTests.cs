using System.Linq;
using LibraryManager.Application.Game;
using LibraryManager.Application.Purchasing;
using LibraryManager.Domain.Purchasing;
using LibraryManager.Domain.World;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Application
{
    public sealed class OrderDraftTests
    {
        [Test]
        public void Draft_GroupsByDistributorAndClampsQuantities()
        {
            WorldState world = WorldState.CreateNew("Libros", 8);
            var books = world.Catalog.Books.Where(b => b.IsReleased(world.Date)).Take(30).ToList();
            var draft = new OrderDraft();

            foreach (var book in books)
            {
                draft.Add(book.Id, 2);
            }

            draft.Add(books[0].Id, 500);
            draft.Add(books[1].Id, -10);

            Assert.AreEqual(PurchasingRules.MaxUnitsPerLine, draft.Quantity(books[0].Id));
            Assert.AreEqual(0, draft.Quantity(books[1].Id));
            var groups = draft.ByDistributor(world.Catalog);
            Assert.AreEqual(books.Count - 1, groups.Sum(g => g.Lines.Count));
            Assert.IsTrue(groups.All(g => g.Lines.All(line => world.Catalog.DistributorOf(line.Book).Id == g.Distributor.Id)));
        }

        [Test]
        public void SendDraftOrder_PlacesTheOrderAndClearsOnlyThatDistributor()
        {
            var session = new GameSession(new InMemorySaveGameRepository(), new FixedClock());
            session.StartNew("Libros", 8);
            WorldState world = session.World;
            var released = world.Catalog.Books.Where(b => b.IsReleased(world.Date)).ToList();
            foreach (var book in released.Take(60))
            {
                session.Draft.Add(book.Id, 10);
            }

            var group = session.Draft.ByDistributor(world.Catalog).First(g => g.MeetsMinimum);
            int otherUnits = session.Draft.ByDistributor(world.Catalog).Where(g => g.Distributor.Id != group.Distributor.Id).Sum(g => g.Units);

            PurchaseOrder order = session.SendDraftOrder(group.Distributor.Id);

            Assert.AreEqual(group.Units, order.TotalUnits);
            Assert.AreEqual(otherUnits, session.Draft.ByDistributor(world.Catalog).Sum(g => g.Units));
            Assert.Throws<PurchasingException>(() => session.SendDraftOrder(group.Distributor.Id));
            Assert.IsFalse(session.IsBusy);
        }
    }
}
