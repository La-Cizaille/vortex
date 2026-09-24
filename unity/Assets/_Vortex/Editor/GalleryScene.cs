using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Vortex.Client.Content;
using Vortex.Client.Gallery;
using Vortex.Client.Presentation;
using Vortex.Client.Theme;

namespace Vortex.Editor
{
    /// <summary>
    /// Creates the Gallery scene when it is missing (through <see cref="ProjectAssets.EnsureAll"/>): a camera and a
    /// light for the ships, a scrolling list of every card on top, and the <see cref="GalleryController"/> that fills
    /// them.
    /// </summary>
    public static class GalleryScene
    {
        /// <summary>Path of the scene.</summary>
        public const string ScenePath = "Assets/_Vortex/Scenes/Gallery.unity";

        /// <summary>Creates the scene if it does not exist.</summary>
        public static void Ensure()
        {
            if (File.Exists(ScenePath))
            {
                return;
            }

            SceneFiles.Create(ScenePath, Build);
        }

        private static void Build()
        {
            var camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.fieldOfView = 40f;
            camera.transform.position = new Vector3(0f, 0f, -12f);

            var light = new GameObject("Lumière", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            _ = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // Interface: reference resolution 1920 x 1080 (INTERFACE.md); the cards scroll in the top 70 %.
            var canvas = new GameObject("Interface", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject scroll = DefaultControls.CreateScrollView(new DefaultControls.Resources());
            scroll.name = "Cartes";
            var scrollShape = (RectTransform)scroll.transform;
            scrollShape.SetParent(canvas.transform, false);
            scrollShape.anchorMin = new Vector2(0f, 0.3f);
            scrollShape.anchorMax = Vector2.one;
            scrollShape.offsetMin = Vector2.zero;
            scrollShape.offsetMax = Vector2.zero;
            scroll.GetComponent<Image>().color = Color.clear;
            var scrollRect = scroll.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.scrollSensitivity = 40f;
            Object.DestroyImmediate(scrollRect.horizontalScrollbar.gameObject);
            scrollRect.horizontalScrollbar = null;
            scrollRect.verticalScrollbar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
            scrollRect.verticalScrollbar.handleRect.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);

            RectTransform sections = scrollRect.content;
            var layout = sections.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 32, 48);
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            sections.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Ships: a row across the bottom 30 % of the view.
            Transform shipRow = new GameObject("Vaisseaux").transform;
            shipRow.position = new Vector3(0f, -3.2f, 0f);

            var gallery = new GameObject("Galerie", typeof(GalleryController)).GetComponent<GalleryController>();
            gallery.Assign(
                AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath),
                AssetDatabase.LoadAssetAtPath<ThemeSettings>(ThemeAssets.ThemePath),
                AssetDatabase.LoadAssetAtPath<CardArtCatalog>(ThemeAssets.CardArtPath),
                AssetDatabase.LoadAssetAtPath<ShipCatalog>(ThemeAssets.ShipsPath),
                AssetDatabase.LoadAssetAtPath<TextTable>(ThemeAssets.TextsPath),
                AssetDatabase.LoadAssetAtPath<GameObject>(ThemeAssets.CardPrefabPath).GetComponent<CardDisplay>(),
                sections,
                shipRow,
                camera);
        }
    }
}
