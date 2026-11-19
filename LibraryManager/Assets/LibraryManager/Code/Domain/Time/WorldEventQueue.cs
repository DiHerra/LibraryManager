using System;
using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Economy;

namespace LibraryManager.Domain.Time
{
    /// <summary>
    /// Eventos pendientes ordenados por fecha y, a igual fecha, por orden de creación.
    /// El orden es siempre el mismo, así que el resultado es reproducible.
    /// </summary>
    public sealed class WorldEventQueue
    {
        private readonly List<WorldEvent> _events = new List<WorldEvent>();

        public WorldEventQueue()
        {
            NextId = 1;
        }

        /// <summary>Restaura la cola desde una partida guardada.</summary>
        public WorldEventQueue(IEnumerable<WorldEvent> events, long nextId)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            foreach (WorldEvent worldEvent in events)
            {
                Insert(worldEvent);
            }

            long minimumNextId = _events.Count == 0 ? 1 : _events.Max(e => e.Id) + 1;
            if (nextId < minimumNextId)
            {
                throw new ArgumentOutOfRangeException(nameof(nextId), "El siguiente identificador ya está en uso.");
            }

            NextId = nextId;
        }

        public long NextId { get; private set; }

        public IReadOnlyList<WorldEvent> Pending => _events;

        public WorldEvent Schedule(
            GameDate date,
            WorldEventKind kind,
            string title,
            Money amount = default,
            EventRecurrence recurrence = EventRecurrence.None,
            bool pausesAdvance = false,
            long referenceId = 0)
        {
            var worldEvent = new WorldEvent(NextId++, date, kind, title, amount, recurrence, pausesAdvance, 0, referenceId);
            Insert(worldEvent);
            return worldEvent;
        }

        public IReadOnlyList<WorldEvent> Upcoming(int count)
        {
            return _events.Take(count).ToList();
        }

        /// <summary>
        /// Saca los eventos con fecha igual o anterior a <paramref name="date"/> y
        /// programa la siguiente repetición de los recurrentes.
        /// </summary>
        public IReadOnlyList<WorldEvent> TakeDue(GameDate date)
        {
            var due = new List<WorldEvent>();
            while (_events.Count > 0 && _events[0].Date <= date)
            {
                WorldEvent worldEvent = _events[0];
                _events.RemoveAt(0);
                due.Add(worldEvent);

                WorldEvent next = worldEvent.NextOccurrence(NextId);
                if (next != null)
                {
                    NextId++;
                    Insert(next);
                }
            }

            return due;
        }

        private void Insert(WorldEvent worldEvent)
        {
            if (_events.Any(existing => existing.Id == worldEvent.Id))
            {
                throw new ArgumentException($"Ya existe un evento con el identificador {worldEvent.Id}.", nameof(worldEvent));
            }

            int index = _events.FindIndex(existing =>
                existing.Date > worldEvent.Date ||
                (existing.Date == worldEvent.Date && existing.Id > worldEvent.Id));
            _events.Insert(index < 0 ? _events.Count : index, worldEvent);
        }
    }
}
