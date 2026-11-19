using LibraryManager.Domain.Common;
using LibraryManager.Domain.Economy;

namespace LibraryManager.Domain.Catalog
{
    /// <summary>Distribuidora: lleva los libros de varias editoriales a la librería.</summary>
    public sealed class Distributor
    {
        public Distributor(int id, string name, int leadTimeDays, Money minimumOrder, int returnWindowDays)
        {
            Id = id > 0 ? id : throw new System.ArgumentOutOfRangeException(nameof(id));
            Name = Guard.NotBlank(name, nameof(name));
            LeadTimeDays = Guard.InRange(leadTimeDays, 1, 30, nameof(leadTimeDays));
            Guard.NotNegative(minimumOrder.Cents, nameof(minimumOrder));
            MinimumOrder = minimumOrder;
            ReturnWindowDays = Guard.InRange(returnWindowDays, 0, 365, nameof(returnWindowDays));
        }

        public int Id { get; }
        public string Name { get; }

        /// <summary>Días desde el pedido hasta que llega a la tienda.</summary>
        public int LeadTimeDays { get; }

        /// <summary>Importe mínimo de un pedido (a precio de coste).</summary>
        public Money MinimumOrder { get; }

        /// <summary>Plazo para devolver novedades no vendidas.</summary>
        public int ReturnWindowDays { get; }
    }
}
