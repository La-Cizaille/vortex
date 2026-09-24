using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Vortex.Client.Content;
using Vortex.Client.Session;
using Vortex.Client.Theme;
using Vortex.Core.Bots;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Runs a game in the game scene (ADR-0014, ADR-0015): starts a local session, places the ships and the panels
    /// around the viewer, plays the engine events one by one, and lets bots play when the presentation is idle. The
    /// displays follow each event through the <see cref="TableModel"/>, then resync on the final public view.
    /// </summary>
    public sealed class GameDirector : MonoBehaviour, IFeedbackStage
    {
        [Header("Contenu et habillage")]
        [SerializeField] private GameContent content = null!;
        [SerializeField] private ThemeSettings theme = null!;
        [SerializeField] private CardArtCatalog cardArt = null!;
        [SerializeField] private ShipCatalog ships = null!;
        [SerializeField] private TextTable texts = null!;
        [SerializeField] private FeedbackProfile feedback = null!;
        [SerializeField] private CardDisplay cardPrefab = null!;

        [Header("Table")]
        [SerializeField] private SeatDisplay opponentPrefab = null!;
        [SerializeField] private RectTransform opponentPanels = null!;
        [SerializeField] private SeatDisplay playerSeat = null!;
        [SerializeField] private MarketDisplay market = null!;
        [SerializeField] private RoundBanner banner = null!;
        [SerializeField] private GameLogDisplay log = null!;
        [SerializeField] private PlaybackControls playback = null!;
        [SerializeField] private CommandPanel commands = null!;
        [SerializeField] private Transform shipRow = null!;
        [SerializeField] private Transform tableCentre = null!;
        [SerializeField] private Camera view = null!;

        [Header("Disposition (positions à l'écran : 0,0 en bas à gauche, 1,1 en haut à droite)")]
        [Tooltip("Position à l'écran du vaisseau du joueur.")]
        [SerializeField] private Vector2 viewerShipOnScreen = new Vector2(0.5f, 0.25f);
        [Tooltip("Taille du vaisseau du joueur : il est près de la caméra, donc réduit pour laisser voir ses chiffres.")]
        [SerializeField, Min(0.1f)] private float viewerShipScale = 0.6f;
        [Tooltip("Centre de l'arc des adversaires, à l'écran.")]
        [SerializeField] private Vector2 arcCentreOnScreen = new Vector2(0.5f, 0.62f);
        [Tooltip("Demi-largeur et demi-hauteur de l'arc des adversaires, à l'écran.")]
        [SerializeField] private Vector2 arcRadiusOnScreen = new Vector2(0.41f, 0.22f);
        [Tooltip("Écart entre les extrémités de l'arc et l'horizontale, en degrés.")]
        [SerializeField, Range(0f, 45f)] private float arcMargin = 12f;
        [Tooltip("Décalage du panneau d'un adversaire par rapport à son vaisseau, en unités d'interface.")]
        [SerializeField] private Vector2 opponentPanelOffset = new Vector2(0f, -45f);

        [Header("Partie de test (en attendant les menus)")]
        [Tooltip("Mode test : le siège 1 est joué par vous, à l'aide du panneau des coups ; les autres sièges par des bots.")]
        [SerializeField] private bool humanFirstSeat = true;
        [SerializeField, Range(2, 5)] private int seatCount = 5;
        [SerializeField] private BotLevel botLevel = BotLevel.Normal;
        [Tooltip("0 : une graine différente à chaque partie.")]
        [SerializeField] private ulong seed;
        [Tooltip("Pause entre deux coups d'un bot, en secondes (divisée par la vitesse de lecture).")]
        [SerializeField, Min(0f)] private float botPause = 0.4f;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private readonly Dictionary<int, Transform> _ships = new Dictionary<int, Transform>();
        private readonly Dictionary<int, SeatDisplay> _seats = new Dictionary<int, SeatDisplay>();
        private readonly HashSet<int> _wrecks = new HashSet<int>();
        private LocalHotSeatSession? _session;
        private EventPlayer? _player;
        private TableModel? _model;
        private TableContext? _context;
        private GameLogFormatter? _log;
        private CommandLabels? _labels;
        private PanelState _panel;
        private int _viewer;
        private float _wait;
        private bool _dirty;

        // False while the opening events play: the session only has the view after them, so patching it again would
        // count their effects twice. The table shows that view, and the log still follows each event.
        private bool _patching;

        /// <summary>The game being played, or null before <see cref="Begin"/>.</summary>
        public IGameSession? Session => _session;

        /// <summary>What the table shows, or null before <see cref="Begin"/>.</summary>
        public TableModel? Model => _model;

        /// <summary>True while events are being played.</summary>
        public bool IsPlaying => _player?.IsPlaying ?? false;

        /// <summary>Test mode: the first seat is played by a person through the command panel.</summary>
        public bool HumanFirstSeat
        {
            get => humanFirstSeat;
            set => humanFirstSeat = value;
        }

        /// <summary>Seat displays by seat number.</summary>
        public IReadOnlyDictionary<int, SeatDisplay> Seats => _seats;

        /// <summary>Ships by seat number.</summary>
        public IReadOnlyDictionary<int, Transform> Ships => _ships;

        /// <summary>Starts a new game: bots, and the first seat for a person in test mode (the local game menu comes with M4.6).</summary>
        public void Begin()
        {
            Clear();
            GameData data = content.LoadData();
            _context = new TableContext(theme, cardArt, texts, data, cardPrefab);
            _log = new GameLogFormatter(texts, data);
            _labels = new CommandLabels(_context);
            List<SeatSetup> seats = Enumerable.Range(1, seatCount)
                .Select(n => new SeatSetup(
                    string.Format(CultureInfo.InvariantCulture, texts.Get(TextKeys.SeatDefaultName), n),
                    n == 1 && humanFirstSeat ? SeatKind.Human : SeatKind.Bot,
                    botLevel))
                .ToList();
            ulong gameSeed = seed != 0 ? seed : (ulong)DateTime.UtcNow.Ticks;
            _session = new LocalHotSeatSession(content.CreateEngine(), gameSeed, seats);
            _viewer = 0;
            view.backgroundColor = theme.Background;
            _model = TableModel.From(_session.View, _session.Rules);

            _player = new EventPlayer(type => feedback.For(type), this) { Speed = theme.PlaybackSpeed };
            _player.EventStarted += OnEventStarted;
            _player.Idle += Resync;

            PlaceSeats();
            market.Bind(_context);
            banner.Bind(_context);
            log.Bind(_context);
            playback.Bind(_context, _player);
            commands.Hide();
            _panel = PanelState.Hidden;
            ShowAll(redrawCards: true);
            _patching = false;
            Play(_session.OpeningEvents);
        }

        /// <summary>Advances the playback, then lets a bot play when the presentation is idle (called every frame).</summary>
        public void Advance(float deltaTime)
        {
            if (_player is null || _session is null)
            {
                return;
            }

            _player.Tick(deltaTime);

            // Displays are redrawn once per frame at most: skipping can start hundreds of events in one frame.
            if (_dirty)
            {
                _dirty = false;
                ShowAll(redrawCards: false);
            }

            if (_player.IsPlaying)
            {
                return;
            }

            if (_session.IsOver)
            {
                SetPanel(PanelState.Hidden);
                return;
            }

            if (!_session.IsBotTurn)
            {
                // A person's turn, or a person's decision during another seat's command.
                SetPanel(PanelState.Choices);
                return;
            }

            SetPanel(PanelState.Waiting);
            _wait += deltaTime * _player.Speed;
            if (_wait < botPause)
            {
                return;
            }

            _wait = 0f;
            SessionResult result = _session.PlayBotStep();
            if (!result.Accepted)
            {
                Debug.LogError("A bot command was refused: " + result.Error);
                return;
            }

            Play(result.Events);
        }

        /// <inheritdoc/>
        public Transform? AnchorFor(FeedbackAnchor anchor, GameEvent gameEvent) => anchor switch
        {
            FeedbackAnchor.Player => Ship(gameEvent.Player),
            FeedbackAnchor.Other => Ship(gameEvent.Other),
            FeedbackAnchor.Market => market.transform,
            FeedbackAnchor.Banner => banner.transform,
            _ => tableCentre,
        };

        private void Start() => Begin();

        private void Update() => Advance(Time.deltaTime);

        private Transform? Ship(int seat) => _ships.TryGetValue(seat, out Transform ship) ? ship : null;

        // Test mode: the command panel lists what the engine allows the person who must act (INTERFACE.md 6 maps
        // each of these commands to the gesture that will replace its button).
        private void SetPanel(PanelState state)
        {
            if (state == _panel)
            {
                return;
            }

            _panel = state;
            switch (state)
            {
                case PanelState.Hidden:
                    commands.Hide();
                    break;
                case PanelState.Waiting:
                    commands.ShowMessage(texts.Get(TextKeys.PanelWaiting));
                    break;
                default:
                    OfferChoices();
                    break;
            }
        }

        private void OfferChoices()
        {
            int actor = _session!.Actor;
            var view = _session.View;
            var decision = _session.Decision;
            var choices = _session.LegalCommands(actor)
                .Select(command => (_labels!.Describe(command, view, decision), (Action)(() => Submit(actor, command))))
                .ToList();
            string heading = decision != null
                ? view.Players[actor].Name + " : " + _labels!.Question(decision)
                : string.Format(CultureInfo.InvariantCulture, texts.Get(TextKeys.PanelYourTurn), view.Players[actor].Name);
            commands.Show(heading, choices);
        }

        private void Submit(int seat, Command command)
        {
            SetPanel(PanelState.Hidden);
            SessionResult result = _session!.Submit(seat, command);
            if (!result.Accepted)
            {
                // The panel only offers legal commands; a refusal would be an engine or panel bug.
                Debug.LogError("A command from the panel was refused: " + result.Error);
                return;
            }

            Play(result.Events);
        }

        private void Play(IReadOnlyList<GameEvent> events)
        {
            if (events.Count == 0)
            {
                Resync();
                return;
            }

            _player!.Enqueue(events);
        }

        private void OnEventStarted(GameEvent gameEvent)
        {
            if (_patching)
            {
                _dirty |= _model!.Apply(gameEvent);
            }

            string? line = _log!.Describe(gameEvent, _model!.Seats);
            if (line != null)
            {
                log.Add(line);
            }
        }

        // The playback is over: the displays catch up with the final public view (cards, markets, statuses). While a
        // command waits for a decision, the engine's view is still the state before that command (ADR-0009): the
        // model keeps following the events instead, and catches up once the command is complete.
        private void Resync()
        {
            if (_session!.Decision != null)
            {
                _patching = true;
                return;
            }

            _model = TableModel.From(_session.View, _session.Rules);
            _dirty = true;
            _patching = true;
        }

        private void ShowAll(bool redrawCards)
        {
            TableModel model = _model!;
            foreach (KeyValuePair<int, SeatDisplay> seat in _seats)
            {
                seat.Value.Show(model.Seats[seat.Key], model, redrawCards);
                if (model.Seats[seat.Key].Eliminated && _wrecks.Add(seat.Key) && _ships.TryGetValue(seat.Key, out Transform ship))
                {
                    Wreck(ship);
                }
            }

            market.Show(model, redrawCards);
            banner.Show(model);
        }

        // The viewer's ship in front, the opponents' ships in an arc in turn order, each with its panel under it.
        private void PlaceSeats()
        {
            int count = _model!.Seats.Count;
            Vector3 centre = OnTable(arcCentreOnScreen);
            Transform viewerShip = SpawnShip(_viewer, OnTable(viewerShipOnScreen), centre);
            viewerShip.localScale = Vector3.one * viewerShipScale;
            _seats[_viewer] = playerSeat;
            playerSeat.Bind(_context!);

            IReadOnlyList<int> opponents = SeatLayout.Opponents(count, _viewer);
            for (int i = 0; i < opponents.Count; i++)
            {
                float angle = SeatLayout.ArcAngle(i, opponents.Count, arcMargin) * Mathf.Deg2Rad;
                Vector2 onScreen = arcCentreOnScreen + new Vector2(Mathf.Cos(angle) * arcRadiusOnScreen.x, Mathf.Sin(angle) * arcRadiusOnScreen.y);
                Transform ship = SpawnShip(opponents[i], OnTable(onScreen), viewerShip.position);

                SeatDisplay panel = Instantiate(opponentPrefab, opponentPanels, false);
                panel.name = "Adversaire " + (opponents[i] + 1).ToString(CultureInfo.InvariantCulture);
                panel.gameObject.AddComponent<ScreenAnchor>().Follow(ship, view, opponentPanelOffset);
                panel.Bind(_context!);
                _seats[opponents[i]] = panel;
            }
        }

        // The point of the table (the y = 0 plane) seen at a screen position, so the layout is set in screen terms.
        private Vector3 OnTable(Vector2 viewport)
        {
            Ray ray = view.ViewportPointToRay(new Vector3(viewport.x, viewport.y, 0f));
            return new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance) ? ray.GetPoint(distance) : Vector3.zero;
        }

        private Transform SpawnShip(int seat, Vector3 position, Vector3 facing)
        {
            GameObject ship = ships.Spawn(seat, shipRow, theme.Seat(seat));
            ship.transform.position = position;
            Vector3 direction = facing - position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                ship.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            _ships[seat] = ship.transform;
            return ship.transform;
        }

        private void Clear()
        {
            if (_player != null)
            {
                _player.EventStarted -= OnEventStarted;
                _player.Idle -= Resync;
            }

            foreach (KeyValuePair<int, Transform> ship in _ships)
            {
                Discard(ship.Value.gameObject);
            }

            foreach (KeyValuePair<int, SeatDisplay> seat in _seats.Where(s => s.Value != playerSeat))
            {
                Discard(seat.Value.gameObject);
            }

            _ships.Clear();
            _seats.Clear();
            _wrecks.Clear();
            _wait = 0f;
            _dirty = false;
        }

        // An eliminated player's ship stays in place as a wreck (INTERFACE.md 3.1): tinted with the wreck colour and
        // listing, whatever its model. Elimination is final, so the change is never undone.
        private void Wreck(Transform ship)
        {
            var block = new MaterialPropertyBlock();
            foreach (Renderer part in ship.GetComponentsInChildren<Renderer>())
            {
                part.GetPropertyBlock(block);
                block.SetColor(BaseColor, theme.Wreck);
                part.SetPropertyBlock(block);
            }

            ship.Rotate(0f, 0f, 25f, Space.Self);
        }

        private static void Discard(GameObject target)
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

        /// <summary>Wires the scene (editor setup).</summary>
        public void Assign(GameContent gameContent, ThemeSettings themeSettings, CardArtCatalog artCatalog, ShipCatalog shipCatalog, TextTable textTable, FeedbackProfile feedbackProfile, CardDisplay card)
        {
            content = gameContent;
            theme = themeSettings;
            cardArt = artCatalog;
            ships = shipCatalog;
            texts = textTable;
            feedback = feedbackProfile;
            cardPrefab = card;
        }

        /// <summary>Wires the command panel of the test mode (editor setup).</summary>
        public void AssignTestMode(CommandPanel panel) => commands = panel;

        /// <summary>Wires the table of the scene (editor setup).</summary>
        public void AssignTable(SeatDisplay opponent, RectTransform opponentRoot, SeatDisplay player, MarketDisplay markets, RoundBanner roundBanner, GameLogDisplay gameLog, PlaybackControls controls, Transform shipRoot, Transform centre, Camera sceneCamera)
        {
            opponentPrefab = opponent;
            opponentPanels = opponentRoot;
            playerSeat = player;
            market = markets;
            banner = roundBanner;
            log = gameLog;
            playback = controls;
            shipRow = shipRoot;
            tableCentre = centre;
            view = sceneCamera;
        }
    }
}
