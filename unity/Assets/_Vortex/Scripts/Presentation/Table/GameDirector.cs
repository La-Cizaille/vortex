using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Vortex.Client.Content;
using Vortex.Client.Menus;
using Vortex.Client.Session;
using Vortex.Client.Theme;
using Vortex.Core.Bots;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Events;
using Vortex.Core.Projection;
using Vortex.Core.Rules;
using Vortex.Core.State;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Runs a game in the game scene (ADR-0014, ADR-0015): starts a local session, places the ships and the panels
    /// around the viewer, plays the engine events one by one, and lets bots play when the presentation is idle. The
    /// displays follow each event through the <see cref="TableModel"/>, then resync on the final public view. The game
    /// is the one chosen in the menu (ADR-0019), or the inspector's test game when the scene is opened directly; it can
    /// be paused, restarted and left, and with several people on the device the view turns to each one's turn.
    /// </summary>
    public sealed class GameDirector : MonoBehaviour, IFeedbackStage, IControlsHost
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
        [SerializeField] private PlayerControls controls = null!;
        [SerializeField] private IconCatalog icons = null!;
        [SerializeField] private CardZoom zoom = null!;
        [SerializeField] private Transform shipRow = null!;
        [SerializeField] private Transform tableCentre = null!;
        [SerializeField] private Camera view = null!;
        [SerializeField] private Transform cardRoot = null!;
        [SerializeField] private RectTransform foreground = null!;
        [Tooltip("Distance des cartes à la caméra : plus près que le plan de l'interface, pour passer devant ses panneaux.")]
        [SerializeField, Min(0.5f)] private float cardDepth = 6f;

        [Header("Menus de la partie")]
        [SerializeField] private PauseMenu pause = null!;
        [SerializeField] private GameOverPanel gameOver = null!;
        [SerializeField] private TurnAnnouncement announcement = null!;
        [SerializeField] private TurnTimerDisplay timer = null!;
        [SerializeField] private RectTransform? cockpitPlace;

        [Header("Disposition (positions à l'écran : 0,0 en bas à gauche, 1,1 en haut à droite)")]
        [Tooltip("Position à l'écran du vaisseau du joueur.")]
        [SerializeField] private Vector2 viewerShipOnScreen = new Vector2(0.5f, 0.335f);
        [Tooltip("Taille du vaisseau du joueur : il est au premier plan, près de la caméra.")]
        [SerializeField, Min(0.1f)] private float viewerShipScale = 1.2f;
        [Tooltip("Taille des vaisseaux des adversaires.")]
        [SerializeField, Min(0.1f)] private float opponentShipScale = 1.3f;
        [Tooltip("Centre de l'arc des adversaires, à l'écran.")]
        [SerializeField] private Vector2 arcCentreOnScreen = new Vector2(0.5f, 0.62f);
        [Tooltip("Demi-largeur et demi-hauteur de l'arc des adversaires, à l'écran.")]
        [SerializeField] private Vector2 arcRadiusOnScreen = new Vector2(0.41f, 0.22f);
        [Tooltip("Écart entre les extrémités de l'arc et l'horizontale, en degrés.")]
        [SerializeField, Range(0f, 45f)] private float arcMargin = 12f;
        [Tooltip("Décalage du panneau d'un adversaire par rapport à son vaisseau, en unités d'interface.")]
        [SerializeField] private Vector2 opponentPanelOffset = new Vector2(0f, -45f);

        [Header("Partie de test (scène ouverte directement, sans passer par le menu)")]
        [Tooltip("Le siège 1 est joué par vous (gestes à la souris ou au doigt), les autres par des bots.")]
        [SerializeField] private bool humanFirstSeat = true;
        [Tooltip("Mode test : affiche aussi le panneau qui liste tous les coups permis, un bouton par coup.")]
        [SerializeField] private bool showCommandPanel;
        [SerializeField, Range(2, 5)] private int seatCount = 5;
        [SerializeField] private BotLevel botLevel = BotLevel.Normal;
        [Tooltip("0 : une graine différente à chaque partie.")]
        [SerializeField] private ulong seed;
        [Tooltip("Temps d'un tour en secondes ; 0 : pas de limite (ARB-80).")]
        [SerializeField, Min(0)] private int turnSeconds;
        [Tooltip("Pause entre deux coups d'un bot, en secondes (divisée par la vitesse de lecture).")]
        [SerializeField, Min(0f)] private float botPause = 0.4f;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private readonly Dictionary<int, Transform> _ships = new Dictionary<int, Transform>();
        private readonly Dictionary<int, SeatDisplay> _seats = new Dictionary<int, SeatDisplay>();
        private readonly HashSet<int> _wrecks = new HashSet<int>();
        private LocalHotSeatSession? _session;
        private MatchSetup? _setup;
        private bool _paused;
        private bool _outcomeShown;
        private readonly AttackMemory _attack = new AttackMemory();
        private GameObject? _background;
        private CockpitDisplay? _cockpit;
        private readonly HashSet<int> _placedByHand = new HashSet<int>();
        private TurnClock _clock = new TurnClock(0f, MatchSetup.DecisionSeconds);
        private EventPlayer? _player;
        private TableModel? _model;
        private TableContext? _context;
        private GameLogFormatter? _log;
        private Narrator? _narrator;
        private CommandLabels? _labels;
        private PanelState _panel;
        private bool _controlsOffered;
        private int _viewer;
        private float _wait;
        private bool _dirty;
        private bool _unprotected;

        // False while the opening events play: the session only has the view after them, so patching it again would
        // count their effects twice. The table shows that view, and the log still follows each event.
        private bool _patching;

        /// <summary>The game being played, or null before <see cref="Begin"/>.</summary>
        public IGameSession? Session => _session;

        /// <summary>What the table shows, or null before <see cref="Begin"/>.</summary>
        public TableModel? Model => _model;

        /// <summary>True while events are being played.</summary>
        public bool IsPlaying => _player?.IsPlaying ?? false;

        /// <summary>True while the game waits (pause menu open).</summary>
        public bool Paused => _paused;

        /// <summary>Seat whose side of the table is shown at the bottom.</summary>
        public int Viewer => _viewer;

        /// <summary>The game being played, or null before <see cref="Begin()"/>.</summary>
        public MatchSetup? Setup => _setup;

        /// <summary>The pause menu.</summary>
        public PauseMenu Pause => pause;

        /// <summary>The end of game panel.</summary>
        public GameOverPanel GameOver => gameOver;

        /// <summary>The "Tour de X" banner.</summary>
        public TurnAnnouncement Announcement => announcement;

        /// <summary>The time the person who has to act has left (ARB-80).</summary>
        public TurnClock Clock => _clock;

        /// <summary>The display of that time.</summary>
        public TurnTimerDisplay Timer => timer;

        /// <summary>Test mode: the first seat is played by a person through the command panel.</summary>
        public bool HumanFirstSeat
        {
            get => humanFirstSeat;
            set => humanFirstSeat = value;
        }

        /// <summary>Test game: time of a turn in seconds, 0 for no limit (captures and tests).</summary>
        public int TurnSeconds
        {
            get => turnSeconds;
            set => turnSeconds = value > 0 ? value : 0;
        }

        /// <summary>The black market (tests).</summary>
        public MarketDisplay Market => market;

        /// <summary>Test mode: the panel listing every allowed move is shown too.</summary>
        public bool ShowCommandPanel
        {
            get => showCommandPanel;
            set => showCommandPanel = value;
        }

        /// <summary>The controls of the person playing.</summary>
        public PlayerControls Controls => controls;

        /// <summary>Seat displays by seat number.</summary>
        public IReadOnlyDictionary<int, SeatDisplay> Seats => _seats;

        /// <summary>Ships by seat number.</summary>
        public IReadOnlyDictionary<int, Transform> Ships => _ships;

        /// <summary>
        /// Starts the game chosen in the menu (<see cref="MatchLauncher"/>), or the inspector's test game when the scene was
        /// opened directly.
        /// </summary>
        public void Begin()
        {
            MatchLauncher? launcher = MatchLauncher.Find();
            Begin(launcher != null && launcher.Setup != null ? launcher.Setup : TestSetup());
        }

        /// <summary>Starts a game: its seats, and in development its seed and rule options.</summary>
        public void Begin(MatchSetup setup)
        {
            _setup = setup ?? throw new ArgumentNullException(nameof(setup));
            Clear();
            GameData data = content.LoadData();
            _context = new TableContext(theme, cardArt, texts, data, cardPrefab, cardRoot, view, cardDepth, zoom);
            zoom.Bind(_context);
            _log = new GameLogFormatter(texts, data);
            _narrator = new Narrator(texts, UserOptions.Load(theme.PlaybackSpeed).Commentary, () => UnityEngine.Random.value);
            _labels = new CommandLabels(_context);
            ulong gameSeed = setup.Seed != 0 ? setup.Seed : (ulong)DateTime.UtcNow.Ticks;
            _session = new LocalHotSeatSession(content.CreateEngine(setup.Rules), gameSeed, setup.Seats);

            // The first person's side of the table; with bots only, the first seat's.
            _viewer = Math.Max(0, setup.Seats.ToList().FindIndex(s => s.Kind == SeatKind.Human));
            view.backgroundColor = theme.Background;
            _model = TableModel.From(_session.View, _session.Rules);
            controls.Bind(_context, _labels, icons, _session.Rules, this);
            _controlsOffered = false;

            _player = new EventPlayer(type => feedback.For(type), this) { Speed = UserOptions.Load(theme.PlaybackSpeed).Speed };
            _player.EventStarted += OnEventStarted;
            _player.Idle += Resync;

            // A new game: the camera comes back from its turn around the last winner.
            if (view.TryGetComponent(out CameraOrbit orbit))
            {
                orbit.Restore();
            }

            PlaceSeats();
            PlaceBackground();
            PlaceCockpit();
            market.Bind(_context);
            market.SetPurchase(controls.CanBuy, controls.Buy, controls.ExplainBuy, controls.HideHelp, MarkLoss);
            market.SetCardTap(uid => controls.ChooseCard(uid));
            banner.Bind(_context);
            log.Bind(_context);
            playback.Bind(_context, _player);
            commands.Hide();
            _panel = PanelState.Hidden;
            pause.Bind(texts, theme.PlaybackSpeed, paused => _paused = paused, Restart, ApplyOptions, MatchLauncher.BackToMenu);
            gameOver.Bind(texts, Restart, MatchLauncher.BackToMenu);
            announcement.Hide();
            _clock = new TurnClock(setup.TurnSeconds, MatchSetup.DecisionSeconds);
            timer.Bind(theme);
            _paused = false;
            _outcomeShown = false;
            ShowAll(redrawCards: true);
            _patching = false;
            Play(_session.OpeningEvents);
        }

        /// <summary>Advances the playback, then lets a bot play when the presentation is idle (called every frame).</summary>
        public void Advance(float deltaTime)
        {
            if (_player is null || _session is null || _paused)
            {
                return;
            }

            announcement.Tick(deltaTime);
            market.Tick(deltaTime);
            _player.Tick(deltaTime);

            // Displays are redrawn once per frame at most: skipping can start hundreds of events in one frame.
            if (_dirty)
            {
                _dirty = false;
                ShowAll(redrawCards: false);
            }

            if (_player.IsPlaying)
            {
                WithdrawControls();
                StopClock();
                return;
            }

            // The market opens for the person's market phase and folds after it (ARB-81).
            market.Follow(!_session.IsOver && !_session.IsBotTurn && _session.Decision == null && _session.View.Phase == TurnPhase.Market);
            if (_session.IsOver)
            {
                WithdrawControls();
                StopClock();
                SetPanel(PanelState.Hidden);
                ShowOutcome();
                return;
            }

            if (!_session.IsBotTurn)
            {
                // A person's turn, or a person's decision during another seat's command.
                FollowPerson();
                OfferControls();
                SetPanel(showCommandPanel ? PanelState.Choices : PanelState.Hidden);
                FollowClock(deltaTime);
                return;
            }

            WithdrawControls();
            StopClock();
            SetPanel(showCommandPanel ? PanelState.Waiting : PanelState.Hidden);
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

        /// <summary>Starts the same game again: same seats, and a new seed unless one was fixed in development.</summary>
        public void Restart() => Begin(_setup ?? TestSetup());

        /// <inheritdoc/>
        public float PlaybackSpeed => _player?.Speed ?? 1f;

        /// <inheritdoc/>
        ThemeSettings? IFeedbackStage.Theme => theme;

        /// <inheritdoc/>
        AttackMemory IFeedbackStage.Attack => _attack;

        /// <inheritdoc/>
        Camera? IFeedbackStage.View => view;

        /// <inheritdoc/>
        MarketDisplay? IFeedbackStage.Markets => market;

        /// <inheritdoc/>
        public ShipMotion? MotionOf(int seat) => _ships.TryGetValue(seat, out Transform ship) ? ship.GetComponent<ShipMotion>() : null;

        /// <inheritdoc/>
        public Transform? AnchorFor(FeedbackAnchor anchor, GameEvent gameEvent) => anchor switch
        {
            FeedbackAnchor.Player => Ship(gameEvent.Player),
            FeedbackAnchor.Other => Ship(gameEvent.Other),
            FeedbackAnchor.Market => market.transform,
            FeedbackAnchor.Banner => banner.transform,
            FeedbackAnchor.Foreground => foreground,
            _ => tableCentre,
        };

        private void Start() => Begin();

        // The inspector's test game: the first seat a person's when set, the others bots.
        private MatchSetup TestSetup() => new MatchSetup(
            Enumerable.Range(1, seatCount)
                .Select(n => new SeatSetup(
                    string.Format(CultureInfo.InvariantCulture, texts.Get(TextKeys.SeatDefaultName), n),
                    n == 1 && humanFirstSeat ? SeatKind.Human : SeatKind.Bot,
                    botLevel))
                .ToList(),
            seed,
            turnSeconds: turnSeconds);

        // The time of the person who has to act runs while they can act. When it is up, their turn ends by itself, or a
        // bot answers their decision (ARB-80); the events of that step play, then the next step follows if needed.
        private void FollowClock(float deltaTime)
        {
            GameView table = _session!.View;
            _clock.Follow(table.Round, table.CurrentPlayer, _session.Actor, _session.Decision?.Id);
            _clock.Tick(deltaTime);
            timer.Show(_clock);
            if (!_clock.Expired)
            {
                return;
            }

            int actor = _session.Actor;
            WithdrawControls();
            SetPanel(PanelState.Hidden);
            SessionResult result = _session.Expire(actor);
            if (!result.Accepted)
            {
                Debug.LogError("The step played when the time was up was refused: " + result.Error);
                return;
            }

            Play(result.Events);
        }

        private void StopClock()
        {
            _clock.Stop();
            timer.Show(_clock);
        }

        // While a market card is dragged, the viewer's card it would replace is marked (INTERFACE.md 3.3).
        private void MarkLoss(CardSlot slot, bool held)
        {
            if (_seats.TryGetValue(_viewer, out SeatDisplay seat))
            {
                seat.MarkLoss(slot, held);
            }
        }

        // The options were changed from the pause menu: the new default speed applies at once.
        private void ApplyOptions(UserOptions options)
        {
            if (_player != null)
            {
                _player.Speed = options.Speed;
                playback.Refresh();
            }

            if (_narrator != null)
            {
                _narrator.Speaks = options.Commentary;
            }
        }

        // Once the last events are played: the winner and how, with "Rejouer" and "Menu".
        private void ShowOutcome()
        {
            if (_outcomeShown || _model is null)
            {
                return;
            }

            _outcomeShown = true;
            banner.Show(_model);
            gameOver.Show(banner.OutcomeText, Verdict(_model), Epitaphs(_model));
        }

        // The stamp of the end poster: the last ship standing is wanted, the elected is elected, a draw closes the file.
        private string Verdict(TableModel model) => texts.Get(model.Outcome?.Condition switch
        {
            WinCondition.Domination => TextKeys.GameOverWanted,
            WinCondition.GalacticElection => TextKeys.GameOverElected,
            _ => TextKeys.GameOverClosed,
        });

        // One epitaph per fallen captain: "Ci-gît ..." and, when the narrator speaks, one of her lines.
        private string Epitaphs(TableModel model) => string.Join("\n", model.Seats
            .Where(s => s.Eliminated)
            .Select(s =>
            {
                string line = string.Format(CultureInfo.InvariantCulture, texts.Get(TextKeys.EpitaphOf), s.Name);
                string? remark = _narrator?.RemarkOnEpitaph();
                return remark is null ? line : line + " " + remark;
            }));

        // Several people on this device: the view turns to the person whose turn starts (INTERFACE.md 4). A decision asked
        // of another person, or a bot's turn, never moves it.
        private void FollowPerson()
        {
            int actor = _session!.Actor;
            if (_setup!.Humans > 1 && _session.Decision is null && actor >= 0 && actor != _viewer && !_session.IsBot(actor))
            {
                TurnViewTo(actor);
            }
        }

        // What lasts around a ship (ANIMATIONS.md §3): its contamination by Torment (spores, and its hull drifting to a sick
        // tint), a ring per effect in play, and a halo under it on its turn.
        private void ShowAura(ShipAura aura, int seat, SeatModel shown, TableModel model)
        {
            int torments = (shown.AttackCard?.Torments ?? 0) + (shown.DefenseCard?.Torments ?? 0);
            float contamination = shown.Eliminated ? 0f : Mathf.Clamp01(torments / 6f);
            var effects = shown.Eliminated
                ? new List<Color>()
                : shown.Statuses.Where(s => s.Active).Select(s => theme.StatusColorFor(s.Kind)).ToList();
            bool turn = model.Outcome is null && model.CurrentPlayer == seat && !shown.Eliminated;
            aura.Show(contamination, theme.SporeColor, effects, turn, theme.Highlight * 0.35f, theme.GlowMaterial);
            if (!shown.Eliminated && MotionOf(seat) is ShipMotion motion)
            {
                ShipCatalog.PaintSeat(motion.Body.gameObject, Color.Lerp(theme.Seat(seat), theme.SickTint, contamination * 0.6f));
            }
        }

        // The player's cockpit (ARB-90): the theme's model over the player's panel, in front of the interface, as far as
        // puts its sockets at the cards' depth, so that the player's cards lie in them. Built once per game.
        private void PlaceCockpit()
        {
            if (_cockpit != null)
            {
                Discard(_cockpit.gameObject);
                _cockpit = null;
            }

            if (theme.CockpitModel == null || cockpitPlace == null)
            {
                return;
            }

            GameObject console = Instantiate(theme.CockpitModel, cardRoot, false);
            console.name = "Cockpit";
            _cockpit = console.AddComponent<CockpitDisplay>();
            _cockpit.Bind(cockpitPlace, view, cardDepth, theme.GlowMaterial, theme.GlassMaterial, () =>
            {
                controls.ToggleOvercharge();
                _dirty = true;
            });
            _cockpit.Tapped = () => controls.ChooseSeat(_viewer);
            _cockpit.Armed = () => controls.OverchargeArmed;
        }

        // The sky behind the table: the theme's background, or a generated starfield around the camera. Built once per game.
        private void PlaceBackground()
        {
            if (_background != null)
            {
                Discard(_background);
            }

            _background = theme.TableBackground != null
                ? Instantiate(theme.TableBackground, Vector3.zero, Quaternion.identity, shipRow)
                : Starfield.Create(shipRow, view.transform.position, 450, 60f, theme.GlowMaterial).gameObject;
        }

        private void TurnViewTo(int seat)
        {
            WithdrawControls();
            ClearTable();
            _viewer = seat;
            PlaceSeats();
            ShowAll(redrawCards: true);
            announcement.Show(string.Format(CultureInfo.InvariantCulture, texts.Get(TextKeys.TurnOf), _model!.Seats[seat].Name), _narrator?.RemarkOnTurn());
        }

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

        private void OfferControls()
        {
            if (_controlsOffered)
            {
                return;
            }

            int actor = _session!.Actor;
            controls.Offer(actor, _session.View, _session.LegalCommands(actor), _session.Decision);
            _controlsOffered = true;
        }

        private void WithdrawControls()
        {
            if (_controlsOffered)
            {
                controls.Withdraw();
                _controlsOffered = false;
            }
        }

        /// <inheritdoc/>
        RectTransform? IControlsHost.SeatPanel(int seat) => _seats.TryGetValue(seat, out SeatDisplay display) ? (RectTransform)display.transform : null;

        /// <inheritdoc/>
        void IControlsHost.Submit(int seat, Command command) => Submit(seat, command);

        /// <inheritdoc/>
        void IControlsHost.ShowTargets(IReadOnlyCollection<int>? seats) => ShowTargets(seats);

        /// <inheritdoc/>
        void IControlsHost.PlacedByHand(int uid) => _placedByHand.Add(uid);

        /// <inheritdoc/>
        bool IFeedbackStage.TakePlacedByHand(int uid) => _placedByHand.Remove(uid);

        /// <inheritdoc/>
        CardDisplay? IFeedbackStage.NewCard(string cardId)
        {
            if (_context is null)
            {
                return null;
            }

            CardDisplay card = Instantiate(cardPrefab, cardRoot, false);
            card.name = "Carte en vol";
            card.Show(_context.Face(cardId), theme, cardArt);
            card.SetPointable(false);
            return card;
        }

        /// <inheritdoc/>
        bool IControlsHost.ShowsCard(int uid) => _model != null && _model.Shows(uid);

        /// <inheritdoc/>
        void IControlsHost.MarkCards(IReadOnlyDictionary<int, Color>? marks)
        {
            Func<int, Color?> mark = uid => marks != null && marks.TryGetValue(uid, out Color color) ? color : (Color?)null;
            foreach (SeatDisplay seat in _seats.Values)
            {
                seat.MarkCards(mark);
            }

            market.MarkCards(mark);
        }

        /// <inheritdoc/>
        CommandPreview? IControlsHost.Preview(int seat, Command command) => _session?.Preview(seat, command);

        /// <inheritdoc/>
        int IControlsHost.SeatAt(Vector2 screen) => SeatAt(screen);

        /// <inheritdoc/>
        CommandError? IControlsHost.Explain(int seat, Command command) => _session?.Explain(seat, command);

        // The opponent under a screen position (their panel, or near their ship), or -1: where an aimed action lands.
        private int SeatAt(Vector2 screen)
        {
            foreach (KeyValuePair<int, SeatDisplay> seat in _seats)
            {
                if (seat.Key != _viewer && CardAnchor.ScreenRectOf((RectTransform)seat.Value.transform).Contains(screen))
                {
                    return seat.Key;
                }
            }

            float reach = 0.07f * view.pixelHeight;
            foreach (KeyValuePair<int, Transform> ship in _ships)
            {
                if (ship.Key != _viewer && Vector2.Distance(view.WorldToScreenPoint(ship.Value.position), screen) < reach)
                {
                    return ship.Key;
                }
            }

            return -1;
        }

        // While an action is aimed, or a decision offers seats, those light up and the others dim; the viewer's own seat
        // only takes part when it is offered.
        private void ShowTargets(IReadOnlyCollection<int>? targets)
        {
            foreach (KeyValuePair<int, SeatDisplay> seat in _seats.Where(s => s.Key != _viewer || (targets != null && targets.Contains(_viewer)) || targets is null))
            {
                seat.Value.ShowTargeting(targets is null ? (bool?)null : targets.Contains(seat.Key));
            }

            _dirty = true;
        }

        private void Submit(int seat, Command command)
        {
            WithdrawControls();
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
                // The fact, then the narrator's remark when she has one.
                string? remark = _narrator?.RemarkOn(gameEvent);
                log.Add(remark is null ? line : line + " — " + remark);
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

            // The command is played out: a card placed by hand no longer needs to be remembered.
            _placedByHand.Clear();
        }

        private void ShowAll(bool redrawCards)
        {
            TableModel model = _model!;
            foreach (KeyValuePair<int, SeatDisplay> seat in _seats)
            {
                seat.Value.Show(model.Seats[seat.Key], model, redrawCards);
                SeatModel shown = model.Seats[seat.Key];
                if (_ships.TryGetValue(seat.Key, out Transform hull) && hull.GetComponentInChildren<HullDamage>() is HullDamage damage)
                {
                    damage.Show(1f - ((float)shown.Hp / Math.Max(1, model.MaxHp)), shown.Eliminated, theme.GlowMaterial, theme.SmokePrefab);
                }

                if (_ships.TryGetValue(seat.Key, out Transform auraRoot) && auraRoot.GetComponent<ShipAura>() is ShipAura aura)
                {
                    ShowAura(aura, seat.Key, shown, model);
                }

                if (_ships.TryGetValue(seat.Key, out Transform charged) && charged.GetComponentInChildren<OverchargeArcs>() is OverchargeArcs arcs)
                {
                    arcs.Show(shown.Overcharge > 0 && !shown.Eliminated, seat.Key == _viewer && controls.OverchargeArmed, theme.Overcharge, theme.GlowMaterial, theme.ArcPrefab);
                }

                if (model.Seats[seat.Key].Eliminated && _wrecks.Add(seat.Key) && _ships.TryGetValue(seat.Key, out Transform ship))
                {
                    Wreck(ship);
                }
            }

            market.Show(model, redrawCards);
            banner.Show(model);
            if (_cockpit != null && _session != null)
            {
                SeatModel me = model.Seats[_viewer];
                _cockpit.Show(
                    me.Name,
                    theme.Seat(_viewer),
                    me.Hp,
                    model.MaxHp,
                    me.Shield,
                    _session.Rules.MaxShield,
                    Unprotected(),
                    me.Overcharge > 0,
                    controls.OverchargeArmed,
                    me.Technologies.Select(theme.Technology).ToList(),
                    texts,
                    theme);
            }
        }

        // Whether the viewer's shield protects nothing: the engine's reckoning, which follows every effect (disabled
        // shield...) without naming any. It reads the game as it stands, so it is asked once the events have played out,
        // and kept while they play, when the figures shown are still catching up.
        private bool Unprotected()
        {
            if (!IsPlaying && _session != null)
            {
                _unprotected = _session.Protection(_viewer) <= 0.0;
            }

            return _unprotected;
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
            playerSeat.SetCardUse(controls.CanUse, controls.UseCard, controls.ExplainUse, controls.HideHelp);
            AnswerOn(playerSeat, _viewer);

            IReadOnlyList<int> opponents = SeatLayout.Opponents(count, _viewer);
            for (int i = 0; i < opponents.Count; i++)
            {
                float angle = SeatLayout.ArcAngle(i, opponents.Count, arcMargin) * Mathf.Deg2Rad;
                Vector2 onScreen = arcCentreOnScreen + new Vector2(Mathf.Cos(angle) * arcRadiusOnScreen.x, Mathf.Sin(angle) * arcRadiusOnScreen.y);
                Transform ship = SpawnShip(opponents[i], OnTable(onScreen), viewerShip.position);
                ship.localScale = Vector3.one * opponentShipScale;

                SeatDisplay panel = Instantiate(opponentPrefab, opponentPanels, false);
                panel.name = "Adversaire " + (opponents[i] + 1).ToString(CultureInfo.InvariantCulture);
                panel.gameObject.AddComponent<ScreenAnchor>().Follow(ship, view, opponentPanelOffset);
                panel.Bind(_context!);

                // Another player's card is dragged only to answer a decision (steal or destroy, ARB-82).
                panel.SetCardUse(controls.CanUse, controls.UseCard);
                AnswerOn(panel, opponents[i]);
                _seats[opponents[i]] = panel;
            }
        }

        // A decision is answered by touching a seat's panel or one of its cards (ARB-82).
        private void AnswerOn(SeatDisplay panel, int seat)
        {
            panel.Tapped = () => controls.ChooseSeat(seat);
            panel.SetCardTap(uid => controls.ChooseCard(uid));
        }

        // The point of the table (the y = 0 plane) seen at a screen position, so the layout is set in screen terms.
        private Vector3 OnTable(Vector2 viewport)
        {
            Ray ray = view.ViewportPointToRay(new Vector3(viewport.x, viewport.y, 0f));
            return new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance) ? ray.GetPoint(distance) : Vector3.zero;
        }

        private Transform SpawnShip(int seat, Vector3 position, Vector3 facing)
        {
            // A still root the table places (the panel follows it), and the model under it, which moves (ANIMATIONS.md
            // §2): a model may carry its mesh on its own root, which must move too.
            var root = new GameObject("Siège " + (seat + 1).ToString(CultureInfo.InvariantCulture)).transform;
            root.SetParent(shipRow, false);
            root.position = position;
            Vector3 direction = facing - position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                root.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            GameObject ship = ships.Spawn(seat, root, theme.Seat(seat));

            // Each ship sways on its own rhythm: the phase follows the seat.
            ShipMotion.Attach(root, ship.transform, theme.ShipSwayHeight, theme.ShipSwayRoll, theme.ShipSwayPitch, theme.ShipSwaySeconds, seat * 0.37f);
            ship.AddComponent<HullDamage>();
            ship.AddComponent<OverchargeArcs>();
            root.gameObject.AddComponent<ShipAura>();
            _ships[seat] = root;
            return root;
        }

        private void Clear()
        {
            if (_player != null)
            {
                _player.EventStarted -= OnEventStarted;
                _player.Idle -= Resync;
            }

            ClearTable();
            _wait = 0f;
            _dirty = false;
        }

        // The ships and the seat panels, with their 3D cards, before the table is laid out again.
        private void ClearTable()
        {
            foreach (KeyValuePair<int, Transform> ship in _ships)
            {
                Discard(ship.Value.gameObject);
            }

            foreach (KeyValuePair<int, SeatDisplay> seat in _seats)
            {
                seat.Value.Release();
                if (seat.Value != playerSeat)
                {
                    Discard(seat.Value.gameObject);
                }
            }

            _ships.Clear();
            _seats.Clear();
            _wrecks.Clear();
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
            if (ship.TryGetComponent(out ShipMotion motion))
            {
                motion.Wreck();
            }
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

        /// <summary>Wires the 3D cards: their parent, the zoom and the foreground layer (editor setup).</summary>
        public void AssignCards(Transform cards, CardZoom cardZoom, RectTransform foregroundLayer)
        {
            cardRoot = cards;
            zoom = cardZoom;
            foreground = foregroundLayer;
        }

        /// <summary>Wires the controls of the person playing and the pictograms (editor setup).</summary>
        public void AssignControls(PlayerControls playerControls, IconCatalog iconCatalog)
        {
            controls = playerControls;
            icons = iconCatalog;
        }

        /// <summary>Wires the display of the turn time (editor setup).</summary>
        public void AssignTimer(TurnTimerDisplay turnTimer) => timer = turnTimer;

        /// <summary>Wires the place of the player's cockpit (editor setup).</summary>
        public void AssignCockpitPlace(RectTransform place) => cockpitPlace = place;

        /// <summary>The player's cockpit, or null when the theme has none (tests).</summary>
        public CockpitDisplay? Cockpit => _cockpit;

        /// <summary>Wires the menus of the game: pause, end of game, turn banner (editor setup).</summary>
        public void AssignMenus(PauseMenu pauseMenu, GameOverPanel gameOverPanel, TurnAnnouncement turnAnnouncement)
        {
            pause = pauseMenu;
            gameOver = gameOverPanel;
            announcement = turnAnnouncement;
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
