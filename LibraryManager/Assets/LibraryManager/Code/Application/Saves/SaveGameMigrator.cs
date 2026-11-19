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

            // Cuando exista la versión 2: if (data.schemaVersion == 1) { MigrateV1ToV2(data); }
            return data;
        }
    }
}
