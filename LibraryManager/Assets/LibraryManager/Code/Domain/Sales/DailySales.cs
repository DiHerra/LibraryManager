using System;
using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.Sales
{
    /// <summary>Lo vendido de un título en un día.</summary>
    public readonly struct SaleLine
    {
        public SaleLine(int bookId, int units, Money revenue)
        {
            BookId = bookId;
            Units = units;
            Revenue = revenue;
        }

        public int BookId { get; }
        public int Units { get; }
        public Money Revenue { get; }
    }

    /// <summary>
    /// Resumen de una jornada de venta. Las líneas por título solo existen en la jornada
    /// recién simulada; en el registro guardado quedan los totales.
    /// </summary>
    public sealed class DailySales
    {
        public static readonly int GenreCount = Enum.GetValues(typeof(Genre)).Length;

        public DailySales(GameDate date, int openingHour, int hoursOpen)
        {
            Date = date;
            OpeningHour = openingHour;
            UnitsByHour = new int[Math.Max(0, hoursOpen)];
            UnitsByGenre = new int[GenreCount];
        }

        public GameDate Date { get; }

        public int OpeningHour { get; }

        public int Visitors { get; set; }

        /// <summary>Visitantes y recogidas de encargo que se llevaron algo.</summary>
        public int Buyers { get; set; }

        public int Units { get; set; }

        /// <summary>Lo cobrado, con IVA.</summary>
        public Money Revenue { get; set; }

        /// <summary>Coste de compra de lo vendido (FIFO).</summary>
        public Money Cost { get; set; }

        /// <summary>Descuento de tarjeta concedido.</summary>
        public Money Discounts { get; set; }

        public int Searches { get; set; }
        public int SearchesFound { get; set; }

        /// <summary>Búsquedas de títulos que no había y que no acabaron en recomendación ni encargo.</summary>
        public int LostSearches { get; set; }

        public int Recommendations { get; set; }
        public int RecommendationsAccepted { get; set; }
        public int SpecialOrdersTaken { get; set; }
        public int SpecialOrdersDelivered { get; set; }
        public int SpecialOrdersExpired { get; set; }

        /// <summary>Satisfacción media del día, 0–100.</summary>
        public int SatisfactionPercent { get; set; }

        public int[] UnitsByHour { get; }

        /// <summary>Unidades por género, indexadas por el valor de <see cref="Genre"/>.</summary>
        public int[] UnitsByGenre { get; }

        public IReadOnlyList<SaleLine> Lines { get; private set; } = new SaleLine[0];

        /// <summary>IVA superreducido (4 %) incluido en lo cobrado.</summary>
        public Money VatIncluded => SalesRules.VatIncluded(Revenue);

        /// <summary>Margen bruto: ingresos sin IVA menos coste.</summary>
        public Money GrossMargin => Revenue - VatIncluded - Cost;

        public void SetLines(IEnumerable<SaleLine> lines)
        {
            Lines = lines.OrderByDescending(line => line.Units).ThenByDescending(line => line.Revenue).ThenBy(line => line.BookId).ToList();
        }
    }
}
