namespace LibraryManager.Domain.Time
{
    /// <summary>Por qué se detiene un avance de varios días.</summary>
    public enum PauseReasonKind
    {
        /// <summary>Un evento pide la atención del jugador.</summary>
        EventNeedsAttention = 1,
        /// <summary>Empieza una temporada comercial.</summary>
        SeasonStarted = 2,
        /// <summary>La caja ha pasado a negativo.</summary>
        CashBecameNegative = 3
    }

    public readonly struct PauseReason
    {
        public PauseReason(PauseReasonKind kind, string detail)
        {
            Kind = kind;
            Detail = detail ?? string.Empty;
        }

        public PauseReasonKind Kind { get; }

        /// <summary>Dato para el texto (título del evento o nombre de la temporada en clave).</summary>
        public string Detail { get; }
    }
}
