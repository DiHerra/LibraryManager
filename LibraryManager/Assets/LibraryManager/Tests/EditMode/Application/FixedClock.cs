using System;
using LibraryManager.Application.Game;

namespace LibraryManager.Tests.EditMode.Application
{
    internal sealed class FixedClock : IClock
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 11, 19, 18, 0, 0, DateTimeKind.Utc);
    }
}
