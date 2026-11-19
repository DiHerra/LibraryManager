using System;
using System.Collections.Generic;
using System.Linq;
using LibraryManager.Application.Game;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.World;
using LibraryManager.Presentation.Common;
using UnityEngine.UIElements;

namespace LibraryManager.Presentation.Catalog
{
    /// <summary>
    /// Catálogo del mundo: los títulos más populares del día, filtrables por género, y las
    /// próximas novedades. Desde aquí se añaden libros al borrador de pedido.
    /// </summary>
    public sealed class CatalogPanel : IScreenPanel
    {
        private const int RowsShown = 40;
        private const int UpcomingDays = 30;
        private const string AllGenres = "Todos los géneros";

        private readonly Func<GameSession> _session;
        private readonly DropdownField _genre;
        private readonly ScrollView _list;
        private readonly Label _header;

        public CatalogPanel(Func<GameSession> session, Action onBack)
        {
            _session = session;

            Root = Ui.Panel("Catálogo");
            _header = Ui.Text(string.Empty, 14);
            Root.Add(_header);

            var choices = new List<string> { AllGenres };
            choices.AddRange(((Genre[])Enum.GetValues(typeof(Genre))).Select(Formats.Genre));
            _genre = new DropdownField("Género", choices, 0);
            _genre.RegisterValueChangedCallback(_ => Refresh());
            Root.Add(_genre);

            _list = Ui.List();
            Root.Add(_list);
            Root.Add(Ui.Button("Volver a la tienda", onBack));
        }

        public VisualElement Root { get; }

        public void Refresh()
        {
            GameSession session = _session();
            WorldState world = session.World;
            BookCatalog catalog = world.Catalog;
            Genre? genre = _genre.index <= 0 ? (Genre?)null : (Genre)(_genre.index - 1);

            int draftUnits = session.Draft.ByDistributor(catalog).Sum(group => group.Units);
            _header.text = $"{catalog.ReleasedBy(world.Date).Count()} títulos publicados · {catalog.Publishers.Count} editoriales · " +
                           $"{catalog.Distributors.Count} distribuidoras · en el borrador de pedido: {draftUnits} ejemplares";
            _list.Clear();

            _list.Add(Ui.Section("Más populares hoy"));
            foreach (Book book in catalog.MostPopular(world.Date, RowsShown, genre))
            {
                _list.Add(Row(book, session, true));
            }

            List<Book> upcoming = catalog.UpcomingWithin(world.Date, UpcomingDays)
                .Where(b => genre == null || b.Genre == genre)
                .ToList();
            _list.Add(Ui.Section($"Salen en los próximos {UpcomingDays} días ({upcoming.Count})"));
            foreach (Book book in upcoming)
            {
                _list.Add(Row(book, session, false));
            }
        }

        private VisualElement Row(Book book, GameSession session, bool canOrder)
        {
            WorldState world = session.World;
            BookCatalog catalog = world.Catalog;
            Publisher publisher = catalog.GetPublisher(book.PublisherId);
            string phase = Formats.Phase(book.PhaseOn(world.Date));
            string when = book.IsReleased(world.Date) ? Formats.ShortDate(book.PublicationDate) : "sale el " + Formats.ShortDate(book.PublicationDate);
            int inStock = world.Inventory.Quantity(book.Id);
            int onOrder = world.Orders.PendingQuantity(book.Id);
            int inDraft = session.Draft.Quantity(book.Id);

            string detail = $"{Formats.Genre(book.Genre)} · {Formats.Format(book.Format)} · {publisher.Name} · {Formats.Money(book.Price)} · " +
                            $"{phase} ({when}) · interés {book.PopularityOn(world.Date):0} · en tienda {inStock}" +
                            (onOrder > 0 ? $" · en camino {onOrder}" : string.Empty) +
                            (inDraft > 0 ? $" · en el borrador {inDraft}" : string.Empty);

            if (!canOrder)
            {
                return Ui.Row($"{book.Title} — {book.Author}", detail);
            }

            return Ui.Row($"{book.Title} — {book.Author}", detail,
                ("+1", () => AddToDraft(book.Id, 1)),
                ("+5", () => AddToDraft(book.Id, 5)));
        }

        private void AddToDraft(int bookId, int quantity)
        {
            _session().Draft.Add(bookId, quantity);
            Refresh();
        }
    }
}
