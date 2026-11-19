using System;
using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Catalog;
using LibraryManager.Domain.World;
using LibraryManager.Presentation.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace LibraryManager.Presentation.Catalog
{
    /// <summary>
    /// Consulta del catálogo del mundo: los títulos más populares del día, filtrables por
    /// género, y las próximas novedades. Solo lectura hasta que lleguen los pedidos (L04).
    /// </summary>
    public sealed class CatalogPanel
    {
        private const int RowsShown = 40;
        private const int UpcomingDays = 30;
        private const string AllGenres = "Todos los géneros";

        private readonly Func<WorldState> _world;
        private readonly DropdownField _genre;
        private readonly ScrollView _list;
        private readonly Label _header;

        public CatalogPanel(Func<WorldState> world, Action onBack)
        {
            _world = world;

            Root = new VisualElement();
            Root.style.width = Length.Percent(85);
            Root.style.height = Length.Percent(92);
            Root.style.borderTopLeftRadius = Root.style.borderTopRightRadius = 8;
            Root.style.borderBottomLeftRadius = Root.style.borderBottomRightRadius = 8;
            Root.style.backgroundColor = Ui.Paper;
            Root.style.paddingTop = Root.style.paddingBottom = 24;
            Root.style.paddingLeft = Root.style.paddingRight = 40;

            Root.Add(Ui.Title("Catálogo"));
            _header = Ui.Text(string.Empty, 14);
            Root.Add(_header);

            var choices = new List<string> { AllGenres };
            choices.AddRange(((Genre[])Enum.GetValues(typeof(Genre))).Select(Formats.Genre));
            _genre = new DropdownField("Género", choices, 0);
            _genre.RegisterValueChangedCallback(_ => Refresh());
            Root.Add(_genre);

            _list = new ScrollView(ScrollViewMode.Vertical);
            _list.style.flexGrow = 1;
            _list.style.marginTop = 8;
            Root.Add(_list);

            Root.Add(Ui.Button("Volver a la tienda", onBack));
        }

        public VisualElement Root { get; }

        public void Refresh()
        {
            WorldState world = _world();
            BookCatalog catalog = world.Catalog;
            Genre? genre = _genre.index <= 0 ? (Genre?)null : (Genre)(_genre.index - 1);

            _header.text = $"{catalog.ReleasedBy(world.Date).Count()} títulos publicados · {catalog.Publishers.Count} editoriales · {catalog.Distributors.Count} distribuidoras";
            _list.Clear();

            _list.Add(Section("Más populares hoy"));
            foreach (Book book in catalog.MostPopular(world.Date, RowsShown, genre))
            {
                _list.Add(Row(book, world, catalog));
            }

            List<Book> upcoming = catalog.UpcomingWithin(world.Date, UpcomingDays)
                .Where(b => genre == null || b.Genre == genre)
                .ToList();
            _list.Add(Section($"Salen en los próximos {UpcomingDays} días ({upcoming.Count})"));
            foreach (Book book in upcoming)
            {
                _list.Add(Row(book, world, catalog));
            }
        }

        private static Label Section(string text)
        {
            Label label = Ui.Text(text, 18);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 12;
            return label;
        }

        private static VisualElement Row(Book book, WorldState world, BookCatalog catalog)
        {
            Publisher publisher = catalog.GetPublisher(book.PublisherId);
            var row = new VisualElement();
            row.style.marginBottom = 6;

            Label title = Ui.Text($"{book.Title} — {book.Author}", 16);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 0;
            row.Add(title);

            string phase = Formats.Phase(book.PhaseOn(world.Date));
            string when = book.IsReleased(world.Date) ? Formats.ShortDate(book.PublicationDate) : "sale el " + Formats.ShortDate(book.PublicationDate);
            row.Add(Ui.Text(
                $"{Formats.Genre(book.Genre)} · {Formats.Format(book.Format)} · {publisher.Name} · {Formats.Money(book.Price)} · {phase} ({when}) · interés {book.PopularityOn(world.Date):0}",
                13));
            return row;
        }
    }
}
