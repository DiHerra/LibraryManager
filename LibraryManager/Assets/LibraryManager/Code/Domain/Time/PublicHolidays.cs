namespace LibraryManager.Domain.Time
{
    /// <summary>
    /// Festivos nacionales de fecha fija. La librería cierra estos días aunque
    /// le toque abrir. Los festivos locales y móviles quedan fuera de la 1.0.
    /// </summary>
    public static class PublicHolidays
    {
        private static readonly (int Month, int Day)[] Fixed =
        {
            (1, 1),   // Año Nuevo
            (1, 6),   // Reyes
            (5, 1),   // Día del Trabajo
            (8, 15),  // Asunción
            (10, 12), // Fiesta Nacional
            (11, 1),  // Todos los Santos
            (12, 6),  // Constitución
            (12, 8),  // Inmaculada
            (12, 25)  // Navidad
        };

        public static bool IsHoliday(GameDate date)
        {
            int month = date.Month;
            int day = date.Day;
            foreach ((int Month, int Day) holiday in Fixed)
            {
                if (holiday.Month == month && holiday.Day == day)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
