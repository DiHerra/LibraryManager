using System.Collections.Generic;
using System.Linq;
using LibraryManager.Application.Game;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Sales;
using LibraryManager.Domain.Stock;
using LibraryManager.Domain.Time;
using LibraryManager.Domain.World;
using LibraryManager.Presentation.Catalog;
using LibraryManager.Presentation.Common;
using LibraryManager.Presentation.Purchasing;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace LibraryManager.Presentation.Store
{
    /// <summary>
    /// Pantalla provisional de la tienda: día, temporada, caja, avisos, próximos eventos y
    /// avance del tiempo. Catálogo, pedidos e inventario se abren como paneles.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class StoreScreenController : MonoBehaviour
    {
        private const int UpcomingEventsShown = 4;
        private const int AlertsShown = 5;
        private const int SpecialOrdersShown = 5;
        private const int TopTitlesShown = 3;
        private const int DiscountStep = 100;

        private Label _name;
        private Label _date;
        private Label _openState;
        private Label _season;
        private Label _cash;
        private Label _stock;
        private Label _reputation;
        private Label _lastDay;
        private VisualElement _specialOrders;
        private VisualElement _alerts;
        private VisualElement _upcoming;
        private Label _summary;
        private Label _status;
        private VisualElement _storeCard;
        private readonly List<IScreenPanel> _panels = new List<IScreenPanel>();
        private IScreenPanel _openPanel;

        private GameSession Session => GameServices.Session;

        private void OnEnable()
        {
            if (!Session.HasWorld)
            {
                // Se abrió la escena directamente en el editor: volver al menú.
                SceneManager.LoadScene(SceneNames.MainMenu);
                return;
            }

            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            VisualElement card = Ui.Screen(root);
            _storeCard = card;

            var catalog = new CatalogPanel(() => Session, ShowStore);
            var orders = new OrdersPanel(() => Session, ShowStore);
            var inventory = new InventoryPanel(() => Session, ShowStore);
            var plan = new StorePlanPanel(() => Session, ShowStore);
            _panels.Clear();
            _panels.AddRange(new IScreenPanel[] { catalog, orders, inventory, plan });
            foreach (IScreenPanel panel in _panels)
            {
                root.Add(panel.Root);
            }

            _name = Ui.Title(string.Empty);
            card.Add(_name);
            _date = Ui.Text(string.Empty, 20);
            card.Add(_date);
            _openState = Ui.Text(string.Empty);
            card.Add(_openState);
            _season = Ui.Text(string.Empty);
            card.Add(_season);
            _cash = Ui.Text(string.Empty, 20);
            card.Add(_cash);
            _stock = Ui.Text(string.Empty, 14);
            card.Add(_stock);
            _lastDay = Ui.Text(string.Empty, 14);
            _lastDay.style.whiteSpace = WhiteSpace.Normal;
            card.Add(_lastDay);

            var reputationRow = new VisualElement();
            reputationRow.style.flexDirection = FlexDirection.Row;
            reputationRow.style.alignItems = Align.Center;
            _reputation = Ui.Text(string.Empty, 14);
            _reputation.style.flexGrow = 1;
            _reputation.style.whiteSpace = WhiteSpace.Normal;
            reputationRow.Add(_reputation);
            reputationRow.Add(SmallButton("− 1 %", () => ChangeDiscount(-DiscountStep)));
            reputationRow.Add(SmallButton("+ 1 %", () => ChangeDiscount(DiscountStep)));
            card.Add(reputationRow);

            _alerts = new VisualElement();
            card.Add(_alerts);

            _specialOrders = new VisualElement();
            card.Add(_specialOrders);

            card.Add(Ui.Text("Próximos eventos", 16));
            _upcoming = new VisualElement();
            _upcoming.style.marginBottom = 8;
            card.Add(_upcoming);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.Add(Grow(Ui.Button("Avanzar día", () => Session.AdvanceDay())));
            row.Add(Grow(Ui.Button("Avanzar semana", () => Session.AdvanceWeek())));
            card.Add(row);

            var panelsRow = new VisualElement();
            panelsRow.style.flexDirection = FlexDirection.Row;
            panelsRow.Add(Grow(Ui.Button("Catálogo", () => Show(catalog))));
            panelsRow.Add(Grow(Ui.Button("Pedidos", () => Show(orders))));
            panelsRow.Add(Grow(Ui.Button("Inventario", () => Show(inventory))));
            panelsRow.Add(Grow(Ui.Button("Tienda", () => Show(plan))));
            card.Add(panelsRow);

            card.Add(Ui.Button("Guardar", Save));
            card.Add(Ui.Button("Guardar y volver al menú", SaveAndExit));

            _summary = Ui.Text(string.Empty, 14);
            _summary.style.marginTop = 12;
            _summary.style.whiteSpace = WhiteSpace.Normal;
            card.Add(_summary);

            _status = Ui.Text(string.Empty, 14);
            card.Add(_status);

            Session.WorldChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            Session.WorldChanged -= Refresh;
        }

        private static Button SmallButton(string text, System.Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.style.fontSize = 14;
            button.style.height = 30;
            button.style.minWidth = 56;
            return button;
        }

        private void ChangeDiscount(int delta)
        {
            int current = Session.World.Sales.LoyaltyDiscountBasisPoints;
            int updated = System.Math.Max(0, System.Math.Min(SalesRules.MaxLoyaltyDiscountBasisPoints, current + delta));
            if (updated != current)
            {
                Session.SetLoyaltyDiscount(updated);
            }
        }

        private static Button Grow(Button button)
        {
            button.style.flexGrow = 1;
            button.style.flexBasis = 0;
            return button;
        }

        private void Refresh()
        {
            WorldState world = Session.World;
            _name.text = world.Bookstore.Name;
            _date.text = Formats.Date(world.Date);
            _openState.text = world.IsOpenToday
                ? $"Abierto de {world.Schedule.OpeningHour}:00 a {world.Schedule.ClosingHour}:00"
                : "Cerrado hoy";
            _season.text = Formats.Season(world.Season);
            _cash.text = "Caja: " + Formats.Money(world.Bookstore.Cash);
            _stock.text = $"{world.Inventory.TotalUnits} ejemplares de {world.Inventory.TitleCount} títulos · {world.Orders.Pending.Count()} pedidos en camino";

            SalesState sales = world.Sales;
            _reputation.text = $"Reputación en el barrio: {Formats.Reputation(sales.Reputation)} · Tarjeta de lector: {Formats.Percent(sales.LoyaltyDiscountBasisPoints)} de descuento";
            DailySales latest = sales.Ledger.Latest;
            _lastDay.text = latest == null
                ? "Todavía no hay ventas: avanza el día para abrir la tienda."
                : $"Última jornada ({Formats.ShortDate(latest.Date)}): {Formats.Units(latest.Units)} · {Formats.Money(latest.Revenue)} · {latest.Visitors} visitantes · satisfacción {latest.SatisfactionPercent} %";

            RefreshAlerts(world);
            RefreshSpecialOrders(world);

            _upcoming.Clear();
            IReadOnlyList<WorldEvent> upcoming = world.Events.Upcoming(UpcomingEventsShown);
            if (upcoming.Count == 0)
            {
                _upcoming.Add(Ui.Text("Nada programado", 14));
            }

            foreach (WorldEvent worldEvent in upcoming)
            {
                _upcoming.Add(Ui.Text(Formats.Event(worldEvent), 14));
            }

            _summary.text = Summary(Session.LastAdvance);
            _status.text = string.Empty;
            _openPanel?.Refresh();
        }

        private void RefreshAlerts(WorldState world)
        {
            _alerts.Clear();
            var lines = new List<string>();

            IReadOnlyList<Book> missing = InventoryAlerts.MissingBestsellers(world.Catalog, world.Inventory, world.Orders, world.Date);
            if (missing.Count > 0)
            {
                lines.Add($"Sin stock de {missing.Count} superventas: " + string.Join(", ", missing.Take(3).Select(b => "«" + b.Title + "»")) + (missing.Count > 3 ? "…" : string.Empty));
            }

            foreach (ExpiringReturn item in InventoryAlerts.ExpiringReturns(world.Inventory, world.Date).Take(AlertsShown))
            {
                lines.Add($"Devolución de «{world.Catalog.GetBook(item.BookId).Title}» ({item.Quantity}) vence el {Formats.ShortDate(item.Deadline)}");
            }

            int stale = InventoryAlerts.StaleTitles(world.Inventory, world.Date, world.Sales.Ledger).Count;
            if (stale > 0)
            {
                lines.Add($"{stale} títulos llevan más de {InventoryAlerts.StaleDays} días sin venderse ni reponerse");
            }

            foreach (string line in lines)
            {
                Label label = Ui.Text("Aviso: " + line, 14);
                label.style.color = Ui.Accent;
                label.style.whiteSpace = WhiteSpace.Normal;
                _alerts.Add(label);
            }
        }

        /// <summary>Encargos de clientes agrupados por título, con un botón para añadirlos al pedido.</summary>
        private void RefreshSpecialOrders(WorldState world)
        {
            _specialOrders.Clear();
            var groups = world.Sales.SpecialOrders
                .GroupBy(order => order.BookId)
                .Select(group => (Book: world.Catalog.GetBook(group.Key), Count: group.Count(), DaysLeft: group.Min(order => order.DaysLeft(world.Date))))
                .OrderBy(item => item.DaysLeft)
                .ThenBy(item => item.Book.Id)
                .ToList();
            if (groups.Count == 0)
            {
                return;
            }

            _specialOrders.Add(Ui.Text($"Encargos de clientes ({world.Sales.SpecialOrders.Count})", 16));
            foreach ((Book book, int count, int daysLeft) in groups.Take(SpecialOrdersShown))
            {
                int coming = world.Orders.PendingQuantity(book.Id) + Session.Draft.Quantity(book.Id);
                string detail = $"{count} {(count == 1 ? "cliente" : "clientes")} · caduca en {daysLeft} días"
                    + (coming > 0 ? $" · {coming} en camino o en el borrador" : " · sin pedir");
                int missing = System.Math.Max(0, count - coming);
                if (missing > 0)
                {
                    _specialOrders.Add(Ui.Row(book.Title, detail, ("+ pedido", () =>
                    {
                        Session.Draft.Add(book.Id, missing);
                        Session.NotifyChanged();
                    })));
                }
                else
                {
                    _specialOrders.Add(Ui.Row(book.Title, detail));
                }
            }

            if (groups.Count > SpecialOrdersShown)
            {
                _specialOrders.Add(Ui.Text($"… y {groups.Count - SpecialOrdersShown} títulos más", 13));
            }
        }

        private string Summary(AdvanceResult result)
        {
            if (result == null)
            {
                return string.Empty;
            }

            var lines = new List<string>
            {
                result.Days.Count == 1
                    ? "Ha pasado 1 día."
                    : $"Han pasado {result.Days.Count} días ({result.DaysOpen} de apertura)."
            };

            if (result.Sales.Any())
            {
                lines.Add($"Ventas: {Formats.Units(result.UnitsSold)} por {Formats.Money(result.Revenue)} ({result.Visitors} visitantes).");
                IReadOnlyList<SaleLine> top = result.TopTitles(TopTitlesShown);
                if (top.Count > 0)
                {
                    lines.Add("Lo más vendido: " + string.Join(", ", top.Select(line => $"«{Session.World.Catalog.GetBook(line.BookId).Title}» ({line.Units})")));
                }

                if (result.LostSearches > 0)
                {
                    lines.Add($"{result.LostSearches} clientes buscaron algo que no había y se fueron sin nada.");
                }

                if (result.SpecialOrdersTaken > 0)
                {
                    lines.Add($"Nuevos encargos: {result.SpecialOrdersTaken}.");
                }
            }

            lines.AddRange(result.Events.Select(e => "• " + e.Title + (e.Amount.Cents > 0 ? " " + Formats.Money(e.Amount) : string.Empty)));

            if (result.CashChange.Cents != 0)
            {
                lines.Add("Caja: " + Formats.SignedMoney(result.CashChange));
            }

            lines.AddRange(result.PauseReasons.Select(Formats.PauseReason));
            return string.Join("\n", lines);
        }

        private void Show(IScreenPanel panel)
        {
            _openPanel = panel;
            panel.Refresh();
            _storeCard.style.display = DisplayStyle.None;
            foreach (IScreenPanel other in _panels)
            {
                other.Root.style.display = other == panel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void ShowStore()
        {
            _openPanel = null;
            foreach (IScreenPanel panel in _panels)
            {
                panel.Root.style.display = DisplayStyle.None;
            }

            _storeCard.style.display = DisplayStyle.Flex;
            Refresh();
        }

        private void Save()
        {
            Session.Save();
            _status.text = "Partida guardada.";
        }

        private void SaveAndExit()
        {
            Session.Save();
            SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}
