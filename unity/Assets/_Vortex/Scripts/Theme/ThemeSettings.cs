using System;
using System.Collections.Generic;
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
        [SerializeField] private Color background = new Color32(14, 15, 17, 255);
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
        [Tooltip("Texte posé sur les faces des modules qui portent du texte (plaque du nom, terminal de la règle).")]
        [SerializeField] private Color cardInk = new Color32(207, 196, 168, 255);
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

        [Header("Palette de la direction artistique (DIRECTION_ARTISTIQUE §6.1)")]
        [Tooltip("Suie : fonds, ciel, ombres.")]
        [SerializeField] private Color soot = new Color32(14, 15, 17, 255);
        [Tooltip("Os : papier jauni des affiches et des textes de cartes.")]
        [SerializeField] private Color bone = new Color32(207, 196, 168, 255);
        [Tooltip("Encre : texte sur l'os.")]
        [SerializeField] private Color ink = new Color32(20, 19, 18, 255);
        [Tooltip("Sang : affiches, tampons, verdicts.")]
        [SerializeField] private Color blood = new Color32(142, 27, 27, 255);
        [Tooltip("Vert terminal : écrans de bord, journal ; au-dessus de 1,5, il rayonne.")]
        [SerializeField, ColorUsage(false, true)] private Color terminal = new Color32(77, 255, 122, 255);
        [Tooltip("Ambre sale : réacteurs, feux, lumières de bord.")]
        [SerializeField] private Color amber = new Color32(224, 146, 47, 255);
        [Tooltip("Jaune des bandes de danger.")]
        [SerializeField] private Color hazard = new Color32(217, 165, 20, 255);

        [Header("Textes")]
        [Tooltip("Police des titres, affiches et gros chiffres (Anton). Vide : la police par défaut de TextMeshPro.")]
        [SerializeField] private TMP_FontAsset? titleFont;
        [Tooltip("Police des textes, dont les règles des cartes (Barlow). Vide : la police par défaut de TextMeshPro.")]
        [SerializeField] private TMP_FontAsset? bodyFont;
        [Tooltip("Police des libellés et boutons (Barlow Condensed), aussi la police par défaut de TextMeshPro. Vide : la police par défaut.")]
        [SerializeField] private TMP_FontAsset? labelFont;
        [Tooltip("Police des pochoirs : noms sur les coques, marquages, verdicts (Big Shoulders Stencil). Vide : la police des titres.")]
        [SerializeField] private TMP_FontAsset? stencilFont;
        [Tooltip("Police gravée des épitaphes et textes d'ambiance (IM Fell English). Vide : la police des textes.")]
        [SerializeField] private TMP_FontAsset? engravedFont;
        [Tooltip("Police du terminal : chiffres de bord et journal (Share Tech Mono). Vide : la police des textes.")]
        [SerializeField] private TMP_FontAsset? terminalFont;
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
        [Tooltip("Matériau du bouclier de plasma (parade, déviation, bouclier qui change) : une sphère, teintée par le jeu à la couleur du siège. Vide : le matériau lumineux provisoire.")]
        [SerializeField] private Material? shieldMaterial;
        [Tooltip("Fumée d'un vaisseau endommagé : un effet qui s'élève, posé sur la coque et détruit après quelques secondes. Vide : des bouffées provisoires.")]
        [SerializeField] private GameObject? smokePrefab;
        [Tooltip("Arc électrique d'un vaisseau surchargé : un effet court, posé sur la coque et détruit après une seconde. Vide : des arcs provisoires.")]
        [SerializeField] private GameObject? arcPrefab;
        [Tooltip("Fond de la table (panorama, ciel étoilé), posé à l'origine de la scène au début de la partie. Vide : un champ d'étoiles généré.")]
        [SerializeField] private GameObject? tableBackground;
        [Tooltip("Effet joué au centre de la table quand un événement de manche est révélé, par identifiant d'événement (docs/CARDS.md). Un événement sans entrée montre une vague de lumière.")]
        [SerializeField] private List<EventEffect> eventEffects = new List<EventEffect>();
        [Tooltip("Couleur de l'anneau d'un effet en jeu autour d'un vaisseau, par type d'effet (clés status.* de la table des textes). Un effet sans entrée prend la couleur par défaut.")]
        [SerializeField] private List<StatusColor> statusColors = new List<StatusColor>();
        [Tooltip("Couleur par défaut de l'anneau d'un effet ; au-dessus de 1,5, il rayonne.")]
        [SerializeField, ColorUsage(false, true)] private Color statusDefault = new Color(1.1f, 1.1f, 1.5f);
        [Tooltip("Teinte vers laquelle glisse la coque d'un vaisseau contaminé par le Tourment.")]
        [SerializeField] private Color sickTint = new Color32(120, 170, 60, 255);
        [Tooltip("Couleur des spores qui s'échappent d'un vaisseau contaminé ; au-dessus de 1,5, elles rayonnent.")]
        [SerializeField, ColorUsage(false, true)] private Color sporeColor = new Color(1.1f, 2f, 0.5f);
        [Tooltip("Modèle du cockpit du joueur (ARB-90, tools/blender/build_cockpit.py), avec ses pièces nommées. Vide : le panneau de chiffres de l'interface.")]
        [SerializeField] private GameObject? cockpitModel;
        [Tooltip("Matériau du verre du cockpit (verre du manomètre, tube de la jauge de PV) : transparent et brillant. Vide : le verre garde le matériau du modèle.")]
        [SerializeField] private Material? glassMaterial;
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

        /// <summary>Soot: backgrounds, sky, shadows (art direction palette).</summary>
        public Color Soot => soot;

        /// <summary>Bone: yellowed paper of posters and card texts.</summary>
        public Color Bone => bone;

        /// <summary>Ink: text on bone.</summary>
        public Color Ink => ink;

        /// <summary>Blood: posters, stamps, verdicts.</summary>
        public Color Blood => blood;

        /// <summary>Terminal green: on-board screens and the log.</summary>
        public Color Terminal => terminal;

        /// <summary>Dirty amber: engines, lights on board.</summary>
        public Color Amber => amber;

        /// <summary>Yellow of the hazard stripes.</summary>
        public Color Hazard => hazard;

        /// <summary>Title font, or null for the TextMeshPro default.</summary>
        public TMP_FontAsset? TitleFont => titleFont;

        /// <summary>Body font, or null for the TextMeshPro default.</summary>
        public TMP_FontAsset? BodyFont => bodyFont;

        /// <summary>Font of labels and buttons, or null for TextMeshPro's default.</summary>
        public TMP_FontAsset? LabelFont => labelFont;

        /// <summary>Stencil font (hull names, markings, verdicts); the title font when none.</summary>
        public TMP_FontAsset? StencilFont => stencilFont != null ? stencilFont : titleFont;

        /// <summary>Engraved font (epitaphs, flavour text); the body font when none.</summary>
        public TMP_FontAsset? EngravedFont => engravedFont != null ? engravedFont : bodyFont;

        /// <summary>Terminal font (on-board figures, log); the body font when none.</summary>
        public TMP_FontAsset? TerminalFont => terminalFont != null ? terminalFont : bodyFont;

        /// <summary>Sets the fonts of the art direction where the theme has none (editor setup); true when one was set.</summary>
        public bool AssignFontsIfMissing(TMP_FontAsset title, TMP_FontAsset body, TMP_FontAsset label, TMP_FontAsset stencil, TMP_FontAsset engraved, TMP_FontAsset terminal)
        {
            // Non-short-circuit "|": every slot is filled.
            return Fill(ref titleFont, title) | Fill(ref bodyFont, body) | Fill(ref labelFont, label)
                | Fill(ref stencilFont, stencil) | Fill(ref engravedFont, engraved) | Fill(ref terminalFont, terminal);
        }

        private static bool Fill(ref TMP_FontAsset? slot, TMP_FontAsset font)
        {
            if (slot != null)
            {
                return false;
            }

            slot = font;
            return true;
        }

        /// <summary>Sprites of the card text icons, or null.</summary>
        public TMP_SpriteAsset? TextIcons => textIcons;

        /// <summary>Text on the faces of a card that carry text (name plate, rule terminal).</summary>
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

        /// <summary>Material of the shield's plasma sphere: the designer's, or the glow material.</summary>
        public Material? ShieldLook => shieldMaterial != null ? shieldMaterial : glowMaterial;

        /// <summary>The colour of the ring of an effect in play, by its kind.</summary>
        public Color StatusColorFor(string kind)
        {
            foreach (StatusColor entry in statusColors)
            {
                if (entry.Kind == kind)
                {
                    return entry.Color;
                }
            }

            return statusDefault;
        }

        /// <summary>The tint a contaminated hull drifts towards.</summary>
        public Color SickTint => sickTint;

        /// <summary>The colour of the spores of a contaminated ship.</summary>
        public Color SporeColor => sporeColor;

        /// <summary>The player's cockpit model, or null for the interface's panel of figures.</summary>
        public GameObject? CockpitModel => cockpitModel;

        /// <summary>Sets the cockpit model when the theme has none (editor setup).</summary>
        public bool AssignCockpitIfMissing(GameObject model)
        {
            if (cockpitModel != null)
            {
                return false;
            }

            cockpitModel = model;
            return true;
        }

        /// <summary>Material of the cockpit's glass parts, or null to keep the model's own.</summary>
        public Material? GlassMaterial => glassMaterial;

        /// <summary>Sets the glass material when the theme has none (editor setup).</summary>
        public bool AssignGlassIfMissing(Material material)
        {
            if (glassMaterial != null)
            {
                return false;
            }

            glassMaterial = material;
            return true;
        }

        /// <summary>The table's background, or null for the generated starfield.</summary>
        public GameObject? TableBackground => tableBackground;

        /// <summary>The effect of an event of the round, or null for the generic wave.</summary>
        public GameObject? EventEffectFor(string eventId)
        {
            foreach (EventEffect entry in eventEffects)
            {
                if (entry.EventId == eventId)
                {
                    return entry.Effect;
                }
            }

            return null;
        }

        /// <summary>The electric arc of an overcharged ship, or null for the placeholder arcs.</summary>
        public GameObject? ArcPrefab => arcPrefab;

        /// <summary>The smoke of a damaged ship, or null for the placeholder puffs.</summary>
        public GameObject? SmokePrefab => smokePrefab;

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

        /// <summary>One line of the effect colours: a kind of effect and its ring's colour.</summary>
        [Serializable]
        public sealed class StatusColor
        {
            /// <summary>Kind of effect, as the engine names it (status.* keys of the text table).</summary>
            public string Kind = string.Empty;

            /// <summary>Colour of its ring; above 1.5 it glows.</summary>
            [ColorUsage(false, true)]
            public Color Color = Color.white;
        }

        /// <summary>One line of the event effects: an event id and its effect.</summary>
        [Serializable]
        public sealed class EventEffect
        {
            /// <summary>Identifier of the event (docs/CARDS.md).</summary>
            public string EventId = string.Empty;

            /// <summary>What plays in the middle of the table.</summary>
            public GameObject? Effect;
        }
    }
}
