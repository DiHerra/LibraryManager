using System;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.Sales
{
    /// <summary>Encargo de un cliente: un ejemplar de un título que no había en la tienda.</summary>
    public sealed class SpecialOrder
    {
        /// <summary>Días que el cliente espera antes de desistir.</summary>
        public const int PatienceDays = 30;

        public SpecialOrder(int id, int bookId, GameDate requestedOn, GameDate expiresOn)
        {
            Id = id > 0 ? id : throw new ArgumentOutOfRangeException(nameof(id));
            BookId = bookId;
            RequestedOn = requestedOn;
            if (expiresOn < requestedOn)
            {
                throw new ArgumentException("El encargo no puede caducar antes de hacerse.", nameof(expiresOn));
            }

            ExpiresOn = expiresOn;
        }

        public int Id { get; }
        public int BookId { get; }
        public GameDate RequestedOn { get; }

        /// <summary>Último día en que el cliente aún pasa a recogerlo.</summary>
        public GameDate ExpiresOn { get; }

        public bool IsExpiredOn(GameDate date) => date > ExpiresOn;

        public int DaysLeft(GameDate date) => Math.Max(0, date.DaysUntil(ExpiresOn));
    }
}
