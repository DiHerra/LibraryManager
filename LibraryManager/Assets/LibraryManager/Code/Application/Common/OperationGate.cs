using System;

namespace LibraryManager.Application.Common
{
    /// <summary>
    /// Impide que dos operaciones del flujo principal (guardar, cargar, avanzar día,
    /// nueva partida) se solapen, por ejemplo con un doble clic.
    /// </summary>
    public sealed class OperationGate
    {
        public string ActiveOperation { get; private set; }

        public bool IsBusy => ActiveOperation != null;

        public bool TryEnter(string operation)
        {
            if (string.IsNullOrWhiteSpace(operation))
            {
                throw new ArgumentException("La operación necesita un nombre.", nameof(operation));
            }

            if (IsBusy)
            {
                return false;
            }

            ActiveOperation = operation;
            return true;
        }

        public void Exit(string operation)
        {
            if (!IsBusy)
            {
                throw new InvalidOperationException("No hay ninguna operación activa.");
            }

            if (ActiveOperation != operation)
            {
                throw new InvalidOperationException(
                    $"La operación activa es '{ActiveOperation}', no '{operation}'.");
            }

            ActiveOperation = null;
        }
    }
}
