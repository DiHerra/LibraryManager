using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LibraryManager.Presentation.Common
{
    /// <summary>
    /// Ayudas para construir pantallas con UI Toolkit desde código. Provisional hasta
    /// L14, cuando las pantallas pasen a UXML/USS con el estilo final.
    /// </summary>
    public static class Ui
    {
        public static readonly Color Background = new Color(0.16f, 0.12f, 0.10f);
        public static readonly Color Paper = new Color(0.95f, 0.91f, 0.82f);
        public static readonly Color Accent = new Color(0.55f, 0.20f, 0.15f);

        public static VisualElement Screen(VisualElement root)
        {
            root.Clear();
            root.style.flexGrow = 1;
            root.style.backgroundColor = Background;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;

            var card = new VisualElement();
            card.style.backgroundColor = Paper;
            card.style.paddingTop = card.style.paddingBottom = 32;
            card.style.paddingLeft = card.style.paddingRight = 40;
            card.style.minWidth = 480;
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius = 8;
            card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 8;
            root.Add(card);
            return card;
        }

        public static Label Title(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 36;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Accent;
            label.style.marginBottom = 16;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            return label;
        }

        public static Label Text(string text, int size = 18)
        {
            var label = new Label(text);
            label.style.fontSize = size;
            label.style.color = Background;
            label.style.marginBottom = 6;
            return label;
        }

        public static Button Button(string text, Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.style.fontSize = 18;
            button.style.height = 40;
            button.style.marginTop = 8;
            return button;
        }
    }
}
