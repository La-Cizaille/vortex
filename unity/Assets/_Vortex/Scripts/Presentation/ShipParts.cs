using System.Collections.Generic;
using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Where effects start on a ship (ANIMATIONS.md, ASSETS §2): a model may carry empty markers named <c>Canon</c> (where
    /// a shot leaves) and <c>Reacteur…</c> (each engine). Without them, points taken from the model stand in.
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

        /// <summary>
        /// Where the ship's engines are, in the world: its engine markers; without them, two outlets at the back of the
        /// model, left and right, taken from its meshes (a ship has two engines, playtest 4).
        /// </summary>
        public static IReadOnlyList<Vector3> EnginesOf(Transform ship)
        {
            var engines = new List<Vector3>();
            foreach (Transform part in ship.GetComponentsInChildren<Transform>())
            {
                if (part.name.StartsWith(Engine, System.StringComparison.Ordinal))
                {
                    engines.Add(part.position);
                }
            }

            if (engines.Count > 0)
            {
                return engines;
            }

            Bounds shape = LocalBounds(ship);
            float x = shape.extents.x * 0.35f;
            float y = Mathf.Lerp(shape.min.y, shape.max.y, 0.45f);
            engines.Add(ship.TransformPoint(new Vector3(shape.center.x - x, y, shape.min.z)));
            engines.Add(ship.TransformPoint(new Vector3(shape.center.x + x, y, shape.min.z)));
            return engines;
        }

        // The box around the ship's meshes, in the ship's own axes (1 x 0.3 x 2 when it has none).
        private static Bounds LocalBounds(Transform ship)
        {
            Bounds? box = null;
            foreach (MeshFilter mesh in ship.GetComponentsInChildren<MeshFilter>())
            {
                if (mesh.sharedMesh == null)
                {
                    continue;
                }

                Bounds local = mesh.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(i % 2 == 0 ? local.min.x : local.max.x, (i / 2) % 2 == 0 ? local.min.y : local.max.y, i / 4 == 0 ? local.min.z : local.max.z);
                    Vector3 inShip = ship.InverseTransformPoint(mesh.transform.TransformPoint(corner));
                    if (box is Bounds grown)
                    {
                        grown.Encapsulate(inShip);
                        box = grown;
                    }
                    else
                    {
                        box = new Bounds(inShip, Vector3.zero);
                    }
                }
            }

            return box ?? new Bounds(Vector3.zero, new Vector3(1f, 0.3f, 2f));
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
