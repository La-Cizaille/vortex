using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vortex.Core.Content;
using Vortex.Core.Effects.Bricks;

namespace Vortex.ContentTool
{
    /// <summary>Renders docs/BRICKS.md: the catalog of effect bricks a designer can combine without code.</summary>
    internal static class BricksDocGenerator
    {
        public static string Generate(GameData data)
        {
            Dictionary<string, List<string>> usage = UsageByBrick(data);
            IReadOnlyList<BrickInfo> bricks = BrickCatalog.Entries();
            var sb = new StringBuilder();
            Line(sb, "# Catalogue des briques d'effets");
            Line(sb);
            Line(sb, "> **Fichier généré** par `Vortex.ContentTool` depuis le code des briques (`core/Runtime/Effects/Bricks`). Ne pas le modifier à la main.");
            Line(sb, ">");
            Line(sb, "> Une carte, un événement ou une technologie déclare ses effets dans son champ `effects`, par exemple :");
            Line(sb, "> `\"effects\": [ { \"brick\": \"AttackValueBonus\", \"amount\": 4, \"when\": \"Overcharged\" } ]`.");
            Line(sb, "> Les paramètres sont typés et bornés ; un paramètre inconnu ou hors plage est refusé au chargement.");
            Line(sb, "> **Portée** : portée par une carte, une brique agit pour le porteur de la carte ; portée par un événement, pour tous les joueurs.");
            Line(sb, "> **Passive** : agit tant que la carte est équipée ou l'événement actif. **Activation** : s'exécute une fois (carte activée, événement révélé, technologie activée).");
            Line(sb, "> Un besoin qu'aucune brique ne couvre ? On ajoute une brique **générique** dans le code (RULES B1, ADR-0007), jamais une exception propre à une carte.");
            Line(sb);
            Line(sb, bricks.Count + " briques.");
            Line(sb);
            Line(sb, "| Brique | Type | Utilisée par |");
            Line(sb, "|---|---|---|");
            foreach (BrickInfo b in bricks)
            {
                Line(sb, $"| [`{b.Name}`](#{b.Name.ToLowerInvariant()}) | {KindFr(b.Kind)} | {UsageText(usage, b.Name)} |");
            }

            Line(sb);
            foreach (BrickInfo b in bricks)
            {
                Line(sb, "### " + b.Name);
                Line(sb);
                Line(sb, "*" + KindFr(b.Kind) + "*. " + b.Description);
                Line(sb);
                if (b.Parameters.Count > 0)
                {
                    Line(sb, "| Paramètre | Type | Plage | Défaut | Rôle |");
                    Line(sb, "|---|---|---|---|---|");
                    foreach (BrickParamInfo p in b.Parameters)
                    {
                        Line(sb, $"| `{p.Name}` | {p.Type} | {p.Range ?? string.Empty} | {(p.DefaultValue == null ? "**requis**" : "`" + p.DefaultValue + "`")} | {p.Description} |");
                    }

                    Line(sb);
                }

                Line(sb, "Utilisée par : " + UsageText(usage, b.Name));
                Line(sb);
            }

            return sb.ToString().TrimEnd('\n') + "\n";
        }

        private static Dictionary<string, List<string>> UsageByBrick(GameData data)
        {
            var usage = new Dictionary<string, List<string>>(System.StringComparer.Ordinal);
            void Add(string owner, IEnumerable<EffectSpec> specs)
            {
                foreach (EffectSpec spec in specs)
                {
                    if (!usage.TryGetValue(spec.Brick, out List<string>? list))
                    {
                        usage[spec.Brick] = list = new List<string>();
                    }

                    if (!list.Contains(owner))
                    {
                        list.Add(owner);
                    }
                }
            }

            foreach (CardDefinition c in data.Modifiers)
            {
                Add(c.Id, c.Effects);
            }

            foreach (EventDefinition e in data.Events)
            {
                Add(e.Id, e.Effects);
            }

            foreach (TechnologyDefinition t in data.Technologies)
            {
                Add(t.Id, t.Effects);
            }

            return usage;
        }

        private static string UsageText(Dictionary<string, List<string>> usage, string brick)
        {
            return usage.TryGetValue(brick, out List<string>? owners) ? string.Join(", ", owners.Select(o => "`" + o + "`")) : "*(aucune carte)*";
        }

        private static string KindFr(BrickKind kind) => kind == BrickKind.Passive ? "Passive" : "Activation";

        private static void Line(StringBuilder sb, string text = "")
        {
            sb.Append(text).Append('\n');
        }
    }
}
