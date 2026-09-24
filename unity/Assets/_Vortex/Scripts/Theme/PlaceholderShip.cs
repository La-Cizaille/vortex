using UnityEngine;

namespace Vortex.Client.Theme
{
    /// <summary>
    /// A stand-in ship built from Unity primitives (hull, wings, cockpit), tinted with its seat colour. Used by
    /// <see cref="ShipCatalog"/> while a seat has no ship model.
    /// </summary>
    public static class PlaceholderShip
    {
        /// <summary>Name of the generated object.</summary>
        public const string Name = "Vaisseau provisoire";

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        /// <summary>Builds a placeholder ship under <paramref name="parent"/>, nose along its forward axis.</summary>
        public static GameObject Build(Transform parent, Color color)
        {
            var root = new GameObject(Name);
            root.transform.SetParent(parent, false);
            Part(root.transform, PrimitiveType.Capsule, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.6f, 1f, 0.6f), color);
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, -0.05f, -0.2f), Vector3.zero, new Vector3(2.2f, 0.08f, 0.7f), color * 0.75f);
            Part(root.transform, PrimitiveType.Sphere, new Vector3(0f, 0.22f, 0.45f), Vector3.zero, new Vector3(0.35f, 0.3f, 0.45f), Color.Lerp(color, Color.white, 0.6f));
            return root;
        }

        private static void Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 rotation, Vector3 scale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localEulerAngles = rotation;
            part.transform.localScale = scale;

            // Primitives come with a collider; a placeholder is only something to look at.
            Collider collider = part.GetComponent<Collider>();
            if (Application.isPlaying)
            {
                Object.Destroy(collider);
            }
            else
            {
                Object.DestroyImmediate(collider);
            }

            // The render pipeline's default material, recoloured per object without creating a material.
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColor, color);
            part.GetComponent<Renderer>().SetPropertyBlock(block);
        }
    }
}
