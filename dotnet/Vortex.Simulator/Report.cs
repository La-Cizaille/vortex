using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Vortex.Core.Content;

namespace Vortex.Simulator
{
    /// <summary>A batch of games sharing a table size and a bot line-up.</summary>
    internal sealed class ScenarioResult
    {
        public ScenarioResult(int players, bool skillGap, List<GameRecord> records)
        {
            Players = players;
            SkillGap = skillGap;
            Records = records;
        }

        public int Players { get; }

        /// <summary>True: one hero bot against weaker bots. False: the main line-up.</summary>
        public bool SkillGap { get; }

        public List<GameRecord> Records { get; }
    }

    /// <summary>Settings echoed in the report header, so any figure can be reproduced.</summary>
    internal sealed class RunSettings
    {
        public string Command { get; set; } = string.Empty;

        public string ContentSha256 { get; set; } = string.Empty;

        public string MainBot { get; set; } = "normal";

        public string SkillHero { get; set; } = "normal";

        public string SkillOthers { get; set; } = "random";

        public int SecondsPerTurn { get; set; } = 30;
    }

    /// <summary>Renders the balance report of a single content set (French: written for the game designer).</summary>
    internal static class Report
    {
        public static string Write(RunSettings settings, GameData data, IReadOnlyList<ScenarioResult> scenarios)
        {
            var sb = new StringBuilder();
            List<ScenarioStats> full = scenarios.Where(s => !s.SkillGap).Select(s => ScenarioStats.Of(s.Players, s.Records, data)).ToList();

            L(sb, "# Rapport d'équilibrage");
            L(sb);
            Header(sb, settings);
            L(sb, "## Comment lire ce rapport");
            L(sb);
            L(sb, "- Les parties sont jouées par des **bots** qui ne connaissent aucune carte : ils essaient chaque coup en le simulant, sans voir le futur (ADR-0010). Niveau des bots du scénario principal : **" + settings.MainBot + "**.");
            L(sb, "- Les bots ne jouent pas comme des humains : les tendances et les écarts sont utiles, les valeurs absolues le sont moins.");
            L(sb, "- **Écart** d'une carte : taux de victoire des joueurs qui l'ont prise, moins le même taux sur toutes les cartes (ce qui neutralise le biais de survie). Un `*` signale un écart au-delà du bruit (plus de 2 écarts-types).");
            L(sb);

            L(sb, "## 1. Durée et fin des parties");
            L(sb);
            L(sb, "Durée estimée à " + settings.SecondsPerTurn + " s par tour de joueur (hypothèse, à confronter aux parties réelles).");
            L(sb);
            L(sb, "| Joueurs | Parties | Terminées | Égalités | Manches (moy. / méd. / p90) | Tours de joueur | Fin des temps atteinte | Victoires par Élection | Durée estimée |");
            L(sb, "|---|---|---|---|---|---|---|---|---|");
            foreach (ScenarioStats s in full)
            {
                L(sb, $"| {s.Players} | {s.Games} | {Pct(s.Finished)} | {Pct(s.Draws)} | {Num(s.Rounds.Value)} / {Percentile(s.RoundsSorted, 0.5)} / {Percentile(s.RoundsSorted, 0.9)} | {Num(s.Turns.Value)} | {Pct(s.DoomReached)} | {Pct(s.Elections)} | {Num(s.Turns.Value * settings.SecondsPerTurn / 60.0)} min |");
            }

            L(sb);
            L(sb, "## 2. Avantage de position");
            L(sb);
            L(sb, "Taux de victoire selon la position dans le premier tour de table (1 = gagnant de l'initiative). Part équitable = 1/nombre de joueurs. **Écart max** = plus grand écart d'une position à la part équitable.");
            L(sb);
            int maxPlayers = full.Count == 0 ? 0 : full.Max(s => s.Players);
            L(sb, "| Joueurs | Part équitable | " + string.Join(" | ", Enumerable.Range(1, maxPlayers).Select(i => "Position " + i)) + " | Écart max |");
            L(sb, "|---|---|" + string.Concat(Enumerable.Repeat("---|", maxPlayers)) + "---|");
            foreach (ScenarioStats s in full)
            {
                IEnumerable<string> cells = Enumerable.Range(0, maxPlayers).Select(pos => pos < s.Players ? Pct(s.PositionWins[pos]) : "—");
                L(sb, $"| {s.Players} | {Pct(1.0 / s.Players)} | {string.Join(" | ", cells)} | {Num(s.MaxPositionGap)} pts |");
            }

            L(sb);
            L(sb, "## 3. Poids des choix (écart de niveau)");
            L(sb);
            L(sb, "Un bot **" + settings.SkillHero + "** (place tournante) contre des bots **" + settings.SkillOthers + "**. **Ratio** = victoires ÷ part équitable : proche de 1, le niveau ne compte guère.");
            L(sb);
            L(sb, "| Joueurs | Parties | Victoires du bot " + settings.SkillHero + " | Part équitable | Ratio |");
            L(sb, "|---|---|---|---|---|");
            foreach (ScenarioResult sr in scenarios.Where(s => s.SkillGap))
            {
                List<GameRecord> ok = sr.Records.Where(r => r.Error == null).ToList();
                var rate = new Proportion(ok.Count(r => r.Winner >= 0 && r.Winner == r.Index % r.Players), ok.Count);
                L(sb, $"| {sr.Players} | {ok.Count} | {Pct(rate)} | {Pct(1.0 / sr.Players)} | {Num(rate.Rate * sr.Players)} |");
            }

            L(sb);
            L(sb, "## 4. Combats, éliminations et retournements");
            L(sb);
            L(sb, "- **Sur le meneur / le plus faible** : parmi les attaques faites avec au moins deux adversaires en vie, part visant l'adversaire qui avait le plus / le moins de PV.");
            L(sb, "- **Le meneur à mi-partie gagne** : le joueur qui avait seul le plus de PV à la manche du milieu remporte la partie. Élevé = peu de retournements.");
            L(sb);
            L(sb, "| Joueurs | Attaques par tour | PV retirés par attaque | Attaques sans dégâts | Sur le meneur | Sur le plus faible | Première élimination (manche) | Le meneur à mi-partie gagne |");
            L(sb, "|---|---|---|---|---|---|---|---|");
            foreach (ScenarioStats s in full)
            {
                L(sb, $"| {s.Players} | {Num(s.AttacksPerTurn)} | {Num(s.HpPerAttack)} | {Pct(s.Harmless)} | {Pct(s.AttacksOnLeader)} | {Pct(s.AttacksOnWeakest)} | {Num(s.FirstElimination.Value)} | {Pct(s.MidGameLeaderWins)} |");
            }

            L(sb);
            L(sb, "**Actions d'équipage choisies** (part de toutes les actions d'équipage) :");
            L(sb);
            L(sb, "| Joueurs | " + string.Join(" | ", Enumerable.Range(0, GameRecord.CrewActionCount).Select(a => CrewActionFr(a))) + " |");
            L(sb, "|---|" + string.Concat(Enumerable.Repeat("---|", GameRecord.CrewActionCount)));
            foreach (ScenarioStats s in full)
            {
                L(sb, "| " + s.Players + " | " + string.Join(" | ", s.CrewActions.Select(p => Pct(p))) + " |");
            }

            ScenarioStats all = ScenarioStats.Of(0, scenarios.Where(s => !s.SkillGap).SelectMany(s => s.Records).ToList(), data);
            WriteCards(sb, data, all);
            WriteColors(sb, all);
            WriteEvents(sb, data, all);
            WriteAnomalies(sb, scenarios);
            return sb.ToString().TrimEnd('\n') + "\n";
        }

        internal static void Header(StringBuilder sb, RunSettings settings)
        {
            L(sb, "> Généré par `Vortex.Simulator`. Commande : `" + settings.Command + "`.");
            L(sb, "> Contenu analysé : empreinte `" + Short(settings.ContentSha256) + "` (SHA-256 des quatre fichiers de contenu, variante appliquée). Même commande et même contenu ⇒ mêmes chiffres.");
            L(sb);
        }

        private static void WriteCards(StringBuilder sb, GameData data, ScenarioStats all)
        {
            L(sb);
            L(sb, "## 5. Cartes");
            L(sb);
            L(sb, "Toutes tailles de table confondues. **Prises** = prises au marché par partie. **Preneurs** = couples (partie, joueur) ayant pris la carte au moins une fois. Taux de victoire moyen d'un preneur, toutes cartes confondues : **" + Pct(all.CardBaseline) + "**.");
            L(sb);
            L(sb, "| Carte | Nom | Prises / partie | Activations / prise | Preneurs | Victoires des preneurs | Écart |");
            L(sb, "|---|---|---|---|---|---|---|");
            foreach (CardDefinition card in data.Modifiers.OrderByDescending(c => all.Cards[c.Id].Deviation))
            {
                CardStats c = all.Cards[card.Id];
                bool activatable = card.Usage == CardUsage.SingleUse || c.Activations > 0;
                string activations = activatable ? Num(c.Picks == 0 ? 0 : (double)c.Activations / c.Picks) : "—";
                L(sb, $"| `{card.Id}` | {card.Name} | {Num(c.PerGame)} | {activations} | {c.Takers} | {Pct(new Proportion(c.Wins, c.Takers))} | {Signed(c.Deviation * 100)} pts{(c.Significant ? " *" : string.Empty)} |");
            }
        }

        private static void WriteColors(StringBuilder sb, ScenarioStats all)
        {
            L(sb);
            L(sb, "## 6. Couleurs et technologies");
            L(sb);
            L(sb, "**Couleur dominante** d'un joueur : la couleur non neutre qu'il a le plus prise au marché (joueurs à égalité exclus).");
            L(sb);
            L(sb, "| Couleur | Joueurs à dominante | Taux de victoire | Combos activés par partie |");
            L(sb, "|---|---|---|---|");
            foreach (KeyValuePair<TechColor, Proportion> c in all.DominantColor)
            {
                L(sb, $"| {ColorFr(c.Key)} | {c.Value.Total} | {Pct(c.Value)} | {Num(all.CombosPerGame[c.Key])} |");
            }
        }

        private static void WriteEvents(StringBuilder sb, GameData data, ScenarioStats all)
        {
            L(sb);
            L(sb, "## 7. Événements");
            L(sb);
            L(sb, "**Le meneur garde la tête** : le joueur qui avait seul le plus de PV avant l'événement l'a encore à la fin de la manche. Plus c'est bas, plus l'événement rebat les cartes. « Le calme avant la tempête » (sans effet) sert de témoin.");
            L(sb);
            L(sb, "| Événement | Révélations par partie | Le meneur garde la tête |");
            L(sb, "|---|---|---|");
            foreach (EventDefinition e in data.Events)
            {
                EventStats es = all.Events[e.Id];
                L(sb, $"| {e.Name} | {Num(es.PerGame)} | {Pct(es.LeaderKept)} |");
            }
        }

        private static void WriteAnomalies(StringBuilder sb, IReadOnlyList<ScenarioResult> scenarios)
        {
            L(sb);
            L(sb, "## 8. Anomalies");
            L(sb);
            List<GameRecord> errors = scenarios.SelectMany(s => s.Records).Where(r => r.Error != null).ToList();
            List<GameRecord> unfinished = scenarios.SelectMany(s => s.Records).Where(r => r.Error == null && !r.Finished).ToList();
            if (errors.Count == 0 && unfinished.Count == 0)
            {
                L(sb, "Aucune : toutes les parties se sont terminées sans erreur du moteur.");
                return;
            }

            L(sb, errors.Count + " erreur(s) du moteur, " + unfinished.Count + " partie(s) non terminée(s) après " + GameRunner.MaxCommands + " commandes. Graines pour les rejouer :");
            L(sb);
            foreach (GameRecord r in errors.Take(20))
            {
                L(sb, $"- graine `{r.Seed}` ({r.Players} joueurs) : {r.Error}");
            }

            foreach (GameRecord r in unfinished.Take(20))
            {
                L(sb, $"- graine `{r.Seed}` ({r.Players} joueurs) : non terminée");
            }
        }

        internal static string CrewActionFr(int action)
        {
            switch (action)
            {
                case 0: return "Attaque";
                case 1: return "Reparamétrage";
                case 2: return "Sabotage";
                case 3: return "Surcharge";
                default: return "Posture défensive";
            }
        }

        internal static string ColorFr(TechColor color)
        {
            switch (color)
            {
                case TechColor.Blue: return "Bleu";
                case TechColor.Red: return "Rouge";
                case TechColor.Green: return "Vert";
                case TechColor.Yellow: return "Jaune";
                default: return "Neutre";
            }
        }

        internal static string Short(string sha256) => sha256.Length <= 12 ? sha256 : string.Concat(sha256.AsSpan(0, 12), "…");

        internal static int Percentile(List<int> sorted, double p) => sorted.Count == 0 ? 0 : sorted[Math.Min(sorted.Count - 1, (int)Math.Floor(p * sorted.Count))];

        internal static string Pct(Proportion p) => p.Total == 0 ? "—" : Pct(p.Rate);

        internal static string Pct(double rate) => French(100.0 * rate) + " %";

        internal static string Num(double value) => French(value);

        internal static string Signed(double value) => (value >= 0 ? "+" : string.Empty) + French(value);

        // French decimal comma without depending on culture data (the tool runs with invariant globalization).
        internal static string French(double value) => value.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');

        internal static void L(StringBuilder sb, string text = "")
        {
            sb.Append(text).Append('\n');
        }
    }
}
