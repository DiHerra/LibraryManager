namespace LibraryManager.Application.Saves
{
    public static class SaveGameSchema
    {
        /// <summary>
        /// Versión 1: fecha, generador y librería con nombre y caja (L01).
        /// Versión 2: horario de la tienda y cola de eventos del mundo (L02).
        /// Versión 3: catálogo de libros, editoriales y distribuidoras (L03).
        /// </summary>
        public const int CurrentVersion = 3;

        public const int OldestSupportedVersion = 1;
    }
}
