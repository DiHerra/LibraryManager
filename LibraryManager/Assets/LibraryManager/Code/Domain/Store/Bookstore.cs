using LibraryManager.Domain.Common;
using LibraryManager.Domain.Economy;

namespace LibraryManager.Domain.Store
{
    /// <summary>La librería que dirige el jugador. En L01 solo tiene nombre y caja.</summary>
    public sealed class Bookstore
    {
        public const int MaxNameLength = 40;

        public Bookstore(string name, Money cash)
        {
            Name = ValidateName(name);
            Cash = cash;
        }

        public string Name { get; private set; }

        public Money Cash { get; private set; }

        public void Rename(string name)
        {
            Name = ValidateName(name);
        }

        public void ReceivePayment(Money amount)
        {
            Guard.NotNegative(amount.Cents, nameof(amount));
            Cash += amount;
        }

        /// <summary>Paga un gasto. La caja puede quedar en negativo; las reglas de quiebra llegan en L08.</summary>
        public void Pay(Money amount)
        {
            Guard.NotNegative(amount.Cents, nameof(amount));
            Cash -= amount;
        }

        private static string ValidateName(string name)
        {
            string trimmed = Guard.NotBlank(name, nameof(name));
            if (trimmed.Length > MaxNameLength)
            {
                throw new System.ArgumentException($"El nombre no puede superar {MaxNameLength} caracteres.", nameof(name));
            }

            return trimmed;
        }
    }
}
