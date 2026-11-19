using System;

namespace LibraryManager.Application.Game
{
    /// <summary>Hora real del sistema, inyectable para que las pruebas sean deterministas.</summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
