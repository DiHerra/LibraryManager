using System.IO;
using System.Linq;
using LibraryManager.Presentation.Common;
using LibraryManager.Presentation.MainMenu;
using LibraryManager.Presentation.Store;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace LibraryManager.Editor
{
    /// <summary>
    /// Crea las escenas base (MainMenu y Store), el PanelSettings de UI Toolkit y la lista
    /// de escenas de la build. Se puede repetir: no duplica nada que ya exista.
    /// </summary>
    public static class ProjectSetupMenu
    {
        private const string Root = "Assets/LibraryManager";
        private const string ThemePath = Root + "/UI/LibraryTheme.tss";
        private const string PanelSettingsPath = Root + "/UI/LibraryPanelSettings.asset";
        private const string ScenesFolder = Root + "/Scenes";

        [MenuItem("Library Manager/Configurar escenas base")]
        public static void ConfigureScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsurePanelSettings();
            AssetDatabase.SaveAssets();
            string mainMenuPath = EnsureScene(SceneNames.MainMenu, typeof(MainMenuController));
            string storePath = EnsureScene(SceneNames.Store, typeof(StoreScreenController));

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(mainMenuPath, true),
                new EditorBuildSettingsScene(storePath, true)
            };

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(mainMenuPath);
            Debug.Log("Library Manager: escenas MainMenu y Store configuradas y añadidas a la build.");
        }

        private static void EnsurePanelSettings()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ThemePath));

            if (!File.Exists(ThemePath))
            {
                File.WriteAllText(ThemePath, "@import url(\"unity-theme://default\");\nVisualElement {}\n");
                AssetDatabase.ImportAsset(ThemePath);
            }

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(1920, 1080);
                panelSettings.match = 0.5f;
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            }

            panelSettings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            EditorUtility.SetDirty(panelSettings);
        }

        private static string EnsureScene(string sceneName, System.Type controllerType)
        {
            Directory.CreateDirectory(ScenesFolder);
            string path = $"{ScenesFolder}/{sceneName}.unity";

            var scene = File.Exists(path)
                ? EditorSceneManager.OpenScene(path, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject screen = scene.GetRootGameObjects().FirstOrDefault(gameObject => gameObject.name == "Screen");
            if (screen == null)
            {
                screen = new GameObject("Screen");
            }

            // GetComponent devuelve un "null falso" de Unity en el editor: no usar ?? aquí.
            UIDocument document = screen.GetComponent<UIDocument>();
            if (document == null)
            {
                document = screen.AddComponent<UIDocument>();
            }

            // Se carga después de abrir la escena: al cambiar de escena Unity descarga los
            // assets sin referencias y una referencia guardada antes quedaría vacía.
            document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (document.panelSettings == null)
            {
                throw new System.InvalidOperationException($"No se pudo cargar '{PanelSettingsPath}'.");
            }

            if (screen.GetComponent(controllerType) == null)
            {
                screen.AddComponent(controllerType);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }
    }
}
