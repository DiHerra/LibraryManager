using System.Globalization;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Time;

namespace LibraryManager.Presentation.Common
{
    /// <summary>Formato de textos para la interfaz (español; la localización llega en L16).</summary>
    public static class Formats
    {
        private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");

        public static string Money(Money money)
        {
            return money.ToEuros().ToString("N2", Spanish) + " €";
        }

        public static string SignedMoney(Money money)
        {
            return (money.IsNegative ? "−" : "+") + Money(money.IsNegative ? -money : money);
        }

        public static string Date(GameDate date)
        {
            return Capitalize(date.ToDateTime().ToString("dddd, d 'de' MMMM 'de' yyyy", Spanish));
        }

        public static string ShortDate(GameDate date)
        {
            return date.ToDateTime().ToString("ddd d MMM", Spanish);
        }

        public static string Season(CommercialSeason season)
        {
            switch (season)
            {
                case CommercialSeason.BackToSchool: return "Vuelta al cole";
                case CommercialSeason.Christmas: return "Navidad";
                case CommercialSeason.BookDay: return "Día del Libro";
                case CommercialSeason.Summer: return "Verano";
                default: return "Temporada normal";
            }
        }

        public static string Event(WorldEvent worldEvent)
        {
            string text = $"{ShortDate(worldEvent.Date)} · {worldEvent.Title}";
            if (worldEvent.Kind == WorldEventKind.Payment)
            {
                text += " · " + Money(worldEvent.Amount);
            }

            return text;
        }

        public static string PauseReason(PauseReason reason)
        {
            switch (reason.Kind)
            {
                case PauseReasonKind.EventNeedsAttention:
                    return "Atención: " + reason.Detail;
                case PauseReasonKind.SeasonStarted:
                    return System.Enum.TryParse(reason.Detail, out CommercialSeason season)
                        ? "Empieza la temporada: " + Season(season)
                        : "Empieza una temporada comercial";
                case PauseReasonKind.CashBecameNegative:
                    return "¡La caja está en negativo!";
                default:
                    return reason.Detail;
            }
        }

        public static string Genre(Genre genre)
        {
            switch (genre)
            {
                case Domain.Catalog.Genre.Fiction: return "Narrativa";
                case Domain.Catalog.Genre.Crime: return "Novela negra";
                case Domain.Catalog.Genre.Fantasy: return "Fantasía";
                case Domain.Catalog.Genre.ScienceFiction: return "Ciencia ficción";
                case Domain.Catalog.Genre.Romance: return "Romántica";
                case Domain.Catalog.Genre.Historical: return "Novela histórica";
                case Domain.Catalog.Genre.Children: return "Infantil";
                case Domain.Catalog.Genre.YoungAdult: return "Juvenil";
                case Domain.Catalog.Genre.Comics: return "Cómic";
                case Domain.Catalog.Genre.Essay: return "Ensayo";
                case Domain.Catalog.Genre.SelfHelp: return "Autoayuda";
                case Domain.Catalog.Genre.Cooking: return "Cocina";
                case Domain.Catalog.Genre.Travel: return "Viajes";
                case Domain.Catalog.Genre.Poetry: return "Poesía";
                case Domain.Catalog.Genre.School: return "Texto escolar";
                default: return genre.ToString();
            }
        }

        public static string Format(BookFormat format)
        {
            switch (format)
            {
                case BookFormat.Hardcover: return "Tapa dura";
                case BookFormat.Pocket: return "Bolsillo";
                default: return "Rústica";
            }
        }

        public static string Phase(BookPhase phase)
        {
            switch (phase)
            {
                case BookPhase.Upcoming: return "Próximamente";
                case BookPhase.NewRelease: return "Novedad";
                default: return "Fondo";
            }
        }

        private static string Capitalize(string text)
        {
            return string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0], Spanish) + text.Substring(1);
        }
    }
}
