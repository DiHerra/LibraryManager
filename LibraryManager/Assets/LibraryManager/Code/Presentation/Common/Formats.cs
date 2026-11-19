using System.Globalization;
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

        private static string Capitalize(string text)
        {
            return string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0], Spanish) + text.Substring(1);
        }
    }
}
