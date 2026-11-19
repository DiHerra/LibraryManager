using System;
using LibraryManager.Domain.Time;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class GameDateTests
    {
        [Test]
        public void Create_RoundTripsThroughDayNumber()
        {
            GameDate date = GameDate.Create(2026, 9, 1);
            GameDate copy = GameDate.FromDayNumber(date.DayNumber);

            Assert.AreEqual(date, copy);
            Assert.AreEqual(2026, copy.Year);
            Assert.AreEqual(9, copy.Month);
            Assert.AreEqual(1, copy.Day);
            Assert.AreEqual(DayOfWeek.Tuesday, copy.DayOfWeek);
        }

        [Test]
        public void AddDays_CrossesMonthsAndLeapYears()
        {
            Assert.AreEqual(GameDate.Create(2028, 2, 29), GameDate.Create(2028, 2, 28).AddDays(1));
            Assert.AreEqual(GameDate.Create(2027, 1, 1), GameDate.Create(2026, 12, 31).AddDays(1));
            Assert.AreEqual(365, GameDate.Create(2026, 1, 1).DaysUntil(GameDate.Create(2027, 1, 1)));
        }

        [Test]
        public void ToString_IsIsoDate()
        {
            Assert.AreEqual("2026-09-01", GameDate.Create(2026, 9, 1).ToString());
        }

        [Test]
        public void NegativeDayNumber_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GameDate.FromDayNumber(-1));
        }
    }
}
