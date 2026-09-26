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
    /// player's panel, in front of the interface, the player's cards lying in its two sockets. It shows the player's name,
    /// hit points (a liquid in a glass tube, with the figure on it), shield (the manometer's needle, and its red zone lit
    /// while the shield protects nothing), overcharge (the switch, up while the token is armed, and the diode under it,
    /// lit while there is a token) and technologies (three diodes). It only reads the parts' names
    /// (tools/blender/build_cockpit.py), so a new model with the same names takes its place. A touch of the switch arms
    /// the token; a touch of the console answers a decision that offers the player's own seat.
    /// </summary>
    public sealed class CockpitDisplay : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>Degrees the needle turns on each side of straight up, from 0 to the full shield.</summary>
        public const float NeedleSweep = 120f;

        /// <summary>Degrees the switch lever tilts up when the token is armed.</summary>
        public const float LeverTilt = 50f;

        /// <summary>Marks of the dial, from 0 to the full shield.</summary>
        public const int DialMarks = 9;

        // The liquid is viscous: a slow spring that barely overshoots. The needle is a light spring that quivers a little.
        private const float LiquidStiffness = 18f;
        private const float LiquidDamping = 7.5f;
        private const float NeedleStiffness = 90f;
        private const float NeedleDamping = 11f;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private readonly List<Renderer> _diodes = new List<Renderer>();
        private readonly List<Material> _diodeLooks = new List<Material>();
        private readonly List<TMP_Text> _figures = new List<TMP_Text>();
        private RectTransform? _place;
        private Camera? _view;
        private float _cardDepth;
        private float _cardPlane;
        private Transform? _fill;
        private Transform? _needle;
        private Transform? _lever;
        private Renderer? _chargeDiode;
        private Material? _chargeLook;
        private Renderer? _redZone;
        private Material? _redLook;
        private TMP_Text? _name;
        private TMP_Text? _hp;
        private Quaternion _leverRest = Quaternion.identity;
        private Material? _glow;
        private bool _shown;
        private float _fillTarget = 1f;
        private float _fillNow = 1f;
        private float _fillSpeed;
        private float _needleTarget;
        private float _needleNow;
        private float _needleSpeed;
        private float _leverNow;

        /// <summary>What a touch of the console does (a decision that offers the player's own seat), or null.</summary>
        public Action? Tapped { get; set; }

        /// <summary>Whether the token is armed now, read every frame so the lever drops as soon as it is disarmed.</summary>
        public Func<bool>? Armed { get; set; }

        /// <summary>The share of hit points the liquid is heading to, from 0 to 1 (tests).</summary>
        public float Fill => _fillTarget;

        /// <summary>The needle's turn from straight up the needle is heading to, in degrees, clockwise (tests).</summary>
        public float Needle => _needleTarget;

        /// <summary>Whether the switch is up (tests).</summary>
        public bool SwitchUp { get; private set; }

        /// <summary>Whether the overcharge diode is lit (tests).</summary>
        public bool ChargeLit { get; private set; }

        /// <summary>Whether the dial's red zone is lit (tests).</summary>
        public bool RedZoneLit { get; private set; }

        /// <summary>The name, the hit points and the dial's figures written on the console (tests).</summary>
        public (string Name, string Hp, string Figures) Texts => (
            _name != null ? _name.text : string.Empty,
            _hp != null ? _hp.text : string.Empty,
            string.Join(" ", _figures.ConvertAll(f => f.text)));

        /// <summary>Diodes lit (tests).</summary>
        public int DiodesLit { get; private set; }

        /// <summary>
        /// Prepares the console: finds its parts, writes on its zones, gives its glass parts <paramref name="glass"/>, and
        /// lets <paramref name="toggle"/> arm the token from the switch. It follows <paramref name="place"/>, as far from
        /// <paramref name="view"/> as puts its sockets at <paramref name="cardDepth"/>, where the cards lie.
        /// </summary>
        public void Bind(RectTransform place, Camera view, float cardDepth, Material? glow, Material? glass, Action toggle)
        {
            _place = place;
            _view = view;
            _cardDepth = cardDepth;
            _glow = glow;
            _fill = Part("Remplissage_PV");
            _needle = Part("Aiguille_Bouclier");
            _lever = Part("Levier_Surcharge");
            _leverRest = _lever != null ? _lever.localRotation : Quaternion.identity;
            (_chargeDiode, _chargeLook) = Look(Part("Diode_Surcharge"));
            (_redZone, _redLook) = Look(Part("Zone_Rouge"));
            for (int i = 1; i <= 3; i++)
            {
                (Renderer? diode, Material? own) = Look(Part("Diode_" + i.ToString(CultureInfo.InvariantCulture)));
                if (diode != null)
                {
                    _diodes.Add(diode);
                    _diodeLooks.Add(own!);
                }
            }

            foreach (string part in new[] { "Verre_Cadran", "Tube_PV" })
            {
                if (glass != null && Part(part) is Transform pane && pane.TryGetComponent(out Renderer look))
                {
                    look.sharedMaterial = glass;
                }
            }

            // The cards lie on the sockets' marker: how far it stands in front of the console, in the model's units.
            Transform? socket = Part("Socket_ATK");
            _cardPlane = socket != null ? -transform.InverseTransformPoint(socket.position).z : 0f;

            _name = Writing(Part("Zone_Nom"), new Color32(22, 24, 30, 255));
            _hp = Writing(Part("Zone_PV"), Color.white);
            if (_hp != null)
            {
                _hp.outlineWidth = 0.25f;
                _hp.outlineColor = new Color32(10, 10, 14, 255);
            }

            for (int i = 0; i < DialMarks; i++)
            {
                TMP_Text? figure = Writing(Part("Chiffre_" + i.ToString(CultureInfo.InvariantCulture)), new Color32(22, 24, 30, 255));
                if (figure != null)
                {
                    _figures.Add(figure);
                }
            }

            // The console's body answers a touch; the lever, over it, arms the token. Nothing stands in front of the cards,
            // which lie over the sockets and keep their own touches.
            Bounds shape = BodyBounds();
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
        /// out of <paramref name="maxShield"/> (the red zone lit when <paramref name="unprotected"/>: the shield counts for
        /// nothing), its overcharge token (held, armed) and its technologies.
        /// </summary>
        public void Show(string name, Color seat, int hp, int maxHp, int shield, int maxShield, bool unprotected, bool charged, bool armed, IReadOnlyList<Color> technologies, TextTable texts, ThemeSettings theme)
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

            // The liquid: a glowing plasma that turns to blood as the hit points drain.
            _fillTarget = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
            if (_fill != null)
            {
                Color liquid = Color.Lerp(theme.Loss, new Color(0.3f, 0.95f, 0.55f), _fillTarget);
                Paint(_fill.GetComponent<Renderer>(), liquid, liquid * 1.3f);
            }

            float share = maxShield > 0 ? Mathf.Clamp01((float)shield / maxShield) : 0f;
            _needleTarget = Mathf.Lerp(-NeedleSweep, NeedleSweep, share);
            for (int i = 0; i < _figures.Count; i++)
            {
                // A figure on each mark that falls on a whole shield value.
                int scaled = i * maxShield;
                _figures[i].text = scaled % (DialMarks - 1) == 0 ? (scaled / (DialMarks - 1)).ToString(CultureInfo.InvariantCulture) : string.Empty;
            }

            RedZoneLit = unprotected || shield <= 0;
            if (_redZone != null)
            {
                Light(_redZone, RedZoneLit ? _glow : null, _redLook, theme.Loss * 2.5f);
            }

            SwitchUp = armed;
            ChargeLit = charged;
            if (_chargeDiode != null)
            {
                Light(_chargeDiode, charged ? _glow : null, _chargeLook, theme.Overcharge * (armed ? 4f : 2.5f));
            }

            DiodesLit = 0;
            for (int i = 0; i < _diodes.Count; i++)
            {
                bool lit = i < technologies.Count;
                Light(_diodes[i], lit ? _glow : null, _diodeLooks[i], lit ? technologies[i] * 3f : Color.clear);
                DiodesLit += lit ? 1 : 0;
            }

            // The first time, straight to the figures; then the liquid, the needle and the lever move there.
            if (!_shown)
            {
                _shown = true;
                Settle();
            }
        }

        /// <summary>Moves the moving parts to where they are heading now (first show, captures).</summary>
        public void Settle()
        {
            _fillNow = _fillTarget;
            _fillSpeed = 0f;
            _needleNow = _needleTarget;
            _needleSpeed = 0f;
            _leverNow = SwitchUp ? 1f : 0f;
            Pose();
        }

        /// <summary>Moves the console over its place now.</summary>
        public void Place()
        {
            if (_place == null || _view == null)
            {
                return;
            }

            // The model is one metre high: it takes the place's height. Its scale grows with its distance, so the distance
            // that puts the sockets at the cards' depth is solved at once: depth - plane * depth * size = card depth.
            Rect rect = CardAnchor.ScreenRectOf(_place);
            float sizePerDepth = CardAnchor.WorldHeightAt(_view, 1f, rect.height);
            float depth = _cardDepth / Mathf.Max(0.1f, 1f - (_cardPlane * sizePerDepth));
            transform.SetPositionAndRotation(
                _view.ViewportToWorldPoint(new Vector3(rect.center.x / _view.pixelWidth, rect.center.y / _view.pixelHeight, depth)),
                _view.transform.rotation);
            transform.localScale = Vector3.one * sizePerDepth * depth;
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

        private static (Renderer?, Material?) Look(Transform? part) =>
            part != null && part.TryGetComponent(out Renderer renderer) ? (renderer, renderer.sharedMaterial) : (null, null);

        // A spring step: the value, its speed, towards the target.
        private static void Spring(ref float value, ref float speed, float target, float stiffness, float damping, float step)
        {
            speed += ((stiffness * (target - value)) - (damping * speed)) * step;
            value += speed * step;
        }

        private void Pose()
        {
            if (_fill != null)
            {
                // Moving, the liquid thickens a little, as a viscous fluid would.
                float swell = 1f + Mathf.Clamp(Mathf.Abs(_fillSpeed) * 0.15f, 0f, 0.12f);
                _fill.localScale = new Vector3(Mathf.Max(0.001f, _fillNow), swell, swell);
            }

            if (_needle != null)
            {
                _needle.localRotation = Quaternion.Euler(0f, 0f, -_needleNow);
            }

            if (_lever != null)
            {
                _lever.localRotation = Quaternion.AngleAxis(LeverTilt * _leverNow, Vector3.right) * _leverRest;
            }
        }

        // A text on a zone of the model: facing the camera with the console, the zone's size in the model's units.
        private TMP_Text? Writing(Transform? zone, Color color)
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
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.richText = false;
            text.color = color;
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

        // The box around the console's body (the part named Console), in its own axes; the whole model without one.
        private Bounds BodyBounds()
        {
            Transform? body = Part("Console");
            MeshFilter[] meshes = body != null && body.TryGetComponent(out MeshFilter only) ? new[] { only } : GetComponentsInChildren<MeshFilter>();
            Bounds? box = null;
            foreach (MeshFilter mesh in meshes)
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

        private void LateUpdate()
        {
            Place();

            // The lever follows the armed state as it changes (disarmed after each command); the liquid and the needle
            // spring towards their figures.
            if (Armed != null)
            {
                SwitchUp = Armed();
            }

            float step = Mathf.Min(Time.deltaTime, 0.05f);
            Spring(ref _fillNow, ref _fillSpeed, _fillTarget, LiquidStiffness, LiquidDamping, step);
            Spring(ref _needleNow, ref _needleSpeed, _needleTarget, NeedleStiffness, NeedleDamping, step);
            _leverNow = Mathf.Lerp(_leverNow, SwitchUp ? 1f : 0f, 1f - Mathf.Exp(-12f * step));
            Pose();
        }
    }
}
