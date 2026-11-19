using System;
using System.Linq;
using LibraryManager.Application.Game;
using LibraryManager.Application.Purchasing;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Purchasing;
using LibraryManager.Domain.World;
using LibraryManager.Presentation.Common;
using UnityEngine.UIElements;

namespace LibraryManager.Presentation.Purchasing
{
    /// <summary>Borrador de pedido separado por distribuidora y pedidos en camino.</summary>
    public sealed class OrdersPanel : IScreenPanel
    {
        private readonly Func<GameSession> _session;
        private readonly ScrollView _list;
        private readonly Label _status;

        public OrdersPanel(Func<GameSession> session, Action onBack)
        {
            _session = session;
            Root = Ui.Panel("Pedidos");
            Root.Add(Ui.Text("Añade libros desde el catálogo. Cada distribuidora recibe su propio pedido, llega tras su plazo de entrega y se paga al recibirlo.", 14));
            _list = Ui.List();
            Root.Add(_list);
            _status = Ui.Text(string.Empty, 14);
            Root.Add(_status);
            Root.Add(Ui.Button("Volver a la tienda", onBack));
        }

        public VisualElement Root { get; }

        public void Refresh()
        {
            GameSession session = _session();
            WorldState world = session.World;
            BookCatalog catalog = world.Catalog;
            _list.Clear();

            _list.Add(Ui.Section("Borrador"));
            var groups = session.Draft.ByDistributor(catalog);
            if (groups.Count == 0)
            {
                _list.Add(Ui.Text("El borrador está vacío.", 14));
            }

            foreach (DraftGroup group in groups)
            {
                Distributor distributor = group.Distributor;
                string minimum = group.MeetsMinimum
                    ? "mínimo cubierto"
                    : $"faltan {Formats.Money(distributor.MinimumOrder - group.Total)} para el mínimo";
                _list.Add(Ui.Row(
                    distributor.Name,
                    $"{group.Units} ejemplares · {Formats.Money(group.Total)} a coste · {minimum} · entrega en {distributor.LeadTimeDays} días",
                    ("Enviar pedido", () => Send(distributor.Id))));

                foreach ((Book book, int quantity, var unitCost) in group.Lines)
                {
                    VisualElement line = Ui.Row(
                        $"{quantity} × {book.Title}",
                        $"{Formats.Money(unitCost)} a coste (PVP {Formats.Money(book.Price)})",
                        ("−1", () => Change(book.Id, -1)),
                        ("+1", () => Change(book.Id, 1)));
                    line.style.marginLeft = 24;
                    _list.Add(line);
                }
            }

            _list.Add(Ui.Section("En camino"));
            var pending = world.Orders.Pending.OrderBy(order => order.ExpectedDate).ThenBy(order => order.Id).ToList();
            if (pending.Count == 0)
            {
                _list.Add(Ui.Text("No hay pedidos en camino.", 14));
            }

            foreach (PurchaseOrder order in pending)
            {
                _list.Add(Ui.Row(
                    $"Pedido n.º {order.Id} · {catalog.GetDistributor(order.DistributorId).Name}",
                    $"{order.TotalUnits} ejemplares · {Formats.Money(order.Total)} · llega el {Formats.ShortDate(order.ExpectedDate)}"));
            }
        }

        private void Change(int bookId, int quantity)
        {
            _session().Draft.Add(bookId, quantity);
            Refresh();
        }

        private void Send(int distributorId)
        {
            try
            {
                PurchaseOrder order = _session().SendDraftOrder(distributorId);
                _status.text = $"Pedido n.º {order.Id} enviado: llega el {Formats.ShortDate(order.ExpectedDate)}.";
            }
            catch (PurchasingException exception)
            {
                _status.text = exception.Message;
            }

            Refresh();
        }
    }
}
