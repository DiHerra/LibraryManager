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

        // Versión 3 (L03)
        public CatalogData catalog;
    }

    [Serializable]
    public sealed class CatalogData
    {
        public List<DistributorData> distributors = new List<DistributorData>();
        public List<PublisherData> publishers = new List<PublisherData>();
        public List<BookData> books = new List<BookData>();
    }

    [Serializable]
    public sealed class DistributorData
    {
        public int id;
        public string name;
        public int leadTimeDays;
        public long minimumOrderCents;
        public int returnWindowDays;
    }

    [Serializable]
    public sealed class PublisherData
    {
        public int id;
        public string name;
        public int size;
        public List<int> focusGenres = new List<int>();
        public int distributorId;
        public int tradeDiscountBasisPoints;
    }

    [Serializable]
    public sealed class BookData
    {
        public int id;
        public string title;
        public string author;
        public int publisherId;
        public int genre;
        public int format;
        public long priceCents;
        public int publicationDayNumber;
        public int basePopularity;
        public int halfLifeDays;
        public int backlistFloorPercent;
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
