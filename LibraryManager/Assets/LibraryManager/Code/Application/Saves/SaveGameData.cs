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

        // Versión 4 (L04)
        public List<StockLotData> stock = new List<StockLotData>();
        public List<PurchaseOrderData> orders = new List<PurchaseOrderData>();
        public int nextOrderId;

        // Versión 5 (L05)
        public List<FixtureData> fixtures = new List<FixtureData>();
        public List<PlacementData> placements = new List<PlacementData>();
    }

    [Serializable]
    public sealed class FixtureData
    {
        public int id;
        public int kind;
        public string name;
        public int capacity;
        /// <summary>Género de la sección; -1 si no es estantería.</summary>
        public int section = -1;
        public int x;
        public int y;
        public int width;
        public int height;
    }

    [Serializable]
    public sealed class PlacementData
    {
        public int fixtureId;
        public int bookId;
        public int quantity;
    }

    [Serializable]
    public sealed class StockLotData
    {
        public int bookId;
        public int quantity;
        public long unitCostCents;
        public int receivedDayNumber;
        /// <summary>-1 si el lote no se puede devolver.</summary>
        public int returnDeadlineDayNumber = -1;
    }

    [Serializable]
    public sealed class PurchaseOrderData
    {
        public int id;
        public int distributorId;
        public int orderDayNumber;
        public int expectedDayNumber;
        public int status;
        public List<OrderLineData> lines = new List<OrderLineData>();
    }

    [Serializable]
    public sealed class OrderLineData
    {
        public int bookId;
        public int quantity;
        public long unitCostCents;
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
        // Versión 4
        public long referenceId;
    }
}
