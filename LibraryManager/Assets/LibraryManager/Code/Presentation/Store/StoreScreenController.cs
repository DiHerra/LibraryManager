using System.Collections.Generic;
using System.Linq;
using LibraryManager.Application.Game;
using LibraryManager.Domain.Time;
using LibraryManager.Domain.World;
using LibraryManager.Presentation.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace LibraryManager.Presentation.Store
{
    /// <summary>Pantalla provisional de la tienda: día, temporada, caja, próximos eventos y avance del tiempo.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class StoreScreenController : MonoBehaviour
    {
        private const int UpcomingEventsShown = 4;

        private Label _name;
        private Label _date;
        private Label _openState;
        private Label _season;
        private Label _cash;
        private VisualElement _upcoming;
        private Label _summary;
        private Label _status;

        private GameSession Session => GameServices.Session;

        private void OnEnable()
        {
            if (!Session.HasWorld)
            {
                // Se abrió la escena directamente en el editor: volver al menú.
                SceneManager.LoadScene(SceneNames.MainMenu);
                return;
            }

            VisualElement card = Ui.Screen(GetComponent<UIDocument>().rootVisualElement);
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

            card.Add(Ui.Text("Próximos eventos", 16));
            _upcoming = new VisualElement();
            _upcoming.style.marginBottom = 8;
            card.Add(_upcoming);

            card.Add(Ui.Button("Avanzar día", () => Session.AdvanceDay()));
            card.Add(Ui.Button("Avanzar semana", () => Session.AdvanceWeek()));
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

            lines.AddRange(result.Events.Select(e => "• " + e.Title + (e.Kind == WorldEventKind.Payment ? " " + Formats.Money(e.Amount) : string.Empty)));

            if (result.CashChange.Cents != 0)
            {
                lines.Add("Caja: " + Formats.SignedMoney(result.CashChange));
            }

            lines.AddRange(result.PauseReasons.Select(Formats.PauseReason));
            return string.Join("\n", lines);
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
