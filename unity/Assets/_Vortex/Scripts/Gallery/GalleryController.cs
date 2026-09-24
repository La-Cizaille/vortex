using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;
using Vortex.Client.Presentation;
using Vortex.Client.Theme;
using Vortex.Core.Content;

namespace Vortex.Client.Gallery
{
    /// <summary>
    /// Sandbox scene: every modifier, event and technology, and the ship of every seat, shown with the current theme
    /// and art. The look can be tuned without playing a game: theme edits show at once in Play mode. Not part of the
    /// game build.
    /// </summary>
    public sealed class GalleryController : MonoBehaviour
    {
        private const float ShipSpacing = 3f;

        [SerializeField] private GameContent content = null!;
        [SerializeField] private ThemeSettings theme = null!;
        [SerializeField] private CardArtCatalog cardArt = null!;
        [SerializeField] private ShipCatalog ships = null!;
        [SerializeField] private TextTable texts = null!;
        [SerializeField] private CardView cardPrefab = null!;
        [SerializeField] private RectTransform sections = null!;
        [SerializeField] private Transform shipRow = null!;
        [SerializeField] private Camera view = null!;
        [SerializeField, Range(2, 5)] private int seatCount = 5;

        private readonly List<CardView> _cards = new List<CardView>();
        private readonly List<TMP_Text> _headings = new List<TMP_Text>();
        private readonly List<GameObject> _ships = new List<GameObject>();

        /// <summary>The card views built by <see cref="Build"/>.</summary>
        public IReadOnlyList<CardView> Cards => _cards;

        /// <summary>The ships built by <see cref="Build"/>.</summary>
        public IReadOnlyList<GameObject> Ships => _ships;

        /// <summary>Builds the gallery from the game content (also called by tests, outside Play mode).</summary>
        public void Build()
        {
            Clear();
            GameData data = content.LoadData();
            Section(TextKeys.GalleryAttack, data.Modifiers.Where(c => c.Slot == CardSlot.Attack).Select(c => CardFace.Of(c, texts)));
            Section(TextKeys.GalleryDefense, data.Modifiers.Where(c => c.Slot == CardSlot.Defense).Select(c => CardFace.Of(c, texts)));
            Section(TextKeys.GalleryEvents, data.Events.Select(e => CardFace.Of(e, texts)));
            Section(TextKeys.GalleryTechnologies, data.Technologies.Select(t => CardFace.Of(t, texts)));
            Refresh();
        }

        /// <summary>Shows everything again with the current theme and art.</summary>
        public void Refresh()
        {
            view.backgroundColor = theme.Background;
            foreach (TMP_Text heading in _headings)
            {
                heading.color = theme.Text;
            }

            foreach (CardView card in _cards)
            {
                card.Show(card.Face, theme, cardArt);
            }

            // Ships are rebuilt: a placeholder takes its colour when it is created.
            DestroyAll(_ships);
            for (int seat = 0; seat < seatCount; seat++)
            {
                GameObject ship = ships.Spawn(seat, shipRow, theme.Seat(seat));
                ship.transform.localPosition = new Vector3((seat - ((seatCount - 1) / 2f)) * ShipSpacing, 0f, 0f);
                ship.transform.localRotation = Quaternion.Euler(35f, 200f, 0f);
                _ships.Add(ship);
            }
        }

        private void Start() => Build();

        private void OnEnable()
        {
            if (theme != null)
            {
                theme.Changed += OnThemeChanged;
            }
        }

        private void OnDisable()
        {
            if (theme != null)
            {
                theme.Changed -= OnThemeChanged;
            }
        }

        private void OnThemeChanged()
        {
            if (_cards.Count > 0)
            {
                Refresh();
            }
        }

        private void Section(string titleKey, IEnumerable<CardFace> faces)
        {
            var heading = new GameObject("Titre", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            heading.transform.SetParent(sections, false);
            heading.text = texts.Get(titleKey);
            heading.richText = false;
            heading.fontSize = 40;
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 64;
            _headings.Add(heading);

            var grid = new GameObject("Cartes", typeof(RectTransform)).AddComponent<GridLayoutGroup>();
            grid.transform.SetParent(sections, false);
            RectTransform cardShape = (RectTransform)cardPrefab.transform;
            grid.cellSize = cardShape.sizeDelta;
            grid.spacing = new Vector2(24f, 24f);
            foreach (CardFace face in faces)
            {
                CardView card = Instantiate(cardPrefab, grid.transform, false);
                card.name = face.Id;
                card.Show(face, theme, cardArt);
                _cards.Add(card);
            }
        }

        private void Clear()
        {
            _cards.Clear();
            _headings.Clear();
            var children = new List<GameObject>();
            foreach (Transform child in sections)
            {
                children.Add(child.gameObject);
            }

            DestroyAll(children);
            DestroyAll(_ships);
        }

        private static void DestroyAll(List<GameObject> objects)
        {
            foreach (GameObject target in objects)
            {
                if (Application.isPlaying)
                {
                    Destroy(target);
                }
                else
                {
                    DestroyImmediate(target);
                }
            }

            objects.Clear();
        }

        /// <summary>Wires the scene (editor setup).</summary>
        public void Assign(GameContent gameContent, ThemeSettings themeSettings, CardArtCatalog artCatalog, ShipCatalog shipCatalog, TextTable textTable, CardView card, RectTransform sectionRoot, Transform shipRoot, Camera sceneCamera)
        {
            content = gameContent;
            theme = themeSettings;
            cardArt = artCatalog;
            ships = shipCatalog;
            texts = textTable;
            cardPrefab = card;
            sections = sectionRoot;
            shipRow = shipRoot;
            view = sceneCamera;
        }
    }
}
