using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;
using Vortex.Client.Session;

namespace Vortex.Client.Menus
{
    /// <summary>
    /// The local game menu (INTERFACE.md 5, ARB-65): 2 to 5 players, 5 by default (the standard table, ARB-52), and for
    /// each seat a person or a bot (with its level) and a name. In development, a link to the development menu (rule
    /// options and seed).
    /// </summary>
    public sealed class LocalGameMenu : MonoBehaviour
    {
        /// <summary>Players of the standard table (ARB-52), chosen by default.</summary>
        public const int DefaultPlayers = 5;

        [SerializeField] private TMP_Text title = null!;
        [SerializeField] private Button fewer = null!;
        [SerializeField] private Button more = null!;
        [SerializeField] private TMP_Text players = null!;
        [SerializeField] private SeatRow[] rows = Array.Empty<SeatRow>();
        [SerializeField] private Button back = null!;
        [SerializeField] private Button launch = null!;
        [SerializeField] private Button development = null!;
        [SerializeField] private DevMenu devMenu = null!;

        private TextTable? _texts;
        private Action<MatchSetup>? _launch;
        private int _min = 2;
        private int _max = 5;

        /// <summary>Number of players chosen.</summary>
        public int Players { get; private set; }

        /// <summary>The seats, the first <see cref="Players"/> of them in use.</summary>
        public IReadOnlyList<SeatRow> Rows => rows;

        /// <summary>The development menu, or null in a published build.</summary>
        public DevMenu? Development => devMenu != null ? devMenu : null;

        /// <summary>Connects the menu.</summary>
        /// <param name="texts">Interface texts.</param>
        /// <param name="playerCounts">Player counts the engine supports.</param>
        /// <param name="currentRules">Rule options of the content, shown first in the development menu.</param>
        /// <param name="closed">Back to the home menu.</param>
        /// <param name="start">Starts the chosen game.</param>
        public void Bind(TextTable texts, IEnumerable<int> playerCounts, RuleOptions currentRules, Action closed, Action<MatchSetup> start)
        {
            _texts = texts != null ? texts : throw new ArgumentNullException(nameof(texts));
            _launch = start ?? throw new ArgumentNullException(nameof(start));
            List<int> counts = (playerCounts ?? throw new ArgumentNullException(nameof(playerCounts))).Where(n => n >= 1 && n <= rows.Length).ToList();
            _min = counts.Count > 0 ? counts.Min() : rows.Length;
            _max = counts.Count > 0 ? counts.Max() : rows.Length;

            title.richText = false;
            title.text = texts.Get(TextKeys.LocalTitle);
            MenuButtons.Label(back, texts.Get(TextKeys.MenuBack));
            MenuButtons.Label(launch, texts.Get(TextKeys.LocalLaunch));
            MenuButtons.Label(development, texts.Get(TextKeys.LocalDevelopment));
            MenuButtons.Wire(fewer, () => SetPlayers(Players - 1));
            MenuButtons.Wire(more, () => SetPlayers(Players + 1));
            MenuButtons.Wire(back, closed ?? throw new ArgumentNullException(nameof(closed)));
            MenuButtons.Wire(launch, Launch);
            MenuButtons.Wire(development, OpenDevelopment);

            // The first seat is a person's, the others bots': the usual game against the computer.
            for (int seat = 0; seat < rows.Length; seat++)
            {
                rows[seat].Bind(texts, seat, seat == 0 ? SeatKind.Human : SeatKind.Bot);
            }

            if (DevMenu.Available && devMenu != null)
            {
                devMenu.Bind(texts, currentRules, CloseDevelopment);
                devMenu.gameObject.SetActive(false);
            }
            else
            {
                // A published build has no development menu at all.
                development.gameObject.SetActive(false);
                if (devMenu != null)
                {
                    Destroy(devMenu.gameObject);
                }
            }

            SetPlayers(Mathf.Clamp(DefaultPlayers, _min, _max));
        }

        /// <summary>Chooses the number of players, within what the engine supports.</summary>
        public void SetPlayers(int count)
        {
            Players = Mathf.Clamp(count, _min, _max);
            for (int seat = 0; seat < rows.Length; seat++)
            {
                rows[seat].gameObject.SetActive(seat < Players);
            }

            fewer.interactable = Players > _min;
            more.interactable = Players < _max;
            players.text = string.Format(CultureInfo.InvariantCulture, _texts!.Get(TextKeys.LocalPlayers), Players);
        }

        /// <summary>The game chosen: the seats in use, and in development the seed and the rule options.</summary>
        public MatchSetup BuildSetup()
        {
            List<SeatSetup> seats = rows.Take(Players).Select(r => r.ToSeat()).ToList();

            // A published build never reads the development menu (it has been removed anyway).
            return DevMenu.Available && devMenu != null
                ? new MatchSetup(seats, devMenu.Seed, devMenu.Changed ? devMenu.Options : null)
                : new MatchSetup(seats);
        }

        /// <summary>Starts the chosen game.</summary>
        public void Launch() => _launch?.Invoke(BuildSetup());

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(TMP_Text titleLabel, Button fewerButton, Button moreButton, TMP_Text playersLabel, SeatRow[] seatRows, Button backButton, Button launchButton, Button developmentButton, DevMenu developmentMenu)
        {
            title = titleLabel;
            fewer = fewerButton;
            more = moreButton;
            players = playersLabel;
            rows = seatRows;
            back = backButton;
            launch = launchButton;
            development = developmentButton;
            devMenu = developmentMenu;
        }

        private void OpenDevelopment()
        {
            if (devMenu != null)
            {
                devMenu.gameObject.SetActive(true);
            }
        }

        private void CloseDevelopment()
        {
            if (devMenu != null)
            {
                devMenu.gameObject.SetActive(false);
            }
        }
    }
}
