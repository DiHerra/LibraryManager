using System;
using System.Linq;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Time;
using LibraryManager.Domain.World;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class WorldClockTests
    {
        [Test]
        public void NewWorld_SchedulesMonthlyRent()
        {
            WorldState world = WorldState.CreateNew("Libros", 1);

            WorldEvent rent = world.Events.Pending.Single();
            Assert.AreEqual(GameDate.Create(2026, 10, 1), rent.Date);
            Assert.AreEqual(WorldState.DefaultMonthlyRent, rent.Amount);
            Assert.AreEqual(EventRecurrence.Monthly, rent.Recurrence);
        }

        [Test]
        public void RentDay_PaysFromCashAndSchedulesNextMonth()
        {
            WorldState world = WorldState.CreateNew("Libros", 1);
            AdvanceUntil(world, GameDate.Create(2026, 9, 30));
            Money cashBefore = world.Bookstore.Cash;

            DayReport report = world.AdvanceDay();

            // El 30 de septiembre abre: la caja recibe sus ventas y paga el alquiler del 1 de octubre.
            Assert.AreEqual(GameDate.Create(2026, 10, 1), report.Date);
            Assert.IsNotNull(report.Sales);
            Assert.AreEqual(cashBefore - WorldState.DefaultMonthlyRent + report.Sales.Revenue, world.Bookstore.Cash);
            Assert.AreEqual(report.Sales.Revenue - WorldState.DefaultMonthlyRent, report.CashChange);
            Assert.AreEqual(GameDate.Create(2026, 11, 1), world.Events.Pending.Single().Date);
        }

        [Test]
        public void AdvanceToNextWeek_StopsOnMonday()
        {
            WorldState world = WorldState.CreateNew("Libros", 1); // martes 1 de septiembre

            AdvanceResult result = world.AdvanceToNextWeek();

            Assert.AreEqual(6, result.Days.Count);
            Assert.AreEqual(DayOfWeek.Monday, world.Date.DayOfWeek);
            Assert.AreEqual(5, result.DaysOpen, "Del 2 al 7 de septiembre abre todos los días salvo el domingo.");
            Assert.IsFalse(result.StoppedEarly);
        }

        [Test]
        public void AdvanceToNextWeek_FromMonday_TakesAWholeWeek()
        {
            WorldState world = WorldState.CreateNew("Libros", 1);
            world.AdvanceToNextWeek();

            Assert.AreEqual(7, world.AdvanceToNextWeek().Days.Count);
        }

        [Test]
        public void EventThatPauses_StopsTheAdvanceThatDay()
        {
            WorldState world = WorldState.CreateNew("Libros", 1);
            world.Events.Schedule(world.Date.AddDays(2), WorldEventKind.Reminder, "Llega un pedido", pausesAdvance: true);

            AdvanceResult result = world.AdvanceDays(10);

            Assert.AreEqual(2, result.Days.Count);
            Assert.IsTrue(result.StoppedEarly);
            Assert.AreEqual(PauseReasonKind.EventNeedsAttention, result.PauseReasons.Single().Kind);
        }

        [Test]
        public void SeasonStart_PausesTheAdvance()
        {
            WorldState world = WorldState.CreateNew("Libros", 1);
            AdvanceUntil(world, GameDate.Create(2026, 11, 30));

            DayReport report = world.AdvanceDay();

            Assert.AreEqual(CommercialSeason.Christmas, report.Season);
            Assert.AreEqual(PauseReasonKind.SeasonStarted, report.PauseReasons.Single().Kind);
        }

        [Test]
        public void CashGoingNegative_PausesOnce()
        {
            WorldState world = WorldState.CreateNew("Libros", 1);
            world.Events.Schedule(world.Date.AddDays(1), WorldEventKind.Payment, "Reforma", Money.FromEuros(40000m));

            DayReport firstDay = world.AdvanceDay();
            DayReport secondDay = world.AdvanceDay();

            Assert.IsTrue(firstDay.PauseReasons.Any(p => p.Kind == PauseReasonKind.CashBecameNegative));
            Assert.IsFalse(secondDay.ShouldPause);
        }

        [Test]
        public void SameSeedAndActions_GiveTheSameYear()
        {
            WorldState first = WorldState.CreateNew("Libros", 9);
            WorldState second = WorldState.CreateNew("Libros", 9);

            Money revenue = Money.Zero;
            for (int i = 0; i < 365; i++)
            {
                revenue += first.AdvanceDay().Sales?.Revenue ?? Money.Zero;
                second.AdvanceDay();
            }

            Assert.AreEqual(first.Date, second.Date);
            Assert.AreEqual(first.Bookstore.Cash, second.Bookstore.Cash);
            Assert.AreEqual(first.Events.NextId, second.Events.NextId);
            Assert.AreEqual(first.Sales.Reputation, second.Sales.Reputation);
            Assert.AreEqual(WorldState.DefaultStartingCash - WorldState.DefaultMonthlyRent * 12 + revenue, first.Bookstore.Cash);
        }

        [Test]
        public void AdvanceDays_RejectsInvalidCounts()
        {
            WorldState world = WorldState.CreateNew("Libros", 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => world.AdvanceDays(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => world.AdvanceDays(WorldState.MaxDaysPerAdvance + 1));
        }

        private static void AdvanceUntil(WorldState world, GameDate date)
        {
            while (world.Date < date)
            {
                world.AdvanceDay();
            }
        }
    }
}
