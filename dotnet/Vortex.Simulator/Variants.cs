using System;
using System.Collections.Generic;
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
    ///   "config": { "roundStartRotation": "Clockwise" },
    ///   "cards": { "A_005": { "effects": [ { "brick": "AttackValueBonus", "amount": 3 } ] } },
    ///   "events": { "EVT_…": { … } }, "technologies": { "TECH_…": { … } } }
    /// </code>
    /// Objects are merged recursively, other values (including arrays) are replaced; null values, id changes,
    /// unknown ids, unknown top-level keys and repeated keys are refused.
    /// </remarks>
    internal static class Variants
    {
        private const long MaxVariantBytes = 256 * 1024;
        private static readonly string[] AllowedKeys = { "name", "description", "config", "cards", "events", "technologies" };

        public static ContentSet LoadReference(string dataDir)
        {
            return Build("Référence", "Contenu actuel du dépôt.", ReadContent(dataDir));
        }

        public static ContentSet LoadVariant(string dataDir, string variantPath)
        {
            var info = new FileInfo(variantPath);
            if (!info.Exists || info.Length > MaxVariantBytes)
            {
                throw new GameDataException(variantPath + ": missing or larger than " + MaxVariantBytes + " bytes.");
            }

            JObject variant = Parse(File.ReadAllText(variantPath), variantPath);
            foreach (JProperty p in variant.Properties())
            {
                if (!AllowedKeys.Contains(p.Name))
                {
                    throw new GameDataException(variantPath + ": unknown key '" + p.Name + "'.");
                }
            }

            string name = (string?)variant["name"] ?? throw new GameDataException(variantPath + ": 'name' is required.");
            string description = (string?)variant["description"] ?? string.Empty;
            Dictionary<string, JObject> content = ReadContent(dataDir);
            if (variant["config"] is JObject configPatch)
            {
                Merge(content[GameConfig.FileName], configPatch, variantPath + " config");
            }

            PatchItems(content[CardsFile.FileName], "cards", variant["cards"], variantPath);
            PatchItems(content[EventsFile.FileName], "events", variant["events"], variantPath);
            PatchItems(content[TechnologiesFile.FileName], "technologies", variant["technologies"], variantPath);
            return Build(name, description, content);
        }

        private static Dictionary<string, JObject> ReadContent(string dataDir)
        {
            var files = new Dictionary<string, JObject>(StringComparer.Ordinal);
            foreach (string file in new[] { CardsFile.FileName, EventsFile.FileName, TechnologiesFile.FileName, GameConfig.FileName })
            {
                string path = Path.Combine(dataDir, file);
                files[file] = Parse(File.ReadAllText(path), path);
            }

            return files;
        }

        private static ContentSet Build(string name, string description, Dictionary<string, JObject> content)
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
