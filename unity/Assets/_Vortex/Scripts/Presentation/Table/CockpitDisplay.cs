using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Vortex.Client.Content;
using Vortex.Client.Theme;
using Vortex.Core.Content;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The player's cockpit (ARB-90, INTERFACE.md 3.2): the console model under the player's ship, over the place of the
    /// player's panel, in front of the interface and behind the player's cards, which lie in its two sockets. It shows the
    /// player's name, hit points (a gauge that fills and empties, with the figure on it), shield (the manometer's needle),
    /// overcharge (the switch, up when armed, its knob lit while there is a token) and technologies (three diodes). It only
    /// reads the parts' names (tools/blender/build_cockpit.py), so a new model with the same names takes its place. A
    /// touch of the switch arms the token; a touch of the console answers a decision that offers the player's own seat.
    /// </summary>
    public sealed class CockpitDisplay : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>Degrees the needle turns on each side of straight up, from 0 to the full shield.</summary>
        public const float NeedleSweep = 120f;

        /// <summary>Degrees the switch lever tilts up when the token is armed.</summary>
        public const float LeverTilt = 50f;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private readonly List<Renderer> _diodes = new List<Renderer>();
        private readonly List<Material> _diodeLooks = new List<Material>();
        private RectTransform? _place;
        private Camera? _view;
        private float _depth;
        private Transform? _fill;
        private Transform? _needle;
        private Transform? _lever;
        private Renderer? _knob;
        private Material? _knobLook;
        private TMP_Text? _name;
        private TMP_Text? _hp;
        private Quaternion _leverRest = Quaternion.identity;
        private Material? _glow;

        /// <summary>What a touch of the console does (a decision that offers the player's own seat), or null.</summary>
        public Action? Tapped { get; set; }

        /// <summary>The gauge's fill, from 0 to 1 (tests).</summary>
        public float Fill => _fill != null ? _fill.localScale.x : 0f;

        /// <summary>The needle's turn from straight up, in degrees, clockwise (tests).</summary>
        public float Needle => _needle != null ? -Mathf.DeltaAngle(0f, _needle.localEulerAngles.z) : 0f;

        /// <summary>Whether the switch is up (tests).</summary>
        public bool SwitchUp { get; private set; }

        /// <summary>The name and the hit points written on the console (tests).</summary>
        public (string Name, string Hp) Texts => (_name != null ? _name.text : string.Empty, _hp != null ? _hp.text : string.Empty);

        /// <summary>Diodes lit (tests).</summary>
        public int DiodesLit { get; private set; }

        /// <summary>
        /// Prepares the console: finds its parts, writes on its zones, and lets <paramref name="toggle"/> arm the token
        /// from the switch. It follows <paramref name="place"/> at <paramref name="depth"/> units from <paramref name="view"/>.
        /// </summary>
        public void Bind(RectTransform place, Camera view, float depth, Material? glow, Action toggle)
        {
            _place = place;
            _view = view;
            _depth = depth;
            _glow = glow;
            _fill = Part("Remplissage_PV");
            _needle = Part("Aiguille_Bouclier");
            _lever = Part("Levier_Surcharge");
            _leverRest = _lever != null ? _lever.localRotation : Quaternion.identity;
            _knob = Part("Bouton du levier")?.GetComponent<Renderer>();
            _knobLook = _knob != null ? _knob.sharedMaterial : null;
            for (int i = 1; i <= 3; i++)
            {
                Renderer? diode = Part("Diode_" + i.ToString(CultureInfo.InvariantCulture))?.GetComponent<Renderer>();
                if (diode != null)
                {
                    _diodes.Add(diode);
                    _diodeLooks.Add(diode.sharedMaterial);
                }
            }

            _name = Writing(Part("Zone_Nom"), FontStyles.Bold);
            _hp = Writing(Part("Zone_PV"), FontStyles.Bold);

            // The console answers a touch; the lever, over it, arms the token.
            Bounds shape = MeshBounds();
            BoxCollider area = gameObject.AddComponent<BoxCollider>();
            area.center = shape.center;
            area.size = shape.size;
            if (_lever != null)
            {
                BoxCollider handle = _lever.gameObject.AddComponent<BoxCollider>();
                handle.center = new Vector3(0f, 0f, -0.1f);
                handle.size = new Vector3(0.2f, 0.2f, 0.25f);
                _lever.gameObject.AddComponent<CockpitSwitch>().Pressed = toggle;
            }

            Place();
        }

        /// <summary>
        /// Shows a seat: its name in its seat colour's trims, its hit points out of <paramref name="maxHp"/>, its shield
        /// out of <paramref name="maxShield"/>, its overcharge token (held, armed) and its technologies.
        /// </summary>
        public void Show(string name, Color seat, int hp, int maxHp, int shield, int maxShield, bool charged, bool armed, IReadOnlyList<Color> technologies, TextTable texts, ThemeSettings theme)
        {
            ShipCatalog.PaintSeat(gameObject, seat);
            if (_name != null)
            {
                _name.text = name;
            }

            if (_hp != null)
            {
                _hp.text = string.Format(CultureInfo.InvariantCulture, texts.Get(TextKeys.SeatHp), hp);
            }

            float health = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
            if (_fill != null)
            {
                _fill.localScale = new Vector3(Mathf.Max(0.001f, health), 1f, 1f);
                Color gauge = Color.Lerp(theme.Loss, new Color(0.25f, 0.85f, 0.35f), health);
                Paint(_fill.GetComponent<Renderer>(), gauge, gauge * 0.6f);
            }

            if (_needle != null)
            {
                float share = maxShield > 0 ? Mathf.Clamp01((float)shield / maxShield) : 0f;
                _needle.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Lerp(-NeedleSweep, NeedleSweep, share));
            }

            SwitchUp = armed;
            if (_lever != null)
            {
                _lever.localRotation = armed ? Quaternion.AngleAxis(LeverTilt, Vector3.right) * _leverRest : _leverRest;
            }

            if (_knob != null)
            {
                Light(_knob, charged ? _glow : null, _knobLook, theme.Overcharge * (armed ? 3f : 1.6f));
            }

            DiodesLit = 0;
            for (int i = 0; i < _diodes.Count; i++)
            {
                bool lit = i < technologies.Count;
                Light(_diodes[i], lit ? _glow : null, _diodeLooks[i], lit ? technologies[i] * 3f : Color.clear);
                DiodesLit += lit ? 1 : 0;
            }
        }

        /// <summary>Moves the console over its place now.</summary>
        public void Place()
        {
            if (_place == null || _view == null)
            {
                return;
            }

            Rect rect = CardAnchor.ScreenRectOf(_place);
            transform.SetPositionAndRotation(
                _view.ViewportToWorldPoint(new Vector3(rect.center.x / _view.pixelWidth, rect.center.y / _view.pixelHeight, _depth)),
                _view.transform.rotation);

            // The model is one metre high: it takes the place's height.
            transform.localScale = Vector3.one * CardAnchor.WorldHeightAt(_view, _depth, rect.height);
        }

        /// <inheritdoc/>
        public void OnPointerClick(PointerEventData eventData) => Tapped?.Invoke();

        private static void Paint(Renderer? renderer, Color color, Color emission)
        {
            if (renderer == null)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColor, color);
            block.SetColor(EmissionColor, emission);
            renderer.SetPropertyBlock(block);
        }

        // A lit part takes the glow material in a bright colour; unlit, its own material back.
        private static void Light(Renderer renderer, Material? glow, Material? own, Color color)
        {
            renderer.sharedMaterial = glow != null ? glow : own;
            var block = new MaterialPropertyBlock();
            if (glow != null)
            {
                block.SetColor(BaseColor, color);
            }

            renderer.SetPropertyBlock(block);
        }

        // A text on a zone of the model: facing the camera with the console, the zone's size in the model's units.
        private TMP_Text? Writing(Transform? zone, FontStyles style)
        {
            if (zone == null)
            {
                return null;
            }

            var holder = new GameObject("Texte " + zone.name, typeof(RectTransform));
            holder.transform.SetParent(transform, false);
            holder.transform.localPosition = transform.InverseTransformPoint(zone.position) + new Vector3(0f, 0f, -0.01f);
            holder.transform.localRotation = Quaternion.identity;
            TextMeshPro text = holder.AddComponent<TextMeshPro>();
            text.rectTransform.sizeDelta = new Vector2(Mathf.Abs(zone.localScale.x), Mathf.Abs(zone.localScale.y));
            text.enableAutoSizing = true;
            text.fontSizeMin = 0.1f;
            text.fontSizeMax = 2f;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.richText = false;
            text.color = new Color32(22, 24, 30, 255);
            return text;
        }

        private Transform? Part(string name)
        {
            foreach (Transform part in GetComponentsInChildren<Transform>(true))
            {
                if (part.name == name)
                {
                    return part;
                }
            }

            return null;
        }

        // The box around the console's meshes, in its own axes.
        private Bounds MeshBounds()
        {
            Bounds? box = null;
            foreach (MeshFilter mesh in GetComponentsInChildren<MeshFilter>())
            {
                if (mesh.sharedMesh == null)
                {
                    continue;
                }

                Bounds local = mesh.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(i % 2 == 0 ? local.min.x : local.max.x, (i / 2) % 2 == 0 ? local.min.y : local.max.y, i / 4 == 0 ? local.min.z : local.max.z);
                    Vector3 inside = transform.InverseTransformPoint(mesh.transform.TransformPoint(corner));
                    if (box is Bounds grown)
                    {
                        grown.Encapsulate(inside);
                        box = grown;
                    }
                    else
                    {
                        box = new Bounds(inside, Vector3.zero);
                    }
                }
            }

            return box ?? new Bounds(Vector3.zero, new Vector3(3.84f, 1f, 0.1f));
        }

        private void LateUpdate() => Place();
    }
}
