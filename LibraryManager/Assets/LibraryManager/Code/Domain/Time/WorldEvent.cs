using System;
using LibraryManager.Domain.Common;
using LibraryManager.Domain.Economy;

namespace LibraryManager.Domain.Time
{
    public enum WorldEventKind
    {
        /// <summary>Aviso sin efecto en la partida.</summary>
        Reminder = 0,
        /// <summary>Pago que sale de la caja (alquiler, suministros…).</summary>
        Payment = 1,
        /// <summary>Llega un pedido; <see cref="WorldEvent.ReferenceId"/> es su número.</summary>
        OrderDelivery = 2
    }

    public enum EventRecurrence
    {
        None = 0,
        Weekly = 1,
        Monthly = 2
    }

    /// <summary>Algo que ocurre en una fecha concreta del mundo.</summary>
    public sealed class WorldEvent
    {
        public WorldEvent(
            long id,
            GameDate date,
            WorldEventKind kind,
            string title,
            Money amount,
            EventRecurrence recurrence,
            bool pausesAdvance,
            int anchorDay = 0,
            long referenceId = 0)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "El identificador debe ser positivo.");
            }

            Guard.NotNegative(amount.Cents, nameof(amount));

            Id = id;
            Date = date;
            Kind = kind;
            Title = Guard.NotBlank(title, nameof(title));
            Amount = amount;
            Recurrence = recurrence;
            PausesAdvance = pausesAdvance;
            ReferenceId = referenceId >= 0 ? referenceId : throw new ArgumentOutOfRangeException(nameof(referenceId));
            AnchorDay = anchorDay > 0 ? Guard.InRange(anchorDay, 1, 31, nameof(anchorDay)) : date.Day;
        }

        public long Id { get; }
        public GameDate Date { get; }
        public WorldEventKind Kind { get; }
        public string Title { get; }
        public Money Amount { get; }
        public EventRecurrence Recurrence { get; }
        public bool PausesAdvance { get; }

        /// <summary>Día del mes original de un evento mensual (31 se mantiene aunque febrero lo recorte).</summary>
        public int AnchorDay { get; }

        /// <summary>Objeto al que se refiere el evento (por ejemplo, el número de pedido); 0 si ninguno.</summary>
        public long ReferenceId { get; }

        public bool IsRecurring => Recurrence != EventRecurrence.None;

        /// <summary>Siguiente repetición, o null si no se repite.</summary>
        public WorldEvent NextOccurrence(long newId)
        {
            switch (Recurrence)
            {
                case EventRecurrence.Weekly:
                    return new WorldEvent(newId, Date.AddDays(7), Kind, Title, Amount, Recurrence, PausesAdvance, AnchorDay, ReferenceId);
                case EventRecurrence.Monthly:
                    DateTime next = Date.ToDateTime().AddMonths(1);
                    int day = Math.Min(AnchorDay, DateTime.DaysInMonth(next.Year, next.Month));
                    return new WorldEvent(newId, GameDate.Create(next.Year, next.Month, day), Kind, Title, Amount, Recurrence, PausesAdvance, AnchorDay, ReferenceId);
                default:
                    return null;
            }
        }
    }
}
