using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using static Vortex.Simulator.Report;

namespace Vortex.Simulator
{
    /// <summary>Ranked table of a grid run (French: written for the game designer).</summary>
    internal static class GridReport
    {
        public static string Write(RunSettings settings, GridSpec spec, int players, int games, string referenceSha256, IReadOnlyList<GridResult> ranked)
        {
            var sb = new StringBuilder();
            L(sb, "# Grille : " + spec.Name);
            L(sb);
            Header(sb, settings);
            if (spec.Description.Length > 0)
            {
                L(sb, spec.Description);
                L(sb);
            }

            L(sb, "- **" + ranked.Count + " combinaisons**, " + games + " parties chacune à " + players + " joueurs, bots **" + settings.MainBot + "**. Toutes les combinaisons jouent les mêmes graines.");
            L(sb, "- **(réf.)** marque la combinaison identique au contenu du dépôt.");
            L(sb, "- Les valeurs **en gras** sont dans la cible. **Écart** : somme des écarts relatifs aux bornes manquées (0 = toutes les cibles atteintes). Le classement met d'abord le plus de cibles atteintes, puis le plus petit écart.");
            L(sb, "- Précision : avec " + games + " parties, un pourcentage est connu à environ ±" + Num(1.96 * Math.Sqrt(0.25 / Math.Max(games, 1)) * 100) + " pts. Cette grille sert à **trier** : les meilleures combinaisons se confirment ensuite avec `compare` sur plus de parties.");
            L(sb);

            L(sb, "## Axes et cibles");
            L(sb);
            foreach (GridAxis axis in spec.Axes)
            {
                L(sb, "- **" + axis.Name + "** : " + string.Join(", ", axis.Values.Select(v => v.Label)));
            }

            L(sb);
            L(sb, "| Indicateur | Cible |");
            L(sb, "|---|---|");
            foreach (GridTarget t in spec.Targets)
            {
                L(sb, "| " + IndicatorName(t.Indicator) + " | " + Range(t) + " |");
            }

            L(sb);
            L(sb, "## Classement");
            L(sb);
            L(sb, "| Rang | " + string.Join(" | ", spec.Axes.Select(a => a.Name)) + " | Cibles atteintes | Écart | " + string.Join(" | ", spec.Targets.Select(t => IndicatorName(t.Indicator))) + " | Manches | PV retirés par attaque |");
            L(sb, "|---|" + string.Concat(Enumerable.Repeat("---|", spec.Axes.Count + 2 + spec.Targets.Count + 2)));
            for (int i = 0; i < ranked.Count; i++)
            {
                GridResult r = ranked[i];
                string rank = (i + 1).ToString(CultureInfo.InvariantCulture) + (r.Cell.Content.ContentSha256 == referenceSha256 ? " (réf.)" : string.Empty);
                IEnumerable<string> values = spec.Targets.Select(t => Cell(t, r.Values[t.Indicator]));
                L(sb, "| " + rank + " | " + string.Join(" | ", r.Cell.Labels) + " | " + r.Met + "/" + spec.Targets.Count + " | " + Num(r.Score) + " | " + string.Join(" | ", values) + " | " + Num(r.Stats.Rounds.Value) + " | " + Num(r.Stats.HpPerAttack) + " |");
            }

            int errors = ranked.Sum(r => r.Stats.Errors);
            L(sb);
            L(sb, "## Anomalies");
            L(sb);
            L(sb, errors == 0 ? "Aucune erreur du moteur." : errors + " partie(s) en erreur : relancer `run --variant` sur la combinaison concernée pour obtenir les graines.");
            return sb.ToString().TrimEnd('\n') + "\n";
        }

        internal static string IndicatorName(GridIndicator indicator)
        {
            switch (indicator)
            {
                case GridIndicator.DurationMinutes: return "Durée (min)";
                case GridIndicator.DoomReached: return "Fin des temps atteinte";
                case GridIndicator.ElectionShare: return "Victoires par Élection";
                case GridIndicator.FirstEliminationRound: return "Première élimination (manche)";
                default: return "Écart de position (pts)";
            }
        }

        private static bool IsShare(GridIndicator indicator) => indicator == GridIndicator.DoomReached || indicator == GridIndicator.ElectionShare;

        private static string Format(GridIndicator indicator, double value)
        {
            if (value == double.MaxValue)
            {
                return "—";
            }

            return IsShare(indicator) ? Pct(value) : Num(value);
        }

        private static string Range(GridTarget t)
        {
            if (t.Min.HasValue && t.Max.HasValue)
            {
                return Format(t.Indicator, t.Min.Value) + " à " + Format(t.Indicator, t.Max.Value);
            }

            return t.Min.HasValue ? "au moins " + Format(t.Indicator, t.Min.Value) : "au plus " + Format(t.Indicator, t.Max!.Value);
        }

        private static string Cell(GridTarget t, double value)
        {
            string text = Format(t.Indicator, value);
            return t.Distance(value) == 0 ? "**" + text + "**" : text;
        }
    }
}
