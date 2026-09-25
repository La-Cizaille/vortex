using System;
using TMPro;
using UnityEngine.UI;

namespace Vortex.Client.Menus
{
    /// <summary>Small helpers shared by the menus: one action per button, and its label.</summary>
    internal static class MenuButtons
    {
        /// <summary>Makes <paramref name="button"/> do <paramref name="action"/> only (earlier listeners are dropped).</summary>
        public static void Wire(Button button, Action action)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action());
        }

        /// <summary>Writes the label of a button (text from the text table, shown without rich text).</summary>
        public static void Label(Button button, string text)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            label.richText = false;
            label.text = text;
        }
    }
}
