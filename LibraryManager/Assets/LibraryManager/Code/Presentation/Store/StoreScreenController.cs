using LibraryManager.Application.Game;
using LibraryManager.Presentation.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace LibraryManager.Presentation.Store
{
    /// <summary>Pantalla provisional de la tienda: fecha, caja, avanzar día y guardar.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class StoreScreenController : MonoBehaviour
    {
        private Label _name;
        private Label _date;
        private Label _cash;
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
            _cash = Ui.Text(string.Empty, 20);
            card.Add(_cash);

            card.Add(Ui.Button("Avanzar día", AdvanceDay));
            card.Add(Ui.Button("Guardar", Save));
            card.Add(Ui.Button("Guardar y volver al menú", SaveAndExit));

            _status = Ui.Text(string.Empty, 14);
            _status.style.marginTop = 12;
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
            _name.text = Session.World.Bookstore.Name;
            _date.text = Formats.Date(Session.World.Date);
            _cash.text = "Caja: " + Formats.Money(Session.World.Bookstore.Cash);
            _status.text = string.Empty;
        }

        private void AdvanceDay()
        {
            Session.AdvanceDay();
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
