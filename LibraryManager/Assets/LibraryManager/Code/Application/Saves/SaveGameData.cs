using System;
using System.Collections.Generic;

namespace LibraryManager.Application.Saves
{
    /// <summary>
    /// Formato persistente de la partida. Solo campos públicos y tipos simples para
    /// que cualquier serializador JSON lo lea. Todo cambio sube <see cref="SaveGameSchema.CurrentVersion"/>
    /// y añade una migración con su prueba.
    /// </summary>
    [Serializable]
    public sealed class SaveGameData
    {
        public int schemaVersion;
        public string savedAtUtc;
        public WorldData world = new WorldData();
    }

    [Serializable]
    public sealed class WorldData
    {
        public int dateDayNumber;
        /// <summary>Estado del generador como texto: JSON no representa bien ulong grandes.</summary>
        public string randomState;
        public BookstoreData bookstore = new BookstoreData();

        // Versión 2 (L02)
        public ScheduleData schedule;
        public List<WorldEventData> events = new List<WorldEventData>();
        public long nextEventId;
    }

    [Serializable]
    public sealed class BookstoreData
    {
        public string name;
        public long cashCents;
    }

    [Serializable]
    public sealed class ScheduleData
    {
        /// <summary>Días de apertura como System.DayOfWeek (0 = domingo).</summary>
        public List<int> openDays = new List<int>();
        public int openingHour;
        public int closingHour;
    }

    [Serializable]
    public sealed class WorldEventData
    {
        public long id;
        public int dayNumber;
        public int kind;
        public string title;
        public long amountCents;
        public int recurrence;
        public bool pausesAdvance;
        public int anchorDay;
    }
}
