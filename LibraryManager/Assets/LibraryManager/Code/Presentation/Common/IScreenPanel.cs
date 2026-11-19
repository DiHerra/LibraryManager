using UnityEngine.UIElements;

namespace LibraryManager.Presentation.Common
{
    /// <summary>Panel que la tienda muestra en lugar de su tarjeta principal.</summary>
    public interface IScreenPanel
    {
        VisualElement Root { get; }

        void Refresh();
    }
}
