namespace LibraryManager.Domain.Time
{
    public enum CommercialSeason
    {
        Normal = 0,
        BackToSchool = 1,
        Christmas = 2,
        BookDay = 3,
        Summer = 4
    }

    /// <summary>
    /// Temporadas comerciales del año y su efecto base en la demanda. La demanda
    /// (L06) multiplica por <see cref="DemandBasisPoints"/>; 10000 es lo normal.
    /// </summary>
    public static class CommercialCalendar
    {
        public static CommercialSeason GetSeason(GameDate date)
        {
            int month = date.Month;
            int day = date.Day;

            if (month == 12 || (month == 1 && day <= 6))
            {
                return CommercialSeason.Christmas;
            }

            if (month == 4 && day >= 16 && day <= 23)
            {
                return CommercialSeason.BookDay;
            }

            if ((month == 8 && day >= 25) || month == 9)
            {
                return CommercialSeason.BackToSchool;
            }

            if (month == 7 || month == 8)
            {
                return CommercialSeason.Summer;
            }

            return CommercialSeason.Normal;
        }

        public static int DemandBasisPoints(CommercialSeason season)
        {
            switch (season)
            {
                case CommercialSeason.BackToSchool: return 13000;
                case CommercialSeason.Christmas: return 18000;
                case CommercialSeason.BookDay: return 25000;
                case CommercialSeason.Summer: return 8000;
                default: return 10000;
            }
        }
    }
}
