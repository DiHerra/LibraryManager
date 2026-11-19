using LibraryManager.Domain.Time;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class CommercialCalendarTests
    {
        [TestCase(2026, 9, 15, CommercialSeason.BackToSchool)]
        [TestCase(2026, 8, 25, CommercialSeason.BackToSchool)]
        [TestCase(2026, 8, 24, CommercialSeason.Summer)]
        [TestCase(2026, 7, 1, CommercialSeason.Summer)]
        [TestCase(2026, 12, 1, CommercialSeason.Christmas)]
        [TestCase(2027, 1, 6, CommercialSeason.Christmas)]
        [TestCase(2027, 1, 7, CommercialSeason.Normal)]
        [TestCase(2027, 4, 23, CommercialSeason.BookDay)]
        [TestCase(2027, 4, 24, CommercialSeason.Normal)]
        [TestCase(2026, 10, 15, CommercialSeason.Normal)]
        public void GetSeason_MatchesTheCalendar(int year, int month, int day, CommercialSeason expected)
        {
            Assert.AreEqual(expected, CommercialCalendar.GetSeason(GameDate.Create(year, month, day)));
        }

        [Test]
        public void BookDay_HasTheStrongestDemand()
        {
            int bookDay = CommercialCalendar.DemandBasisPoints(CommercialSeason.BookDay);
            foreach (CommercialSeason season in System.Enum.GetValues(typeof(CommercialSeason)))
            {
                Assert.That(CommercialCalendar.DemandBasisPoints(season), Is.LessThanOrEqualTo(bookDay));
            }

            Assert.AreEqual(10000, CommercialCalendar.DemandBasisPoints(CommercialSeason.Normal));
        }
    }
}
