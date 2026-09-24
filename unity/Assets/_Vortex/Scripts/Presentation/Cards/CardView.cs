using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Theme;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Shows one card: a frame in its technology colour, its illustration (or a generated placeholder tinted with that
    /// colour), its name, kind, text and id. The layout lives in the Card prefab, which the designer edits freely; this
    /// component only fills it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private Image frame = null!;
        [SerializeField] private Image background = null!;
        [SerializeField] private Image art = null!;
        [SerializeField] private TMP_Text title = null!;
        [SerializeField] private TMP_Text caption = null!;
        [SerializeField] private TMP_Text body = null!;
        [SerializeField] private TMP_Text id = null!;

        /// <summary>The face shown.</summary>
        public CardFace Face { get; private set; }

        /// <summary>True while the card shows a generated placeholder instead of its art.</summary>
        public bool ShowsPlaceholder { get; private set; }

        /// <summary>Text given to the body label (TextMeshPro rich text).</summary>
        public string BodyText => body.text;

        /// <summary>Shows a card with the current theme.</summary>
        public void Show(CardFace face, ThemeSettings theme, CardArtCatalog catalog)
        {
            Face = face;
            Color color = theme.Technology(face.Color);
            frame.color = color;
            background.color = theme.CardBackground;

            CardArt cardArt = catalog.ArtFor(face.Id);
            art.sprite = cardArt.Sprite;
            art.color = cardArt.IsPlaceholder ? color : Color.white;
            ShowsPlaceholder = cardArt.IsPlaceholder;

            // Names and ids are plain text; only the card text is rich text, converted by CardText.
            Label(title, face.Title, theme.TitleFont, theme.Text, richText: false);
            Label(caption, face.Caption, theme.BodyFont, theme.MutedText, richText: false);
            Label(id, face.Id, theme.BodyFont, theme.MutedText, richText: false);
            Label(body, CardText.ToRichText(face.Text, theme.HasTextIcon), theme.BodyFont, theme.Text, richText: true);
            if (theme.TextIcons != null)
            {
                body.spriteAsset = theme.TextIcons;
            }
        }

        /// <summary>Wires the prefab's parts (editor setup).</summary>
        public void Assign(Image frameImage, Image backgroundImage, Image artImage, TMP_Text titleLabel, TMP_Text captionLabel, TMP_Text bodyLabel, TMP_Text idLabel)
        {
            frame = frameImage;
            background = backgroundImage;
            art = artImage;
            title = titleLabel;
            caption = captionLabel;
            body = bodyLabel;
            id = idLabel;
        }

        private static void Label(TMP_Text label, string text, TMP_FontAsset? font, Color color, bool richText)
        {
            label.richText = richText;
            label.text = text;
            label.color = color;
            TMP_FontAsset? chosen = font != null ? font : TMP_Settings.defaultFontAsset;
            if (chosen != null)
            {
                label.font = chosen;
            }
        }
    }
}
