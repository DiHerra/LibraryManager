using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Time;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class BookTests
    {
        private static readonly GameDate Release = GameDate.Create(2026, 9, 1);

        private static Book CreateBook(int popularity = 80, int halfLife = 30, int floor = 20)
        {
            return new Book(1, "El faro de la niebla", "Lucía Alcázar", 1, Genre.Fiction, BookFormat.Paperback,
                Money.FromEuros(19.95m), Release, popularity, halfLife, floor);
        }

        [Test]
        public void Phase_GoesFromUpcomingToNewReleaseToBacklist()
        {
            Book book = CreateBook();

            Assert.AreEqual(BookPhase.Upcoming, book.PhaseOn(Release.AddDays(-1)));
            Assert.AreEqual(BookPhase.NewRelease, book.PhaseOn(Release));
            Assert.AreEqual(BookPhase.NewRelease, book.PhaseOn(Release.AddDays(Book.NewReleaseDays)));
            Assert.AreEqual(BookPhase.Backlist, book.PhaseOn(Release.AddDays(Book.NewReleaseDays + 1)));
        }

        [Test]
        public void Popularity_PeaksOnRelease_HalvesTheNoveltyAndKeepsTheFloor()
        {
            Book book = CreateBook(popularity: 80, halfLife: 30, floor: 20);

            Assert.AreEqual(0d, book.PopularityOn(Release.AddDays(-1)));
            Assert.AreEqual(80d, book.PopularityOn(Release), 1e-9);
            // Suelo 16 + mitad de los 64 restantes.
            Assert.AreEqual(48d, book.PopularityOn(Release.AddDays(30)), 1e-9);
            Assert.AreEqual(16d, book.PopularityOn(Release.AddDays(3650)), 0.01);
        }

        [Test]
        public void InvalidBooks_AreRejected()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => CreateBook(popularity: 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => CreateBook(halfLife: 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new Book(1, "T", "A", 1, Genre.Fiction, BookFormat.Paperback,
                Money.Zero, Release, 10, 30, 10));
        }
    }
}
