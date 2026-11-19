namespace LibraryManager.Application.Saves
{
    /// <summary>Almacén de la partida. La implementación JSON vive en Infrastructure.</summary>
    public interface ISaveGameRepository
    {
        bool Exists();

        void Save(SaveGameData data);

        /// <exception cref="SaveGameException">Si la partida no existe o no se puede leer.</exception>
        SaveGameData Load();
    }
}
