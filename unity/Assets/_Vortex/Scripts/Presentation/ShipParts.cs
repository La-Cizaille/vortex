using System.Collections.Generic;
using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Where effects start on a ship (ANIMATIONS.md, ASSETS §2): a model may carry empty markers named <c>Canon</c> (where
    /// a shot leaves) and <c>Reacteur…</c> (each engine). Without them, points in front of and behind the ship stand in.
    /// </summary>
    public static class ShipParts
    {
        /// <summary>Name of the marker a shot leaves from.</summary>
        public const string Muzzle = "Canon";

        /// <summary>Name prefix of the engine markers.</summary>
        public const string Engine = "Reacteur";

        /// <summary>Where a shot leaves the ship: its muzzle marker, or a point ahead of its nose.</summary>
        public static Vector3 MuzzleOf(Transform ship)
        {
            Transform? marker = Find(ship, name => name == Muzzle);
            return marker != null ? marker.position : ship.TransformPoint(new Vector3(0f, 0.1f, 0.9f));
        }

        /// <summary>Where a shot hits the ship: a little above its centre.</summary>
        public static Vector3 HullOf(Transform ship) => ship.TransformPoint(new Vector3(0f, 0.15f, 0f));

        /// <summary>The ship's engines: its engine markers, or a single point behind its tail.</summary>
        public static IReadOnlyList<Transform> EnginesOf(Transform ship, out Vector3 fallback)
        {
            var engines = new List<Transform>();
            foreach (Transform part in ship.GetComponentsInChildren<Transform>())
            {
                if (part.name.StartsWith(Engine, System.StringComparison.Ordinal))
                {
                    engines.Add(part);
                }
            }

            fallback = ship.TransformPoint(new Vector3(0f, 0f, -1f));
            return engines;
        }

        private static Transform? Find(Transform root, System.Func<string, bool> match)
        {
            foreach (Transform part in root.GetComponentsInChildren<Transform>())
            {
                if (match(part.name))
                {
                    return part;
                }
            }

            return null;
        }
    }
}
