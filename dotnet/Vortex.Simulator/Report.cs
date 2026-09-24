using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Vortex.Core.Content;
using Vortex.Core.State;

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

        /// <summary>True: one heuristic bot against random bots. False: heuristic bots only.</summary>
        public bool SkillGap { get; }

        public List<GameRecord> Records { get; }
    }

    /// <summary>Settings echoed in the report header, so any figure can be reproduced.</summary>
    internal sealed class RunSettings
    {
        public int Games { get; set; }

        public ulong Seed { get; set; }

        public int Samples { get; set; }

        public string ContentSha256 { get; set; } = string.Empty;

        public int SecondsPerTurn { get; set; } = 30;
    }

    /// <summary>Renders the balance report (French: it is written for the game designer).</summary>
    internal static class Report
    {
        public static string Write(RunSettings settings, GameData data, IReadOnlyList<ScenarioResult> scenarios)
        {
            var sb = new StringBuilder();
            List<ScenarioResult> full = scenarios.Where(s => !s.SkillGap).ToList();
            List<ScenarioResult> gap = scenarios.Where(s => s.SkillGap).ToList();

            L(sb, "# Rapport d'équilibrage");
            L(sb);
            L(sb, "> Généré par `Vortex.Simulator`. Commande : `dotnet run -c Release --project dotnet/Vortex.Simulator -- --games " + settings.Games + " --seed " + settings.Seed + " --samples " + settings.Samples + "`.");
            L(sb, string.Concat("> Contenu analysé : `cards.json` SHA-256 `", settings.ContentSha256.AsSpan(0, 12), "…`. Mêmes paramètres et même contenu ⇒ mêmes chiffres."));
            L(sb);
            L(sb, "## Comment lire ce rapport");
            L(sb);
            L(sb, "- Les parties sont jouées par des **bots**. Le bot *heuristique* essaie chaque coup légal en le simulant (sans voir le futur : il invente les tirages cachés), termine son tour avec une politique simple, puis garde le coup qui donne la meilleure position. Le bot *aléatoire* joue n'importe quel coup légal. Aucun bot ne connaît les cartes : ils les découvrent en les jouant (ADR-0010).");
            L(sb, "- Les bots ne jouent pas comme des humains. Les tendances et les écarts sont utiles ; les valeurs absolues le sont moins.");
            L(sb, "- **Écart** d'une carte : taux de victoire des joueurs qui l'ont prise, moins le même taux calculé sur **toutes** les cartes, en points. Comparer à la moyenne des cartes (et non à 1/nombre de joueurs) neutralise le biais de survie : un joueur qui survit longtemps prend plus de cartes et gagne plus souvent. Un `*` signale un écart au-delà du bruit statistique (plus de 2 écarts-types).");
            L(sb);

            L(sb, "## 1. Durée et fin des parties");
            L(sb);
            L(sb, "Bots heuristiques à toutes les places. Durée estimée à " + settings.SecondsPerTurn + " s par tour de joueur (hypothèse, à confronter aux parties réelles).");
            L(sb);
            L(sb, "| Joueurs | Parties | Terminées | Égalités | Manches (moy. / méd. / p90) | Tours de joueur | Fin des temps atteinte | Victoires par Élection | Durée estimée |");
            L(sb, "|---|---|---|---|---|---|---|---|---|");
            foreach (ScenarioResult s in full)
            {
                List<GameRecord> ok = s.Records.Where(r => r.Error == null).ToList();
                List<GameRecord> done = ok.Where(r => r.Finished).ToList();
                List<int> rounds = ok.Select(r => r.Rounds).OrderBy(x => x).ToList();
                double turns = ok.Count == 0 ? 0 : ok.Average(r => r.Turns);
                L(sb, $"| {s.Players} | {ok.Count} | {Pct(done.Count, ok.Count)} | {Pct(done.Count(r => r.Condition == WinCondition.Draw), ok.Count)} | {Num(Avg(rounds))} / {Percentile(rounds, 0.5)} / {Percentile(rounds, 0.9)} | {Num(turns)} | {Pct(ok.Count(r => r.DoomReached), ok.Count)} | {Pct(done.Count(r => r.Condition == WinCondition.GalacticElection), done.Count)} | {Num(turns * settings.SecondsPerTurn / 60.0)} min |");
            }

            L(sb);
            L(sb, "## 2. Avantage de position");
            L(sb);
            L(sb, "Taux de victoire selon l'ordre de jeu (1 = le joueur qui a gagné l'initiative), bots heuristiques. Part équitable = 1/nombre de joueurs.");
            L(sb);
            int maxPlayers = full.Count == 0 ? 0 : full.Max(s => s.Players);
            L(sb, "| Joueurs | Part équitable | " + string.Join(" | ", Enumerable.Range(1, maxPlayers).Select(i => "Position " + i)) + " |");
            L(sb, "|---|---|" + string.Concat(Enumerable.Repeat("---|", maxPlayers)));
            foreach (ScenarioResult s in full)
            {
                List<GameRecord> won = s.Records.Where(r => r.Error == null && r.Finished && r.Winner >= 0).ToList();
                var cells = new List<string>();
                for (int pos = 0; pos < maxPlayers; pos++)
                {
                    cells.Add(pos < s.Players ? Pct(won.Count(r => ((r.Winner - r.InitiativeSeat + s.Players) % s.Players) == pos), won.Count) : "—");
                }

                L(sb, $"| {s.Players} | {Pct(1, s.Players)} | {string.Join(" | ", cells)} |");
            }

            L(sb);
            L(sb, "## 3. Poids des choix (écart de niveau)");
            L(sb);
            L(sb, "Un bot heuristique (place tournante) contre des bots aléatoires. Si le hasard dominait le jeu, il ne gagnerait guère plus que sa part équitable. **Ratio** = victoires ÷ part équitable.");
            L(sb);
            L(sb, "| Joueurs | Parties | Victoires de l'heuristique | Part équitable | Ratio |");
            L(sb, "|---|---|---|---|---|");
            foreach (ScenarioResult s in gap)
            {
                List<GameRecord> ok = s.Records.Where(r => r.Error == null).ToList();
                int wins = ok.Count(r => r.Winner >= 0 && r.Bots[r.Winner] == "heuristic");
                double rate = ok.Count == 0 ? 0 : (double)wins / ok.Count;
                L(sb, $"| {s.Players} | {ok.Count} | {Pct(wins, ok.Count)} | {Pct(1, s.Players)} | {Num(rate * s.Players)} |");
            }

            L(sb);
            L(sb, "## 4. Combats");
            L(sb);
            L(sb, "| Joueurs | Attaques par tour de joueur | PV retirés par attaque | Attaques sans dégâts |");
            L(sb, "|---|---|---|---|");
            foreach (ScenarioResult s in full)
            {
                List<GameRecord> ok = s.Records.Where(r => r.Error == null).ToList();
                long attacks = ok.Sum(r => (long)r.Attacks.Sum());
                long harmless = ok.Sum(r => (long)r.HarmlessAttacks.Sum());
                long damage = ok.Sum(r => (long)r.DamageDealt.Sum());
                long turns = ok.Sum(r => (long)r.Turns);
                L(sb, $"| {s.Players} | {Num(Ratio(attacks, turns))} | {Num(Ratio(damage, attacks))} | {Pct(harmless, attacks)} |");
            }

            WriteCards(sb, data, full);
            WriteEvents(sb, data, full);
            WriteAnomalies(sb, scenarios);
            return sb.ToString().TrimEnd('\n') + "\n";
        }

        private static void WriteCards(StringBuilder sb, GameData data, List<ScenarioResult> full)
        {
            L(sb);
            L(sb, "## 5. Cartes");
            L(sb);
            L(sb, "Toutes tailles de table confondues, bots heuristiques. **Prises** = nombre moyen de prises au marché par partie. **Preneurs** = couples (partie, joueur) ayant pris la carte au moins une fois. Trié du plus fort au plus faible écart.");
            L(sb);

            List<GameRecord> games = full.SelectMany(s => s.Records).Where(r => r.Error == null && r.Finished).ToList();

            // Takers: (game, seat) pairs that took the card at least once, with whether that seat won.
            var takers = new Dictionary<string, List<bool>>(StringComparer.Ordinal);
            foreach (GameRecord g in games)
            {
                foreach ((int seat, string cardId) in g.Picks.Distinct())
                {
                    if (!takers.TryGetValue(cardId, out List<bool>? list))
                    {
                        takers[cardId] = list = new List<bool>();
                    }

                    list.Add(g.Winner == seat);
                }
            }

            int allTakers = takers.Values.Sum(l => l.Count);
            double baseline = allTakers == 0 ? 0 : (double)takers.Values.Sum(l => l.Count(w => w)) / allTakers;
            L(sb, "Taux de victoire moyen d'un preneur, toutes cartes confondues : **" + Pct(baseline, 1) + "** (référence des écarts).");
            L(sb);
            L(sb, "| Carte | Nom | Prises / partie | Activations / prise | Preneurs | Victoires des preneurs | Écart |");
            L(sb, "|---|---|---|---|---|---|---|");

            var rows = new List<(string Line, double Surplus)>();
            foreach (CardDefinition card in data.Modifiers)
            {
                int picks = games.Sum(g => g.Picks.Count(p => p.CardId == card.Id));
                int activations = games.Sum(g => g.Activations.Count(a => a.CardId == card.Id));
                List<bool> results = takers.TryGetValue(card.Id, out List<bool>? r) ? r : new List<bool>();
                double rate = results.Count == 0 ? 0 : (double)results.Count(w => w) / results.Count;
                double delta = rate - baseline;
                double stderr = results.Count == 0 ? 0 : Math.Sqrt(baseline * (1 - baseline) / results.Count);
                string marker = results.Count > 0 && Math.Abs(delta) > 2 * stderr ? " *" : string.Empty;
                bool activatable = card.Usage == CardUsage.SingleUse || activations > 0;
                string activationText = activatable ? Num(Ratio(activations, picks)) : "—";
                rows.Add(($"| `{card.Id}` | {card.Name} | {Num(Ratio(picks, games.Count))} | {activationText} | {results.Count} | {Pct(results.Count(w => w), results.Count)} | {Signed(delta * 100)} pts{marker} |", delta));
            }

            foreach ((string line, double _) in rows.OrderByDescending(r => r.Surplus))
            {
                L(sb, line);
            }
        }

        private static void WriteEvents(StringBuilder sb, GameData data, List<ScenarioResult> full)
        {
            L(sb);
            L(sb, "## 6. Événements");
            L(sb);
            List<GameRecord> games = full.SelectMany(s => s.Records).Where(r => r.Error == null).ToList();
            L(sb, "| Événement | Révélations par partie |");
            L(sb, "|---|---|");
            foreach (EventDefinition e in data.Events)
            {
                int count = games.Sum(g => g.EventsRevealed.Count(id => id == e.Id));
                L(sb, $"| {e.Name} | {Num(Ratio(count, games.Count))} |");
            }
        }

        private static void WriteAnomalies(StringBuilder sb, IReadOnlyList<ScenarioResult> scenarios)
        {
            L(sb);
            L(sb, "## 7. Anomalies");
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

        private static double Avg(List<int> values) => values.Count == 0 ? 0 : values.Average();

        private static int Percentile(List<int> sorted, double p) => sorted.Count == 0 ? 0 : sorted[Math.Min(sorted.Count - 1, (int)Math.Floor(p * sorted.Count))];

        private static double Ratio(double a, double b) => b == 0 ? 0 : a / b;

        private static string Pct(double part, double whole) => whole == 0 ? "—" : French(100.0 * part / whole) + " %";

        private static string Num(double value) => French(value);

        private static string Signed(double value) => (value >= 0 ? "+" : string.Empty) + French(value);

        // French decimal comma without depending on culture data (the tool runs with invariant globalization).
        private static string French(double value) => value.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');

        private static void L(StringBuilder sb, string text = "")
        {
            sb.Append(text).Append('\n');
        }
    }
}
