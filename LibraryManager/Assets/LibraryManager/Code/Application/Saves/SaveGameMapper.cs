using System;
using System.Globalization;
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
                    }
                }
            };
        }

        public static WorldState ToWorld(SaveGameData data)
        {
            SaveGameData current = SaveGameMigrator.MigrateToCurrent(data);

            if (current.world == null || current.world.bookstore == null)
            {
                throw new SaveGameException("La partida no contiene el mundo o la librería.");
            }

            if (!ulong.TryParse(current.world.randomState, NumberStyles.None, CultureInfo.InvariantCulture, out ulong randomState))
            {
                throw new SaveGameException("El estado del generador aleatorio no es válido.");
            }

            try
            {
                var bookstore = new Bookstore(current.world.bookstore.name, new Money(current.world.bookstore.cashCents));
                return new WorldState(
                    bookstore,
                    GameDate.FromDayNumber(current.world.dateDayNumber),
                    DeterministicRandom.FromState(randomState));
            }
            catch (ArgumentException exception)
            {
                throw new SaveGameException("La partida contiene datos no válidos: " + exception.Message, exception);
            }
        }
    }
}
