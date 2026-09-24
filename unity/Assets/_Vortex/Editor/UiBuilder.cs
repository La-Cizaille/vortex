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

        /// <summary>A button with a centred label.</summary>
        public static (Button Button, TMP_Text Label) Button(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image background = Box(Fixed<Image>(parent, name, anchor, position, size), new Color(1f, 1f, 1f, 0.12f), receivesPointer: true);
            Button button = background.gameObject.AddComponent<Button>();
            TMP_Text label = Label(Part<TextMeshProUGUI>(background.transform, "Texte", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), 20f, FontStyles.Normal, TextAlignmentOptions.Center);
            return (button, label);
        }
    }
}
