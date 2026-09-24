using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Vortex.Core.Content
{
    /// <summary>
    /// Hardened JSON reading and canonical writing of content files (docs/SECURITY.md, surface S2/S3).
    /// </summary>
    /// <remarks>
    /// Security properties:
    /// <list type="bullet">
    /// <item><c>TypeNameHandling.None</c> and <c>MetadataPropertyHandling.Ignore</c>: no type is ever chosen by the input (no deserialization gadgets).</item>
    /// <item>Input length and nesting depth are bounded.</item>
    /// <item>Unknown members and integer-encoded enums are rejected instead of silently ignored.</item>
    /// <item>Repeated keys (even differing only by case) and content after the root value are rejected, so what a reviewer reads is what the game uses.</item>
    /// </list>
    /// Output is canonical (stable property order, 2-space indent, LF, trailing newline) so files diff cleanly
    /// and CI can verify they are formatted.
    /// </remarks>
    public static class ContentJson
    {
        /// <summary>Maximum accepted input size per file, in characters.</summary>
        public const int MaxInputChars = 1_000_000;

        /// <summary>Maximum JSON nesting depth. The schema needs 3.</summary>
        public const int MaxDepth = 16;

        /// <summary>Parses one content file. Shape only: cross-file validation is done by <see cref="GameDataLoader"/>.</summary>
        /// <param name="json">File content.</param>
        /// <param name="fileName">Used in error messages.</param>
        /// <exception cref="GameDataException">Input is too large, malformed, or does not match the schema.</exception>
        public static T Parse<T>(string json, string fileName)
            where T : ContentFile
        {
            if (json is null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            if (json.Length > MaxInputChars)
            {
                throw new GameDataException(fileName + ": exceeds " + MaxInputChars + " characters.");
            }

            T? result;
            try
            {
                RejectRepeatedKeys(json, fileName);
                var serializer = JsonSerializer.Create(CreateSettings());
                using var reader = CreateReader(json);
                result = serializer.Deserialize<T>(reader);
            }
            catch (JsonException ex)
            {
                throw new GameDataException(fileName + ": " + ex.Message, ex);
            }

            if (result is null)
            {
                throw new GameDataException(fileName + ": empty file.");
            }

            if (result.SchemaVersion != ContentFile.CurrentSchemaVersion)
            {
                throw new GameDataException(fileName + ": unsupported schemaVersion " + result.SchemaVersion + " (expected " + ContentFile.CurrentSchemaVersion + ").");
            }

            return result;
        }

        /// <summary>Writes a content file in canonical form.</summary>
        public static string Serialize(ContentFile file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var serializer = JsonSerializer.Create(CreateSettings());
            using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture) { NewLine = "\n" };
            using (var json = new JsonTextWriter(writer) { Formatting = Formatting.Indented, IndentChar = ' ', Indentation = 2, CloseOutput = false })
            {
                serializer.Serialize(json, file);
            }

            writer.Write('\n');
            return writer.ToString();
        }

        /// <summary>
        /// Token-only pass that rejects a key repeated within one object, and anything after the root value.
        /// Newtonsoft would silently keep the last value (and ignore trailing content): a reviewer would read one
        /// value while the game uses another, the same "misleading text" risk as Trojan Source.
        /// </summary>
        /// <remarks>
        /// Keys are compared once unescaped and ignoring case, because Newtonsoft falls back to a case-insensitive
        /// member match (<c>"Copies"</c> would set <c>copies</c>). Being a separate pass over an identically
        /// configured reader, it leaves the deserialization itself unchanged. One key set per nesting level is
        /// reused, so memory is bounded by <see cref="MaxDepth"/> whatever the file size.
        /// </remarks>
        /// <exception cref="GameDataException">A key is repeated.</exception>
        /// <exception cref="JsonReaderException">Malformed JSON, excessive nesting, or content after the root value.</exception>
        private static void RejectRepeatedKeys(string json, string fileName)
        {
            // Per open object: key -> its first spelling and line, for an error message a designer can act on.
            var keysByLevel = new List<Dictionary<string, (string Spelling, int Line)>>();
            int level = 0;
            using var reader = CreateReader(json);
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.StartObject:
                        if (level == keysByLevel.Count)
                        {
                            keysByLevel.Add(new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase));
                        }
                        else
                        {
                            keysByLevel[level].Clear();
                        }

                        level++;
                        break;
                    case JsonToken.EndObject:
                        level--;
                        break;
                    case JsonToken.PropertyName:
                        string key = (string)reader.Value!;
                        if (keysByLevel[level - 1].TryGetValue(key, out (string Spelling, int Line) first))
                        {
                            string spelling = first.Spelling == key ? string.Empty : " as '" + first.Spelling + "', keys match ignoring case";
                            throw new GameDataException(FormattableString.Invariant(
                                $"{fileName}: duplicate key '{key}' in the same object (first at line {first.Line}{spelling}). Path '{reader.Path}', line {reader.LineNumber}, position {reader.LinePosition}."));
                        }

                        keysByLevel[level - 1].Add(key, (key, reader.LineNumber));
                        break;
                }
            }
        }

        // Both passes read through identically configured readers, so they see exactly the same tokens.
        private static JsonTextReader CreateReader(string json)
        {
            return new JsonTextReader(new StringReader(json))
            {
                MaxDepth = MaxDepth,
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Decimal,
            };
        }

        // Settings are built per call: no shared mutable static state (CLAUDE.md hard rules).
        private static JsonSerializerSettings CreateSettings()
        {
            return new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Error,
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Decimal,
                NullValueHandling = NullValueHandling.Ignore,
                MaxDepth = MaxDepth,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = { new StringEnumConverter { AllowIntegerValues = false } },
            };
        }
    }

    /// <summary>Assembles and validates <see cref="GameData"/> from the three content files.</summary>
    public static class GameDataLoader
    {
        /// <summary>
        /// Parses the content files and validates them together (unique ids across files, etc.).
        /// The caller does the I/O (core has none): Unity passes TextAssets, .NET tools read files.
        /// </summary>
        /// <exception cref="GameDataException">Any file is invalid, or the set fails validation.</exception>
        public static GameData Load(string cardsJson, string eventsJson, string technologiesJson)
        {
            CardsFile cards = ContentJson.Parse<CardsFile>(cardsJson, CardsFile.FileName);
            EventsFile events = ContentJson.Parse<EventsFile>(eventsJson, EventsFile.FileName);
            TechnologiesFile techs = ContentJson.Parse<TechnologiesFile>(technologiesJson, TechnologiesFile.FileName);

            var data = new GameData(cards.Cards, events.Events, techs.Technologies);
            var errors = GameDataValidator.Validate(data);
            if (errors.Count > 0)
            {
                throw new GameDataException("Game content failed validation:\n - " + string.Join("\n - ", errors));
            }

            // Compiling the effect bricks validates brick names, parameter types and ranges, and usage rules.
            Effects.EffectCatalog.Build(data);
            return data;
        }

        /// <summary>Parses and validates <c>config.json</c> against already loaded content.</summary>
        /// <exception cref="GameDataException">The configuration is invalid or inconsistent with the content.</exception>
        public static Config.GameConfig LoadConfig(string configJson, GameData data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Config.GameConfig config = ContentJson.Parse<Config.GameConfig>(configJson, Config.GameConfig.FileName);
            var errors = new System.Collections.Generic.List<string>(config.Validate());
            bool doomExists = false;
            foreach (EventDefinition e in data.Events)
            {
                doomExists |= e.Id == config.DoomEventId;
            }

            if (config.TechnologiesToWin > data.Technologies.Count)
            {
                errors.Add("config: technologiesToWin exceeds the number of technologies in technologies.json");
            }

            if (!doomExists)
            {
                errors.Add("config: doomEventId '" + config.DoomEventId + "' is not an event of events.json");
            }

            if (errors.Count > 0)
            {
                throw new GameDataException("Configuration failed validation:\n - " + string.Join("\n - ", errors));
            }

            return config;
        }
    }

    /// <summary>Raised when game content cannot be loaded. Never raised by player commands.</summary>
    public sealed class GameDataException : Exception
    {
        /// <summary>Creates the exception.</summary>
        public GameDataException(string message)
            : base(message)
        {
        }

        /// <summary>Creates the exception with an inner cause.</summary>
        public GameDataException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>Creates the exception with no message.</summary>
        public GameDataException()
        {
        }
    }
}
