using System;
using System.Collections.Generic;
using System.Linq;
using LibraryManager.Application.Game;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.Store;
using LibraryManager.Domain.World;
using LibraryManager.Presentation.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace LibraryManager.Presentation.Store
{
    /// <summary>
    /// Plano esquemático de la tienda (decisión de Diego, 2026-11-22: esquema ahora, pixel
    /// art en L16). Al pulsar un mueble se ve lo expuesto y se puede colocar o retirar.
    /// </summary>
    public sealed class StorePlanPanel : IScreenPanel
    {
        private const int CellSize = 34;
        private const int BackroomRowsShown = 30;

        private readonly Func<GameSession> _session;
        private readonly VisualElement _plan;
        private readonly Label _fixtureTitle;
        private readonly ScrollView _details;
        private readonly Label _status;
        private int _selectedFixtureId;

        public StorePlanPanel(Func<GameSession> session, Action onBack)
        {
            _session = session;
            Root = Ui.Panel("La tienda");

            var top = new VisualElement();
            top.style.flexDirection = FlexDirection.Row;
            top.style.flexGrow = 1;
            Root.Add(top);

            var left = new VisualElement();
            left.style.width = CellSize * DefaultStoreLayout.GridWidth + 4;
            left.style.marginRight = 16;
            top.Add(left);

            _plan = new VisualElement();
            _plan.style.width = CellSize * DefaultStoreLayout.GridWidth;
            _plan.style.height = CellSize * DefaultStoreLayout.GridHeight;
            _plan.style.backgroundColor = new Color(0.88f, 0.83f, 0.72f);
            _plan.style.borderTopWidth = _plan.style.borderBottomWidth = _plan.style.borderLeftWidth = _plan.style.borderRightWidth = 2;
            _plan.style.borderTopColor = _plan.style.borderBottomColor = _plan.style.borderLeftColor = _plan.style.borderRightColor = Ui.Background;
            left.Add(_plan);
            left.Add(Ui.Text("Entrada abajo. Visibilidad: escaparate ×2,5 · mesa de novedades ×1,8 · expositor de caja ×1,5 · estantería de su sección ×1 · otra sección ×0,6 · almacén ×0,2.", 12));
            left.Add(Ui.Button("Colocar automáticamente", AutoArrange));

            var right = new VisualElement();
            right.style.flexGrow = 1;
            right.style.flexShrink = 1;
            top.Add(right);
            _fixtureTitle = Ui.Section(string.Empty);
            right.Add(_fixtureTitle);
            _details = Ui.List();
            right.Add(_details);

            _status = Ui.Text(string.Empty, 14);
            Root.Add(_status);
            Root.Add(Ui.Button("Volver a la tienda", onBack));
        }

        public VisualElement Root { get; }

        public void Refresh()
        {
            WorldState world = _session().World;
            StoreLayout layout = world.Layout;
            if (_selectedFixtureId == 0 || layout.Fixtures.All(f => f.Id != _selectedFixtureId))
            {
                _selectedFixtureId = layout.Fixtures[0].Id;
            }

            DrawPlan(layout);
            DrawDetails(world);
        }

        private void DrawPlan(StoreLayout layout)
        {
            _plan.Clear();
            foreach (Fixture fixture in layout.Fixtures)
            {
                int used = layout.UsedCapacity(fixture.Id);
                var box = new Button(() => Select(fixture.Id))
                {
                    text = $"{ShortName(fixture)}\n{used}/{fixture.Capacity}"
                };
                box.style.position = Position.Absolute;
                box.style.left = fixture.X * CellSize;
                box.style.top = fixture.Y * CellSize;
                box.style.width = fixture.Width * CellSize - 2;
                box.style.height = fixture.Height * CellSize - 2;
                box.style.marginLeft = box.style.marginTop = box.style.marginRight = box.style.marginBottom = 0;
                box.style.paddingLeft = box.style.paddingRight = 1;
                box.style.fontSize = 10;
                box.style.whiteSpace = WhiteSpace.Normal;
                box.style.backgroundColor = ColorFor(fixture, used);
                if (fixture.Id == _selectedFixtureId)
                {
                    box.style.borderTopWidth = box.style.borderBottomWidth = box.style.borderLeftWidth = box.style.borderRightWidth = 3;
                    box.style.borderTopColor = box.style.borderBottomColor = box.style.borderLeftColor = box.style.borderRightColor = Ui.Accent;
                }

                _plan.Add(box);
            }

            var door = new Label("PUERTA");
            door.style.position = Position.Absolute;
            door.style.left = 9 * CellSize;
            door.style.top = 13 * CellSize;
            door.style.width = 3 * CellSize;
            door.style.unityTextAlign = TextAnchor.MiddleCenter;
            door.style.fontSize = 11;
            _plan.Add(door);
        }

        private void DrawDetails(WorldState world)
        {
            StoreLayout layout = world.Layout;
            Fixture fixture = layout.GetFixture(_selectedFixtureId);
            _fixtureTitle.text = $"{Formats.Fixture(fixture)} · {layout.UsedCapacity(fixture.Id)}/{fixture.Capacity} ejemplares";
            _details.Clear();

            _details.Add(Ui.Section("Expuesto"));
            List<Placement> placements = layout.PlacementsOn(fixture.Id).ToList();
            if (placements.Count == 0)
            {
                _details.Add(Ui.Text("Vacío.", 14));
            }

            foreach (Placement placement in placements.OrderByDescending(p => world.Catalog.GetBook(p.BookId).PopularityOn(world.Date)))
            {
                Book book = world.Catalog.GetBook(placement.BookId);
                int bookId = book.Id;
                _details.Add(Ui.Row(
                    $"{placement.Quantity} × {book.Title}",
                    $"{Formats.Genre(book.Genre)} · {Formats.Phase(book.PhaseOn(world.Date))} · interés {book.PopularityOn(world.Date):0} · {Formats.Visibility(StoreLayout.VisibilityOf(fixture, book.Genre))}",
                    ("−1", () => Remove(bookId, 1)),
                    ("Quitar", () => Remove(bookId, placement.Quantity))));
            }

            _details.Add(Ui.Section("En el almacén"));
            List<(Book Book, int Quantity)> backroom = world.Inventory.BookIds
                .Select(id => (Book: world.Catalog.GetBook(id), Quantity: world.Inventory.Quantity(id) - layout.DisplayedQuantity(id)))
                .Where(item => item.Quantity > 0)
                .OrderByDescending(item => fixture.Section == item.Book.Genre)
                .ThenByDescending(item => item.Book.PopularityOn(world.Date))
                .ThenBy(item => item.Book.Id)
                .Take(BackroomRowsShown)
                .ToList();
            if (backroom.Count == 0)
            {
                _details.Add(Ui.Text("No hay nada en el almacén: todo está expuesto.", 14));
            }

            foreach ((Book book, int quantity) in backroom)
            {
                int bookId = book.Id;
                _details.Add(Ui.Row(
                    $"{quantity} × {book.Title}",
                    $"{Formats.Genre(book.Genre)} · {Formats.Phase(book.PhaseOn(world.Date))} · interés {book.PopularityOn(world.Date):0} · aquí {Formats.Visibility(StoreLayout.VisibilityOf(fixture, book.Genre))}",
                    ("+1", () => Place(bookId, 1)),
                    ("Todos", () => Place(bookId, Math.Min(quantity, world.Layout.FreeCapacity(fixture.Id))))));
            }
        }

        private void Select(int fixtureId)
        {
            _selectedFixtureId = fixtureId;
            _status.text = string.Empty;
            Refresh();
        }

        private void Place(int bookId, int quantity)
        {
            Try(() => _session().PlaceOnFixture(_selectedFixtureId, bookId, quantity));
        }

        private void Remove(int bookId, int quantity)
        {
            Try(() => _session().RemoveFromFixture(_selectedFixtureId, bookId, quantity));
        }

        private void AutoArrange()
        {
            Try(() => _session().AutoArrangeStore());
            _status.text = "Tienda recolocada.";
        }

        private void Try(Action action)
        {
            try
            {
                _status.text = string.Empty;
                action();
            }
            catch (StoreLayoutException exception)
            {
                _status.text = exception.Message;
            }

            Refresh();
        }

        private static string ShortName(Fixture fixture)
        {
            switch (fixture.Kind)
            {
                case FixtureKind.ShopWindow: return "Escaparate";
                case FixtureKind.NewReleaseTable: return "Novedades";
                case FixtureKind.CounterDisplay: return "Caja";
                default: return fixture.Section.HasValue ? Formats.Genre(fixture.Section.Value) : fixture.Name;
            }
        }

        private static Color ColorFor(Fixture fixture, int used)
        {
            Color baseColor;
            switch (fixture.Kind)
            {
                case FixtureKind.ShopWindow: baseColor = new Color(0.55f, 0.75f, 0.90f); break;
                case FixtureKind.NewReleaseTable: baseColor = new Color(0.95f, 0.75f, 0.45f); break;
                case FixtureKind.CounterDisplay: baseColor = new Color(0.80f, 0.65f, 0.85f); break;
                default: baseColor = new Color(0.65f, 0.50f, 0.35f); break;
            }

            // Más claro cuanto más vacío.
            float fill = fixture.Capacity == 0 ? 0f : used / (float)fixture.Capacity;
            return Color.Lerp(Color.white, baseColor, 0.35f + 0.65f * fill);
        }
    }
}
