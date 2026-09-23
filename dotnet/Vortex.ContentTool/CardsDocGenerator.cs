using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Vortex.Core.Content;

namespace Vortex.ContentTool
{
    /// <summary>Renders the readable card catalogue (docs/CARDS.md) from the content files.</summary>
    internal static class CardsDocGenerator
    {
        private static readonly Regex IconTag = new Regex("<([A-Za-z]{3})>", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

        public static string Generate(GameData data)
        {
            var sb = new StringBuilder();
            Line(sb, "# Catalogue des cartes");
            Line(sb);
            Line(sb, "> **Fichier généré** par `Vortex.ContentTool` depuis `core/Runtime/Data/*.json`. Ne pas le modifier à la main :");
            Line(sb, "> modifier le JSON puis lancer `dotnet run --project dotnet/Vortex.ContentTool -- docs core/Runtime/Data docs/CARDS.md`.");
            Line(sb, ">");
            Line(sb, "> Le **texte** est celui imprimé sur la carte. L'**arbitrage** en donne l'interprétation exacte selon le modèle d'effets ([RULES.md, partie B](RULES.md#partie-b--modèle-deffets)).");
            Line(sb, "> Les icônes du jeu apparaissent entre crochets : [ATQ] attaque, [BOU] bouclier, [MOD] modificateur, [TOR] Tourment, [SUR] surcharge, [MKT] marché noir, [DIC] dé.");
            Line(sb);

            WriteModifiers(sb, data, CardSlot.Attack, "Modificateurs d'attaque");
            WriteModifiers(sb, data, CardSlot.Defense, "Modificateurs de défense");

            Line(sb, "## Événements");
            Line(sb);
            Line(sb, "| ID | Nom | Ex. |");
            Line(sb, "|---|---|---|");
            foreach (EventDefinition e in data.Events)
            {
                Line(sb, $"| `{e.Id}` | {Cell(e.Name)} | {e.Copies.ToString(CultureInfo.InvariantCulture)} |");
            }

            Line(sb);
            foreach (EventDefinition e in data.Events)
            {
                Line(sb, $"### {e.Id} — {e.Name}");
                Line(sb);
                Line(sb, $"{e.Copies.ToString(CultureInfo.InvariantCulture)} exemplaire(s)");
                Line(sb);
                Quote(sb, e.Text);
                Line(sb, "**Arbitrage** : " + Render(e.Ruling));
                Line(sb);
            }

            Line(sb, "## Technologies (combos)");
            Line(sb);
            foreach (TechnologyDefinition t in data.Technologies)
            {
                Line(sb, $"### {t.Id} — {t.Name}");
                Line(sb);
                Line(sb, $"`{ColorFr(t.Color)}`");
                Line(sb);
                Quote(sb, t.Text);
                Line(sb, "**Arbitrage** : " + Render(t.Ruling));
                Line(sb);
            }

            // Single trailing newline.
            return sb.ToString().TrimEnd('\n') + "\n";
        }

        private static void WriteModifiers(StringBuilder sb, GameData data, CardSlot slot, string title)
        {
            List<CardDefinition> cards = data.Modifiers.Where(m => m.Slot == slot).ToList();
            int copies = cards.Sum(c => c.Copies);
            Line(sb, "## " + title);
            Line(sb);
            Line(sb, $"{cards.Count.ToString(CultureInfo.InvariantCulture)} cartes, {copies.ToString(CultureInfo.InvariantCulture)} exemplaires.");
            Line(sb);
            Line(sb, "| ID | Nom | Couleur | Usage | Ex. | À revoir |");
            Line(sb, "|---|---|---|---|---|---|");
            foreach (CardDefinition c in cards)
            {
                Line(sb, $"| [`{c.Id}`](#{c.Id.ToLowerInvariant()}) | {Cell(c.Name)} | {ColorFr(c.Color)} | {UsageFr(c.Usage)} | {c.Copies.ToString(CultureInfo.InvariantCulture)} | {(c.NeedsReview ? "oui" : string.Empty)} |");
            }

            Line(sb);
            foreach (CardDefinition c in cards)
            {
                Line(sb, $"### {c.Id}");
                Line(sb);
                Line(sb, $"**{c.Name}** · `{ColorFr(c.Color)}` · `{UsageFr(c.Usage)}` · {c.Copies.ToString(CultureInfo.InvariantCulture)} exemplaire(s){(c.NeedsReview ? " · **à revoir**" : string.Empty)}");
                Line(sb);
                Quote(sb, c.Text);
                Line(sb, "**Arbitrage** : " + Render(c.Ruling));
                Line(sb);
            }
        }

        // Icon tags like <ATQ> would be swallowed as HTML by Markdown renderers: show them as [ATQ].
        private static string Render(string text)
        {
            return IconTag.Replace(text, m => "[" + m.Groups[1].Value.ToUpperInvariant() + "]");
        }

        private static string Cell(string text)
        {
            return Render(text).Replace("|", "\\|", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
        }

        private static void Quote(StringBuilder sb, string text)
        {
            foreach (string line in Render(text).Split('\n'))
            {
                Line(sb, line.Length == 0 ? ">" : "> " + line);
            }

            Line(sb);
        }

        private static void Line(StringBuilder sb, string text = "")
        {
            sb.Append(text).Append('\n');
        }

        private static string ColorFr(TechColor color)
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

        private static string UsageFr(CardUsage usage)
        {
            switch (usage)
            {
                case CardUsage.SingleUse: return "Usage unique";
                case CardUsage.Triggered: return "Déclenchement";
                default: return "Durable";
            }
        }
    }
}
