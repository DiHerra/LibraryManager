using System;
using LibraryManager.Application.Saves;
using LibraryManager.Presentation.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace LibraryManager.Presentation.MainMenu
{
    /// <summary>Menú principal: nueva partida, continuar y salir.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        private TextField _nameField;
        private Label _status;

        private void OnEnable()
        {
            VisualElement card = Ui.Screen(GetComponent<UIDocument>().rootVisualElement);
            card.Add(Ui.Title("Library Manager"));

            _nameField = new TextField("Nombre de la librería") { value = "La Página en Blanco", maxLength = 40 };
            _nameField.style.marginBottom = 8;
            card.Add(_nameField);

            card.Add(Ui.Button("Nueva partida", StartNewGame));

            Button continueButton = Ui.Button("Continuar", ContinueGame);
            continueButton.SetEnabled(GameServices.Session.HasSavedGame);
            card.Add(continueButton);

            card.Add(Ui.Button("Salir", Quit));

            _status = Ui.Text(string.Empty, 14);
            _status.style.marginTop = 12;
            card.Add(_status);
        }

        private void StartNewGame()
        {
            try
            {
                ulong seed = (ulong)DateTime.UtcNow.Ticks;
                GameServices.Session.StartNew(_nameField.value, seed);
                GameServices.Session.Save();
                SceneManager.LoadScene(SceneNames.Store);
            }
            catch (ArgumentException exception)
            {
                _status.text = exception.Message;
            }
        }

        private void ContinueGame()
        {
            try
            {
                GameServices.Session.Load();
                SceneManager.LoadScene(SceneNames.Store);
            }
            catch (SaveGameException exception)
            {
                _status.text = "No se pudo cargar la partida: " + exception.Message;
            }
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}
