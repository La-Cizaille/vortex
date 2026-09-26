using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using Vortex.Client.Presentation;
using Vortex.Client.Theme;

namespace Vortex.Editor
{
    /// <summary>
    /// Lays out the card prefab (ADR-0017) from the card model (ARB-85): its body takes the model's mesh and the game's
    /// metal materials, and the illustration and the texts go where the model's Zone_* empties say (their X and Y scales
    /// give the zone's size; Zone_Dos sizes the back). Without the model, the body is a box with the first layout. Runs when the model changes and
    /// with <see cref="ProjectAssets.EnsureAll"/>; the prefab is only edited when something differs.
    /// </summary>
    public static class CardPrefabSync
    {
        /// <summary>Parts of the prefab placed by a zone of the same name in the model (Zone_Nom places Nom).</summary>
        public static readonly string[] Zoned = { "Illustration", "Nom", "Emplacement", "Usage", "Texte", "Identifiant", "Tourments" };

        /// <summary>Prefix of the empties that mark the zones in the model.</summary>
        public const string ZonePrefix = "Zone_";

        /// <summary>Dark metal: body, disc, usage tab (model material Cadre).</summary>
        public const string FrameMaterialPath = "Assets/_Vortex/Theme/Materials/CardFrame.mat";

        /// <summary>Light metal: name bar, text panel (model material Panneau).</summary>
        public const string PanelMaterialPath = "Assets/_Vortex/Theme/Materials/CardPanel.mat";

        /// <summary>Trims in the technology colour (model material Lisere).</summary>
        public const string TrimMaterialPath = "Assets/_Vortex/Theme/Materials/CardTrim.mat";

        /// <summary>Gem in the technology colour (model material Gemme).</summary>
        public const string GemMaterialPath = "Assets/_Vortex/Theme/Materials/CardGem.mat";

        private const string Visual = "Visuel";
        private const string IconPart = "Pictogramme";
        private const string Back = "Dos";
        private const string TrimName = "Lisere";
        private const string GemName = "Gemme";
        private const float Epsilon = 0.0005f;

        // Game material of each model material; any other model material becomes the dark metal.
        private static readonly Dictionary<string, string> MaterialFor = new Dictionary<string, string>
        {
            ["Cadre"] = FrameMaterialPath,
            ["Panneau"] = PanelMaterialPath,
            [TrimName] = TrimMaterialPath,
            [GemName] = GemMaterialPath,
        };

        private static string TexturePath(string suffix) => ArtImportRules.CardModelsFolder + "/Card.fbm/Card_" + suffix + ".png";

        /// <summary>Brings the card prefab in step with the card model, or back to the box without it.</summary>
        public static void Sync()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ThemeAssets.CardPrefabPath);
            if (prefab == null || prefab.transform.Find(ThemeAssets.CardBodyPath) == null)
            {
                return;
            }

            Layout layout = Layout.Build();
            if (layout.Matches(prefab.transform))
            {
                return;
            }

            using var editing = new PrefabUtility.EditPrefabContentsScope(ThemeAssets.CardPrefabPath);
            layout.Apply(editing.prefabContentsRoot.transform);
        }

        // Where everything goes, from the model or for the box.
        private sealed class Layout
        {
            private readonly Dictionary<string, (Vector3 Position, Vector2 Size)> _zones = new Dictionary<string, (Vector3, Vector2)>();
            private Mesh _mesh = null!;
            private Vector3 _scale;
            private Material[] _materials = null!;
            private int _trims = -1;
            private int _gem = -1;

            private float Front => -ThemeAssets.CardSize.z / 2f;

            public static Layout Build()
            {
                var layout = new Layout();
                layout.Box();
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ThemeAssets.CardModelPath);
                MeshFilter? filter = model != null ? model.GetComponentInChildren<MeshFilter>() : null;
                if (filter != null && filter.sharedMesh != null)
                {
                    layout.FromModel(filter);
                }

                return layout;
            }

            // The first layout (ADR-0017), on a box of the card's size, tinted whole with the technology colour.
            private void Box()
            {
                _mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                _scale = ThemeAssets.CardSize;
                _materials = new[] { AssetDatabase.LoadAssetAtPath<Material>(ThemeAssets.CardBodyMaterialPath) };
                const float ArtWidth = 0.86f;
                const float ArtHeight = ArtWidth * 9f / 16f;
                Zone("Illustration", 0f, 0.65f - (ArtHeight / 2f), ArtWidth, ArtHeight);
                Zone("Nom", 0f, 0.1f, 0.86f, 0.1f);
                Zone("Emplacement", -0.3f, 0.01f, 0.2f, 0.07f);
                Zone("Usage", 0.12f, 0.01f, 0.56f, 0.07f);
                Zone("Texte", 0f, -0.34f, 0.84f, 0.56f);
                Zone("Identifiant", 0.24f, -0.655f, 0.4f, 0.05f);
                Zone("Tourments", 0.42f, 0.62f, 0.3f, 0.3f);
                Zone(Back, 0f, 0f, 0.96f, 1.36f);
            }

            private void Zone(string name, float x, float y, float width, float height) =>
                _zones[name] = (new Vector3(x, y, Front), new Vector2(width, height));

            private void FromModel(MeshFilter filter)
            {
                Mesh mesh = filter.sharedMesh;
                if (Vector3.Distance(mesh.bounds.size, ThemeAssets.CardSize) > 0.01f)
                {
                    Debug.LogWarning(ThemeAssets.CardModelPath + " mesure " + mesh.bounds.size.ToString("F3") + " : une carte mesure "
                        + ThemeAssets.CardSize.ToString("F3") + " (docs/ASSETS.md §2). La face de la carte risque de déborder.");
                }

                _mesh = mesh;
                _scale = Vector3.one;
                Material[] modelMaterials = filter.GetComponent<Renderer>().sharedMaterials;
                EnsureMaterials();
                _materials = modelMaterials
                    .Select(material => AssetDatabase.LoadAssetAtPath<Material>(MaterialFor.TryGetValue(material.name, out string path) ? path : FrameMaterialPath))
                    .ToArray();
                _trims = System.Array.FindIndex(modelMaterials, material => material.name == TrimName);
                _gem = System.Array.FindIndex(modelMaterials, material => material.name == GemName);

                // A zone missing from the model keeps its place of the box layout.
                foreach (Transform zone in filter.transform.root.GetComponentsInChildren<Transform>(true))
                {
                    if (zone.name.StartsWith(ZonePrefix, System.StringComparison.Ordinal))
                    {
                        Vector3 scale = zone.lossyScale;
                        _zones[zone.name.Substring(ZonePrefix.Length)] = (zone.position, new Vector2(scale.x, scale.y));
                    }
                }
            }

            // Parts sit just in front of their zone's surface (the front faces -Z), in this order from the surface out.
            private static Vector3 Place(string part, Vector3 zone) => part switch
            {
                "Illustration" => zone + new Vector3(0f, 0f, -0.0005f),
                "Tourments" => zone + new Vector3(0f, 0f, -0.004f),
                _ => zone + new Vector3(0f, 0f, -0.001f),
            };

            private Vector3 BackPosition => new Vector3(0f, 0f, ThemeAssets.CardSize.z / 2f + 0.001f);

            private Vector3 BackgroundPosition => new Vector3(0f, 0f, Front - 0.001f);

            public bool Matches(Transform root)
            {
                Transform visual = root.Find(Visual);
                Transform body = visual.Find("Corps");
                var display = root.GetComponent<CardDisplay>();
                var serialized = new SerializedObject(display);
                if (body.GetComponent<MeshFilter>().sharedMesh != _mesh || body.localScale != _scale
                    || !body.GetComponent<Renderer>().sharedMaterials.SequenceEqual(_materials)
                    || serialized.FindProperty("trimSlot").intValue != _trims || serialized.FindProperty("gemSlot").intValue != _gem
                    || serialized.FindProperty("badge").objectReferenceValue == null
                    || root.GetComponent<BoxCollider>().size != ThemeAssets.CardSize
                    || !Near(visual.Find(Back).localPosition, BackPosition)
                    || !Near(SizeOf(visual.Find(Back)), _zones[Back].Size, Back))
                {
                    return false;
                }

                Transform? background = visual.Find("Fond");
                if (background != null && (background.gameObject.activeSelf != (_trims < 0) || !Near(background.localPosition, BackgroundPosition)))
                {
                    return false;
                }

                foreach (string part in Zoned)
                {
                    Transform? placed = visual.Find(part);
                    if (placed == null || !Near(placed.localPosition, Place(part, _zones[part].Position)) || !Near(SizeOf(placed), _zones[part].Size, part))
                    {
                        return false;
                    }
                }

                Transform? icon = visual.Find(IconPart);
                return icon != null && icon.GetComponent<SpriteRenderer>() != null && Near(icon.localPosition, IconPosition)
                    && Mathf.Abs(serialized.FindProperty("badgeIconSize").floatValue - IconSize) < Epsilon
                    && serialized.FindProperty("icons").objectReferenceValue == AssetDatabase.LoadAssetAtPath<IconCatalog>(ThemeAssets.IconsPath);
            }

            // The slot icon sits in the disc, just in front of the short text it replaces, and fills most of the disc.
            private Vector3 IconPosition => Place("Emplacement", _zones["Emplacement"].Position) + new Vector3(0f, 0f, -0.0002f);

            private float IconSize => Mathf.Min(_zones["Emplacement"].Size.x, _zones["Emplacement"].Size.y * 1.4f);

            public void Apply(Transform root)
            {
                Transform visual = root.Find(Visual);
                Transform body = visual.Find("Corps");
                body.GetComponent<MeshFilter>().sharedMesh = _mesh;
                body.localScale = _scale;
                body.GetComponent<Renderer>().sharedMaterials = _materials;
                root.GetComponent<BoxCollider>().size = ThemeAssets.CardSize;
                Transform back = visual.Find(Back);
                back.localPosition = BackPosition;
                back.localScale = new Vector3(_zones[Back].Size.x, _zones[Back].Size.y, 1f);

                // The modelled body has its own panels: the flat background of the box is hidden.
                Transform? background = visual.Find("Fond");
                if (background != null)
                {
                    background.localPosition = BackgroundPosition;
                    background.gameObject.SetActive(_trims < 0);
                }

                // Prefabs made before the model: the kind line becomes the usage, and the slot gets its disc label.
                Transform? kind = visual.Find("Type");
                if (kind != null)
                {
                    kind.name = "Usage";
                }

                if (visual.Find("Emplacement") == null)
                {
                    TMP_Text badge = ThemeAssets.Text3D(visual, "Emplacement", Vector2.zero, Vector2.one, 0.3f, 0.9f, FontStyles.Bold, TextAlignmentOptions.Center);
                    badge.textWrappingMode = TextWrappingModes.NoWrap;
                }

                foreach (string part in Zoned)
                {
                    Transform placed = visual.Find(part);
                    placed.localPosition = Place(part, _zones[part].Position);
                    if (part == "Illustration")
                    {
                        placed.localScale = new Vector3(_zones[part].Size.x, _zones[part].Size.y, 1f);
                    }
                    else if (placed is RectTransform shape)
                    {
                        shape.sizeDelta = _zones[part].Size;
                    }
                }

                // The slot icon (ARB-86): a sprite tinted by the card, hidden until a card shows one.
                Transform? icon = visual.Find(IconPart);
                if (icon == null)
                {
                    icon = new GameObject(IconPart, typeof(SpriteRenderer)).transform;
                    icon.SetParent(visual, false);
                    icon.gameObject.SetActive(false);
                }

                icon.localPosition = IconPosition;
                root.GetComponent<CardDisplay>().AssignBadgeIcon(icon.GetComponent<SpriteRenderer>(), IconSize, AssetDatabase.LoadAssetAtPath<IconCatalog>(ThemeAssets.IconsPath));

                Transform torments = visual.Find("Tourments");
                root.GetComponent<CardDisplay>().Assign(
                    body.GetComponent<Renderer>(), _trims, _gem, background != null ? background.GetComponent<Renderer>() : null,
                    visual.Find("Illustration").GetComponent<Renderer>(), Label(visual, "Nom"), Label(visual, "Emplacement"), Label(visual, "Usage"),
                    Label(visual, "Texte"), Label(visual, "Identifiant"), torments.gameObject, torments.GetComponentInChildren<TMP_Text>(true),
                    visual.gameObject, root.GetComponent<BoxCollider>(), new Vector2(ThemeAssets.CardSize.x, ThemeAssets.CardSize.y));
            }

            private static TMP_Text Label(Transform visual, string name) => visual.Find(name).GetComponent<TMP_Text>();

            // The size a part shows: a label's rectangle, the illustration's scale; the Torment badge keeps its own.
            private static Vector2 SizeOf(Transform part) => part switch
            {
                RectTransform shape => shape.sizeDelta,
                _ => new Vector2(part.localScale.x, part.localScale.y),
            };

            private static bool Near(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < Epsilon * Epsilon;

            private static bool Near(Vector2 a, Vector2 b, string part) => part == "Tourments" || (a - b).sqrMagnitude < Epsilon * Epsilon;
        }

        // The metal materials of the modelled card, made once: they belong to the designer afterwards. Only a texture slot
        // left empty is filled again (the textures come with the model).
        private static void EnsureMaterials()
        {
            Texture2D? color = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath("BaseColor"));
            Texture2D? normal = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath("Normal"));
            Texture2D? metal = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath("MetallicSmoothness"));

            // The module's steel (ARB-96): the texture carries its colours (blackened steel, worn edges, brass pins), the
            // metal map where it is bare metal. The metals of the card reflect their surroundings but not the lamp's
            // highlight: flat and facing the camera, a whole card would flash at once and set the Bloom off.
            Metal(FrameMaterialPath, Color.white, color, normal, material =>
            {
                material.SetFloat("_Metallic", 1f);
                material.SetFloat("_Smoothness", 1f);
                NoHighlight(material);
                if (metal != null && material.GetTexture("_MetallicGlossMap") == null)
                {
                    material.SetTexture("_MetallicGlossMap", metal);
                    material.EnableKeyword("_METALLICSPECGLOSSMAP");
                }
            });

            // The faces that carry text (name plate, terminal): dark glass, so the light ink on it stays legible.
            Metal(PanelMaterialPath, Color.white, color, normal, material =>
            {
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Smoothness", 0.8f);
                NoHighlight(material);

                // No reflection of the surroundings either: even 4 % of a bright sky greys a black screen.
                material.SetFloat("_EnvironmentReflections", 0f);
                material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            });

            // Trims and gem: their colour and glow come from each card (CardDisplay), through the emission.
            Metal(TrimMaterialPath, Color.white, null, null, material =>
            {
                material.SetFloat("_Metallic", 0.2f);
                material.SetFloat("_Smoothness", 0.7f);
                Glow(material);
            });
            Metal(GemMaterialPath, Color.white, null, null, material =>
            {
                material.SetFloat("_Metallic", 0.1f);
                material.SetFloat("_Smoothness", 0.92f);
                Glow(material);
            });
        }

        private static void Metal(string path, Color tint, Texture2D? color, Texture2D? normal, System.Action<Material> setUp)
        {
            Material? existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created = existing == null;
            Material material = existing != null ? existing : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (created)
            {
                material.SetColor("_BaseColor", tint);
                setUp(material);
            }

            bool changed = false;
            if (color != null && material.GetTexture("_BaseMap") == null)
            {
                material.SetTexture("_BaseMap", color);
                changed = true;
            }

            if (normal != null && material.HasProperty("_BumpMap") && material.GetTexture("_BumpMap") == null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
                changed = true;
            }

            if (created)
            {
                ProjectAssets.Create(material, path);
            }
            else if (changed)
            {
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssetIfDirty(material);
            }
        }

        private static void NoHighlight(Material material)
        {
            material.SetFloat("_SpecularHighlights", 0f);
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        }

        private static void Glow(Material material)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.white);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
    }
}
