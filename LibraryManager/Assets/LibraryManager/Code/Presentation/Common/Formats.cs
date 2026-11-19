using System.Globalization;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Time;

namespace LibraryManager.Presentation.Common
{
    /// <summary>Formato de textos para la interfaz (español en L01; la localización llega en L16).</summary>
    public static class Formats
    {
        private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");

        public static string Money(Money money)
        {
            return money.ToEuros().ToString("N2", Spanish) + " €";
        }

        public static string Date(GameDate date)
        {
            string text = date.ToDateTime().ToString("dddd, d 'de' MMMM 'de' yyyy", Spanish);
            return char.ToUpper(text[0], Spanish) + text.Substring(1);
        }
    }
}
