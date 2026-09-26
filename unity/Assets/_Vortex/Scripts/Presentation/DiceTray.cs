using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Dice rolling, then settling on the values the engine rolled (ADR-0014). The faces shown during the roll are
    /// cosmetic only: the result always comes from the engine's event. Spawned by <see cref="DiceFeedback"/>; its look
    /// is the DiceTray prefab. With a camera (<see cref="UseDice3D"/>), each die is a 3D d8 spinning over its place
    /// (<see cref="DieSpinner"/>, ANIMATIONS.md §2), and the tray keeps only its layout and the total.
    /// </summary>
    public sealed class DiceTray : MonoBehaviour
    {
        [SerializeField] private RectTransform dieTemplate = null!;
        [SerializeField] private TMP_Text total = null!;
        [SerializeField, Min(2)] private int faces = 8;
        [Tooltip("Rotation des dés pendant le lancer, en degrés par seconde.")]
        [SerializeField] private float spin = 540f;
        [Tooltip("Intervalle entre deux faces affichées pendant le lancer, en secondes.")]
        [SerializeField, Min(0.01f)] private float faceInterval = 0.06f;
        [Tooltip("Distance des dés 3D à la caméra : devant les cartes (6) et la carte agrandie (3).")]
        [SerializeField, Min(0.5f)] private float dieDepth = 2.5f;
        [Tooltip("Durée pendant laquelle un dé 3D tourne sa face vers la caméra, en secondes.")]
        [SerializeField, Min(0f)] private float settleSeconds = 0.25f;

        private readonly List<(RectTransform Shape, TMP_Text Face)> _dice = new List<(RectTransform, TMP_Text)>();
        private readonly List<DieSpinner> _spinners = new List<DieSpinner>();
        private Camera? _view;
        private GameObject? _dieModel;
        private IReadOnlyList<int> _values = System.Array.Empty<int>();
        private float _roll;
        private float _hold;
        private float _time;
        private float _nextFace;

        /// <summary>True once the dice show the engine's values.</summary>
        public bool Settled { get; private set; }

        /// <summary>Faces shown now (tests).</summary>
        public IEnumerable<string> Faces
        {
            get
            {
                foreach ((RectTransform _, TMP_Text face) in _dice)
                {
                    yield return face.text;
                }
            }
        }

        /// <summary>The 3D dice (tests).</summary>
        public IReadOnlyList<DieSpinner> Dice3D => _spinners;

        /// <summary>
        /// Shows each die as a 3D d8 in front of <paramref name="view"/>: <paramref name="model"/> (with its Face_1 to Face_8
        /// markers), or a generated one. Call it before <see cref="Roll"/>.
        /// </summary>
        public void UseDice3D(Camera view, GameObject? model)
        {
            _view = view;
            _dieModel = model;
        }

        /// <summary>Keeps the dice on screen <paramref name="seconds"/> longer (a die that leaps for a critical hit).</summary>
        public void Extend(float seconds) => _hold += Mathf.Max(0f, seconds);

        /// <summary>Total shown once settled, or empty (tests).</summary>
        public string Total => total.text;

        /// <summary>Starts a roll of these values; <paramref name="sum"/> is shown once settled (null: none).</summary>
        public void Roll(IReadOnlyList<int> values, int? sum, float rollSeconds, float holdSeconds)
        {
            _values = values;
            _roll = rollSeconds;
            _hold = holdSeconds;
            foreach (int _ in values)
            {
                RectTransform die = Instantiate(dieTemplate, dieTemplate.parent, false);
                die.gameObject.SetActive(true);
                _dice.Add(((RectTransform)die.GetChild(0), die.GetComponentInChildren<TMP_Text>(true)));
                if (_view != null)
                {
                    Spin3D(die);
                }
            }

            total.transform.SetAsLastSibling();
            total.text = sum.HasValue ? "= " + sum.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
            total.gameObject.SetActive(false);
            ShowRandomFaces();
            if (rollSeconds <= 0f)
            {
                Settle();
            }
        }

        /// <summary>Moves the animation on (called every frame; tests call it directly).</summary>
        public void Advance(float deltaTime)
        {
            _time += deltaTime;
            if (!Settled)
            {
                if (_time >= _roll)
                {
                    SettleDice(settleSeconds);
                    return;
                }

                foreach ((RectTransform shape, TMP_Text _) in _dice)
                {
                    shape.Rotate(0f, 0f, spin * deltaTime);
                }

                if (_time >= _nextFace)
                {
                    ShowRandomFaces();
                }
            }
            else if (_time >= _roll + _hold)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }
        }

        /// <summary>Shows the engine's values at once (captures and tests).</summary>
        public void Settle() => SettleDice(0f);

        // Shows the engine's values; the 3D dice turn their face to the camera in <paramref name="turnSeconds"/>.
        private void SettleDice(float turnSeconds)
        {
            for (int i = 0; i < _dice.Count; i++)
            {
                _dice[i].Shape.localRotation = Quaternion.Euler(0f, 0f, 45f);
                _dice[i].Face.text = _values[i].ToString(CultureInfo.InvariantCulture);
                if (i < _spinners.Count)
                {
                    _spinners[i].Show(_values[i], turnSeconds);
                }
            }

            total.gameObject.SetActive(total.text.Length > 0);
            Settled = true;
        }

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(RectTransform die, TMP_Text sum)
        {
            dieTemplate = die;
            total = sum;
        }

        private void Update() => Advance(Time.deltaTime);

        // The 3D dice live outside the interface: they go with the tray.
        private void OnDestroy()
        {
            foreach (DieSpinner spinner in _spinners)
            {
                if (spinner == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(spinner.gameObject);
                }
                else
                {
                    DestroyImmediate(spinner.gameObject);
                }
            }
        }

        // A 3D d8 over the die's place; the flat die and the tray's background step aside, as the interface is drawn
        // over the 3D scene and would hide it.
        private void Spin3D(RectTransform place)
        {
            foreach (Graphic flat in place.GetComponentsInChildren<Graphic>(true))
            {
                flat.enabled = false;
            }

            if (TryGetComponent(out Image background))
            {
                background.enabled = false;
            }

            GameObject die = _dieModel != null
                ? Instantiate(_dieModel)
                : Vortex.Client.Theme.PlaceholderDie.Build(null, new Color32(236, 238, 245, 255), new Color32(26, 28, 40, 255));
            DieSpinner spinner = die.AddComponent<DieSpinner>();
            spinner.Follow(place, _view!, dieDepth, spin * 1.5f);
            _spinners.Add(spinner);
        }

        // Cosmetic faces while the dice spin: presentation only, never a game value.
        private void ShowRandomFaces()
        {
            foreach ((RectTransform _, TMP_Text face) in _dice)
            {
                face.text = Random.Range(1, faces + 1).ToString(CultureInfo.InvariantCulture);
            }

            _nextFace = _time + faceInterval;
        }
    }
}
