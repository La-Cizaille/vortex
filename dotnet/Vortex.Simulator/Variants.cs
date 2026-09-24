using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vortex.Core.Config;
using Vortex.Core.Content;

namespace Vortex.Simulator
{
    /// <summary>A validated content set (the reference or a variant) ready to be simulated.</summary>
    internal sealed class ContentSet
    {
        public ContentSet(string name, string description, GameData data, GameConfig config, string contentSha256)
        {
            Name = name;
            Description = description;
            Data = data;
            Config = config;
            ContentSha256 = contentSha256;
        }

        public string Name { get; }

        public string Description { get; }

        public GameData Data { get; }

        public GameConfig Config { get; }

        /// <summary>Fingerprint of the four content files as simulated (after the variant patch).</summary>
        public string ContentSha256 { get; }
    }

    /// <summary>
    /// Loads the reference content and applies variant files: small JSON patches that change the configuration or
    /// some cards without touching the content files (docs/balance/README.md). The patched content goes through the
    /// same validating loader as the game, so an invalid variant is rejected with the loader's message.
    /// </summary>
    /// <remarks>
    /// Variant format:
    /// <code>
    /// { "name": "…", "description": "…",
    ///   "config": { "roundStartRotation": "Clockwise", "playerCounts": { "5": { "startShield": 5 } } },
    ///   "cards": { "A_005": { "effects": [ { "brick": "AttackValueBonus", "amount": 3 } ] } },
    ///   "events": { "EVT_…": { … } }, "technologies": { "TECH_…": { … } } }
    /// </code>
    /// Objects are merged recursively, other values (including arrays) are replaced. Cards, events, technologies
    /// and <c>config.playerCounts</c> entries are addressed by id or player count. Null values, id changes, unknown
    /// ids or player counts, unknown top-level keys and repeated keys are refused.
    /// </remarks>
    internal static class Variants
    {
        /// <summary>Largest variant or grid file accepted.</summary>
        public const long MaxFileBytes = 256 * 1024;

        public const int MaxNameLength = 64;

        public const int MaxDescriptionLength = 500;

        /// <summary>Keys of a content patch (a variant, or one value of a grid axis).</summary>
        public static readonly IReadOnlyList<string> PatchKeys = new[] { "config", "cards", "events", "technologies" };

        private static readonly string[] VariantKeys = PatchKeys.Concat(new[] { "name", "description" }).ToArray();

        public static ContentSet LoadReference(string dataDir)
        {
            return Build("Référence", "Contenu actuel du dépôt.", ReadContent(dataDir));
        }

        public static ContentSet LoadVariant(string dataDir, string variantPath)
        {
            JObject variant = ReadFile(variantPath);
            CheckKeys(variant, variantPath, VariantKeys);
            string name = Text(variant, "name", required: true, MaxNameLength, singleCell: true, variantPath);
            string description = Text(variant, "description", required: false, MaxDescriptionLength, singleCell: false, variantPath);
            Dictionary<string, JObject> content = ReadContent(dataDir);
            ApplyPatch(content, variant, variantPath);
            return Build(name, description, content);
        }

        /// <summary>Reads a JSON object from a bounded file (size, depth, no repeated keys).</summary>
        public static JObject ReadFile(string path)
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length > MaxFileBytes)
            {
                throw new GameDataException(path + ": missing or larger than " + MaxFileBytes + " bytes.");
            }

            return Parse(File.ReadAllText(path), path);
        }

        /// <summary>Reads the four content files as JSON trees, keyed by file name.</summary>
        public static Dictionary<string, JObject> ReadContent(string dataDir)
        {
            var files = new Dictionary<string, JObject>(StringComparer.Ordinal);
            foreach (string file in new[] { CardsFile.FileName, EventsFile.FileName, TechnologiesFile.FileName, GameConfig.FileName })
            {
                string path = Path.Combine(dataDir, file);
                files[file] = Parse(File.ReadAllText(path), path);
            }

            return files;
        }

        /// <summary>Independent copy of a content tree, so that several patches can start from the same reference.</summary>
        public static Dictionary<string, JObject> Copy(Dictionary<string, JObject> content)
        {
            return content.ToDictionary(kv => kv.Key, kv => (JObject)kv.Value.DeepClone(), StringComparer.Ordinal);
        }

        /// <summary>Applies the content keys of a patch (<see cref="PatchKeys"/>); other keys are the caller's business.</summary>
        public static void ApplyPatch(Dictionary<string, JObject> content, JObject patch, string origin)
        {
            JToken? config = patch["config"];
            if (config != null)
            {
                if (!(config is JObject configPatch))
                {
                    throw new GameDataException(origin + ": 'config' must be an object.");
                }

                MergeConfig(content[GameConfig.FileName], configPatch, origin + " config");
            }

            PatchItems(content[CardsFile.FileName], "cards", patch["cards"], origin);
            PatchItems(content[EventsFile.FileName], "events", patch["events"], origin);
            PatchItems(content[TechnologiesFile.FileName], "technologies", patch["technologies"], origin);
        }

        /// <summary>Validates the patched content with the game's loader and fingerprints it.</summary>
        public static ContentSet Build(string name, string description, Dictionary<string, JObject> content)
        {
            string cards = content[CardsFile.FileName].ToString(Formatting.None);
            string events = content[EventsFile.FileName].ToString(Formatting.None);
            string technologies = content[TechnologiesFile.FileName].ToString(Formatting.None);
            string configJson = content[GameConfig.FileName].ToString(Formatting.None);
            GameData data = GameDataLoader.Load(cards, events, technologies);
            GameConfig config = GameDataLoader.LoadConfig(configJson, data);

            // Compact re-serialisation: the fingerprint ignores formatting and changes with any simulated value.
            string all = string.Join("\n", cards, events, technologies, configJson);
            string sha = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(all)));
            return new ContentSet(name, description, data, config, sha);
        }

        /// <summary>Refuses any key outside <paramref name="allowed"/>.</summary>
        public static void CheckKeys(JObject obj, string origin, IEnumerable<string> allowed)
        {
            var set = new HashSet<string>(allowed, StringComparer.Ordinal);
            foreach (JProperty p in obj.Properties())
            {
                if (!set.Contains(p.Name))
                {
                    throw new GameDataException(origin + ": unknown key '" + p.Name + "'.");
                }
            }
        }

        /// <summary>
        /// Reads a text shown in a report. Same character policy as the content (no control or bidi characters),
        /// on a single line; a text shown in a table cell (<paramref name="singleCell"/>) cannot contain '|' either.
        /// </summary>
        public static string Text(JObject obj, string key, bool required, int maxLength, bool singleCell, string origin)
        {
            JToken? token = obj[key];
            if (token == null)
            {
                return required ? throw new GameDataException(origin + ": '" + key + "' is required.") : string.Empty;
            }

            if (token.Type != JTokenType.String)
            {
                throw new GameDataException(origin + ": '" + key + "' must be a string.");
            }

            string text = (string)token!;
            CheckText(text, maxLength, singleCell, origin + " " + key);
            return text;
        }

        /// <summary>Same checks as <see cref="Text"/> for a text that is not a JSON value (a property name).</summary>
        public static void CheckText(string text, int maxLength, bool singleCell, string origin)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length > maxLength)
            {
                throw new GameDataException(origin + ": must be 1 to " + maxLength + " characters.");
            }

            foreach (char c in text)
            {
                if (c == '\n' || GameDataValidator.IsForbiddenTextCharacter(c) || (singleCell && c == '|'))
                {
                    throw new GameDataException(origin + string.Format(CultureInfo.InvariantCulture, ": forbidden character U+{0:X4}.", (int)c));
                }
            }
        }

        /// <summary>
        /// Every value path a patch touches, such as <c>config.startingHp</c> or <c>config.playerCounts.5.startShield</c>.
        /// Used to refuse grids whose axes would overwrite each other.
        /// </summary>
        public static IEnumerable<string> TouchedPaths(JObject patch)
        {
            foreach (string key in PatchKeys)
            {
                if (patch[key] is JObject section)
                {
                    foreach (string path in Leaves(section, key))
                    {
                        yield return path;
                    }
                }
            }
        }

        private static IEnumerable<string> Leaves(JObject obj, string prefix)
        {
            foreach (JProperty p in obj.Properties())
            {
                string path = prefix + "." + p.Name;
                if (p.Value is JObject child && child.HasValues)
                {
                    foreach (string leaf in Leaves(child, path))
                    {
                        yield return leaf;
                    }
                }
                else
                {
                    yield return path;
                }
            }
        }

        // config.playerCounts may be given as an object keyed by player count, merged into the matching entries.
        private static void MergeConfig(JObject config, JObject patch, string origin)
        {
            var rest = (JObject)patch.DeepClone();
            if (rest["playerCounts"] is JObject byCount)
            {
                rest.Remove("playerCounts");
                if (!(config["playerCounts"] is JArray entries))
                {
                    throw new GameDataException(origin + ": the reference has no 'playerCounts' list.");
                }

                foreach (JProperty entry in byCount.Properties())
                {
                    JObject? target = int.TryParse(entry.Name, NumberStyles.None, CultureInfo.InvariantCulture, out int players)
                        ? entries.OfType<JObject>().FirstOrDefault(e => (int?)e["players"] == players)
                        : null;
                    if (target == null)
                    {
                        throw new GameDataException(origin + ": unknown player count '" + entry.Name + "' in 'playerCounts'.");
                    }

                    if (!(entry.Value is JObject changes) || changes["players"] != null)
                    {
                        throw new GameDataException(origin + ": 'playerCounts." + entry.Name + "' must be an object that does not change 'players'.");
                    }

                    Merge(target, changes, origin + ".playerCounts." + entry.Name);
                }
            }

            Merge(config, rest, origin);
        }

        private static void PatchItems(JObject file, string key, JToken? patch, string origin)
        {
            if (patch == null)
            {
                return;
            }

            if (!(patch is JObject byId) || !(file[key] is JArray items))
            {
                throw new GameDataException(origin + ": '" + key + "' must be an object keyed by id.");
            }

            foreach (JProperty entry in byId.Properties())
            {
                JObject? target = items.OfType<JObject>().FirstOrDefault(i => (string?)i["id"] == entry.Name);
                if (target == null)
                {
                    throw new GameDataException(origin + ": unknown id '" + entry.Name + "' in '" + key + "'.");
                }

                if (!(entry.Value is JObject changes))
                {
                    throw new GameDataException(origin + ": '" + key + "." + entry.Name + "' must be an object.");
                }

                if (changes["id"] != null)
                {
                    throw new GameDataException(origin + ": an id cannot be changed.");
                }

                Merge(target, changes, origin + " " + key + "." + entry.Name);
            }
        }

        // JSON merge patch without deletion: objects merge recursively, anything else replaces.
        private static void Merge(JObject target, JObject patch, string origin)
        {
            foreach (JProperty p in patch.Properties())
            {
                if (p.Value.Type == JTokenType.Null)
                {
                    throw new GameDataException(origin + ": null values are not allowed ('" + p.Name + "').");
                }

                if (p.Name == "schemaVersion" || p.Name == "$schema")
                {
                    throw new GameDataException(origin + ": '" + p.Name + "' cannot be changed by a variant.");
                }

                if (p.Value is JObject child && target[p.Name] is JObject existing)
                {
                    Merge(existing, child, origin + "." + p.Name);
                }
                else
                {
                    target[p.Name] = p.Value.DeepClone();
                }
            }
        }

        private static JObject Parse(string json, string origin)
        {
            try
            {
                using var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = ContentJson.MaxDepth, DateParseHandling = DateParseHandling.None };
                // A repeated key would silently keep its last value: what a reviewer reads would differ from what runs.
                return JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            }
            catch (JsonException ex)
            {
                throw new GameDataException(origin + ": " + ex.Message, ex);
            }
        }
    }
}
