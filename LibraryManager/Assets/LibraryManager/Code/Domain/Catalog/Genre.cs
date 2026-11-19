namespace LibraryManager.Domain.Catalog
{
    /// <summary>Géneros del catálogo. Los valores se guardan en la partida: no reordenar.</summary>
    public enum Genre
    {
        Fiction = 0,
        Crime = 1,
        Fantasy = 2,
        ScienceFiction = 3,
        Romance = 4,
        Historical = 5,
        Children = 6,
        YoungAdult = 7,
        Comics = 8,
        Essay = 9,
        SelfHelp = 10,
        Cooking = 11,
        Travel = 12,
        Poetry = 13,
        School = 14
    }

    /// <summary>Encuadernación. Los valores se guardan en la partida: no reordenar.</summary>
    public enum BookFormat
    {
        Paperback = 0,
        Hardcover = 1,
        Pocket = 2
    }

    /// <summary>Momento comercial de un título.</summary>
    public enum BookPhase
    {
        /// <summary>Aún no ha salido.</summary>
        Upcoming = 0,
        /// <summary>Publicado hace 90 días o menos.</summary>
        NewRelease = 1,
        /// <summary>Fondo de catálogo.</summary>
        Backlist = 2
    }

    public enum PublisherSize
    {
        Small = 0,
        Medium = 1,
        Large = 2
    }
}
