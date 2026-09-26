using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A generated starry sky around the table (ANIMATIONS.md §2), while the theme has no background: small glowing
    /// stars on a large sphere around the camera, in a single mesh (one draw call), turning very slowly. The stars are
    /// placed from a fixed seed, so the sky is the same every game; it is decoration only.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class Starfield : MonoBehaviour
    {
        private float _degreesPerSecond;

        /// <summary>Stars in the sky (tests).</summary>
        public int Stars { get; private set; }

        /// <summary>Builds a sky of <paramref name="count"/> stars, <paramref name="radius"/> units around <paramref name="centre"/>.</summary>
        public static Starfield Create(Transform? parent, Vector3 centre, int count, float radius, Material? glow, float degreesPerSecond = 0.4f)
        {
            var sky = new GameObject("Ciel étoilé", typeof(MeshFilter), typeof(MeshRenderer));
            sky.transform.SetParent(parent, false);
            sky.transform.position = centre;
            Starfield field = sky.AddComponent<Starfield>();
            field._degreesPerSecond = degreesPerSecond;
            field.Stars = count;
            sky.GetComponent<MeshFilter>().sharedMesh = Build(count, radius);
            MeshRenderer renderer = sky.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (glow != null)
            {
                renderer.sharedMaterial = glow;
                var block = new MaterialPropertyBlock();
                block.SetColor(Shader.PropertyToID("_BaseColor"), new Color(0.8f, 0.82f, 0.95f));
                renderer.SetPropertyBlock(block);
            }

            return field;
        }

        // Small quads facing the centre, their sizes varied so the sky has depth. A local generator with a fixed seed:
        // this is decoration, not a game value, and the game's dice are never touched.
        private static Mesh Build(int count, float radius)
        {
            var random = new System.Random(20260926);
            var vertices = new Vector3[count * 4];
            var triangles = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = new Vector3(Next(random), Next(random), Next(random)).normalized;
                if (direction.sqrMagnitude < 0.5f)
                {
                    direction = Vector3.up;
                }

                Vector3 at = direction * radius;
                float size = Mathf.Lerp(0.02f, 0.07f, (float)(random.NextDouble() * random.NextDouble())) * radius / 40f;
                Vector3 right = Vector3.Cross(direction, Mathf.Abs(direction.y) > 0.9f ? Vector3.forward : Vector3.up).normalized * size;
                Vector3 up = Vector3.Cross(right, direction).normalized * size;
                int v = i * 4;
                vertices[v] = at - right - up;
                vertices[v + 1] = at - right + up;
                vertices[v + 2] = at + right + up;
                vertices[v + 3] = at + right - up;

                // Wound to face the centre: the camera sees the sky from inside.
                int t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 2;
                triangles[t + 2] = v + 1;
                triangles[t + 3] = v;
                triangles[t + 4] = v + 3;
                triangles[t + 5] = v + 2;
            }

            var mesh = new Mesh { name = "Etoiles", vertices = vertices, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float Next(System.Random random) => (float)((random.NextDouble() * 2.0) - 1.0);

        private void Update() => transform.Rotate(Vector3.up, _degreesPerSecond * Time.deltaTime, Space.World);
    }
}
