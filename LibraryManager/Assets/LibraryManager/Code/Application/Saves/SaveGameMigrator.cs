using System.Collections.Generic;
using System.Globalization;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Common;
using LibraryManager.Domain.Purchasing;
using System.Linq;
using LibraryManager.Domain.Time;
using LibraryManager.Domain.World;

namespace LibraryManager.Application.Saves
{
    /// <summary>
    /// Lleva una partida antigua a la versión actual, paso a paso. Compatibilidad hacia
    /// delante: el juego nuevo carga partidas anteriores, nunca al revés.
    /// </summary>
    public static class SaveGameMigrator
    {
        public static SaveGameData MigrateToCurrent(SaveGameData data)
        {
            if (data == null)
            {
                throw new SaveGameException("La partida está vacía.");
            }

            if (data.schemaVersion > SaveGameSchema.CurrentVersion)
            {
                throw new SaveGameException(
                    $"La partida es de la versión {data.schemaVersion} y este juego solo conoce hasta la {SaveGameSchema.CurrentVersion}.");
            }

            if (data.schemaVersion < SaveGameSchema.OldestSupportedVersion)
            {
                throw new SaveGameException($"La versión de partida {data.schemaVersion} no es válida.");
            }

            if (data.world == null)
            {
                throw new SaveGameException("La partida no contiene el mundo.");
            }

            if (data.schemaVersion == 1)
            {
                MigrateV1ToV2(data);
            }

            if (data.schemaVersion == 2)
            {
                MigrateV2ToV3(data);
            }

            if (data.schemaVersion == 3)
            {
                MigrateV3ToV4(data);
            }

            return data;
        }

        /// <summary>Añade el horario por defecto y el alquiler mensual desde el mes siguiente.</summary>
        private static void MigrateV1ToV2(SaveGameData data)
        {
            StoreSchedule schedule = StoreSchedule.CreateDefault();
            data.world.schedule = new ScheduleData
            {
                openDays = schedule.OpenDays.Select(day => (int)day).ToList(),
                openingHour = schedule.OpeningHour,
                closingHour = schedule.ClosingHour
            };

            var events = new WorldEventQueue();
            WorldState.ScheduleDefaultRent(events, GameDate.FromDayNumber(data.world.dateDayNumber));
            data.world.events = events.Pending.Select(item => SaveGameMapper.ToData(item)).ToList();
            data.world.nextEventId = events.NextId;
            data.schemaVersion = 2;
        }

        /// <summary>
        /// Genera el catálogo con una semilla derivada del estado del generador de la
        /// partida (sin consumirlo) y con la fecha de la partida como referencia.
        /// </summary>
        private static void MigrateV2ToV3(SaveGameData data)
        {
            if (!ulong.TryParse(data.world.randomState, NumberStyles.None, CultureInfo.InvariantCulture, out ulong state))
            {
                throw new SaveGameException("El estado del generador aleatorio no es válido.");
            }

            ulong catalogSeed = new DeterministicRandom(state ^ 0x5EED_CA7A_1060_0003UL).NextULong();
            BookCatalog catalog = CatalogGenerator.Generate(catalogSeed, GameDate.FromDayNumber(data.world.dateDayNumber));
            data.world.catalog = SaveGameMapper.ToData(catalog);
            data.schemaVersion = 3;
        }

        /// <summary>Da a la librería el fondo inicial (como en una partida nueva) y ningún pedido.</summary>
        private static void MigrateV3ToV4(SaveGameData data)
        {
            BookCatalog catalog;
            try
            {
                catalog = SaveGameMapper.ToCatalog(data.world.catalog);
            }
            catch (System.ArgumentException exception)
            {
                throw new SaveGameException("La partida contiene un catálogo no válido: " + exception.Message, exception);
            }

            GameDate date = GameDate.FromDayNumber(data.world.dateDayNumber);
            data.world.stock = PurchasingRules.InitialStock(catalog, date).Select(item => SaveGameMapper.ToData(item)).ToList();
            data.world.orders = new List<PurchaseOrderData>();
            data.world.nextOrderId = 1;
            foreach (WorldEventData worldEvent in data.world.events ?? new List<WorldEventData>())
            {
                worldEvent.referenceId = 0;
            }

            data.schemaVersion = 4;
        }
    }
}
