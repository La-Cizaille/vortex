using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Vortex.Core.Effects.Bricks
{
    /// <summary>Whether a brick acts while present or runs once when its carrier is activated.</summary>
    internal enum BrickKind
    {
        /// <summary>Acts through interception points while the card is equipped / the event is active.</summary>
        Passive = 0,
        /// <summary>Runs when the card is activated, the event revealed or the technology activated.</summary>
        Activation = 1,
    }

    /// <summary>Documentation of one brick parameter (generated into docs/BRICKS.md).</summary>
    internal sealed class BrickParamInfo
    {
        public BrickParamInfo(string name, string type, string? range, string? defaultValue, string description)
        {
            Name = name;
            Type = type;
            Range = range;
            DefaultValue = defaultValue;
            Description = description;
        }

        public string Name { get; }

        public string Type { get; }

        public string? Range { get; }

        /// <summary>Null when the parameter is required.</summary>
        public string? DefaultValue { get; }

        public string Description { get; }
    }

    /// <summary>A brick parameter was missing, of the wrong type, out of range or unknown.</summary>
    internal sealed class BrickParamException : Exception
    {
        public BrickParamException(string message)
            : base(message)
        {
        }

        public BrickParamException()
        {
        }

        public BrickParamException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Typed, bounded reader of a brick's parameters. In "describe" mode (no raw values) it records the
    /// declared parameters instead, so documentation is generated from the very code that reads them.
    /// </summary>
    internal sealed class BrickParams
    {
        private readonly IDictionary<string, JToken>? _raw;
        private readonly HashSet<string> _read = new HashSet<string>(StringComparer.Ordinal);

        private BrickParams(IDictionary<string, JToken>? raw)
        {
            _raw = raw;
        }

        /// <summary>Parameters declared by the brick (describe mode).</summary>
        public List<BrickParamInfo> Declared { get; } = new List<BrickParamInfo>();

        public static BrickParams For(IDictionary<string, JToken> raw) => new BrickParams(raw ?? throw new ArgumentNullException(nameof(raw)));

        public static BrickParams Describe() => new BrickParams(null);

        /// <summary>Integer in [min, max]; required when <paramref name="fallback"/> is null.</summary>
        public int Int(string name, int min, int max, int? fallback, string description)
        {
            Declared.Add(new BrickParamInfo(name, "entier", min.ToString(CultureInfo.InvariantCulture) + ".." + max.ToString(CultureInfo.InvariantCulture), fallback?.ToString(CultureInfo.InvariantCulture), description));
            JToken? token = Take(name);
            if (token == null)
            {
                return fallback ?? (_raw == null ? min : throw Missing(name));
            }

            if (token.Type != JTokenType.Integer)
            {
                throw new BrickParamException("parameter '" + name + "' must be an integer.");
            }

            long value = token.Value<long>();
            if (value < min || value > max)
            {
                throw new BrickParamException("parameter '" + name + "' must be in " + min + ".." + max + " (got " + value + ").");
            }

            return (int)value;
        }

        /// <summary>Boolean; required when <paramref name="fallback"/> is null.</summary>
        public bool Bool(string name, bool? fallback, string description)
        {
            Declared.Add(new BrickParamInfo(name, "booléen", null, fallback?.ToString().ToLowerInvariant(), description));
            JToken? token = Take(name);
            if (token == null)
            {
                return fallback ?? (_raw == null ? false : throw Missing(name));
            }

            if (token.Type != JTokenType.Boolean)
            {
                throw new BrickParamException("parameter '" + name + "' must be true or false.");
            }

            return token.Value<bool>();
        }

        /// <summary>Enumeration value written by name; required when <paramref name="fallback"/> is null.</summary>
        public T Enum<T>(string name, T? fallback, string description)
            where T : struct, Enum
        {
            Declared.Add(new BrickParamInfo(name, string.Join(" / ", System.Enum.GetNames(typeof(T))), null, fallback?.ToString(), description));
            JToken? token = Take(name);
            if (token == null)
            {
                return fallback ?? (_raw == null ? default : throw Missing(name));
            }

            if (token.Type != JTokenType.String)
            {
                throw new BrickParamException("parameter '" + name + "' must be one of: " + string.Join(", ", System.Enum.GetNames(typeof(T))) + ".");
            }

            string text = token.Value<string>()!;
            foreach (string candidate in System.Enum.GetNames(typeof(T)))
            {
                if (string.Equals(candidate, text, StringComparison.Ordinal))
                {
                    return System.Enum.Parse<T>(candidate);
                }
            }

            throw new BrickParamException("parameter '" + name + "' must be one of: " + string.Join(", ", System.Enum.GetNames(typeof(T))) + " (got '" + text + "').");
        }

        /// <summary>Rejects parameters the brick does not know (typos must not be silently ignored).</summary>
        public void Finish()
        {
            if (_raw == null)
            {
                return;
            }

            foreach (string key in _raw.Keys)
            {
                if (!_read.Contains(key))
                {
                    throw new BrickParamException("unknown parameter '" + key + "'.");
                }
            }
        }

        private JToken? Take(string name)
        {
            _read.Add(name);
            if (_raw == null || !_raw.TryGetValue(name, out JToken? token))
            {
                return null;
            }

            return token.Type == JTokenType.Null ? throw new BrickParamException("parameter '" + name + "' cannot be null.") : token;
        }

        private static BrickParamException Missing(string name) => new BrickParamException("missing required parameter '" + name + "'.");
    }
}
