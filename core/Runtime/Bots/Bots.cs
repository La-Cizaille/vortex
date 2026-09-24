using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Commands;
using Vortex.Core.Dice;
using Vortex.Core.Rules;
using Vortex.Core.State;

namespace Vortex.Core.Bots
{
    /// <summary>
    /// A computer player. Bots only use the public engine API: they receive the legal commands and pick one,
    /// including answers to decisions addressed to them. They never name a card (ADR-0010).
    /// </summary>
    public interface IBot
    {
        /// <summary>Short name used in reports.</summary>
        string Name { get; }

        /// <summary>Picks one of <paramref name="legal"/> (never empty) for <paramref name="seat"/>.</summary>
        Command Choose(GameEngine engine, GameState state, int seat, IReadOnlyList<Command> legal);
    }

    /// <summary>Uniformly random legal play: the "no skill" baseline for balance measurements.</summary>
    public sealed class RandomBot : IBot
    {
        private readonly Pcg32 _rng;

        /// <summary>Creates a bot with its own random stream.</summary>
        public RandomBot(ulong seed)
        {
            _rng = Pcg32.Seeded(seed, 0x5EEDUL);
        }

        /// <inheritdoc/>
        public string Name => "random";

        /// <inheritdoc/>
        public Command Choose(GameEngine engine, GameState state, int seat, IReadOnlyList<Command> legal)
        {
            if (legal is null || legal.Count == 0)
            {
                throw new ArgumentException("No legal command.", nameof(legal));
            }

            return legal[_rng.NextInt(legal.Count)];
        }
    }

    /// <summary>Weights of <see cref="HeuristicBot"/>'s position evaluation.</summary>
    public sealed class HeuristicWeights
    {
        /// <summary>Own hit point.</summary>
        public double Hp { get; set; } = 1.0;

        /// <summary>Own shield point (roughly the damage it prevents per attack suffered).</summary>
        public double Shield { get; set; } = 1.0;

        /// <summary>Equipped durable or triggered modifier.</summary>
        public double Modifier { get; set; } = 3.0;

        /// <summary>Equipped single-use modifier: its value lies in being used, so keeping it is worth little.</summary>
        public double SingleUseModifier { get; set; } = 1.0;

        /// <summary>Torment token on own modifiers (negative).</summary>
        public double Torment { get; set; } = -2.0;

        /// <summary>Overcharge token (worth about one extra die later, not a lasting asset).</summary>
        public double Overcharge { get; set; } = 1.0;

        /// <summary>Technology obtained.</summary>
        public double Technology { get; set; } = 12.0;

        /// <summary>Per HP of each alive opponent (negative: an HP taken from anyone is worth an HP kept).</summary>
        public double OpponentHp { get; set; } = -1.0;

        /// <summary>Each eliminated opponent.</summary>
        public double EliminatedOpponent { get; set; } = 25.0;
    }

    /// <summary>
    /// Greedy bot with determinization and a short rollout (ADR-0010). For each legal command it:
    /// <list type="number">
    /// <item>replaces hidden information by its own guess (reseeds the generator, reshuffles the piles), so it cannot see future dice or cards;</item>
    /// <item>plays the command, then finishes its own turn with a simple default policy (end the market, attack the weakest opponent), answering any decision at random;</item>
    /// <item>scores the resulting position with <see cref="HeuristicWeights"/>, averaged over a few samples.</item>
    /// </list>
    /// It knows nothing about cards: it discovers what they do by simulating them, so new cards need no bot code.
    /// </summary>
    public sealed class HeuristicBot : IBot
    {
        private const int MaxRolloutSteps = 40;
        private readonly Pcg32 _rng;
        private readonly int _samples;
        private readonly HeuristicWeights _weights;

        /// <summary>Creates a bot.</summary>
        /// <param name="seed">Seed of the bot's own random stream (guesses and tie breaks).</param>
        /// <param name="samples">Simulations per candidate command; more is stronger and slower.</param>
        /// <param name="weights">Evaluation weights, or null for defaults.</param>
        public HeuristicBot(ulong seed, int samples = 2, HeuristicWeights? weights = null)
        {
            if (samples < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(samples));
            }

            _rng = Pcg32.Seeded(seed, 0xB07UL);
            _samples = samples;
            _weights = weights ?? new HeuristicWeights();
        }

        /// <inheritdoc/>
        public string Name => "heuristic";

        /// <inheritdoc/>
        public Command Choose(GameEngine engine, GameState state, int seat, IReadOnlyList<Command> legal)
        {
            if (engine is null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (legal is null || legal.Count == 0)
            {
                throw new ArgumentException("No legal command.", nameof(legal));
            }

            if (legal.Count == 1)
            {
                return legal[0];
            }

            Command best = legal[0];
            double bestScore = double.NegativeInfinity;
            foreach (Command candidate in legal)
            {
                double total = 0;
                for (int i = 0; i < _samples; i++)
                {
                    total += Evaluate(Simulate(engine, state, seat, candidate), seat, engine.Data);
                }

                // Tiny random tie break so equal options do not always resolve to the first one.
                double score = (total / _samples) + (_rng.NextInt(1000) * 1e-6);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        /// <summary>
        /// Position value for <paramref name="seat"/> (higher is better). With <paramref name="data"/>, single-use
        /// modifiers are valued by their public usage (card metadata, never a specific card).
        /// </summary>
        public double Evaluate(GameState state, int seat, Content.GameData? data = null)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.Outcome != null)
            {
                return state.Outcome.Winner == seat ? 10_000 : (state.Outcome.Winner < 0 ? 0 : -10_000);
            }

            PlayerState me = state.Players[seat];
            if (me.Eliminated)
            {
                return -10_000;
            }

            List<PlayerState> opponents = state.Players.Where(p => p.Seat != seat).ToList();
            List<PlayerState> alive = opponents.Where(p => !p.Eliminated).ToList();
            double value = (_weights.Hp * me.Hp)
                + (_weights.Shield * me.Shield)
                + me.Modifiers().Sum(c => IsSingleUse(data, c.CardId) ? _weights.SingleUseModifier : _weights.Modifier)
                + (_weights.Torment * me.Modifiers().Sum(c => c.Torments))
                + (_weights.Overcharge * me.Overcharge)
                + (_weights.Technology * me.Technologies.Count)
                + (_weights.EliminatedOpponent * (opponents.Count - alive.Count));
            if (alive.Count > 0)
            {
                value += _weights.OpponentHp * alive.Sum(p => p.Hp);
            }

            return value;
        }

        private static bool IsSingleUse(Content.GameData? data, string cardId)
        {
            return data != null && data.Modifiers.Any(d => d.Id == cardId && d.Usage == Content.CardUsage.SingleUse);
        }

        // Plays the candidate on a guessed copy of the game, then finishes the bot's turn.
        private GameState Simulate(GameEngine engine, GameState state, int seat, Command candidate)
        {
            // A suspended command must be replayed with the very same generator and piles (ADR-0009): hidden
            // information can only be re-guessed once that command is over. The bot therefore sees how the
            // current command ends, but nothing after it (documented limitation, ADR-0010).
            bool suspended = state.Pending != null;
            EngineResult result = engine.Submit(suspended ? state : Determinize(state), seat, candidate);
            if (!result.Accepted)
            {
                return state;
            }

            GameState after = FinishCommand(engine, result.State);
            return after.Pending == null && after.Outcome == null ? Rollout(engine, Determinize(after), seat) : after;
        }

        // Answers, at random, the remaining decisions of the command being resolved.
        private GameState FinishCommand(GameEngine engine, GameState state)
        {
            for (int step = 0; step < MaxRolloutSteps && state.Pending != null && state.Outcome == null; step++)
            {
                var decision = state.Pending.Decision;
                EngineResult r = engine.Submit(state, decision.Player, Command.Answer(decision.Id, decision.Options[_rng.NextInt(decision.Options.Count)].Key));
                if (!r.Accepted)
                {
                    break;
                }

                state = r.State;
            }

            return state;
        }

        // Hidden information (deck orders, generator state) is replaced by the bot's own guess.
        // Only valid when no command is suspended.
        private GameState Determinize(GameState state)
        {
            GameState guess = state.Clone();
            guess.Rng = Pcg32.Seeded(((ulong)_rng.NextUInt() << 32) | _rng.NextUInt(), 1);

            // Canonical order first, so the guess depends only on what is visible (the pile contents are
            // public knowledge through the card list; their order is not).
            guess.AttackMarket.Deck.Sort((a, b) => a.Uid.CompareTo(b.Uid));
            guess.DefenseMarket.Deck.Sort((a, b) => a.Uid.CompareTo(b.Uid));
            guess.EventDeck.Sort(StringComparer.Ordinal);
            Shuffle(guess.AttackMarket.Deck);
            Shuffle(guess.DefenseMarket.Deck);
            Shuffle(guess.EventDeck);
            return guess;
        }

        // Finishes the bot's own turn with a default policy; decisions (anyone's) are answered at random.
        private GameState Rollout(GameEngine engine, GameState state, int seat)
        {
            for (int step = 0; step < MaxRolloutSteps && state.Outcome == null; step++)
            {
                int actor;
                Command next;
                if (state.Pending != null)
                {
                    actor = state.Pending.Decision.Player;
                    var options = state.Pending.Decision.Options;
                    next = Command.Answer(state.Pending.Decision.Id, options[_rng.NextInt(options.Count)].Key);
                }
                else if (state.CurrentPlayer != seat)
                {
                    break;
                }
                else
                {
                    actor = seat;
                    next = DefaultPolicy(engine, state, seat);
                }

                EngineResult r = engine.Submit(state, actor, next);
                if (!r.Accepted)
                {
                    break;
                }

                state = r.State;
            }

            return state;
        }

        private static Command DefaultPolicy(GameEngine engine, GameState state, int seat)
        {
            if (state.Phase == TurnPhase.Market)
            {
                return Command.EndMarket();
            }

            IReadOnlyList<Command> legal = engine.LegalCommands(state, seat);
            Command? attack = legal
                .Where(c => c.Type == CommandType.Attack && c.UseOvercharge == (state.Players[seat].Overcharge > 0))
                .OrderBy(c => state.Players[c.Target].Hp)
                .FirstOrDefault();
            return attack ?? Command.EndTurn();
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.NextInt(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
