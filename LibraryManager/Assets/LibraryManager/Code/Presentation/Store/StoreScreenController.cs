using System.Collections.Generic;
using System.Linq;
using LibraryManager.Application.Game;
using LibraryManager.Domain.Catalog;
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

        private Label _name;
        private Label _date;
        private Label _openState;
        private Label _season;
        private Label _cash;
        private Label _stock;
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

            _alerts = new VisualElement();
            card.Add(_alerts);

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

            RefreshAlerts(world);

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

            int stale = InventoryAlerts.StaleTitles(world.Inventory, world.Date).Count;
            if (stale > 0)
            {
                lines.Add($"{stale} títulos llevan más de {InventoryAlerts.StaleDays} días sin reponerse");
            }

            foreach (string line in lines)
            {
                Label label = Ui.Text("Aviso: " + line, 14);
                label.style.color = Ui.Accent;
                label.style.whiteSpace = WhiteSpace.Normal;
                _alerts.Add(label);
            }
        }

        private static string Summary(AdvanceResult result)
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
