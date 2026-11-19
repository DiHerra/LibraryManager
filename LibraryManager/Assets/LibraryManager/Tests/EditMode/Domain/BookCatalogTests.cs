using System;
using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Time;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class BookCatalogTests
    {
        private static readonly GameDate Today = GameDate.Create(2026, 9, 1);

        private static Distributor Distributor => new Distributor(1, "Cauce", 3, Money.FromEuros(100m), 90);
        private static Publisher Publisher => new Publisher(1, "Ediciones Trébol", PublisherSize.Small, new[] { Genre.Fiction }, 1, 3500);

        private static Book CreateBook(int id, int daysFromToday, int popularity, Genre genre = Genre.Fiction)
        {
            return new Book(id, "Libro " + id, "Autor", 1, genre, BookFormat.Paperback, Money.FromEuros(18.95m),
                Today.AddDays(daysFromToday), popularity, 60, 20);
        }

        [Test]
        public void MostPopular_OrdersByTodaysInterestAndSkipsUnreleased()
        {
            var catalog = new BookCatalog(new[] { Distributor }, new[] { Publisher }, new[]
            {
                CreateBook(1, -400, 90),   // superventas antiguo
                CreateBook(2, -1, 60),     // novedad
                CreateBook(3, 5, 100),     // aún no ha salido
                CreateBook(4, -10, 40, Genre.Poetry)
            });

            List<int> ids = catalog.MostPopular(Today, 10).Select(b => b.Id).ToList();
            CollectionAssert.AreEqual(new[] { 2, 4, 1 }, ids);
            Assert.AreEqual(4, catalog.MostPopular(Today, 10, Genre.Poetry).Single().Id);
            Assert.AreEqual(3, catalog.UpcomingWithin(Today, 7).Single().Id);
        }

        [Test]
        public void BrokenReferences_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new BookCatalog(new Distributor[0], new[] { Publisher }, new Book[0]));
            Assert.Throws<ArgumentException>(() => new BookCatalog(new[] { Distributor }, new Publisher[0], new[] { CreateBook(1, 0, 10) }));
            Assert.Throws<ArgumentException>(() => new BookCatalog(new[] { Distributor, Distributor }, new Publisher[0], new Book[0]));
        }

        [Test]
        public void DistributorOf_FollowsThePublisher()
        {
            var catalog = new BookCatalog(new[] { Distributor }, new[] { Publisher }, new[] { CreateBook(1, 0, 10) });
            Assert.AreEqual("Cauce", catalog.DistributorOf(catalog.GetBook(1)).Name);
            Assert.Throws<KeyNotFoundException>(() => catalog.GetBook(99));
        }
    }
}
