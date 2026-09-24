using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Dice rolling, then settling on the values the engine rolled (ADR-0014). The faces shown during the roll are
    /// cosmetic only: the result always comes from the engine's event. Spawned by <see cref="DiceFeedback"/>; its look
    /// is the DiceTray prefab.
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

        private readonly List<(RectTransform Shape, TMP_Text Face)> _dice = new List<(RectTransform, TMP_Text)>();
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
                    Settle();
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

        /// <summary>Shows the engine's values.</summary>
        public void Settle()
        {
            for (int i = 0; i < _dice.Count; i++)
            {
                _dice[i].Shape.localRotation = Quaternion.Euler(0f, 0f, 45f);
                _dice[i].Face.text = _values[i].ToString(CultureInfo.InvariantCulture);
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
