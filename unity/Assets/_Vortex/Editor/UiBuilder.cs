using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Vortex.Editor
{
    /// <summary>
    /// Small helpers to build interface layouts from code (card prefab, seat panels, scenes). What they build belongs to
    /// the designer afterwards: it is saved once, then edited in Unity.
    /// </summary>
    internal static class UiBuilder
    {
        /// <summary>Rounded rectangle of the built-in interface resources (nine-sliced).</summary>
        public static Sprite RoundedBox => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        /// <summary>Disc of the built-in interface resources.</summary>
        public static Sprite Disc => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        /// <summary>A root canvas scaled from the 1920 x 1080 reference, with its raycaster.</summary>
        public static Canvas Canvas(string name, RenderMode mode, Camera? camera, int order)
        {
            var canvas = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = mode;
            canvas.worldCamera = camera;
            canvas.sortingOrder = order;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        /// <summary>Creates a child with a component, placed by anchors and offsets.</summary>
        public static T Part<T>(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
            where T : Component
        {
            var part = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)part.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return typeof(T) == typeof(RectTransform) ? (T)(Component)rect : part.AddComponent<T>();
        }

        /// <summary>Creates a child of a fixed size, placed from an anchor point (pivot on the same point).</summary>
        public static T Fixed<T>(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
            where T : Component
        {
            T part = Part<T>(parent, name, anchor, anchor, Vector2.zero, Vector2.zero);
            var rect = (RectTransform)part.transform;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return part;
        }

        /// <summary>A text label that never takes pointer events.</summary>
        public static TMP_Text Label(TMP_Text label, float size, FontStyles style, TextAlignmentOptions alignment)
        {
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>A plain background: a rounded box in a colour.</summary>
        public static Image Box(Image image, Color color, bool receivesPointer = false)
        {
            image.sprite = RoundedBox;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = receivesPointer;
            return image;
        }

        /// <summary>Bone: the light text on blackened metal (docs/DIRECTION_ARTISTIQUE.md 6.1).</summary>
        public static readonly Color Bone = new Color32(207, 196, 168, 255);

        /// <summary>A soft rounded panel (ThemeSprites), nine-sliced, tinted; the rounded box while it is not drawn.</summary>
        public static Image Plate(Image image, Color tint, bool receivesPointer = false)
        {
            Sprite plate = ThemeSprites.Plate;
            image.sprite = plate != null ? plate : RoundedBox;
            image.type = Image.Type.Sliced;
            image.color = tint;
            image.raycastTarget = receivesPointer;
            return image;
        }

        /// <summary>A rounded outline (ThemeSprites), nine-sliced; the rounded box while it is not drawn.</summary>
        public static Image Outline(Image image)
        {
            Sprite outline = ThemeSprites.Outline;
            image.sprite = outline != null ? outline : RoundedBox;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A terminal screen (ThemeSprites), nine-sliced; a dark box while it is not drawn.</summary>
        public static Image Screen(Image image)
        {
            Sprite screen = ThemeSprites.Screen;
            image.sprite = screen != null ? screen : RoundedBox;
            image.type = Image.Type.Sliced;
            image.color = screen != null ? Color.white : new Color(0.02f, 0.04f, 0.03f, 1f);
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A strip of hazard stripes (ThemeSprites), tiled.</summary>
        public static Image Hazard(Image image)
        {
            image.sprite = ThemeSprites.Hazard;
            image.type = Image.Type.Tiled;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Gives a label a font of the theme, when the theme has it.</summary>
        public static TMP_Text Font(TMP_Text label, TMP_FontAsset? font)
        {
            if (font != null)
            {
                label.font = font;
            }

            return label;
        }

        /// <summary>A button: a soft rounded panel with a centred label in bone.</summary>
        public static (Button Button, TMP_Text Label) Button(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image background = Plate(Fixed<Image>(parent, name, anchor, position, size), Color.white, receivesPointer: true);
            Button button = background.gameObject.AddComponent<Button>();
            TMP_Text label = Label(Part<TextMeshProUGUI>(background.transform, "Texte", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), 20f, FontStyles.Normal, TextAlignmentOptions.Center);
            label.color = Bone;
            return (button, label);
        }
    }
}
