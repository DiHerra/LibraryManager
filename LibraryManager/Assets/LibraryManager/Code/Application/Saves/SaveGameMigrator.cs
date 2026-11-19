using System.Collections.Generic;
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
            data.world.events = events.Pending.Select(SaveGameMapper.ToData).ToList();
            data.world.nextEventId = events.NextId;
            data.schemaVersion = 2;
        }
    }
}
