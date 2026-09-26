using TMPro;
using UnityEngine;

namespace Vortex.Client.Theme
{
    /// <summary>
    /// A stand-in d8 (ASSETS §2): an octahedron about one unit high, with a number on each face (opposite faces add up to
    /// 9, as on a real d8). Each face carries an empty marker <c>Face_n</c> whose forward axis leaves the face and whose up
    /// axis is the top of its number: the same convention as the d8 model, so the game turns either the same way.
    /// </summary>
    public static class PlaceholderDie
    {
        /// <summary>Name of the generated object.</summary>
        public const string Name = "Dé provisoire";

        /// <summary>Name prefix of the face markers.</summary>
        public const string FacePrefix = "Face_";

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        // Face normals by sign (x, y, z) and their numbers: opposite normals carry numbers adding up to 9.
        private static readonly (Vector3 Normal, int Number)[] Faces =
        {
            (new Vector3(1f, 1f, 1f), 1), (new Vector3(-1f, -1f, -1f), 8),
            (new Vector3(-1f, 1f, 1f), 2), (new Vector3(1f, -1f, -1f), 7),
            (new Vector3(1f, 1f, -1f), 3), (new Vector3(-1f, -1f, 1f), 6),
            (new Vector3(-1f, 1f, -1f), 4), (new Vector3(1f, -1f, 1f), 5),
        };

        /// <summary>Builds the die under <paramref name="parent"/>.</summary>
        public static GameObject Build(Transform? parent, Color body, Color ink)
        {
            // A primitive gives the render pipeline's default material; its mesh is replaced by the octahedron.
            GameObject die = GameObject.CreatePrimitive(PrimitiveType.Cube);
            die.name = Name;
            Object.DestroyImmediate(die.GetComponent<Collider>());
            if (parent != null)
            {
                die.transform.SetParent(parent, false);
            }

            die.GetComponent<MeshFilter>().sharedMesh = Octahedron();
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColor, body);
            die.GetComponent<Renderer>().SetPropertyBlock(block);

            foreach ((Vector3 normal, int number) in Faces)
            {
                Vector3 outward = normal.normalized;
                var marker = new GameObject(FacePrefix + number).transform;
                marker.SetParent(die.transform, false);

                // The face's centre is a third of the way along each axis; the number's top points to the face's upper
                // corner (the one on the y axis).
                marker.localPosition = normal * (0.5f / 3f);
                Vector3 up = Vector3.ProjectOnPlane(new Vector3(0f, Mathf.Sign(normal.y), 0f), outward).normalized;
                marker.localRotation = Quaternion.LookRotation(outward, up);

                TextMeshPro label = new GameObject("Chiffre", typeof(RectTransform)).AddComponent<TextMeshPro>();
                label.transform.SetParent(marker, false);
                label.transform.localPosition = new Vector3(0f, 0f, 0.005f);
                label.transform.localRotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
                label.rectTransform.sizeDelta = new Vector2(0.3f, 0.3f);
                label.fontSize = 2.2f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.color = ink;
                label.text = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            return die;
        }

        // Eight flat triangles between the six points on the axes, half a unit from the centre.
        private static Mesh Octahedron()
        {
            var vertices = new Vector3[24];
            var triangles = new int[24];
            int v = 0;
            foreach ((Vector3 normal, int _) in Faces)
            {
                var x = new Vector3(0.5f * normal.x, 0f, 0f);
                var y = new Vector3(0f, 0.5f * normal.y, 0f);
                var z = new Vector3(0f, 0f, 0.5f * normal.z);

                // Wind each triangle so that its front faces outward.
                bool flip = normal.x * normal.y * normal.z < 0f;
                vertices[v] = x;
                vertices[v + 1] = flip ? z : y;
                vertices[v + 2] = flip ? y : z;
                triangles[v] = v;
                triangles[v + 1] = v + 1;
                triangles[v + 2] = v + 2;
                v += 3;
            }

            var mesh = new Mesh { name = "Octaedre", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
