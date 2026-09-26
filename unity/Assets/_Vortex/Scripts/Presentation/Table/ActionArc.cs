using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Lays the crew actions out on an arc centred above the player's ship (INTERFACE.md 3.4, playtest 2): the attack
    /// actions on the left, beside the attack card, the shield actions on the right, beside the defense card. On each
    /// side, the actions aimed at an opponent stand at the end of the arc and the player's own ones near its top. Only
    /// the actions shown count, so the arc stays centred whatever the rule options.
    /// </summary>
    public sealed class ActionArc : MonoBehaviour
    {
        [Tooltip("Centre de l'arc (le vaisseau), en unités d'interface, depuis le bas du panneau du joueur.")]
        [SerializeField] private Vector2 centre = new Vector2(0f, 362f);
        [Tooltip("Rayon de l'arc, en unités d'interface.")]
        [SerializeField, Min(10f)] private float radius = 135f;
        [Tooltip("Ouverture de l'arc de part et d'autre de la verticale, en degrés.")]
        [SerializeField, Range(10f, 90f)] private float halfSpread = 70f;
        [Tooltip("Écart en plus entre les deux côtés, en part de l'écart entre deux actions.")]
        [SerializeField, Range(0f, 2f)] private float sideGap = 0.5f;
        [Tooltip("Côté gauche, de l'extrémité vers le haut de l'arc : les actions d'attaque.")]
        [SerializeField] private ActionButton[] attackSide = Array.Empty<ActionButton>();
        [Tooltip("Côté droit, du haut de l'arc vers l'extrémité : les actions de bouclier.")]
        [SerializeField] private ActionButton[] shieldSide = Array.Empty<ActionButton>();

        /// <summary>Places the actions shown along the arc (after the rule options have shown or hidden some).</summary>
        [ContextMenu("Disposer les actions")]
        public void Arrange()
        {
            List<ActionButton> left = attackSide.Where(Shown).ToList();
            List<ActionButton> right = shieldSide.Where(Shown).ToList();
            int count = left.Count + right.Count;
            if (count == 0)
            {
                return;
            }

            float gap = left.Count > 0 && right.Count > 0 ? sideGap : 0f;
            float steps = (count - 1) + gap;
            for (int i = 0; i < count; i++)
            {
                ActionButton button = i < left.Count ? left[i] : right[i - left.Count];
                float step = i + (i >= left.Count ? gap : 0f);
                float degrees = steps > 0f ? 90f + halfSpread - (2f * halfSpread * step / steps) : 90f;
                float angle = degrees * Mathf.Deg2Rad;
                ((RectTransform)button.transform).anchoredPosition = centre + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        /// <summary>Wires the actions of each side (editor setup).</summary>
        public void Assign(ActionButton[] attackActions, ActionButton[] shieldActions)
        {
            attackSide = attackActions;
            shieldSide = shieldActions;
        }

        // An action keeps its place whether it is possible now or not (ARB-87): only the rules of the game remove one.
        private static bool Shown(ActionButton? button) => button != null && button.InRules;
    }
}
