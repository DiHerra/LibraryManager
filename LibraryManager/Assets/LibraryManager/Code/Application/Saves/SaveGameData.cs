using System;

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
    }

    [Serializable]
    public sealed class BookstoreData
    {
        public string name;
        public long cashCents;
    }
}
