using LibraryManager.Domain.Common;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Store;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.World
{
    /// <summary>Estado completo de una partida. Todo lo que se guarda cuelga de aquí.</summary>
    public sealed class WorldState
    {
        public static readonly GameDate DefaultStartDate = GameDate.Create(2026, 9, 1);
        public static readonly Money DefaultStartingCash = Money.FromEuros(30000m);

        public WorldState(Bookstore bookstore, GameDate date, DeterministicRandom random)
        {
            Bookstore = Guard.NotNull(bookstore, nameof(bookstore));
            Date = date;
            Random = Guard.NotNull(random, nameof(random));
        }

        public Bookstore Bookstore { get; }

        public GameDate Date { get; private set; }

        public DeterministicRandom Random { get; }

        public static WorldState CreateNew(string bookstoreName, ulong seed)
        {
            return new WorldState(
                new Bookstore(bookstoreName, DefaultStartingCash),
                DefaultStartDate,
                new DeterministicRandom(seed));
        }

        /// <summary>Pasa al día siguiente. El reloj completo con eventos llega en L02.</summary>
        public void AdvanceDay()
        {
            Date = Date.AddDays(1);
        }
    }
}
