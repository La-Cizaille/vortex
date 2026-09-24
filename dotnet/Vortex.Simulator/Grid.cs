using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vortex.Core.Content;

namespace Vortex.Simulator
{
    /// <summary>Indicators a grid can be ranked on (closed list: a grid file names them, it cannot define new ones).</summary>
    internal enum GridIndicator
    {
        /// <summary>Estimated duration of a game, in minutes (player turns × seconds per turn).</summary>
        DurationMinutes,

        /// <summary>Share of games that reach the doom round (0..1).</summary>
        DoomReached,

        /// <summary>Share of decided games won by galactic election (0..1).</summary>
        ElectionShare,

        /// <summary>Mean round of the first elimination.</summary>
        FirstEliminationRound,

        /// <summary>Largest gap between a turn-order position's win rate and the fair share, in points.</summary>
        PositionGapPoints,
    }

    /// <summary>A target range on one indicator; either bound may be open.</summary>
    internal sealed class GridTarget
    {
        public GridTarget(GridIndicator indicator, double? min, double? max)
        {
            Indicator = indicator;
            Min = min;
            Max = max;
        }

        public GridIndicator Indicator { get; }

        public double? Min { get; }

        public double? Max { get; }

        /// <summary>
        /// 0 inside the range; otherwise the distance to the crossed bound, relative to that bound, so that
        /// indicators with different units can be added up (a 20 % miss weighs the same everywhere).
        /// </summary>
        public double Distance(double value)
        {
            if (Min.HasValue && value < Min.Value)
            {
                return (Min.Value - value) / Math.Max(Math.Abs(Min.Value), 1e-9);
            }

            if (Max.HasValue && value > Max.Value)
            {
                return (value - Max.Value) / Math.Max(Math.Abs(Max.Value), 1e-9);
            }

            return 0;
        }
    }

    /// <summary>One axis of a grid: a named setting and its labelled values, each a content patch.</summary>
    internal sealed class GridAxis
    {
        public GridAxis(string name, IReadOnlyList<(string Label, JObject Patch)> values)
        {
            Name = name;
            Values = values;
        }

        public string Name { get; }

        public IReadOnlyList<(string Label, JObject Patch)> Values { get; }
    }

    /// <summary>One combination of axis values, validated and ready to simulate.</summary>
    internal sealed class GridCell
    {
        public GridCell(IReadOnlyList<string> labels, ContentSet content)
        {
            Labels = labels;
            Content = content;
        }

        public IReadOnlyList<string> Labels { get; }

        public ContentSet Content { get; }
    }

    /// <summary>A grid file: the cartesian product of its axes, ranked on its targets.</summary>
    internal sealed class GridSpec
    {
        public GridSpec(string name, string description, IReadOnlyList<GridAxis> axes, IReadOnlyList<GridTarget> targets)
        {
            Name = name;
            Description = description;
            Axes = axes;
            Targets = targets;
        }

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyList<GridAxis> Axes { get; }

        public IReadOnlyList<GridTarget> Targets { get; }
    }

    /// <summary>
    /// Loads grid files (docs/balance/grids/*.json). A grid crosses a few settings to find the combination that
    /// best meets the balance targets; every combination is a variant built with <see cref="Variants"/> and validated
    /// by the game's loader.
    /// </summary>
    /// <remarks>
    /// Format:
    /// <code>
    /// { "name": "…", "description": "…",
    ///   "axes": [ { "name": "PV", "values": { "25": { "config": { "startingHp": 25, "maxHp": 25 } }, "30": { … } } }, … ],
    ///   "targets": { "durationMinutes": { "min": 20, "max": 25 }, "doomReached": { "max": 0.3 }, … } }
    /// </code>
    /// Bounded: at most 4 axes, 8 values per axis and 64 combinations; two axes may not touch the same setting.
    /// </remarks>
    internal static class Grids
    {
        public const int MaxAxes = 4;
        public const int MaxValuesPerAxis = 8;
        public const int MaxCells = 64;
        public const int MaxLabelLength = 24;

        private static readonly string[] GridKeys = { "name", "description", "axes", "targets" };
        private static readonly string[] AxisKeys = { "name", "values" };
        private static readonly string[] RangeKeys = { "min", "max" };

        // Explicit mapping, no enum parsing of user input (which would also accept numbers).
        private static readonly IReadOnlyDictionary<string, GridIndicator> IndicatorKeys = new Dictionary<string, GridIndicator>(StringComparer.Ordinal)
        {
            ["durationMinutes"] = GridIndicator.DurationMinutes,
            ["doomReached"] = GridIndicator.DoomReached,
            ["electionShare"] = GridIndicator.ElectionShare,
            ["firstEliminationRound"] = GridIndicator.FirstEliminationRound,
            ["positionGapPoints"] = GridIndicator.PositionGapPoints,
        };

        public static (GridSpec Spec, List<GridCell> Cells) Load(string dataDir, string gridPath)
        {
            JObject grid = Variants.ReadFile(gridPath);
            Variants.CheckKeys(grid, gridPath, GridKeys);
            string name = Variants.Text(grid, "name", required: true, Variants.MaxNameLength, singleCell: true, gridPath);
            string description = Variants.Text(grid, "description", required: false, Variants.MaxDescriptionLength, singleCell: false, gridPath);
            List<GridAxis> axes = ReadAxes(grid["axes"], gridPath);
            List<GridTarget> targets = ReadTargets(grid["targets"], gridPath);

            int cells = axes.Aggregate(1, (n, a) => n * a.Values.Count);
            if (cells > MaxCells)
            {
                throw new GameDataException(gridPath + ": " + cells + " combinations, more than " + MaxCells + ".");
            }

            CheckAxesAreIndependent(axes, gridPath);
            Dictionary<string, JObject> reference = Variants.ReadContent(dataDir);
            var result = new List<GridCell>();
            foreach (int[] choice in Combinations(axes))
            {
                Dictionary<string, JObject> content = Variants.Copy(reference);
                var labels = new List<string>();
                for (int a = 0; a < axes.Count; a++)
                {
                    (string label, JObject patch) = axes[a].Values[choice[a]];
                    Variants.ApplyPatch(content, patch, gridPath + " " + axes[a].Name + "=" + label);
                    labels.Add(label);
                }

                string cellName = string.Join(" · ", axes.Select((a, i) => a.Name + " " + labels[i]));
                result.Add(new GridCell(labels, Variants.Build(cellName, string.Empty, content)));
            }

            return (new GridSpec(name, description, axes, targets), result);
        }

        private static List<GridAxis> ReadAxes(JToken? token, string origin)
        {
            if (!(token is JArray array) || array.Count == 0 || array.Count > MaxAxes)
            {
                throw new GameDataException(origin + ": 'axes' must be a list of 1 to " + MaxAxes + " axes.");
            }

            var axes = new List<GridAxis>();
            foreach (JToken item in array)
            {
                if (!(item is JObject axis))
                {
                    throw new GameDataException(origin + ": each axis must be an object.");
                }

                Variants.CheckKeys(axis, origin, AxisKeys);
                string axisName = Variants.Text(axis, "name", required: true, Variants.MaxNameLength, singleCell: true, origin + " axis");
                string where = origin + " axis '" + axisName + "'";
                if (!(axis["values"] is JObject values) || !values.HasValues || values.Count > MaxValuesPerAxis)
                {
                    throw new GameDataException(where + ": 'values' must be an object of 1 to " + MaxValuesPerAxis + " labelled patches.");
                }

                var entries = new List<(string, JObject)>();
                foreach (JProperty value in values.Properties())
                {
                    Variants.CheckText(value.Name, MaxLabelLength, singleCell: true, where + " label");
                    if (!(value.Value is JObject patch))
                    {
                        throw new GameDataException(where + ": value '" + value.Name + "' must be a patch object.");
                    }

                    Variants.CheckKeys(patch, where + " value '" + value.Name + "'", Variants.PatchKeys);
                    entries.Add((value.Name, patch));
                }

                axes.Add(new GridAxis(axisName, entries));
            }

            if (axes.Select(a => a.Name).Distinct(StringComparer.Ordinal).Count() != axes.Count)
            {
                throw new GameDataException(origin + ": two axes have the same name.");
            }

            return axes;
        }

        private static List<GridTarget> ReadTargets(JToken? token, string origin)
        {
            if (!(token is JObject targets) || !targets.HasValues)
            {
                throw new GameDataException(origin + ": 'targets' must name at least one indicator.");
            }

            var result = new List<GridTarget>();
            foreach (JProperty p in targets.Properties())
            {
                if (!IndicatorKeys.TryGetValue(p.Name, out GridIndicator indicator))
                {
                    throw new GameDataException(origin + ": unknown indicator '" + p.Name + "' (" + string.Join(", ", IndicatorKeys.Keys) + ").");
                }

                if (!(p.Value is JObject range))
                {
                    throw new GameDataException(origin + ": target '" + p.Name + "' must be an object with 'min' and/or 'max'.");
                }

                Variants.CheckKeys(range, origin + " target '" + p.Name + "'", RangeKeys);
                double? min = Bound(range["min"], origin, p.Name);
                double? max = Bound(range["max"], origin, p.Name);
                if ((!min.HasValue && !max.HasValue) || (min.HasValue && max.HasValue && min.Value > max.Value))
                {
                    throw new GameDataException(origin + ": target '" + p.Name + "' needs 'min' <= 'max', at least one of them.");
                }

                result.Add(new GridTarget(indicator, min, max));
            }

            return result;
        }

        private static double? Bound(JToken? token, string origin, string indicator)
        {
            if (token == null)
            {
                return null;
            }

            if ((token.Type != JTokenType.Integer && token.Type != JTokenType.Float) || (double)token < 0 || (double)token > 1000)
            {
                throw new GameDataException(origin + ": target '" + indicator + "' bounds must be numbers in 0..1000.");
            }

            return (double)token;
        }

        // Two axes touching the same setting would silently overwrite each other: the grid would lie.
        private static void CheckAxesAreIndependent(List<GridAxis> axes, string origin)
        {
            List<HashSet<string>> paths = axes
                .Select(a => new HashSet<string>(a.Values.SelectMany(v => Variants.TouchedPaths(v.Patch)), StringComparer.Ordinal))
                .ToList();
            for (int i = 0; i < axes.Count; i++)
            {
                for (int j = i + 1; j < axes.Count; j++)
                {
                    string? clash = paths[i].FirstOrDefault(p => paths[j].Any(q => q == p || q.StartsWith(p + ".", StringComparison.Ordinal) || p.StartsWith(q + ".", StringComparison.Ordinal)));
                    if (clash != null)
                    {
                        throw new GameDataException(origin + ": axes '" + axes[i].Name + "' and '" + axes[j].Name + "' both change '" + clash + "'.");
                    }
                }
            }
        }

        // Odometer order: the last axis varies fastest, so the report lists cells in a predictable order.
        private static IEnumerable<int[]> Combinations(List<GridAxis> axes)
        {
            var choice = new int[axes.Count];
            while (true)
            {
                yield return (int[])choice.Clone();
                int a = axes.Count - 1;
                while (a >= 0 && ++choice[a] == axes[a].Values.Count)
                {
                    choice[a] = 0;
                    a--;
                }

                if (a < 0)
                {
                    yield break;
                }
            }
        }
    }

    /// <summary>Simulated indicators of one grid cell, with its distance to the targets.</summary>
    internal sealed class GridResult
    {
        public GridResult(GridCell cell, ScenarioStats stats, IReadOnlyDictionary<GridIndicator, double> values, IReadOnlyList<GridTarget> targets)
        {
            Cell = cell;
            Stats = stats;
            Values = values;
            Met = targets.Count(t => t.Distance(values[t.Indicator]) == 0);
            Score = targets.Sum(t => t.Distance(values[t.Indicator]));
        }

        public GridCell Cell { get; }

        public ScenarioStats Stats { get; }

        public IReadOnlyDictionary<GridIndicator, double> Values { get; }

        /// <summary>Number of targets met.</summary>
        public int Met { get; }

        /// <summary>Sum of the relative distances to the missed targets (0 when all are met).</summary>
        public double Score { get; }

        /// <summary>Indicator values of a batch; the duration uses the report's seconds-per-turn assumption.</summary>
        public static IReadOnlyDictionary<GridIndicator, double> Measure(ScenarioStats s, int secondsPerTurn)
        {
            return new Dictionary<GridIndicator, double>
            {
                [GridIndicator.DurationMinutes] = s.Turns.Value * secondsPerTurn / 60.0,
                [GridIndicator.DoomReached] = s.DoomReached.Rate,
                [GridIndicator.ElectionShare] = s.Elections.Rate,
                // No elimination at all counts as "as late as possible".
                [GridIndicator.FirstEliminationRound] = s.FirstElimination.Count == 0 ? double.MaxValue : s.FirstElimination.Value,
                [GridIndicator.PositionGapPoints] = s.MaxPositionGap,
            };
        }

        /// <summary>Most targets met first, then the smallest total distance; ties keep the grid order.</summary>
        public static List<GridResult> Rank(IEnumerable<GridResult> results)
        {
            return results.Select((r, i) => (r, i)).OrderByDescending(x => x.r.Met).ThenBy(x => x.r.Score).ThenBy(x => x.i).Select(x => x.r).ToList();
        }
    }
}
