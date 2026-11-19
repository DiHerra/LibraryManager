using System;

namespace LibraryManager.Domain.Purchasing
{
    /// <summary>Una operación de compra o devolución que las reglas no permiten. El mensaje es para el jugador.</summary>
    public sealed class PurchasingException : InvalidOperationException
    {
        public PurchasingException(string message) : base(message)
        {
        }
    }
}
