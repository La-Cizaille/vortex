using System.Globalization;
using TMPro;
using UnityEngine;
using Vortex.Client.Theme;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A card as a 3D object (ADR-0017): a body in its technology colour, its illustration (or a generated placeholder
    /// tinted with that colour), its name, kind, text and id, and its Torment tokens. The front faces the card's -Z axis,
    /// the back its +Z axis. The look lives in the Card prefab and its materials, which the designer edits freely (a
    /// modelled body can replace the box); this component only fills them.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CardDisplay : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseMapTransform = Shader.PropertyToID("_BaseMap_ST");

        [SerializeField] private Renderer frame = null!;
        [SerializeField] private Renderer background = null!;
        [SerializeField] private Renderer art = null!;
        [SerializeField] private TMP_Text title = null!;
        [SerializeField] private TMP_Text caption = null!;
        [SerializeField] private TMP_Text body = null!;
        [SerializeField] private TMP_Text id = null!;
        [SerializeField] private GameObject? tormentBadge;
        [SerializeField] private TMP_Text? tormentCount;
        [SerializeField] private GameObject visual = null!;
        [SerializeField] private Collider pointerArea = null!;
        [Tooltip("Taille de la carte, en unités de la scène (largeur, hauteur).")]
        [SerializeField] private Vector2 size = new Vector2(1f, 1.4f);

        private MaterialPropertyBlock? _block;
        private bool _pointable = true;

        /// <summary>The face shown.</summary>
        public CardFace Face { get; private set; }

        /// <summary>Torment tokens shown on the card.</summary>
        public int Torments { get; private set; }

        /// <summary>True while the card shows a generated placeholder instead of its art.</summary>
        public bool ShowsPlaceholder { get; private set; }

        /// <summary>Text given to the body label (TextMeshPro rich text).</summary>
        public string BodyText => body.text;

        /// <summary>Size of the card in scene units, before any scaling.</summary>
        public Vector2 Size => size;

        /// <summary>False while the card is hidden (for instance, scrolled out of view).</summary>
        public bool Visible => visual.activeSelf;

        /// <summary>Shows a card with the current theme.</summary>
        public void Show(CardFace face, ThemeSettings theme, CardArtCatalog catalog)
        {
            Face = face;
            Marked = false;
            Color color = theme.Technology(face.Color);
            Paint(frame, color, null);
            Paint(background, theme.CardBackground, null);

            CardArt cardArt = catalog.ArtFor(face.Id);
            Paint(art, cardArt.IsPlaceholder ? color : Color.white, cardArt.Sprite);
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

        /// <summary>True while the card's frame is marked.</summary>
        public bool Marked { get; private set; }

        /// <summary>
        /// Marks the frame with <paramref name="color"/> (the card a purchase would replace), or gives it back its
        /// technology colour with null.
        /// </summary>
        public void Mark(Color? color, ThemeSettings theme)
        {
            Marked = color.HasValue;
            Paint(frame, color ?? theme.Technology(Face.Color), null);
        }

        /// <summary>Shows the Torment tokens on the card (RULES A7); the badge is hidden when there is none.</summary>
        public void ShowTorments(int count)
        {
            Torments = count;
            if (tormentBadge != null)
            {
                tormentBadge.SetActive(count > 0);
            }

            if (tormentCount != null)
            {
                tormentCount.text = count.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>Hides or shows the card without destroying it (its pointer area follows).</summary>
        public void SetVisible(bool shown)
        {
            if (visual.activeSelf != shown)
            {
                visual.SetActive(shown);
                pointerArea.enabled = shown && _pointable;
            }
        }

        /// <summary>Whether the card answers the pointer (hover, drag); an enlarged copy must not, or it would flicker.</summary>
        public void SetPointable(bool pointable)
        {
            _pointable = pointable;
            pointerArea.enabled = pointable && visual.activeSelf;
        }

        /// <summary>Wires the prefab's parts (editor setup).</summary>
        public void Assign(Renderer frameRenderer, Renderer backgroundRenderer, Renderer artRenderer, TMP_Text titleLabel, TMP_Text captionLabel, TMP_Text bodyLabel, TMP_Text idLabel, GameObject torments, TMP_Text tormentLabel, GameObject visualRoot, Collider area, Vector2 cardSize)
        {
            frame = frameRenderer;
            background = backgroundRenderer;
            art = artRenderer;
            title = titleLabel;
            caption = captionLabel;
            body = bodyLabel;
            id = idLabel;
            tormentBadge = torments;
            tormentCount = tormentLabel;
            visual = visualRoot;
            pointerArea = area;
            size = cardSize;
        }

        // Colours and textures go through a property block: every card shares the same materials.
        private void Paint(Renderer target, Color color, Sprite? sprite)
        {
            _block ??= new MaterialPropertyBlock();
            target.GetPropertyBlock(_block);
            _block.SetColor(BaseColor, color);
            if (sprite != null)
            {
                Texture2D texture = sprite.texture;
                Rect region = sprite.textureRect;
                _block.SetTexture(BaseMap, texture);
                _block.SetVector(BaseMapTransform, new Vector4(region.width / texture.width, region.height / texture.height, region.x / texture.width, region.y / texture.height));
            }

            target.SetPropertyBlock(_block);
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
