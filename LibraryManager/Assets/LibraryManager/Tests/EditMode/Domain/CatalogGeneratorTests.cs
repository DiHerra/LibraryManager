using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Time;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class CatalogGeneratorTests
    {
        private static readonly GameDate Start = GameDate.Create(2026, 9, 1);
        private static BookCatalog _catalog;

        private static BookCatalog Catalog => _catalog ??= CatalogGenerator.Generate(2026, Start);

        [Test]
        public void MeetsTheScopeQuantities()
        {
            Assert.That(Catalog.Books.Count, Is.GreaterThanOrEqualTo(600));
            Assert.AreEqual(20, Catalog.Publishers.Count);
            Assert.AreEqual(4, Catalog.Distributors.Count);
        }

        [Test]
        public void SameSeed_GivesTheSameCatalog_AndAnotherSeedChangesIt()
        {
            BookCatalog again = CatalogGenerator.Generate(2026, Start);
            BookCatalog other = CatalogGenerator.Generate(2027, Start);

            CollectionAssert.AreEqual(Catalog.Books.Select(Describe).ToList(), again.Books.Select(Describe).ToList());
            CollectionAssert.AreNotEqual(Catalog.Books.Select(Describe).ToList(), other.Books.Select(Describe).ToList());
        }

        [Test]
        public void TitlesAreUnique_AndEveryGenreAppears()
        {
            Assert.AreEqual(Catalog.Books.Count, Catalog.Books.Select(b => b.Title).Distinct().Count());
            foreach (Genre genre in System.Enum.GetValues(typeof(Genre)))
            {
                Assert.That(Catalog.Books.Count(b => b.Genre == genre), Is.GreaterThan(0), genre.ToString());
            }
        }

        [Test]
        public void PricesAreRetailPrices()
        {
            foreach (Book book in Catalog.Books)
            {
                long cents = book.Price.Cents % 100;
                Assert.That(cents == 95 || cents == 90, book.Title);
                Assert.That(book.Price.Cents, Is.InRange(400L, 4500L), book.Title);
            }
        }

        [Test]
        public void HasBacklistNewReleasesAndUpcomingTitles()
        {
            Assert.That(Catalog.Books.Count(b => b.PhaseOn(Start) == BookPhase.Upcoming), Is.GreaterThan(40));
            Assert.That(Catalog.Books.Count(b => b.PhaseOn(Start) == BookPhase.NewRelease), Is.GreaterThan(10));
            Assert.That(Catalog.Books.Count(b => b.PhaseOn(Start) == BookPhase.Backlist), Is.GreaterThan(300));
        }

        [Test]
        public void PopularityIsSkewed_FewBestsellers()
        {
            int bestsellers = Catalog.Books.Count(b => b.BasePopularity >= 70);
            int modest = Catalog.Books.Count(b => b.BasePopularity <= 30);

            Assert.That(bestsellers, Is.GreaterThan(0));
            Assert.That(bestsellers, Is.LessThan(Catalog.Books.Count / 10));
            Assert.That(modest, Is.GreaterThan(Catalog.Books.Count / 2));
        }

        [Test]
        public void PublishersHaveRealisticDiscounts()
        {
            foreach (Publisher publisher in Catalog.Publishers)
            {
                Assert.That(publisher.TradeDiscountBasisPoints, Is.InRange(2800, 3700), publisher.Name);
            }
        }

        [Test]
        public void SchoolBooksComeOutInSummer()
        {
            foreach (Book book in Catalog.Books.Where(b => b.Genre == Genre.School))
            {
                Assert.That(book.PublicationDate.Month, Is.InRange(6, 7), book.Title);
            }
        }

        private static string Describe(Book book)
        {
            return $"{book.Id}|{book.Title}|{book.Author}|{book.PublisherId}|{book.Genre}|{book.Format}|{book.Price.Cents}|{book.PublicationDate.DayNumber}|{book.BasePopularity}|{book.HalfLifeDays}|{book.BacklistFloorPercent}";
        }
    }
}
