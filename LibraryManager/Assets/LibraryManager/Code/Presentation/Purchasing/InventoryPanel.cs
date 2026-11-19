using System;
using System.Linq;
using LibraryManager.Application.Game;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Economy;
using LibraryManager.Domain.Purchasing;
using LibraryManager.Domain.World;
using LibraryManager.Presentation.Common;
using UnityEngine.UIElements;

namespace LibraryManager.Presentation.Purchasing
{
    /// <summary>Existencias por título, con devolución de novedades en plazo y saldo del fondo.</summary>
    public sealed class InventoryPanel : IScreenPanel
    {
        private readonly Func<GameSession> _session;
        private readonly Label _summary;
        private readonly ScrollView _list;
        private readonly Label _status;

        public InventoryPanel(Func<GameSession> session, Action onBack)
        {
            _session = session;
            Root = Ui.Panel("Inventario");
            _summary = Ui.Text(string.Empty, 14);
            Root.Add(_summary);
            _list = Ui.List();
            Root.Add(_list);
            _status = Ui.Text(string.Empty, 14);
            Root.Add(_status);
            Root.Add(Ui.Button("Volver a la tienda", onBack));
        }

        public VisualElement Root { get; }

        public void Refresh()
        {
            WorldState world = _session().World;
            BookCatalog catalog = world.Catalog;
            _summary.text = $"{world.Inventory.TotalUnits} ejemplares de {world.Inventory.TitleCount} títulos · valor a coste {Formats.Money(world.Inventory.ValueAtCost)}. " +
                            "Las novedades se pueden devolver a la distribuidora dentro de plazo; el saldista paga el 10 % del PVP.";
            _list.Clear();

            var books = world.Inventory.BookIds
                .Select(catalog.GetBook)
                .OrderByDescending(book => book.PopularityOn(world.Date))
                .ThenBy(book => book.Id)
                .ToList();

            foreach (Book book in books)
            {
                int quantity = world.Inventory.Quantity(book.Id);
                int returnable = world.Inventory.ReturnableQuantity(book.Id, world.Date);
                var deadline = world.Inventory.LotsOf(book.Id)
                    .Where(lot => lot.IsReturnableOn(world.Date))
                    .Select(lot => lot.ReturnDeadline.Value)
                    .DefaultIfEmpty()
                    .Min();

                string detail = $"{quantity} en tienda · {Formats.Phase(book.PhaseOn(world.Date))} · interés {book.PopularityOn(world.Date):0} · PVP {Formats.Money(book.Price)}";
                if (returnable > 0)
                {
                    detail += $" · {returnable} devolvibles hasta el {Formats.ShortDate(deadline)}";
                }

                var actions = returnable > 0
                    ? new (string, Action)[] { ("Devolver", () => Return(book)), ("Saldar 1", () => Liquidate(book)) }
                    : new (string, Action)[] { ("Saldar 1", () => Liquidate(book)) };

                _list.Add(Ui.Row($"{book.Title} — {book.Author}", detail, actions));
            }
        }

        private void Return(Book book)
        {
            try
            {
                (int quantity, Money refund) = _session().ReturnNewReleases(book.Id);
                _status.text = $"Devueltos {quantity} ejemplares de «{book.Title}»: recuperas {Formats.Money(refund)}.";
            }
            catch (PurchasingException exception)
            {
                _status.text = exception.Message;
            }

            Refresh();
        }

        private void Liquidate(Book book)
        {
            try
            {
                Money income = _session().Liquidate(book.Id, 1);
                _status.text = $"Saldado un ejemplar de «{book.Title}» por {Formats.Money(income)}.";
            }
            catch (PurchasingException exception)
            {
                _status.text = exception.Message;
            }

            Refresh();
        }
    }
}
