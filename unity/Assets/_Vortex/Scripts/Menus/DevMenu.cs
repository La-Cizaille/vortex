using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;
using Vortex.Client.Session;

namespace Vortex.Client.Menus
{
    /// <summary>
    /// The development menu (INTERFACE.md 5, ARB-65): the rule options under study (ADR-0011) and the seed of the game.
    /// It exists only in the editor and in development builds: in a published build, the local game menu destroys it and
    /// never reads it (<see cref="Available"/> is false there).
    /// </summary>
    public sealed class DevMenu : MonoBehaviour
    {
        /// <summary>Highest bonus offered for an option (the engine bounds them again).</summary>
        public const int MaxBonus = 8;

        [SerializeField] private TMP_Text title = null!;
        [SerializeField] private Button posture = null!;
        [SerializeField] private Button bounty = null!;
        [SerializeField] private Button ghosts = null!;
        [SerializeField] private TMP_InputField seed = null!;
        [SerializeField] private Button back = null!;

        private TextTable? _texts;
        private RuleOptions _current = new RuleOptions(0, 0, false);
        private RuleOptions _defaults = new RuleOptions(0, 0, false);

        /// <summary>True in the editor and in development builds only (Unity's own flag, false in a published build).</summary>
        public static bool Available => Debug.isDebugBuild;

        /// <summary>The rule options chosen.</summary>
        public RuleOptions Options => _current;

        /// <summary>True when the options differ from the current rules.</summary>
        public bool Changed => _current.DefensivePostureBonus != _defaults.DefensivePostureBonus
            || _current.LeaderBounty != _defaults.LeaderBounty
            || _current.GhostsChooseEvent != _defaults.GhostsChooseEvent;

        /// <summary>The seed typed, or 0 (a new one for each game) when the field is empty or not a number.</summary>
        public ulong Seed => ulong.TryParse(seed.text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out ulong value) ? value : 0UL;

        /// <summary>The seed field (tests type in it).</summary>
        public TMP_InputField SeedField => seed;

        /// <summary>Shows the current rules (<paramref name="defaults"/>); <paramref name="closed"/> runs on "back".</summary>
        public void Bind(TextTable texts, RuleOptions defaults, Action closed)
        {
            _texts = texts != null ? texts : throw new ArgumentNullException(nameof(texts));
            _defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
            _current = defaults;
            title.richText = false;
            title.text = texts.Get(TextKeys.DevTitle);
            seed.richText = false;
            seed.characterLimit = 20;
            seed.contentType = TMP_InputField.ContentType.IntegerNumber;
            seed.text = string.Empty;
            if (seed.placeholder is TMP_Text hint)
            {
                hint.text = texts.Get(TextKeys.DevSeed);
            }

            MenuButtons.Label(back, texts.Get(TextKeys.MenuBack));
            MenuButtons.Wire(posture, NextPosture);
            MenuButtons.Wire(bounty, NextBounty);
            MenuButtons.Wire(ghosts, ToggleGhosts);
            MenuButtons.Wire(back, closed ?? throw new ArgumentNullException(nameof(closed)));
            Refresh();
        }

        /// <summary>The next defensive posture bonus (0: no such action).</summary>
        public void NextPosture()
        {
            _current = new RuleOptions((_current.DefensivePostureBonus + 1) % (MaxBonus + 1), _current.LeaderBounty, _current.GhostsChooseEvent);
            Refresh();
        }

        /// <summary>The next bounty on the leader (0: none).</summary>
        public void NextBounty()
        {
            _current = new RuleOptions(_current.DefensivePostureBonus, (_current.LeaderBounty + 1) % (MaxBonus + 1), _current.GhostsChooseEvent);
            Refresh();
        }

        /// <summary>Eliminated players choose the event, or not.</summary>
        public void ToggleGhosts()
        {
            _current = new RuleOptions(_current.DefensivePostureBonus, _current.LeaderBounty, !_current.GhostsChooseEvent);
            Refresh();
        }

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(TMP_Text titleLabel, Button postureButton, Button bountyButton, Button ghostsButton, TMP_InputField seedField, Button backButton)
        {
            title = titleLabel;
            posture = postureButton;
            bounty = bountyButton;
            ghosts = ghostsButton;
            seed = seedField;
            back = backButton;
        }

        private void Refresh()
        {
            if (_texts is null)
            {
                return;
            }

            MenuButtons.Label(posture, string.Format(CultureInfo.InvariantCulture, _texts.Get(TextKeys.DevPosture), Bonus(_current.DefensivePostureBonus)));
            MenuButtons.Label(bounty, string.Format(CultureInfo.InvariantCulture, _texts.Get(TextKeys.DevBounty), Bonus(_current.LeaderBounty)));
            MenuButtons.Label(ghosts, string.Format(CultureInfo.InvariantCulture, _texts.Get(TextKeys.DevGhosts), _texts.Get(_current.GhostsChooseEvent ? TextKeys.Yes : TextKeys.No)));
        }

        private string Bonus(int value) => value == 0
            ? _texts!.Get(TextKeys.DevOff)
            : string.Format(CultureInfo.InvariantCulture, _texts!.Get(TextKeys.DevBonus), value);
    }
}
