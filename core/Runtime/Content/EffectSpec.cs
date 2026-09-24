using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Vortex.Core.Content
{
    /// <summary>
    /// One effect brick declared in the content files, e.g. <c>{ "brick": "AttackValueBonus", "amount": 4 }</c>
    /// (ADR-0007). The brick name selects an entry of a closed, whitelisted catalog; the other properties
    /// are its parameters, validated by type and range when the content is loaded. Data never carries logic.
    /// </summary>
    [JsonConverter(typeof(EffectSpecConverter))]
    public sealed class EffectSpec
    {
        /// <summary>Creates a spec with no parameter.</summary>
        public EffectSpec(string brick)
        {
            Brick = brick;
        }

        /// <summary>Creates a spec with parameters.</summary>
        public EffectSpec(string brick, IEnumerable<KeyValuePair<string, JToken>> parameters)
            : this(brick)
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            foreach (KeyValuePair<string, JToken> p in parameters)
            {
                Parameters[p.Key] = p.Value;
            }
        }

        /// <summary>Name of the brick in the catalog (see docs/BRICKS.md).</summary>
        public string Brick { get; }

        /// <summary>Brick parameters (scalars only), sorted by name so the canonical form is stable.</summary>
        public IDictionary<string, JToken> Parameters { get; } = new SortedDictionary<string, JToken>(StringComparer.Ordinal);

        /// <inheritdoc/>
        public override string ToString()
        {
            var parts = new List<string>();
            foreach (KeyValuePair<string, JToken> p in Parameters)
            {
                parts.Add(p.Key + "=" + p.Value.ToString(Formatting.None));
            }

            return Brick + (parts.Count > 0 ? "(" + string.Join(", ", parts) + ")" : string.Empty);
        }
    }

    /// <summary>
    /// Explicit (de)serialization of <see cref="EffectSpec"/>: <c>brick</c> first, then parameters in name order.
    /// Only integer, boolean and string parameter values are accepted; objects and arrays are rejected.
    /// </summary>
    internal sealed class EffectSpecConverter : JsonConverter<EffectSpec>
    {
        private const string BrickProperty = "brick";

        public override EffectSpec ReadJson(JsonReader reader, Type objectType, EffectSpec? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonSerializationException("An effect must be an object with a 'brick' property.");
            }

            // ContentJson already rejects repeated keys; this keeps the converter safe from any other entry point.
            JObject obj = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (!(obj[BrickProperty] is JValue brick) || brick.Type != JTokenType.String || string.IsNullOrEmpty((string?)brick))
            {
                throw new JsonSerializationException("An effect needs a non-empty string 'brick' property.");
            }

            var parameters = new List<KeyValuePair<string, JToken>>();
            foreach (JProperty property in obj.Properties())
            {
                if (property.Name == BrickProperty)
                {
                    continue;
                }

                JTokenType t = property.Value.Type;
                if (t != JTokenType.Integer && t != JTokenType.Boolean && t != JTokenType.String)
                {
                    throw new JsonSerializationException("Effect parameter '" + property.Name + "' must be an integer, a boolean or a string.");
                }

                parameters.Add(new KeyValuePair<string, JToken>(property.Name, property.Value.DeepClone()));
            }

            return new EffectSpec((string)brick!, parameters);
        }

        public override void WriteJson(JsonWriter writer, EffectSpec? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName(BrickProperty);
            writer.WriteValue(value.Brick);
            foreach (KeyValuePair<string, JToken> p in value.Parameters)
            {
                writer.WritePropertyName(p.Key);
                p.Value.WriteTo(writer);
            }

            writer.WriteEndObject();
        }
    }
}
