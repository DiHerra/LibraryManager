using System;
using LibraryManager.Domain.Time;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class StoreScheduleTests
    {
        [Test]
        public void Default_OpensMondayToSaturday_TenToEight()
        {
            StoreSchedule schedule = StoreSchedule.CreateDefault();

            Assert.AreEqual(6, schedule.OpenDays.Count);
            Assert.IsFalse(schedule.OpensOn(DayOfWeek.Sunday));
            Assert.AreEqual(10, schedule.HoursOpen);
        }

        [Test]
        public void IsOpen_ClosesOnSundaysAndHolidays()
        {
            StoreSchedule schedule = StoreSchedule.CreateDefault();

            Assert.IsTrue(schedule.IsOpen(GameDate.Create(2026, 9, 1)));    // martes
            Assert.IsFalse(schedule.IsOpen(GameDate.Create(2026, 9, 6)));   // domingo
            Assert.IsFalse(schedule.IsOpen(GameDate.Create(2026, 10, 12))); // lunes festivo
            Assert.IsFalse(schedule.IsOpen(GameDate.Create(2026, 12, 25))); // viernes festivo
        }

        [Test]
        public void InvalidSchedules_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new StoreSchedule(new DayOfWeek[0], 10, 20));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StoreSchedule(new[] { DayOfWeek.Monday }, 20, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StoreSchedule(new[] { DayOfWeek.Monday }, 10, 25));
        }
    }
}
