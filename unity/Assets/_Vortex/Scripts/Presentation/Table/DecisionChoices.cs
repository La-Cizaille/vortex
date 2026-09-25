using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Commands;
using Vortex.Core.Decisions;
using Vortex.Core.Projection;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Where each answer of a decision is taken on the table (INTERFACE.md 3.6, ARB-82): a seat, a card of the table, a die
    /// face, a card shown in the middle, a crew action, or a card dragged somewhere. It only reads what the engine puts in
    /// each option (player, card, number, content), so it never names a card (ADR-0007). An option the table cannot show
    /// is left out of every place and counted in <see cref="Unplaced"/>: the decision is then offered in a window instead.
    /// </summary>
    public sealed class DecisionChoices
    {
        /// <summary>The key of the answer that declines an optional choice.</summary>
        public const string Pass = "none";

        private readonly Dictionary<int, string> _seats = new Dictionary<int, string>();
        private readonly Dictionary<int, string> _cards = new Dictionary<int, string>();
        private readonly List<(int Number, string Key)> _numbers = new List<(int, string)>();
        private readonly List<(string ContentId, string Key)> _middle = new List<(string, string)>();
        private readonly Dictionary<CrewAction, string> _actions = new Dictionary<CrewAction, string>();
        private readonly Dictionary<(CrewAction Action, int Seat), string> _aimed = new Dictionary<(CrewAction, int), string>();
        private readonly Dictionary<(int Card, int Seat), string> _drops = new Dictionary<(int, int), string>();
        private readonly Dictionary<int, string> _dropsInMiddle = new Dictionary<int, string>();

        private DecisionChoices(DecisionRequest request) => Request = request;

        /// <summary>The decision.</summary>
        public DecisionRequest Request { get; }

        /// <summary>Seats to touch (their panel), with the answer each gives; the decider's own seat may be the pass.</summary>
        public IReadOnlyDictionary<int, string> Seats => _seats;

        /// <summary>Cards of the table to touch, by uid, with the answer each gives; the source card may be the pass.</summary>
        public IReadOnlyDictionary<int, string> Cards => _cards;

        /// <summary>Die faces to touch, in order.</summary>
        public IReadOnlyList<(int Number, string Key)> Numbers => _numbers;

        /// <summary>Cards shown in the middle of the table to touch (events, or the source of the decision as the pass).</summary>
        public IReadOnlyList<(string ContentId, string Key)> Middle => _middle;

        /// <summary>Crew actions without a target to touch.</summary>
        public IReadOnlyDictionary<CrewAction, string> Actions => _actions;

        /// <summary>Crew actions to drag onto a seat.</summary>
        public IReadOnlyDictionary<(CrewAction Action, int Seat), string> AimedActions => _aimed;

        /// <summary>Cards to drag onto a seat's side of the table.</summary>
        public IReadOnlyDictionary<(int Card, int Seat), string> Drops => _drops;

        /// <summary>Cards to drag into the middle of the table.</summary>
        public IReadOnlyDictionary<int, string> DropsInMiddle => _dropsInMiddle;

        /// <summary>The uid of the card of the table touched to pass, or -1.</summary>
        public int PassCard { get; private set; } = -1;

        /// <summary>True when the pass is the decider's own seat.</summary>
        public bool PassOnOwnSeat { get; private set; }

        /// <summary>True when the pass is the source shown in the middle.</summary>
        public bool PassInMiddle { get; private set; }

        /// <summary>Options that have no place on the table.</summary>
        public int Unplaced { get; private set; }

        /// <summary>True when the pass is one of the answers.</summary>
        public bool CanPass => Request.Options.Any(o => o.Key == Pass);

        /// <summary>Places every answer of <paramref name="request"/> on the table <paramref name="view"/> shows.</summary>
        public static DecisionChoices From(DecisionRequest request, GameView view)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (view is null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            var choices = new DecisionChoices(request);
            foreach (DecisionOption option in request.Options.Where(o => o.Key != Pass))
            {
                if (!choices.Place(option))
                {
                    choices.Unplaced++;
                }
            }

            if (choices.CanPass)
            {
                choices.PlacePass(view);
            }

            return choices;
        }

        // Where the answer goes, from the kind of question and what the option names.
        private bool Place(DecisionOption option)
        {
            switch (Request.Kind)
            {
                case DecisionKind.ChoosePlayer:
                case DecisionKind.ChooseDirection:
                    // Two directions may give the same neighbour (two seats only): both do the same, the first is kept.
                    return option.Player >= 0 && Add(_seats, option.Player, option.Key);
                case DecisionKind.ChooseModifier:
                case DecisionKind.ChooseMarketCard:
                    return option.CardUid >= 0 && Add(_cards, option.CardUid, option.Key);
                case DecisionKind.ChooseNumber:
                    _numbers.Add((option.Number, option.Key));
                    return true;
                case DecisionKind.ChooseCrewAction:
                    if (!Enum.IsDefined(typeof(CrewAction), option.Number))
                    {
                        return false;
                    }

                    var action = (CrewAction)option.Number;
                    return option.Player >= 0 ? Add(_aimed, (action, option.Player), option.Key) : Add(_actions, action, option.Key);
                case DecisionKind.ChooseOption:
                    if (option.ContentId != null)
                    {
                        _middle.Add((option.ContentId, option.Key));
                        return true;
                    }

                    if (option.CardUid >= 0)
                    {
                        return option.Player >= 0 ? Add(_drops, (option.CardUid, option.Player), option.Key) : Add(_dropsInMiddle, option.CardUid, option.Key);
                    }

                    return false;
                default:
                    return false;
            }
        }

        // Declining (ARB-82): the card that asks, when it is on the table; else the decider's own seat, when it is not an
        // answer already; else the card that asks, shown in the middle.
        private void PlacePass(GameView view)
        {
            int source = Request.SourceId is null ? -1 : SourceOnTable(view, Request.SourceId, Request.Player);
            if (source >= 0 && !_cards.ContainsKey(source))
            {
                PassCard = source;
                _cards[source] = Pass;
            }
            else if (!_seats.ContainsKey(Request.Player))
            {
                PassOnOwnSeat = true;
                _seats[Request.Player] = Pass;
            }
            else if (Request.SourceId != null)
            {
                PassInMiddle = true;
                _middle.Add((Request.SourceId, Pass));
            }
            else
            {
                Unplaced++;
            }
        }

        // The uid of a card showing the source, the decider's own first, or -1.
        private static int SourceOnTable(GameView view, string sourceId, int decider)
        {
            IEnumerable<PlayerView> players = view.Players.OrderBy(p => p.Seat == decider ? 0 : 1);
            foreach (PlayerView player in players)
            {
                foreach (CardView? card in new[] { player.AttackSlot, player.DefenseSlot })
                {
                    if (card != null && card.CardId == sourceId)
                    {
                        return card.Uid;
                    }
                }
            }

            return -1;
        }

        private static bool Add<TKey>(Dictionary<TKey, string> place, TKey key, string answer)
            where TKey : notnull
        {
            if (!place.ContainsKey(key))
            {
                place[key] = answer;
            }

            return true;
        }
    }
}
