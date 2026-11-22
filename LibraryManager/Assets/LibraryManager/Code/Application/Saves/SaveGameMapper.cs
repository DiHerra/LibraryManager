using System;
using System.Globalization;
using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Common;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Purchasing;
using LibraryManager.Domain.Stock;
using LibraryManager.Domain.Store;
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

            SaveGameData data = new SaveGameData
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
                    events = world.Events.Pending.Select(item => ToData(item)).ToList(),
                    nextEventId = world.Events.NextId,
                    catalog = ToData(world.Catalog),
                    stock = world.Inventory.Lots.Select(item => ToData(item)).ToList(),
                    orders = world.Orders.All.Select(item => ToData(item)).ToList(),
                    nextOrderId = world.Orders.NextId
                }
            };
            WriteLayout(data.world, world.Layout);
            return data;
        }

        public static void WriteLayout(WorldData world, StoreLayout layout)
        {
            world.fixtures = layout.Fixtures.Select(f => new FixtureData
            {
                id = f.Id,
                kind = (int)f.Kind,
                name = f.Name,
                capacity = f.Capacity,
                section = f.Section.HasValue ? (int)f.Section.Value : -1,
                x = f.X,
                y = f.Y,
                width = f.Width,
                height = f.Height
            }).ToList();
            world.placements = layout.Placements.Select(p => new PlacementData
            {
                fixtureId = p.FixtureId,
                bookId = p.BookId,
                quantity = p.Quantity
            }).ToList();
        }

        public static Inventory ToInventory(System.Collections.Generic.IEnumerable<StockLotData> stock, BookCatalog catalog)
        {
            if (stock == null)
            {
                throw new ArgumentException("Falta el inventario.");
            }

            return new Inventory(stock.Select(lot => ToLot(lot, catalog)).ToList());
        }

        private static StoreLayout ToLayout(WorldData world, BookCatalog catalog, Inventory inventory)
        {
            if (world.fixtures == null || world.placements == null)
            {
                throw new ArgumentException("Faltan los muebles de la tienda.");
            }

            var layout = new StoreLayout(
                world.fixtures.Select(f => new Fixture(
                    f.id,
                    ToEnum<FixtureKind>(f.kind),
                    f.name,
                    f.capacity,
                    f.section >= 0 ? ToEnum<Genre>(f.section) : (Genre?)null,
                    f.x,
                    f.y,
                    f.width,
                    f.height)).ToList(),
                world.placements.Select(p =>
                {
                    catalog.GetBook(p.bookId);
                    return new Placement(p.fixtureId, p.bookId, p.quantity);
                }).ToList());

            foreach (int bookId in layout.Placements.Select(p => p.BookId).Distinct())
            {
                if (layout.DisplayedQuantity(bookId) > inventory.Quantity(bookId))
                {
                    throw new ArgumentException($"Hay más ejemplares expuestos del libro {bookId} que en el inventario.");
                }
            }

            return layout;
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
                anchorDay = worldEvent.AnchorDay,
                referenceId = worldEvent.ReferenceId
            };
        }

        public static StockLotData ToData(StockLot lot)
        {
            return new StockLotData
            {
                bookId = lot.BookId,
                quantity = lot.Quantity,
                unitCostCents = lot.UnitCost.Cents,
                receivedDayNumber = lot.ReceivedDate.DayNumber,
                returnDeadlineDayNumber = lot.ReturnDeadline?.DayNumber ?? -1
            };
        }

        public static PurchaseOrderData ToData(PurchaseOrder order)
        {
            return new PurchaseOrderData
            {
                id = order.Id,
                distributorId = order.DistributorId,
                orderDayNumber = order.OrderDate.DayNumber,
                expectedDayNumber = order.ExpectedDate.DayNumber,
                status = (int)order.Status,
                lines = order.Lines.Select(line => new OrderLineData
                {
                    bookId = line.BookId,
                    quantity = line.Quantity,
                    unitCostCents = line.UnitCost.Cents
                }).ToList()
            };
        }

        private static StockLot ToLot(StockLotData data, BookCatalog catalog)
        {
            if (data == null)
            {
                throw new ArgumentException("Hay un lote vacío.");
            }

            catalog.GetBook(data.bookId);
            return new StockLot(
                data.bookId,
                data.quantity,
                new Money(data.unitCostCents),
                GameDate.FromDayNumber(data.receivedDayNumber),
                data.returnDeadlineDayNumber >= 0 ? GameDate.FromDayNumber(data.returnDeadlineDayNumber) : (GameDate?)null);
        }

        private static PurchaseOrder ToOrder(PurchaseOrderData data, BookCatalog catalog)
        {
            if (data == null || data.lines == null)
            {
                throw new ArgumentException("Hay un pedido vacío.");
            }

            catalog.GetDistributor(data.distributorId);
            return new PurchaseOrder(
                data.id,
                data.distributorId,
                data.lines.Select(line =>
                {
                    catalog.GetBook(line.bookId);
                    return new OrderLine(line.bookId, line.quantity, new Money(line.unitCostCents));
                }),
                GameDate.FromDayNumber(data.orderDayNumber),
                GameDate.FromDayNumber(data.expectedDayNumber),
                ToEnum<PurchaseOrderStatus>(data.status));
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
                if (world.stock == null || world.orders == null)
                {
                    throw new ArgumentException("Faltan el inventario o los pedidos.");
                }

                Inventory inventory = ToInventory(world.stock, catalog);
                var orders = new PurchaseOrderBook(world.orders.Select(order => ToOrder(order, catalog)).ToList(), world.nextOrderId);

                return new WorldState(
                    bookstore,
                    GameDate.FromDayNumber(world.dateDayNumber),
                    DeterministicRandom.FromState(randomState),
                    schedule,
                    events,
                    catalog,
                    inventory,
                    orders,
                    ToLayout(world, catalog, inventory));
            }
            catch (ArgumentException exception)
            {
                throw new SaveGameException("La partida contiene datos no válidos: " + exception.Message, exception);
            }
            catch (System.Collections.Generic.KeyNotFoundException exception)
            {
                throw new SaveGameException("La partida hace referencia a algo que no existe: " + exception.Message, exception);
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
                data.anchorDay,
                data.referenceId);
        }
    }
}
