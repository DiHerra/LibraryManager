using System;
using System.Linq;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Time;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class WorldEventQueueTests
    {
        private static readonly GameDate Start = GameDate.Create(2026, 9, 1);

        [Test]
        public void Events_AreOrderedByDateThenCreation()
        {
            var queue = new WorldEventQueue();
            queue.Schedule(Start.AddDays(2), WorldEventKind.Reminder, "C");
            queue.Schedule(Start.AddDays(1), WorldEventKind.Reminder, "A");
            queue.Schedule(Start.AddDays(1), WorldEventKind.Reminder, "B");

            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, queue.Pending.Select(e => e.Title).ToArray());
        }

        [Test]
        public void TakeDue_ReturnsOnlyEventsUpToTheDate()
        {
            var queue = new WorldEventQueue();
            queue.Schedule(Start, WorldEventKind.Reminder, "Hoy");
            queue.Schedule(Start.AddDays(1), WorldEventKind.Reminder, "Mañana");

            Assert.AreEqual(new[] { "Hoy" }, queue.TakeDue(Start).Select(e => e.Title).ToArray());
            Assert.AreEqual(1, queue.Pending.Count);
        }

        [Test]
        public void MonthlyEvent_KeepsItsDayAndClampsShortMonths()
        {
            var queue = new WorldEventQueue();
            queue.Schedule(GameDate.Create(2027, 1, 31), WorldEventKind.Payment, "Cuota", Money.FromEuros(10m), EventRecurrence.Monthly);

            queue.TakeDue(GameDate.Create(2027, 1, 31));
            Assert.AreEqual(GameDate.Create(2027, 2, 28), queue.Pending[0].Date);

            queue.TakeDue(GameDate.Create(2027, 2, 28));
            Assert.AreEqual(GameDate.Create(2027, 3, 31), queue.Pending[0].Date);
        }

        [Test]
        public void WeeklyEvent_RepeatsEverySevenDays()
        {
            var queue = new WorldEventQueue();
            queue.Schedule(Start, WorldEventKind.Reminder, "Club de lectura", recurrence: EventRecurrence.Weekly);

            queue.TakeDue(Start);

            Assert.AreEqual(Start.AddDays(7), queue.Pending.Single().Date);
        }

        [Test]
        public void RestoredQueue_KeepsIdsAndRejectsUsedNextId()
        {
            var queue = new WorldEventQueue();
            queue.Schedule(Start, WorldEventKind.Reminder, "A");
            queue.Schedule(Start, WorldEventKind.Reminder, "B");

            var restored = new WorldEventQueue(queue.Pending, queue.NextId);
            Assert.AreEqual(queue.NextId, restored.NextId);
            Assert.AreEqual(3, restored.Schedule(Start, WorldEventKind.Reminder, "C").Id);

            Assert.Throws<ArgumentOutOfRangeException>(() => new WorldEventQueue(queue.Pending, 2));
        }
    }
}
