using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vortex.Core.Content;
using static Vortex.Simulator.Report;

namespace Vortex.Simulator
{
    /// <summary>Results of one content set (reference or variant) for every simulated table size.</summary>
    internal sealed class CompareEntry
    {
        public CompareEntry(ContentSet content, IReadOnlyList<ScenarioStats> byPlayers, ScenarioStats all)
        {
            Content = content;
            ByPlayers = byPlayers;
            All = all;
        }

        public ContentSet Content { get; }

        public IReadOnlyList<ScenarioStats> ByPlayers { get; }

        /// <summary>All table sizes pooled (for card deviations).</summary>
        public ScenarioStats All { get; }
    }

    /// <summary>
    /// Side-by-side report: the reference and each variant, simulated with the very same seeds.
    /// Differences carry a 95 % confidence interval; <c>*</c> marks a difference beyond the noise.
    /// </summary>
    internal static class CompareReport
    {
        private const double Z95 = 1.96;

        public static string Write(RunSettings settings, IReadOnlyList<CompareEntry> entries)
        {
            var sb = new StringBuilder();
            CompareEntry reference = entries[0];
            L(sb, "# Comparaison de variantes");
            L(sb);
            Header(sb, settings);
            L(sb, "Chaque variante est simulée avec **les mêmes graines** que la référence (bots **" + settings.MainBot + "**). Entre parenthèses : écart avec la référence, ± sa marge d'erreur à 95 %. Un `*` signale un écart au-delà du bruit statistique.");
            L(sb);
            L(sb, "## Variantes");
            L(sb);
            foreach (CompareEntry e in entries)
            {
                L(sb, "- **" + e.Content.Name + "** (empreinte `" + Short(e.Content.ContentSha256) + "`) : " + e.Content.Description);
            }

            foreach (ScenarioStats refStats in reference.ByPlayers)
            {
                int n = refStats.Players;
                List<ScenarioStats> columns = entries.Select(e => e.ByPlayers.First(s => s.Players == n)).ToList();
                L(sb);
                L(sb, "## " + n + " joueurs");
                L(sb);
                L(sb, "| Indicateur | " + string.Join(" | ", entries.Select(e => e.Content.Name)) + " |");
                L(sb, "|---|" + string.Concat(Enumerable.Repeat("---|", entries.Count)));
                Row(sb, "Écart max de position (pts)", columns, s => s.MaxPositionGap);
                for (int pos = 0; pos < n; pos++)
                {
                    int p = pos;
                    RowProportion(sb, "Victoires en position " + (p + 1), columns, s => s.PositionWins[p]);
                }

                RowMean(sb, "Manches (moyenne)", columns, s => s.Rounds);
                RowProportion(sb, "Fin des temps atteinte", columns, s => s.DoomReached);
                RowProportion(sb, "Victoires par Élection", columns, s => s.Elections);
                RowProportion(sb, "Égalités", columns, s => s.Draws);
                RowMean(sb, "Première élimination (manche)", columns, s => s.FirstElimination);
                RowProportion(sb, "Le meneur à mi-partie gagne", columns, s => s.MidGameLeaderWins);
                RowProportion(sb, "Attaques sur le meneur", columns, s => s.AttacksOnLeader);
                RowProportion(sb, "Attaques sans dégâts", columns, s => s.Harmless);
                Row(sb, "PV retirés par attaque", columns, s => s.HpPerAttack);
                Row(sb, "Attaques par tour", columns, s => s.AttacksPerTurn);
            }

            WriteCardChanges(sb, entries);
            WriteErrors(sb, entries);
            return sb.ToString().TrimEnd('\n') + "\n";
        }

        private static void RowProportion(StringBuilder sb, string label, List<ScenarioStats> columns, Func<ScenarioStats, Proportion> pick)
        {
            Proportion reference = pick(columns[0]);
            var cells = new List<string> { Pct(reference) };
            foreach (ScenarioStats s in columns.Skip(1))
            {
                Proportion p = pick(s);
                if (p.Total == 0 || reference.Total == 0)
                {
                    // Not applicable (e.g. "attacks on the leader" in a duel): no difference to show.
                    cells.Add(Pct(p));
                    continue;
                }

                double diff = (p.Rate - reference.Rate) * 100;
                double margin = Z95 * Math.Sqrt((p.StdErr * p.StdErr) + (reference.StdErr * reference.StdErr)) * 100;
                cells.Add(Pct(p) + " (" + Signed(diff) + " ± " + Num(margin) + " pts" + (Math.Abs(diff) > margin && margin > 0 ? " *" : string.Empty) + ")");
            }

            L(sb, "| " + label + " | " + string.Join(" | ", cells) + " |");
        }

        private static void RowMean(StringBuilder sb, string label, List<ScenarioStats> columns, Func<ScenarioStats, Mean> pick)
        {
            Mean reference = pick(columns[0]);
            var cells = new List<string> { reference.Count == 0 ? "—" : Num(reference.Value) };
            foreach (ScenarioStats s in columns.Skip(1))
            {
                Mean m = pick(s);
                if (m.Count == 0 || reference.Count == 0)
                {
                    cells.Add(m.Count == 0 ? "—" : Num(m.Value));
                    continue;
                }

                double diff = m.Value - reference.Value;
                double margin = Z95 * Math.Sqrt((m.StdErr * m.StdErr) + (reference.StdErr * reference.StdErr));
                cells.Add(Num(m.Value) + " (" + Signed(diff) + " ± " + Num(margin) + (Math.Abs(diff) > margin && margin > 0 ? " *" : string.Empty) + ")");
            }

            L(sb, "| " + label + " | " + string.Join(" | ", cells) + " |");
        }

        // Indicators without a simple error model: values and raw differences only.
        private static void Row(StringBuilder sb, string label, List<ScenarioStats> columns, Func<ScenarioStats, double> value)
        {
            double reference = value(columns[0]);
            var cells = new List<string> { Num(reference) };
            cells.AddRange(columns.Skip(1).Select(s => Num(value(s)) + " (" + Signed(value(s) - reference) + ")"));
            L(sb, "| " + label + " | " + string.Join(" | ", cells) + " |");
        }

        private static void WriteCardChanges(StringBuilder sb, IReadOnlyList<CompareEntry> entries)
        {
            L(sb);
            L(sb, "## Cartes dont l'écart change nettement");
            L(sb);
            CompareEntry reference = entries[0];
            int tests = reference.Content.Data.Modifiers.Count * (entries.Count - 1);
            L(sb, "Toutes tables confondues. Écart = taux de victoire des preneurs moins la moyenne des cartes. Seules les cartes dont l'écart bouge au-delà du bruit sont listées.");
            L(sb, "Attention : " + tests + " couples (carte, variante) sont testés au seuil de 95 %, donc environ **" + Num(tests * 0.05) + "** lignes peuvent apparaître par pur hasard. Ne retenir que les changements nets et confirmés par une autre graine.");
            L(sb);
            bool any = false;
            foreach (CompareEntry variant in entries.Skip(1))
            {
                var changes = new List<(string Line, double Delta)>();
                foreach (CardDefinition card in reference.Content.Data.Modifiers)
                {
                    CardStats a = reference.All.Cards[card.Id];
                    if (!variant.All.Cards.TryGetValue(card.Id, out CardStats? b))
                    {
                        continue;
                    }

                    double delta = (b.Deviation - a.Deviation) * 100;
                    double margin = Z95 * Math.Sqrt((a.StdErr * a.StdErr) + (b.StdErr * b.StdErr)) * 100;
                    if (margin > 0 && Math.Abs(delta) > margin)
                    {
                        changes.Add(($"| {variant.Content.Name} | `{card.Id}` {card.Name} | {Signed(a.Deviation * 100)} pts | {Signed(b.Deviation * 100)} pts | {Signed(delta)} ± {Num(margin)} pts |", delta));
                    }
                }

                if (changes.Count == 0)
                {
                    continue;
                }

                if (!any)
                {
                    L(sb, "| Variante | Carte | Écart (référence) | Écart (variante) | Changement |");
                    L(sb, "|---|---|---|---|---|");
                    any = true;
                }

                foreach ((string line, double _) in changes.OrderByDescending(c => Math.Abs(c.Delta)))
                {
                    L(sb, line);
                }
            }

            if (!any)
            {
                L(sb, "Aucune carte ne change de façon significative.");
            }
        }

        private static void WriteErrors(StringBuilder sb, IReadOnlyList<CompareEntry> entries)
        {
            int errors = entries.Sum(e => e.ByPlayers.Sum(s => s.Errors));
            L(sb);
            L(sb, "## Anomalies");
            L(sb);
            L(sb, errors == 0 ? "Aucune erreur du moteur." : errors + " partie(s) en erreur : relancer `run` sur la variante concernée pour obtenir les graines.");
        }
    }
}
