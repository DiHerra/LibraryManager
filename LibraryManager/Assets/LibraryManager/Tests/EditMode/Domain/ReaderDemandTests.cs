using System;
using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Common;
using LibraryManager.Domain.Customers;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Sales;
using LibraryManager.Domain.Time;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class ReaderDemandTests
    {
        [Test]
        public void ReaderShares_AddUpToOneHundred()
        {
            Assert.AreEqual(100, ReaderProfiles.All.Sum(profile => profile.SharePercent));
            Assert.AreEqual(5, ReaderProfiles.All.Select(profile => profile.Kind).Distinct().Count());
        }

        [Test]
        public void GiftBuyers_DoubleAtChristmasAndOnBookDay()
        {
            ReaderProfile gift = ReaderProfiles.Get(ReaderKind.Gift);
            ReaderProfile avid = ReaderProfiles.Get(ReaderKind.Avid);

            Assert.AreEqual(gift.SharePercent * 2, gift.ShareIn(CommercialSeason.Christmas));
            Assert.AreEqual(gift.SharePercent * 2, gift.ShareIn(CommercialSeason.BookDay));
            Assert.AreEqual(gift.SharePercent, gift.ShareIn(CommercialSeason.Summer));
            Assert.AreEqual(avid.SharePercent, avid.ShareIn(CommercialSeason.Christmas));
        }

        [Test]
        public void GenreWeights_FavourTastesAndSchoolBooksInSeptember()
        {
            ReaderProfile family = ReaderProfiles.Get(ReaderKind.Family);

            Assert.AreEqual(3, family.GenreWeight(Genre.Children, CommercialSeason.Normal));
            Assert.AreEqual(1, family.GenreWeight(Genre.Crime, CommercialSeason.Normal));
            Assert.AreEqual(12, family.GenreWeight(Genre.School, CommercialSeason.BackToSchool));
            Assert.AreEqual(4, ReaderProfiles.Get(ReaderKind.Casual).GenreWeight(Genre.School, CommercialSeason.BackToSchool));
        }

        [Test]
        public void ExpectedVisitors_DependOnWeekdaySeasonAndReputation()
        {
            GameDate tuesday = GameDate.Create(2026, 10, 13);
            GameDate saturday = GameDate.Create(2026, 10, 17);
            GameDate christmasTuesday = GameDate.Create(2026, 12, 15);

            Assert.AreEqual(FootTraffic.BaseVisitors * 0.9, FootTraffic.ExpectedVisitors(tuesday, 50), 0.0001);
            Assert.That(FootTraffic.ExpectedVisitors(saturday, 50), Is.GreaterThan(FootTraffic.ExpectedVisitors(tuesday, 50)));
            Assert.That(FootTraffic.ExpectedVisitors(christmasTuesday, 50), Is.GreaterThan(FootTraffic.ExpectedVisitors(tuesday, 50)));
            Assert.That(FootTraffic.ExpectedVisitors(tuesday, 90), Is.GreaterThan(FootTraffic.ExpectedVisitors(tuesday, 20)));
            Assert.AreEqual(0.6, FootTraffic.ReputationFactor(0), 0.0001);
            Assert.AreEqual(1.4, FootTraffic.ReputationFactor(100), 0.0001);
        }

        [Test]
        public void Visitors_StayWithinFifteenPercentAndAreReproducible()
        {
            GameDate date = GameDate.Create(2026, 10, 13);
            double expected = FootTraffic.ExpectedVisitors(date, 50);
            var first = new DeterministicRandom(4);
            var second = new DeterministicRandom(4);

            for (int i = 0; i < 200; i++)
            {
                int visitors = FootTraffic.Visitors(date, 50, first);
                Assert.AreEqual(visitors, FootTraffic.Visitors(date, 50, second));
                Assert.That(visitors, Is.InRange((int)Math.Floor(expected * 0.85), (int)Math.Ceiling(expected * 1.15)));
            }
        }

        [Test]
        public void ArrivalHours_FallWithinOpeningHours()
        {
            StoreSchedule schedule = StoreSchedule.CreateDefault();
            var random = new DeterministicRandom(8);
            var seen = new bool[24];

            for (int i = 0; i < 2000; i++)
            {
                int hour = FootTraffic.ArrivalHour(schedule, random);
                Assert.That(hour, Is.InRange(schedule.OpeningHour, schedule.ClosingHour - 1));
                seen[hour] = true;
            }

            Assert.IsTrue(Enumerable.Range(schedule.OpeningHour, schedule.HoursOpen).All(hour => seen[hour]));
        }

        [Test]
        public void CardDiscount_OnlyForCardholdersAndAtMostFivePercent()
        {
            Money price = Money.FromEuros(20m);

            Assert.AreEqual(Money.FromEuros(19m), SalesRules.PriceFor(price, 500, cardholder: true));
            Assert.AreEqual(price, SalesRules.PriceFor(price, 500, cardholder: false));
            Assert.AreEqual(price, SalesRules.PriceFor(price, 0, cardholder: true));
            Assert.AreEqual(500, SalesRules.MaxLoyaltyDiscountBasisPoints);
        }

        [Test]
        public void Vat_IsTheFourPercentIncludedInThePrice()
        {
            Assert.AreEqual(Money.FromEuros(4m), SalesRules.VatIncluded(Money.FromEuros(104m)));
            Assert.AreEqual(Money.FromEuros(0.77m), SalesRules.VatIncluded(Money.FromEuros(19.95m)));
        }

        [Test]
        public void BrowseBuyProbability_GrowsWithInterestVisibilityAndDiscount()
        {
            double shelf = SalesRules.BrowseBuyProbability(40, 10000, 0, false);
            double window = SalesRules.BrowseBuyProbability(40, 25000, 0, false);
            double obscure = SalesRules.BrowseBuyProbability(5, 10000, 0, false);
            double discounted = SalesRules.BrowseBuyProbability(40, 10000, 500, true);

            Assert.That(window, Is.GreaterThan(shelf));
            Assert.That(shelf, Is.GreaterThan(obscure));
            Assert.AreEqual(shelf * 1.2, discounted, 0.0001);
            Assert.That(SalesRules.BrowseBuyProbability(100, 25000, 0, false), Is.EqualTo(SalesRules.MaxBrowseBuyProbability));
        }
    }
}
