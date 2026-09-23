using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Vortex.Core.Content
{
    /// <summary>
    /// Reads and writes <see cref="GameData"/> as JSON with hardened settings (docs/SECURITY.md, surface S2/S3).
    /// </summary>
    /// <remarks>
    /// Security properties:
    /// <list type="bullet">
    /// <item><c>TypeNameHandling.None</c> and <c>MetadataPropertyHandling.Ignore</c>: no type is ever chosen by the input (no deserialization gadgets).</item>
    /// <item>Input length and nesting depth are bounded.</item>
    /// <item>Unknown members and integer-encoded enums are rejected instead of silently ignored.</item>
    /// <item>The result is validated by <see cref="GameDataValidator"/> before being returned.</item>
    /// </list>
    /// Output is deterministic (stable ordering, LF, no timestamps) so regenerated files diff cleanly.
    /// </remarks>
    public static class GameDataSerializer
    {
        /// <summary>Maximum accepted input size, in characters. The real file is ~40 KB.</summary>
        public const int MaxInputChars = 1_000_000;

        /// <summary>Maximum JSON nesting depth. The schema needs 4.</summary>
        public const int MaxDepth = 16;

        /// <summary>Serializes content to indented JSON with LF line endings and a trailing newline.</summary>
        public static string Serialize(GameData data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var serializer = JsonSerializer.Create(CreateSettings());
            using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture) { NewLine = "\n" };
            using (var json = new JsonTextWriter(writer) { Formatting = Formatting.Indented, IndentChar = ' ', Indentation = 2, CloseOutput = false })
            {
                serializer.Serialize(json, data);
            }

            writer.Write('\n');
            return writer.ToString();
        }

        /// <summary>
        /// Parses and validates content.
        /// </summary>
        /// <exception cref="GameDataException">Input is too large, malformed, or fails validation.</exception>
        public static GameData Deserialize(string json)
        {
            if (json is null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            if (json.Length > MaxInputChars)
            {
                throw new GameDataException($"Game data exceeds {MaxInputChars} characters.");
            }

            GameData? data;
            try
            {
                var serializer = JsonSerializer.Create(CreateSettings());
                using var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = MaxDepth };
                data = serializer.Deserialize<GameData>(reader);
            }
            catch (JsonException ex)
            {
                throw new GameDataException("Game data is not valid JSON for the expected schema: " + ex.Message, ex);
            }

            if (data is null)
            {
                throw new GameDataException("Game data is empty.");
            }

            IReadOnlyList<string> errors = GameDataValidator.Validate(data);
            if (errors.Count > 0)
            {
                throw new GameDataException("Game data failed validation:\n - " + string.Join("\n - ", errors));
            }

            return data;
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
                MaxDepth = MaxDepth,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = { new StringEnumConverter { AllowIntegerValues = false } },
            };
        }
    }

    /// <summary>Raised when game data cannot be loaded. Never raised by player commands.</summary>
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
