namespace LibraryManager.Application.Saves
{
    public static class SaveGameSchema
    {
        /// <summary>
        /// Versión 1: fecha, generador y librería con nombre y caja (L01).
        /// Versión 2: horario de la tienda y cola de eventos del mundo (L02).
        /// Versión 3: catálogo de libros, editoriales y distribuidoras (L03).
        /// Versión 4: inventario por lotes, pedidos y referencia de los eventos (L04).
        /// Versión 5: muebles de la tienda y libros expuestos (L05).
        /// Versión 6: reputación, tarjeta de lector, encargos y registro de ventas (L06-L07).
        /// </summary>
        public const int CurrentVersion = 6;

        public const int OldestSupportedVersion = 1;
    }
}
