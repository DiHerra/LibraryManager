using LibraryManager.Application.Game;
using LibraryManager.Infrastructure.Persistence;

namespace LibraryManager.Presentation.Common
{
    /// <summary>
    /// Punto único donde se crean los servicios del juego. La sesión sobrevive a los
    /// cambios de escena (menú → tienda).
    /// </summary>
    public static class GameServices
    {
        private static GameSession _session;

        public static GameSession Session => _session ??= new GameSession(JsonSaveGameRepository.CreateDefault(), new SystemClock());

        /// <summary>Solo para pruebas.</summary>
        public static void Reset(GameSession session = null)
        {
            _session = session;
        }
    }
}
