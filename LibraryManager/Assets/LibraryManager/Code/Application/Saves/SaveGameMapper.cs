using System;
using System.Globalization;
using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Common;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Store;
using LibraryManager.Domain.Time;
using LibraryManager.Domain.World;

namespace LibraryManager.Application.Saves
{
    /// <summary>Convierte entre el estado del dominio y el formato persistente.</summary>
    public static class SaveGameMapper
    {
        public static SaveGameData ToData(WorldState world, DateTime savedAtUtc)
        {
            Guard.NotNull(world, nameof(world));

            return new SaveGameData
            {
                schemaVersion = SaveGameSchema.CurrentVersion,
                savedAtUtc = savedAtUtc.ToString("o", CultureInfo.InvariantCulture),
                world = new WorldData
                {
                    dateDayNumber = world.Date.DayNumber,
                    randomState = world.Random.State.ToString(CultureInfo.InvariantCulture),
                    bookstore = new BookstoreData
                    {
                        name = world.Bookstore.Name,
                        cashCents = world.Bookstore.Cash.Cents
                    },
                    schedule = new ScheduleData
                    {
                        openDays = world.Schedule.OpenDays.Select(day => (int)day).ToList(),
                        openingHour = world.Schedule.OpeningHour,
                        closingHour = world.Schedule.ClosingHour
                    },
                    events = world.Events.Pending.Select(ToData).ToList(),
                    nextEventId = world.Events.NextId,
                    catalog = ToData(world.Catalog)
                }
            };
        }

        public static WorldEventData ToData(WorldEvent worldEvent)
        {
            return new WorldEventData
            {
                id = worldEvent.Id,
                dayNumber = worldEvent.Date.DayNumber,
                kind = (int)worldEvent.Kind,
                title = worldEvent.Title,
                amountCents = worldEvent.Amount.Cents,
                recurrence = (int)worldEvent.Recurrence,
                pausesAdvance = worldEvent.PausesAdvance,
                anchorDay = worldEvent.AnchorDay
            };
        }

        public static CatalogData ToData(BookCatalog catalog)
        {
            return new CatalogData
            {
                distributors = catalog.Distributors.Select(d => new DistributorData
                {
                    id = d.Id,
                    name = d.Name,
                    leadTimeDays = d.LeadTimeDays,
                    minimumOrderCents = d.MinimumOrder.Cents,
                    returnWindowDays = d.ReturnWindowDays
                }).ToList(),
                publishers = catalog.Publishers.Select(p => new PublisherData
                {
                    id = p.Id,
                    name = p.Name,
                    size = (int)p.Size,
                    focusGenres = p.FocusGenres.Select(g => (int)g).ToList(),
                    distributorId = p.DistributorId,
                    tradeDiscountBasisPoints = p.TradeDiscountBasisPoints
                }).ToList(),
                books = catalog.Books.Select(b => new BookData
                {
                    id = b.Id,
                    title = b.Title,
                    author = b.Author,
                    publisherId = b.PublisherId,
                    genre = (int)b.Genre,
                    format = (int)b.Format,
                    priceCents = b.Price.Cents,
                    publicationDayNumber = b.PublicationDate.DayNumber,
                    basePopularity = b.BasePopularity,
                    halfLifeDays = b.HalfLifeDays,
                    backlistFloorPercent = b.BacklistFloorPercent
                }).ToList()
            };
        }

        public static BookCatalog ToCatalog(CatalogData data)
        {
            if (data == null || data.distributors == null || data.publishers == null || data.books == null)
            {
                throw new ArgumentException("La partida no contiene el catálogo.");
            }

            return new BookCatalog(
                data.distributors.Select(d => new Distributor(d.id, d.name, d.leadTimeDays, new Money(d.minimumOrderCents), d.returnWindowDays)),
                data.publishers.Select(p => new Publisher(
                    p.id,
                    p.name,
                    ToEnum<PublisherSize>(p.size),
                    (p.focusGenres ?? new System.Collections.Generic.List<int>()).Select(ToEnum<Genre>),
                    p.distributorId,
                    p.tradeDiscountBasisPoints)),
                data.books.Select(b => new Book(
                    b.id,
                    b.title,
                    b.author,
                    b.publisherId,
                    ToEnum<Genre>(b.genre),
                    ToEnum<BookFormat>(b.format),
                    new Money(b.priceCents),
                    GameDate.FromDayNumber(b.publicationDayNumber),
                    b.basePopularity,
                    b.halfLifeDays,
                    b.backlistFloorPercent)));
        }

        private static T ToEnum<T>(int value) where T : struct, Enum
        {
            if (!Enum.IsDefined(typeof(T), value))
            {
                throw new ArgumentException($"Valor {value} no válido para {typeof(T).Name}.");
            }

            return (T)(object)value;
        }

        public static WorldState ToWorld(SaveGameData data)
        {
            SaveGameData current = SaveGameMigrator.MigrateToCurrent(data);
            WorldData world = current.world;

            if (world.bookstore == null || world.schedule == null || world.events == null)
            {
                throw new SaveGameException("La partida no contiene la librería, el horario o los eventos.");
            }

            if (!ulong.TryParse(world.randomState, NumberStyles.None, CultureInfo.InvariantCulture, out ulong randomState))
            {
                throw new SaveGameException("El estado del generador aleatorio no es válido.");
            }

            try
            {
                var bookstore = new Bookstore(world.bookstore.name, new Money(world.bookstore.cashCents));
                var schedule = new StoreSchedule(
                    world.schedule.openDays.Select(ToDayOfWeek),
                    world.schedule.openingHour,
                    world.schedule.closingHour);
                var events = new WorldEventQueue(world.events.Select(ToEvent), world.nextEventId);
                BookCatalog catalog = ToCatalog(world.catalog);

                return new WorldState(
                    bookstore,
                    GameDate.FromDayNumber(world.dateDayNumber),
                    DeterministicRandom.FromState(randomState),
                    schedule,
                    events,
                    catalog);
            }
            catch (ArgumentException exception)
            {
                throw new SaveGameException("La partida contiene datos no válidos: " + exception.Message, exception);
            }
        }

        private static DayOfWeek ToDayOfWeek(int value)
        {
            if (value < 0 || value > 6)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Día de la semana no válido.");
            }

            return (DayOfWeek)value;
        }

        private static WorldEvent ToEvent(WorldEventData data)
        {
            if (data == null)
            {
                throw new ArgumentException("Hay un evento vacío.");
            }

            if (!Enum.IsDefined(typeof(WorldEventKind), data.kind) || !Enum.IsDefined(typeof(EventRecurrence), data.recurrence))
            {
                throw new ArgumentException($"El evento {data.id} tiene un tipo o una repetición desconocidos.");
            }

            return new WorldEvent(
                data.id,
                GameDate.FromDayNumber(data.dayNumber),
                (WorldEventKind)data.kind,
                data.title,
                new Money(data.amountCents),
                (EventRecurrence)data.recurrence,
                data.pausesAdvance,
                data.anchorDay);
        }
    }
}
