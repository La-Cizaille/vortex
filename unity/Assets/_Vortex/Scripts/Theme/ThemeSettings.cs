using System;
using TMPro;
using UnityEngine;
using Vortex.Core.Content;

namespace Vortex.Client.Theme
{
    /// <summary>
    /// The look of the game that is not an asset of its own: technology, interface and seat colours, fonts, the icons
    /// used inside card texts and the playback speed. Edited in Unity; views listen to <see cref="Changed"/>, so edits
    /// show at once in the Gallery scene.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Habillage/Thème", fileName = "ThemeSettings")]
    public sealed class ThemeSettings : ScriptableObject
    {
        [Header("Technologies")]
        [SerializeField] private Color neutral = new Color32(138, 143, 152, 255);
        [SerializeField] private Color blue = new Color32(58, 123, 213, 255);
        [SerializeField] private Color red = new Color32(214, 69, 69, 255);
        [SerializeField] private Color green = new Color32(76, 175, 80, 255);
        [SerializeField] private Color yellow = new Color32(232, 183, 48, 255);

        [Header("Interface")]
        [SerializeField] private Color background = new Color32(10, 12, 24, 255);
        [SerializeField] private Color cardBackground = new Color32(24, 27, 40, 255);
        [SerializeField] private Color text = new Color32(236, 238, 245, 255);
        [SerializeField] private Color mutedText = new Color32(150, 156, 175, 255);
        [Tooltip("Cadre du siège dont c'est le tour.")]
        [SerializeField] private Color highlight = new Color32(255, 214, 102, 255);
        [Tooltip("Jeton de surcharge allumé.")]
        [SerializeField] private Color overcharge = new Color32(255, 170, 60, 255);
        [Tooltip("Teinte du vaisseau d'un joueur éliminé (épave).")]
        [SerializeField] private Color wreck = new Color32(58, 60, 68, 255);
        [Tooltip("Cadre de la carte qu'un achat remplacerait, pendant qu'on glisse une carte du marché.")]
        [SerializeField] private Color loss = new Color32(229, 57, 53, 255);

        [Header("Cartes")]
        [Tooltip("Texte posé sur les panneaux clairs des cartes (nom, texte).")]
        [SerializeField] private Color cardInk = new Color32(22, 24, 30, 255);
        [Tooltip("Éclat de la gemme d'une carte de technologie : la composante la plus forte de sa couleur est portée à ce niveau, le même pour toutes les couleurs. Au-dessus du seuil du Bloom (1,5), elle rayonne.")]
        [SerializeField, Min(0f)] private float cardGemGlow = 2.5f;
        [Tooltip("Éclat des liserés d'une carte (couleur de sa technologie, ou couleur qui la marque) : leur couleur multipliée par ce nombre. Sous le seuil du Bloom (1,5), ils restent nets.")]
        [SerializeField, Min(0f)] private float cardTrimGlow = 0.8f;

        [Header("Sièges")]
        [Tooltip("Couleur de chaque siège, dans l'ordre de la table. Elle teinte les vaisseaux provisoires.")]
        [SerializeField]
        private Color[] seats =
        {
            new Color32(94, 196, 230, 255),
            new Color32(240, 120, 90, 255),
            new Color32(150, 210, 110, 255),
            new Color32(200, 140, 230, 255),
            new Color32(245, 205, 95, 255),
        };

        [Header("Textes")]
        [Tooltip("Police des titres. Vide : la police par défaut de TextMeshPro.")]
        [SerializeField] private TMP_FontAsset? titleFont;
        [Tooltip("Police des textes. Vide : la police par défaut de TextMeshPro.")]
        [SerializeField] private TMP_FontAsset? bodyFont;
        [Tooltip("Sprite asset TextMeshPro dont les sprites portent le nom des icônes des cartes (ATQ, BOU, DIC, MKT, MOD, SUR, TOR). Vide : les icônes sont omises du texte.")]
        [SerializeField] private TMP_SpriteAsset? textIcons;

        [Header("Animations")]
        [Tooltip("Vitesse de lecture des événements par défaut (1 = normale).")]
        [SerializeField, Min(0.25f)] private float playbackSpeed = 1f;
        [Tooltip("Vaisseaux au repos : hauteur du balancement, en unités de la scène.")]
        [SerializeField, Min(0f)] private float shipSwayHeight = 0.05f;
        [Tooltip("Vaisseaux au repos : roulis, en degrés de part et d'autre.")]
        [SerializeField, Min(0f)] private float shipSwayRoll = 2.5f;
        [Tooltip("Vaisseaux au repos : tangage, en degrés de part et d'autre.")]
        [SerializeField, Min(0f)] private float shipSwayPitch = 1.2f;
        [Tooltip("Vaisseaux au repos : durée d'un balancement complet, en secondes.")]
        [SerializeField, Min(0.1f)] private float shipSwaySeconds = 3.4f;
        [Tooltip("Matériau des effets lumineux provisoires (tirs, réacteurs, explosions) : additif, sans lumière, teinté par chaque effet. Au-dessus du seuil du Bloom, il rayonne. Vide : les effets restent mats.")]
        [SerializeField] private Material? glowMaterial;
        [Tooltip("Modèle du dé à 8 faces (ASSETS §2). Il porte huit repères vides Face_1 à Face_8, dont l'axe avant sort de la face. Vide : un octaèdre généré.")]
        [SerializeField] private GameObject? dieModel;

        [Header("Sons")]
        [Tooltip("Tic des dernières secondes d'un tour limité. Vide : un bip généré en attendant le son définitif.")]
        [SerializeField] private AudioClip? timerTick;

        /// <summary>Raised when the theme is edited in the inspector, so that views refresh.</summary>
        public event Action? Changed;

        /// <summary>Background behind the table.</summary>
        public Color Background => background;

        /// <summary>Inside of a card, behind its illustration and text.</summary>
        public Color CardBackground => cardBackground;

        /// <summary>Main text colour.</summary>
        public Color Text => text;

        /// <summary>Secondary text colour (captions, identifiers).</summary>
        public Color MutedText => mutedText;

        /// <summary>Frame of the seat whose turn it is.</summary>
        public Color Highlight => highlight;

        /// <summary>Lit overcharge token.</summary>
        public Color Overcharge => overcharge;

        /// <summary>Tint of an eliminated player's ship.</summary>
        public Color Wreck => wreck;

        /// <summary>Frame of the card a purchase would replace (INTERFACE.md 3.3).</summary>
        public Color Loss => loss;

        /// <summary>Title font, or null for the TextMeshPro default.</summary>
        public TMP_FontAsset? TitleFont => titleFont;

        /// <summary>Body font, or null for the TextMeshPro default.</summary>
        public TMP_FontAsset? BodyFont => bodyFont;

        /// <summary>Sprites of the card text icons, or null.</summary>
        public TMP_SpriteAsset? TextIcons => textIcons;

        /// <summary>Text on the light panels of a card.</summary>
        public Color CardInk => cardInk;

        /// <summary>Emission of a technology card's gem: the level of its colour's brightest channel (Bloom above 1.5).</summary>
        public float CardGemGlow => cardGemGlow;

        /// <summary>Emission of a card's trims: their colour times this factor.</summary>
        public float CardTrimGlow => cardTrimGlow;

        /// <summary>Default event playback speed.</summary>
        public float PlaybackSpeed => playbackSpeed;

        /// <summary>Height of a ship's sway at rest, in scene units.</summary>
        public float ShipSwayHeight => shipSwayHeight;

        /// <summary>Roll of a ship's sway at rest, in degrees each way.</summary>
        public float ShipSwayRoll => shipSwayRoll;

        /// <summary>Pitch of a ship's sway at rest, in degrees each way.</summary>
        public float ShipSwayPitch => shipSwayPitch;

        /// <summary>Duration of a full sway, in seconds.</summary>
        public float ShipSwaySeconds => shipSwaySeconds;

        /// <summary>Material of the luminous placeholder effects, or null.</summary>
        public Material? GlowMaterial => glowMaterial;

        /// <summary>Sets the glow material when the theme has none (editor setup).</summary>
        public bool AssignGlowIfMissing(Material material)
        {
            if (glowMaterial != null)
            {
                return false;
            }

            glowMaterial = material;
            return true;
        }

        /// <summary>The d8 model with its Face_1 to Face_8 markers, or null for the generated one.</summary>
        public GameObject? DieModel => dieModel;

        /// <summary>Tick of the last seconds of a timed turn, or null for the generated placeholder (ARB-80).</summary>
        public AudioClip? TimerTick => timerTick;

        /// <summary>Colour of a technology; neutral cards use the neutral colour.</summary>
        public Color Technology(TechColor color) => color switch
        {
            TechColor.Blue => blue,
            TechColor.Red => red,
            TechColor.Green => green,
            TechColor.Yellow => yellow,
            _ => neutral,
        };

        /// <summary>Colour of a seat (0-based, table order).</summary>
        public Color Seat(int seat) => seats.Length == 0 || seat < 0 ? neutral : seats[seat % seats.Length];

        /// <summary>True when the text icon sprites contain an icon of this name.</summary>
        public bool HasTextIcon(string icon) => textIcons != null && textIcons.GetSpriteIndexFromName(icon) >= 0;

        private void OnValidate() => Changed?.Invoke();
    }
}
