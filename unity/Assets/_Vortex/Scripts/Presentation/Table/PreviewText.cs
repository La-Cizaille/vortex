using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vortex.Client.Content;
using Vortex.Core.Commands;
using Vortex.Core.Projection;
using Vortex.Core.Rules;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Words for a preview computed by the engine (ADR-0018): an aimed attack or sabotage, shown in the help bubble next
    /// to the target. The figures all come from the engine's <see cref="CommandPreview"/>; this class only writes them.
    /// </summary>
    public sealed class PreviewText
    {
        private const string Minus = "\u2212";
        private readonly TableContext _context;
        private readonly TextTable _texts;
        private readonly int _faces;
        private readonly NumberFormatInfo _numbers;

        /// <summary>Creates the words of a game.</summary>
        /// <param name="context">Interface texts and content names.</param>
        /// <param name="faces">Faces of the game's dice (public rules).</param>
        public PreviewText(TableContext context, int faces)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _texts = context.Texts;
            _faces = faces;
            _numbers = new NumberFormatInfo { NumberDecimalSeparator = _texts.Get(TextKeys.PreviewDecimal), NegativeSign = Minus };
        }

        /// <summary>The preview of a command aimed at <paramref name="command"/>'s target, or null when there is nothing to say.</summary>
        public string? Describe(CommandPreview? preview, Command command, GameView view, int seat)
        {
            if (preview is null || command is null || view is null || command.Target < 0 || command.Target >= preview.Seats.Count)
            {
                return null;
            }

            SeatPreview target = preview.Seats[command.Target];
            var lines = new List<string>();
            if (preview.Attack is AttackPreview attack)
            {
                lines.Add(Dice(attack));
                string? bonuses = Bonuses(attack);
                if (bonuses != null)
                {
                    lines.Add(bonuses);
                }

                lines.Add(Format(TextKeys.PreviewShield, Range(attack.EffectiveShield)));
                lines.Add(attack.Damage.IsExact
                    ? Format(TextKeys.PreviewDamageExact, Number(attack.Damage.Min))
                    : Format(TextKeys.PreviewDamage, Range(attack.Damage), Average(attack.Damage.Average)));
                lines.Add(target.Eliminated > 0
                    ? Format(TextKeys.PreviewChancesDestroyed, Percent(attack.Hit), Percent(attack.Critical), Percent(target.Eliminated))
                    : Format(TextKeys.PreviewChances, Percent(attack.Hit), Percent(attack.Critical)));
                if (attack.Redirected > 0)
                {
                    lines.Add(_texts.Get(TextKeys.PreviewRedirect));
                }
            }
            else if (command.Type == CommandType.Sabotage)
            {
                lines.Add(Format(TextKeys.PreviewSabotage, Number(view.Players[command.Target].Shield), Average(target.Shield.Average), Range(target.Shield)));
            }
            else
            {
                return null;
            }

            // Some effects hit back at the attacker: say how much it may cost.
            if (seat >= 0 && seat < preview.Seats.Count && preview.Seats[seat].Hp.Min < view.Players[seat].Hp)
            {
                lines.Add(Format(TextKeys.PreviewSelfLoss, Number(view.Players[seat].Hp - preview.Seats[seat].Hp.Min)));
            }

            return string.Join("\n", lines);
        }

        private string Dice(AttackPreview attack)
        {
            string dice = attack.DiceKept < attack.DicePerThrow
                ? Format(TextKeys.PreviewDiceKept, Number(attack.DicePerThrow), Number(_faces), Number(attack.DiceKept))
                : Format(TextKeys.PreviewDice, Number(attack.DicePerThrow), Number(_faces));
            return attack.NetAdvantage switch
            {
                > 0 => dice + _texts.Get(TextKeys.PreviewAdvantage),
                < 0 => dice + _texts.Get(TextKeys.PreviewDisadvantage),
                _ => dice,
            };
        }

        // Each source with what it adds ("+4 Rétablir l'ordre, −1 à +3 Pile ou face"); the total alone if no source is known.
        private string? Bonuses(AttackPreview attack)
        {
            if (attack.Bonuses.Count > 0)
            {
                return Format(TextKeys.PreviewBonuses, string.Join(", ", attack.Bonuses.Select(b => Format(TextKeys.PreviewBonusItem, Name(b), Signed(b.Amount)))));
            }

            return attack.Bonus.IsExact && attack.Bonus.Min == 0 ? null : Format(TextKeys.PreviewBonuses, Signed(attack.Bonus));
        }

        private string Name(BonusPreview bonus) => bonus.Origin switch
        {
            BonusOrigin.Rule => _texts.Get(TextKeys.PreviewBonusRule),
            BonusOrigin.Status => _texts.Get(TextKeys.Status(bonus.Id ?? string.Empty)),
            _ => _context.Face(bonus.Id ?? string.Empty).Title,
        };

        private string Format(string key, params object[] values) => string.Format(CultureInfo.InvariantCulture, _texts.Get(key), values);

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private string Signed(Estimate estimate) => estimate.IsExact
            ? SignedNumber(estimate.Min)
            : Format(TextKeys.PreviewRange, SignedNumber(estimate.Min), SignedNumber(estimate.Max));

        private static string SignedNumber(int value) => (value >= 0 ? "+" : Minus) + Math.Abs(value).ToString(CultureInfo.InvariantCulture);

        private string Range(Estimate estimate) => estimate.IsExact ? Number(estimate.Min) : Format(TextKeys.PreviewRange, Number(estimate.Min), Number(estimate.Max));

        private string Average(double value) => Math.Round(value, 1).ToString("0.#", _numbers);

        // Estimates from samples: rounded to 5 %, never shown as certain when they are not.
        private string Percent(double chance)
        {
            int rounded = (int)Math.Round(chance * 20, MidpointRounding.AwayFromZero) * 5;
            string text = chance > 0 && rounded == 0 ? "< 5" : chance < 1 && rounded == 100 ? "> 95" : Number(rounded);
            return Format(TextKeys.PreviewPercent, text);
        }
    }
}
