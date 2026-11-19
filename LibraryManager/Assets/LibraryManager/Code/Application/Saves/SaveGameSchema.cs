namespace LibraryManager.Application.Saves
{
    public static class SaveGameSchema
    {
        /// <summary>
        /// Versión 1: fecha, generador y librería con nombre y caja (L01).
        /// Versión 2: horario de la tienda y cola de eventos del mundo (L02).
        /// </summary>
        public const int CurrentVersion = 2;

        public const int OldestSupportedVersion = 1;
    }
}
