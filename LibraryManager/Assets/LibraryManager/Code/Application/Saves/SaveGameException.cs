using System;

namespace LibraryManager.Application.Saves
{
    /// <summary>Partida ilegible, corrupta o de una versión que este juego no conoce.</summary>
    public sealed class SaveGameException : Exception
    {
        public SaveGameException(string message) : base(message)
        {
        }

        public SaveGameException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
