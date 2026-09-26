using System.Globalization;
using TMPro;
using UnityEngine;
using Vortex.Client.Theme;
using Vortex.Core.Content;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A card as a 3D object (ADR-0017): its body, its illustration (or a generated placeholder tinted with its technology
    /// colour), its name, slot badge, usage, text and id, and its Torment tokens. The front faces the card's -Z axis, the
    /// back its +Z axis. The modelled body (ARB-85) has trims and a gem in the technology colour; the box that stands in
    /// for it takes that colour whole. The look lives in the Card prefab and its materials, which the designer edits
    /// freely; this component only fills them.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CardDisplay : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseMapTransform = Shader.PropertyToID("_BaseMap_ST");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [Tooltip("Corps de la carte : le modèle, ou la boîte qui le remplace.")]
        [SerializeField] private Renderer frame = null!;
        [Tooltip("Matériau des liserés dans le corps (-1 : la boîte, teinte en entier).")]
        [SerializeField] private int trimSlot = -1;
        [Tooltip("Matériau de la gemme dans le corps (-1 : pas de gemme).")]
        [SerializeField] private int gemSlot = -1;
        [SerializeField] private Renderer? background;
        [SerializeField] private Renderer art = null!;
        [SerializeField] private TMP_Text title = null!;
        [SerializeField] private TMP_Text? badge;
        [Tooltip("Icône de l'emplacement dans le disque ; sans icône dans le catalogue, le disque montre le texte court.")]
        [SerializeField] private SpriteRenderer? badgeIcon;
        [Tooltip("Côté de l'icône de l'emplacement, en unités de la scène.")]
        [SerializeField, Min(0f)] private float badgeIconSize = 0.12f;
        [SerializeField] private IconCatalog? icons;
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

        /// <summary>True for the modelled body, whose name and text lie on faces of their own (CardInk).</summary>
        public bool Modelled => trimSlot >= 0;

        /// <summary>True while the disc shows the slot's icon rather than its short text.</summary>
        public bool ShowsBadgeIcon => badgeIcon != null && badgeIcon.gameObject.activeSelf;

        /// <summary>Colour of the trims (or of the whole box): the technology colour, or the colour marking the card.</summary>
        public Color TrimColor { get; private set; }

        /// <summary>Shows a card with the current theme.</summary>
        public void Show(CardFace face, ThemeSettings theme, CardArtCatalog catalog)
        {
            Face = face;
            Marked = false;
            Color color = theme.Technology(face.Color);
            PaintTrims(color, theme);
            PaintGem(face.Color == TechColor.Neutral ? (Color?)null : color, theme);
            if (background != null)
            {
                Paint(background, theme.CardBackground, null);
            }

            CardArt cardArt = catalog.ArtFor(face.Id);
            Paint(art, cardArt.IsPlaceholder ? color : Color.white, cardArt.Sprite);
            ShowsPlaceholder = cardArt.IsPlaceholder;

            // Names and ids are plain text; only the card text is rich text, converted by CardText. On the modelled
            // body, the name and the text lie on faces of their own (the module's plate and terminal), the badge and the
            // usage on its steel.
            Color ink = Modelled ? theme.CardInk : theme.Text;
            Label(title, face.Title, theme.TitleFont, ink, richText: false);
            if (badge != null)
            {
                Label(badge, face.Badge, theme.TitleFont, theme.Text, richText: false);
            }

            ShowBadgeIcon(face.BadgeIcon, theme);

            Label(caption, badge != null ? face.Usage : face.Caption, theme.BodyFont, Modelled ? theme.Text : theme.MutedText, richText: false);
            Label(id, face.Id, theme.BodyFont, theme.MutedText, richText: false);
            Label(body, CardText.ToRichText(face.Text, theme.HasTextIcon), theme.BodyFont, ink, richText: true);
            if (theme.TextIcons != null)
            {
                body.spriteAsset = theme.TextIcons;
            }
        }

        /// <summary>True while the card's frame is marked.</summary>
        public bool Marked { get; private set; }

        /// <summary>
        /// Marks the trims with <paramref name="color"/> (the card a purchase would replace, a card a decision offers),
        /// or gives them back the technology colour with null.
        /// </summary>
        public void Mark(Color? color, ThemeSettings theme)
        {
            Marked = color.HasValue;
            PaintTrims(color ?? theme.Technology(Face.Color), theme);
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

        /// <summary>
        /// Wires the prefab's parts (editor setup). <paramref name="trims"/> and <paramref name="gem"/> are the material
        /// indices of the trims and the gem in the body, or -1 for the box, painted whole.
        /// </summary>
        public void Assign(Renderer frameRenderer, int trims, int gem, Renderer? backgroundRenderer, Renderer artRenderer, TMP_Text titleLabel, TMP_Text? badgeLabel, TMP_Text captionLabel, TMP_Text bodyLabel, TMP_Text idLabel, GameObject torments, TMP_Text tormentLabel, GameObject visualRoot, Collider area, Vector2 cardSize)
        {
            frame = frameRenderer;
            trimSlot = trims;
            gemSlot = gem;
            background = backgroundRenderer;
            art = artRenderer;
            title = titleLabel;
            badge = badgeLabel;
            caption = captionLabel;
            body = bodyLabel;
            id = idLabel;
            tormentBadge = torments;
            tormentCount = tormentLabel;
            visual = visualRoot;
            pointerArea = area;
            size = cardSize;
        }

        /// <summary>Wires the slot icon of the disc and the catalog it comes from (editor setup).</summary>
        public void AssignBadgeIcon(SpriteRenderer? icon, float iconSize, IconCatalog? catalog)
        {
            badgeIcon = icon;
            badgeIconSize = iconSize;
            icons = catalog;
        }

        // The icon, tinted like the text, at the same size whatever the image's resolution; the text hides behind it.
        private void ShowBadgeIcon(string name, ThemeSettings theme)
        {
            Sprite? sprite = icons != null && name.Length > 0 ? icons.Find(name) : null;
            if (badgeIcon != null)
            {
                badgeIcon.gameObject.SetActive(sprite != null);
                if (sprite != null)
                {
                    badgeIcon.sprite = sprite;
                    badgeIcon.color = theme.Text;
                    float scale = badgeIconSize / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y, 0.001f);
                    badgeIcon.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }

            if (badge != null)
            {
                badge.gameObject.SetActive(sprite == null || badgeIcon == null);
            }
        }

        // The trims glow a little in their colour, under the Bloom threshold, so they stay crisp. The box takes the
        // colour whole.
        private void PaintTrims(Color color, ThemeSettings theme)
        {
            TrimColor = color;
            if (trimSlot < 0)
            {
                Paint(frame, color, null);
                return;
            }

            PaintSlot(trimSlot, color, color.linear * theme.CardTrimGlow);
        }

        // A technology card's gem glows in its colour, its brightest channel brought to the same level for every colour
        // (above the Bloom threshold); a neutral card's gem is dark and off.
        private void PaintGem(Color? color, ThemeSettings theme)
        {
            if (gemSlot < 0)
            {
                return;
            }

            Color glow = Color.black;
            if (color.HasValue)
            {
                Color linear = color.Value.linear;
                glow = linear * (theme.CardGemGlow / Mathf.Max(linear.maxColorComponent, 0.001f));
            }

            PaintSlot(gemSlot, color ?? theme.Wreck, glow);
        }

        // One material of the body, through a property block of its own: every card shares the same materials. The
        // emission is given in linear space, as the shader reads it: SetColor would take an HDR colour for an sRGB one.
        private void PaintSlot(int slot, Color color, Color linearEmission)
        {
            _block ??= new MaterialPropertyBlock();
            frame.GetPropertyBlock(_block, slot);
            _block.SetColor(BaseColor, color);
            _block.SetVector(EmissionColor, new Vector4(linearEmission.r, linearEmission.g, linearEmission.b, 1f));
            frame.SetPropertyBlock(_block, slot);
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
