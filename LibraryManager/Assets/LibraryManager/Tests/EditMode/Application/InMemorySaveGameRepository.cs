using LibraryManager.Application.Saves;

namespace LibraryManager.Tests.EditMode.Application
{
    internal sealed class InMemorySaveGameRepository : ISaveGameRepository
    {
        public SaveGameData Stored { get; set; }

        public int SaveCount { get; private set; }

        public bool Exists() => Stored != null;

        public void Save(SaveGameData data)
        {
            Stored = data;
            SaveCount++;
        }

        public SaveGameData Load()
        {
            if (Stored == null)
            {
                throw new SaveGameException("No hay ninguna partida guardada.");
            }

            return Stored;
        }
    }
}
