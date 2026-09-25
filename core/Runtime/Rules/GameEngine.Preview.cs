using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Commands;
using Vortex.Core.Dice;
using Vortex.Core.Effects;
using Vortex.Core.Projection;
using Vortex.Core.State;

namespace Vortex.Core.Rules
{
    /// <summary>Previews: what a command is likely to do, computed by the engine itself (ADR-0015, ADR-0018).</summary>
    public sealed partial class GameEngine
    {
        /// <summary>Guessed games played by a preview when the caller does not say.</summary>
        public const int DefaultPreviewSamples = 400;

        /// <summary>
        /// Estimates what <paramref name="command"/> by <paramref name="player"/> would do, by playing it on
        /// <paramref name="samples"/> copies of the game whose hidden information (deck orders, random generator) is
        /// guessed with <paramref name="guesses"/>; choices met on the way are answered at random. The very rules of
        /// the real command run, so every active effect counts, and the real generator is never used: the preview
        /// cannot reveal a coming die or draw. <paramref name="state"/> is not changed.
        /// Returns null when the command is not legal now, or while a decision is pending.
        /// </summary>
        public CommandPreview? Preview(GameState state, int player, Command command, Pcg32 guesses, int samples = DefaultPreviewSamples)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (guesses is null)
            {
                throw new ArgumentNullException(nameof(guesses));
            }

            if (samples < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(samples));
            }

            if (state.Pending != null || state.Outcome != null || command.Type == CommandType.AnswerDecision)
            {
                return null;
            }

            var legality = new Game(state.Clone(), Data, Config, _catalog, Array.Empty<string>());
            if (legality.Validate(player, command) != null)
            {
                return null;
            }

            int seats = state.Players.Count;
            var hp = new List<int>[seats];
            var shield = new List<int>[seats];
            var eliminated = new int[seats];
            for (int seat = 0; seat < seats; seat++)
            {
                hp[seat] = new List<int>(samples);
                shield[seat] = new List<int>(samples);
            }

            var attacks = new List<AttackInfo>(samples);
            int attacksStarted = 0;
            int redirected = 0;
            for (int sample = 0; sample < samples; sample++)
            {
                Game? run = PlayGuessed(HiddenInformation.Guess(state, guesses), player, command, guesses);
                if (run == null)
                {
                    continue;
                }

                for (int seat = 0; seat < seats; seat++)
                {
                    PlayerState after = run.State.Players[seat];
                    hp[seat].Add(after.Hp);
                    shield[seat].Add(after.Shield);
                    if (after.Eliminated && !state.Players[seat].Eliminated)
                    {
                        eliminated[seat]++;
                    }
                }

                AttackInfo? attack = run.Attacks.FirstOrDefault(a => a.Attacker == player);
                if (attack != null)
                {
                    attacksStarted++;
                    if (attack.Redirected)
                    {
                        redirected++;
                    }
                    else
                    {
                        attacks.Add(attack);
                    }
                }
            }

            int played = hp[0].Count;
            if (played == 0)
            {
                return null;
            }

            var seatPreviews = new SeatPreview[seats];
            for (int seat = 0; seat < seats; seat++)
            {
                seatPreviews[seat] = new SeatPreview(seat, Estimate.Of(hp[seat]), Estimate.Of(shield[seat]), (double)eliminated[seat] / played);
            }

            return new CommandPreview(played, seatPreviews, attacksStarted == 0 ? null : Summarize(attacks, (double)redirected / attacksStarted));
        }

        // Runs the command on a guessed game to its end, answering every decision at random. Null if it cannot end.
        private Game? PlayGuessed(GameState guess, int player, Command command, Pcg32 guesses)
        {
            var answers = new List<string>();
            for (int run = 0; run <= Config.MaxDecisionsPerCommand; run++)
            {
                var game = new Game(guess.Clone(), Data, Config, _catalog, answers);
                try
                {
                    game.Execute(player, command);
                    return game;
                }
                catch (DecisionNeededException needed)
                {
                    List<Decisions.DecisionOption> options = needed.Request.Options;
                    answers.Add(options[guesses.NextInt(options.Count)].Key);
                }
            }

            return null;
        }

        private static AttackPreview Summarize(List<AttackInfo> attacks, double redirected)
        {
            AttackInfo? first = attacks.FirstOrDefault();
            int throws = first != null && first.NetAdvantage != 0 ? 2 : 1;
            return new AttackPreview(
                first == null ? 0 : first.Rolls.Count / throws,
                first == null ? 0 : first.Kept.Count,
                first?.NetAdvantage ?? 0,
                Estimate.Of(attacks.Select(a => a.Value - a.KeptSum).ToList()),
                Bonuses(attacks),
                Estimate.Of(attacks.Select(a => a.EffectiveShield).ToList()),
                Estimate.Of(attacks.Select(a => a.HpLost).ToList()),
                Share(attacks, a => a.HpLost > 0),
                Share(attacks, a => a.Critical),
                redirected);
        }

        // Each source's total per sample (0 where it added nothing), sources in order of first appearance.
        private static List<BonusPreview> Bonuses(List<AttackInfo> attacks)
        {
            var sources = new List<(SourceKind Kind, string? Id)>();
            foreach (ValueShare share in attacks.SelectMany(a => a.Bonuses))
            {
                (SourceKind, string?) key = SourceKinds.Of(share.Source);
                if (!sources.Contains(key))
                {
                    sources.Add(key);
                }
            }

            return sources
                .Select(key => new BonusPreview(key.Kind, key.Id, Estimate.Of(attacks.Select(a => a.Bonuses.Where(b => SourceKinds.Of(b.Source) == key).Sum(b => b.Amount)).ToList())))
                .ToList();
        }

        private static double Share(List<AttackInfo> attacks, Func<AttackInfo, bool> predicate)
        {
            return attacks.Count == 0 ? 0 : (double)attacks.Count(predicate) / attacks.Count;
        }
    }
}
