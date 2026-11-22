using System;
using LibraryManager.Domain.Economy;

namespace LibraryManager.Domain.Sales
{
    /// <summary>Reglas de precio y probabilidades de compra.</summary>
    public static class SalesRules
    {
        /// <summary>IVA superreducido del libro en papel (4 %).</summary>
        public const int VatBasisPoints = 400;

        /// <summary>Descuento máximo que permite la ley del libro (5 %).</summary>
        public const int MaxLoyaltyDiscountBasisPoints = 500;

        /// <summary>Parte de los visitantes con tarjeta de lector.</summary>
        public const double CardholderShare = 0.4;

        /// <summary>Probabilidad de que el librero proponga una alternativa (en L09 dependerá del personal).</summary>
        public const double RecommendationChance = 0.35;

        public const double RecommendationAcceptance = 0.5;

        public const double SpecialOrderChance = 0.4;

        /// <summary>Encargos pendientes como máximo; con más, el cliente ya no encarga.</summary>
        public const int MaxPendingSpecialOrders = 80;

        public const double MaxBrowseBuyProbability = 0.85;

        /// <summary>Precio que paga el cliente: PVP menos el descuento de tarjeta si la tiene.</summary>
        public static Money PriceFor(Money listPrice, int discountBasisPoints, bool cardholder)
        {
            return cardholder && discountBasisPoints > 0 ? listPrice - listPrice.Percentage(discountBasisPoints) : listPrice;
        }

        /// <summary>Parte de IVA incluida en un importe con IVA del 4 %.</summary>
        public static Money VatIncluded(Money grossAmount)
        {
            decimal vat = grossAmount.Cents * (decimal)VatBasisPoints / (10000m + VatBasisPoints);
            return new Money((long)decimal.Round(vat, 0, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// Probabilidad de que un lector que ojea un libro lo compre: crece con el interés
        /// del público (0–100) y con la visibilidad del mueble; la tarjeta con descuento la sube hasta un 20 %.
        /// </summary>
        public static double BrowseBuyProbability(double popularity, int visibilityBasisPoints, int discountBasisPoints, bool cardholder)
        {
            double probability = 0.05 + 0.006 * popularity * visibilityBasisPoints / 10000d;
            probability = Math.Max(0d, Math.Min(MaxBrowseBuyProbability, probability));
            return probability * DiscountBoost(discountBasisPoints, cardholder);
        }

        /// <summary>Aumento de la disposición a comprar por el descuento: ×1,2 con el 5 %.</summary>
        public static double DiscountBoost(int discountBasisPoints, bool cardholder)
        {
            return cardholder ? 1d + discountBasisPoints / 2500d : 1d;
        }
    }
}
