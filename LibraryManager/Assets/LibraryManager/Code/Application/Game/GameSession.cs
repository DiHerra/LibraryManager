using System;
using System.Linq;
using LibraryManager.Application.Common;
using LibraryManager.Application.Purchasing;
using LibraryManager.Application.Saves;
using LibraryManager.Domain.Common;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Purchasing;
using LibraryManager.Domain.Time;
using LibraryManager.Domain.World;

namespace LibraryManager.Application.Game
{
    /// <summary>
    /// Casos de uso del flujo principal: nueva partida, cargar, guardar y avanzar el tiempo.
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

        /// <summary>Último avance hecho en esta sesión, para el resumen de la pantalla.</summary>
        public AdvanceResult LastAdvance { get; private set; }

        /// <summary>Cesta de pedidos en preparación (no se guarda).</summary>
        public OrderDraft Draft { get; } = new OrderDraft();

        public event Action WorldChanged;

        public void StartNew(string bookstoreName, ulong seed)
        {
            Run("nueva partida", () =>
            {
                World = WorldState.CreateNew(bookstoreName, seed);
                LastAdvance = null;
                Draft.Clear();
            });
        }

        public void Load()
        {
            Run("cargar", () =>
            {
                World = SaveGameMapper.ToWorld(_repository.Load());
                LastAdvance = null;
                Draft.Clear();
            });
        }

        public void Save()
        {
            Run("guardar", () =>
            {
                RequireWorld();
                _repository.Save(SaveGameMapper.ToData(World, _clock.UtcNow));
            }, notify: false);
        }

        public AdvanceResult AdvanceDay()
        {
            return Advance("avanzar día", world => world.AdvanceDays(1));
        }

        /// <summary>Avanza hasta el lunes siguiente o hasta el primer motivo de pausa.</summary>
        public AdvanceResult AdvanceWeek()
        {
            return Advance("avanzar semana", world => world.AdvanceToNextWeek());
        }

        /// <summary>Envía la parte del borrador de una distribuidora como pedido y la quita del borrador.</summary>
        /// <exception cref="PurchasingException">Si el pedido no cumple las reglas (mínimo, títulos…).</exception>
        public PurchaseOrder SendDraftOrder(int distributorId)
        {
            PurchaseOrder order = null;
            Run("enviar pedido", () =>
            {
                RequireWorld();
                DraftGroup group = Draft.ByDistributor(World.Catalog).FirstOrDefault(g => g.Distributor.Id == distributorId)
                    ?? throw new PurchasingException("No hay nada en el borrador para esa distribuidora.");
                order = World.PlaceOrder(distributorId, group.ToRequest());
                Draft.ClearDistributor(World.Catalog, distributorId);
            });
            return order;
        }

        public (int Quantity, Money Refund) ReturnNewReleases(int bookId)
        {
            (int Quantity, Money Refund) result = (0, Money.Zero);
            Run("devolver", () =>
            {
                RequireWorld();
                result = World.ReturnNewReleases(bookId);
            });
            return result;
        }

        public Money Liquidate(int bookId, int quantity)
        {
            Money income = Money.Zero;
            Run("saldar", () =>
            {
                RequireWorld();
                income = World.Liquidate(bookId, quantity);
            });
            return income;
        }

        /// <summary>Expone ejemplares del almacén en un mueble.</summary>
        /// <exception cref="LibraryManager.Domain.Store.StoreLayoutException">Si no caben o no hay suficientes en el almacén.</exception>
        public void PlaceOnFixture(int fixtureId, int bookId, int quantity)
        {
            Run("colocar", () =>
            {
                RequireWorld();
                World.Layout.Place(fixtureId, bookId, quantity, World.Inventory);
            });
        }

        /// <summary>Retira ejemplares de un mueble al almacén.</summary>
        public void RemoveFromFixture(int fixtureId, int bookId, int quantity)
        {
            Run("retirar", () =>
            {
                RequireWorld();
                World.Layout.Remove(fixtureId, bookId, quantity);
            });
        }

        public void AutoArrangeStore()
        {
            Run("colocar automáticamente", () =>
            {
                RequireWorld();
                World.AutoArrangeStore();
            });
        }

        /// <summary>Notifica a las pantallas que algo cambió sin tocar el mundo (por ejemplo, el borrador).</summary>
        public void NotifyChanged()
        {
            WorldChanged?.Invoke();
        }

        private AdvanceResult Advance(string operation, Func<WorldState, AdvanceResult> advance)
        {
            Run(operation, () =>
            {
                RequireWorld();
                LastAdvance = advance(World);
            });
            return LastAdvance;
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
