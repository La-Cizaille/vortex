using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Menus;
using Vortex.Client.Theme;

namespace Vortex.Editor
{
    /// <summary>
    /// Builds the pieces shared by the menus of both scenes (windows in a column, buttons, text fields, the options). What
    /// is built belongs to the designer afterwards, like the rest of the scenes.
    /// </summary>
    internal static class MenuBuilder
    {
        /// <summary>Background of the menu windows.</summary>
        public static readonly Color WindowColor = new Color(0.04f, 0.05f, 0.1f, 0.95f);

        /// <summary>Background of the text fields.</summary>
        public static readonly Color FieldColor = new Color(0.08f, 0.07f, 0.06f, 0.12f);

        /// <summary>Hints and secondary texts.</summary>
        public static readonly Color Muted = new Color32(150, 156, 175, 255);

        /// <summary>The theme, for its fonts (editor setup).</summary>
        public static ThemeSettings Theme => AssetDatabase.LoadAssetAtPath<ThemeSettings>(ThemeAssets.ThemePath);

        /// <summary>A label in ink, for the paper windows.</summary>
        public static TMP_Text Inked(TMP_Text label)
        {
            label.color = UiBuilder.Ink;
            return label;
        }

        /// <summary>A screen-wide veil that takes the pointer: what is behind it cannot be touched.</summary>
        public static Image Veil(Transform parent, string name)
        {
            Image veil = UiBuilder.Part<Image>(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            veil.color = new Color(0f, 0f, 0f, 0.72f);
            veil.raycastTarget = true;
            return veil;
        }

        /// <summary>A window whose parts stack in a column, centred on its anchor; its height follows its content.</summary>
        public static Image Window(Transform parent, string name, Vector2 anchor, Vector2 position, float width)
        {
            // A poster on the wall of the sector (docs/DIRECTION_ARTISTIQUE.md 6.5): ink on yellowed paper.
            Image window = UiBuilder.Paper(UiBuilder.Fixed<Image>(parent, name, anchor, position, new Vector2(width, 100f)), receivesPointer: true);
            window.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            VerticalLayoutGroup column = window.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(32, 32, 28, 28);
            column.spacing = 14f;
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            window.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return window;
        }

        /// <summary>A title line of a window.</summary>
        public static TMP_Text Title(Transform window, float size)
        {
            TMP_Text title = UiBuilder.Font(UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(window, "Titre", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), size, FontStyles.Normal, TextAlignmentOptions.Center), Theme.TitleFont);
            title.color = UiBuilder.Blood;
            title.characterSpacing = 2f;
            Height(title.gameObject, size * 1.6f);
            return title;
        }

        /// <summary>A text line of a window.</summary>
        public static TMP_Text Line(Transform window, string name, float size)
        {
            TMP_Text line = UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(window, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), size, FontStyles.Normal, TextAlignmentOptions.Center);
            line.color = UiBuilder.Ink;
            Height(line.gameObject, size * 1.6f);
            return line;
        }

        /// <summary>A button of a column (full width) or of a row (<paramref name="width"/>).</summary>
        public static Button Button(Transform parent, string name, float height, float width = -1f, float fontSize = 24f)
        {
            (Button button, TMP_Text label) = UiBuilder.Button(parent, name, Vector2.zero, Vector2.zero, new Vector2(width > 0f ? width : 100f, height));
            // On the paper windows: a solid ink button, the label in bone (lot 4).
            Image background = button.GetComponent<Image>();
            background.sprite = UiBuilder.RoundedBox;
            background.color = new Color(0.1f, 0.095f, 0.09f, 1f);
            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = fontSize;
            LayoutElement layout = Height(button.gameObject, height);
            if (width > 0f)
            {
                layout.preferredWidth = width;
                layout.minWidth = width;
            }

            return button;
        }

        /// <summary>A row of parts, laid out from left to right, centred.</summary>
        public static HorizontalLayoutGroup Row(Transform parent, string name, float height)
        {
            HorizontalLayoutGroup row = UiBuilder.Part<HorizontalLayoutGroup>(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.spacing = 12f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;
            Height(row.gameObject, height);
            return row;
        }

        /// <summary>A one-line text field with a hint; rich text off.</summary>
        public static TMP_InputField Field(Transform parent, string name, float width, float height)
        {
            Image background = UiBuilder.Box(UiBuilder.Fixed<Image>(parent, name, Vector2.zero, Vector2.zero, new Vector2(width, height)), FieldColor, receivesPointer: true);
            LayoutElement layout = Height(background.gameObject, height);
            layout.preferredWidth = width;
            layout.minWidth = width;
            RectTransform area = UiBuilder.Part<RectTransform>(background.transform, "Zone de texte", Vector2.zero, Vector2.one, new Vector2(14f, 6f), new Vector2(-14f, -6f));
            area.gameObject.AddComponent<RectMask2D>();
            TMP_Text hint = UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(area, "Indication", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), 22f, FontStyles.Italic, TextAlignmentOptions.Left);
            hint.color = new Color(UiBuilder.Ink.r, UiBuilder.Ink.g, UiBuilder.Ink.b, 0.45f);
            TMP_Text text = UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(area, "Texte", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), 22f, FontStyles.Normal, TextAlignmentOptions.Left);
            text.color = UiBuilder.Ink;
            TMP_InputField field = background.gameObject.AddComponent<TMP_InputField>();
            field.targetGraphic = background;
            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = hint;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.richText = false;
            return field;
        }

        /// <summary>The options window (INTERFACE.md 5), hidden until opened.</summary>
        public static OptionsMenu Options(Transform parent)
        {
            Image window = Window(parent, "Options", new Vector2(0.5f, 0.5f), Vector2.zero, 640f);
            TMP_Text title = Title(window.transform, 40f);
            Button speed = Button(window.transform, "Vitesse", 64f);
            Button commentary = Button(window.transform, "Commentaires", 64f);
            Button fullScreen = Button(window.transform, "Plein écran", 64f);
            Button resolution = Button(window.transform, "Résolution", 64f);
            Button back = Button(window.transform, "Retour", 64f);
            OptionsMenu options = window.gameObject.AddComponent<OptionsMenu>();
            options.Assign(title, speed, commentary, fullScreen, resolution, back);
            window.gameObject.SetActive(false);
            return options;
        }

        private static LayoutElement Height(GameObject part, float height)
        {
            // TryGetComponent: in the editor, a missing component comes back as a "fake null" that ?? would keep.
            LayoutElement layout = part.TryGetComponent(out LayoutElement existing) ? existing : part.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;
            return layout;
        }
    }
}
