using System;
using System.Collections.Generic;
using System.Linq;
using LibraryManager.Domain.Time;

namespace LibraryManager.Domain.Catalog
{
    /// <summary>Todos los títulos, editoriales y distribuidoras del mundo.</summary>
    public sealed class BookCatalog
    {
        private readonly Dictionary<int, Book> _books;
        private readonly Dictionary<int, Publisher> _publishers;
        private readonly Dictionary<int, Distributor> _distributors;

        public BookCatalog(IEnumerable<Distributor> distributors, IEnumerable<Publisher> publishers, IEnumerable<Book> books)
        {
            _distributors = ToUniqueDictionary(distributors, d => d.Id, "distribuidora");
            _publishers = ToUniqueDictionary(publishers, p => p.Id, "editorial");
            _books = ToUniqueDictionary(books, b => b.Id, "libro");

            foreach (Publisher publisher in _publishers.Values)
            {
                if (!_distributors.ContainsKey(publisher.DistributorId))
                {
                    throw new ArgumentException($"La editorial {publisher.Name} usa una distribuidora que no existe ({publisher.DistributorId}).");
                }
            }

            foreach (Book book in _books.Values)
            {
                if (!_publishers.ContainsKey(book.PublisherId))
                {
                    throw new ArgumentException($"El libro {book.Title} usa una editorial que no existe ({book.PublisherId}).");
                }
            }

            Books = _books.Values.OrderBy(b => b.Id).ToList();
            Publishers = _publishers.Values.OrderBy(p => p.Id).ToList();
            Distributors = _distributors.Values.OrderBy(d => d.Id).ToList();
        }

        public static BookCatalog Empty => new BookCatalog(new Distributor[0], new Publisher[0], new Book[0]);

        public IReadOnlyList<Book> Books { get; }
        public IReadOnlyList<Publisher> Publishers { get; }
        public IReadOnlyList<Distributor> Distributors { get; }

        public Book GetBook(int id) => _books.TryGetValue(id, out Book book) ? book : throw new KeyNotFoundException($"No existe el libro {id}.");
        public Publisher GetPublisher(int id) => _publishers.TryGetValue(id, out Publisher publisher) ? publisher : throw new KeyNotFoundException($"No existe la editorial {id}.");
        public Distributor GetDistributor(int id) => _distributors.TryGetValue(id, out Distributor distributor) ? distributor : throw new KeyNotFoundException($"No existe la distribuidora {id}.");

        public Distributor DistributorOf(Book book) => GetDistributor(GetPublisher(book.PublisherId).DistributorId);

        public IEnumerable<Book> ReleasedBy(GameDate date) => Books.Where(b => b.IsReleased(date));

        /// <summary>Títulos que salen entre mañana y dentro de <paramref name="days"/> días.</summary>
        public IEnumerable<Book> UpcomingWithin(GameDate date, int days) =>
            Books.Where(b => b.PublicationDate > date && b.PublicationDate <= date.AddDays(days)).OrderBy(b => b.PublicationDate).ThenBy(b => b.Id);

        /// <summary>Los más populares ese día, de mayor a menor (desempate por id para que sea estable).</summary>
        public IReadOnlyList<Book> MostPopular(GameDate date, int count, Genre? genre = null) =>
            ReleasedBy(date)
                .Where(b => genre == null || b.Genre == genre)
                .OrderByDescending(b => b.PopularityOn(date))
                .ThenBy(b => b.Id)
                .Take(count)
                .ToList();

        private static Dictionary<int, T> ToUniqueDictionary<T>(IEnumerable<T> items, Func<T, int> key, string label)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var result = new Dictionary<int, T>();
            foreach (T item in items)
            {
                if (item == null)
                {
                    throw new ArgumentException($"Hay un elemento vacío ({label}).");
                }

                int id = key(item);
                if (result.ContainsKey(id))
                {
                    throw new ArgumentException($"Identificador de {label} repetido: {id}.");
                }

                result.Add(id, item);
            }

            return result;
        }
    }
}
