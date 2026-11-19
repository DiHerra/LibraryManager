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

            // ScrollView para que la tarjeta no se salga de la pantalla si crece.
            var card = new ScrollView(ScrollViewMode.Vertical);
            card.style.maxHeight = Length.Percent(95);
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

        /// <summary>Panel a pantalla casi completa para listas (catálogo, pedidos, inventario).</summary>
        public static VisualElement Panel(string title)
        {
            var panel = new VisualElement();
            panel.style.width = Length.Percent(85);
            panel.style.height = Length.Percent(92);
            panel.style.borderTopLeftRadius = panel.style.borderTopRightRadius = 8;
            panel.style.borderBottomLeftRadius = panel.style.borderBottomRightRadius = 8;
            panel.style.backgroundColor = Paper;
            panel.style.paddingTop = panel.style.paddingBottom = 24;
            panel.style.paddingLeft = panel.style.paddingRight = 40;
            panel.style.display = DisplayStyle.None;
            panel.Add(Title(title));
            return panel;
        }

        public static ScrollView List()
        {
            var list = new ScrollView(ScrollViewMode.Vertical);
            list.style.flexGrow = 1;
            list.style.marginTop = 8;
            return list;
        }

        public static Label Section(string text)
        {
            Label label = Text(text, 18);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 12;
            return label;
        }

        /// <summary>Fila con texto a la izquierda y botones pequeños a la derecha.</summary>
        public static VisualElement Row(string title, string detail, params (string Text, Action OnClick)[] actions)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 6;

            var texts = new VisualElement();
            texts.style.flexGrow = 1;
            texts.style.flexShrink = 1;
            Label titleLabel = Text(title, 16);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 0;
            texts.Add(titleLabel);
            if (!string.IsNullOrEmpty(detail))
            {
                Label detailLabel = Text(detail, 13);
                detailLabel.style.whiteSpace = WhiteSpace.Normal;
                texts.Add(detailLabel);
            }

            row.Add(texts);
            foreach ((string text, Action onClick) in actions)
            {
                var button = new Button(onClick) { text = text };
                button.style.fontSize = 14;
                button.style.minWidth = 44;
                button.style.height = 30;
                row.Add(button);
            }

            return row;
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
