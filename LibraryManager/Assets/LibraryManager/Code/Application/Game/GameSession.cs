using System;
using LibraryManager.Application.Common;
using LibraryManager.Application.Saves;
using LibraryManager.Domain.Common;
using LibraryManager.Domain.World;

namespace LibraryManager.Application.Game
{
    /// <summary>
    /// Casos de uso del flujo principal: nueva partida, cargar, guardar y avanzar día.
    /// Las pantallas llaman aquí y no tocan el dominio ni el guardado directamente.
    /// </summary>
    public sealed class GameSession
    {
        private readonly ISaveGameRepository _repository;
        private readonly IClock _clock;
        private readonly OperationGate _gate = new OperationGate();

        public GameSession(ISaveGameRepository repository, IClock clock)
        {
            _repository = Guard.NotNull(repository, nameof(repository));
            _clock = Guard.NotNull(clock, nameof(clock));
        }

        public WorldState World { get; private set; }

        public bool HasWorld => World != null;

        public bool HasSavedGame => _repository.Exists();

        public bool IsBusy => _gate.IsBusy;

        public event Action WorldChanged;

        public void StartNew(string bookstoreName, ulong seed)
        {
            Run("nueva partida", () => World = WorldState.CreateNew(bookstoreName, seed));
        }

        public void Load()
        {
            Run("cargar", () => World = SaveGameMapper.ToWorld(_repository.Load()));
        }

        public void Save()
        {
            Run("guardar", () =>
            {
                RequireWorld();
                _repository.Save(SaveGameMapper.ToData(World, _clock.UtcNow));
            }, notify: false);
        }

        public void AdvanceDay()
        {
            Run("avanzar día", () =>
            {
                RequireWorld();
                World.AdvanceDay();
            });
        }

        private void Run(string operation, Action action, bool notify = true)
        {
            if (!_gate.TryEnter(operation))
            {
                throw new InvalidOperationException(
                    $"No se puede {operation} mientras se ejecuta '{_gate.ActiveOperation}'.");
            }

            try
            {
                action();
            }
            finally
            {
                _gate.Exit(operation);
            }

            if (notify)
            {
                WorldChanged?.Invoke();
            }
        }

        private void RequireWorld()
        {
            if (World == null)
            {
                throw new InvalidOperationException("No hay ninguna partida abierta.");
            }
        }
    }
}
