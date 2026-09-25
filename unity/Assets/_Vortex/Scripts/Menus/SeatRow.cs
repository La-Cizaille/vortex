using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;
using Vortex.Client.Session;
using Vortex.Core.Bots;
using Vortex.Core.Rules;

namespace Vortex.Client.Menus
{
    /// <summary>One seat of the local game menu (INTERFACE.md 5): played by a person or a bot (with its level), and a name.</summary>
    public sealed class SeatRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text seatLabel = null!;
        [SerializeField] private Button kind = null!;
        [SerializeField] private Button level = null!;
        [SerializeField] private TMP_InputField nameField = null!;

        private TextTable? _texts;
        private string _defaultName = string.Empty;

        /// <summary>Who plays the seat.</summary>
        public SeatKind Kind { get; private set; }

        /// <summary>Level of the bot, when a bot plays the seat.</summary>
        public BotLevel Level { get; private set; } = BotLevel.Normal;

        /// <summary>True when the level can be chosen (a bot plays the seat).</summary>
        public bool LevelShown => level.interactable && level.gameObject.activeSelf;

        /// <summary>The name field (tests type in it).</summary>
        public TMP_InputField NameField => nameField;

        /// <summary>Shows seat <paramref name="seat"/> (0-based) played by <paramref name="who"/>.</summary>
        public void Bind(TextTable texts, int seat, SeatKind who)
        {
            _texts = texts != null ? texts : throw new ArgumentNullException(nameof(texts));
            _defaultName = string.Format(CultureInfo.InvariantCulture, texts.Get(TextKeys.SeatDefaultName), seat + 1);
            seatLabel.richText = false;
            seatLabel.text = string.Format(CultureInfo.InvariantCulture, texts.Get(TextKeys.LocalSeat), seat + 1);
            nameField.richText = false;
            nameField.characterLimit = GameEngine.MaxNameLength;
            nameField.lineType = TMP_InputField.LineType.SingleLine;
            nameField.text = string.Empty;
            if (nameField.placeholder is TMP_Text hint)
            {
                hint.richText = false;
                hint.text = _defaultName;
            }

            MenuButtons.Wire(kind, ToggleKind);
            MenuButtons.Wire(level, NextLevel);
            Kind = who;
            Level = BotLevel.Normal;
            Refresh();
        }

        /// <summary>A person or a bot.</summary>
        public void ToggleKind()
        {
            Kind = Kind == SeatKind.Human ? SeatKind.Bot : SeatKind.Human;
            Refresh();
        }

        /// <summary>The next bot level.</summary>
        public void NextLevel()
        {
            Level = Level == BotLevel.Strong ? BotLevel.Random : Level + 1;
            Refresh();
        }

        /// <summary>The seat as chosen: the typed name once cleaned, or the default name.</summary>
        public SeatSetup ToSeat() => new SeatSetup(CleanName(nameField.text, _defaultName), Kind, Level);

        /// <summary>
        /// A name the engine accepts: control and invisible formatting characters removed (they could hide or reorder the
        /// text, docs/SECURITY.md), spaces trimmed, cut to <see cref="GameEngine.MaxNameLength"/>; <paramref name="fallback"/>
        /// when nothing is left.
        /// </summary>
        public static string CleanName(string? typed, string fallback)
        {
            var kept = new StringBuilder();
            foreach (char c in typed ?? string.Empty)
            {
                UnicodeCategory category = char.GetUnicodeCategory(c);
                if (!char.IsControl(c) && category != UnicodeCategory.Format && category != UnicodeCategory.Surrogate
                    && category != UnicodeCategory.PrivateUse && category != UnicodeCategory.OtherNotAssigned)
                {
                    kept.Append(c);
                }
            }

            string name = kept.ToString().Trim();
            if (name.Length > GameEngine.MaxNameLength)
            {
                name = name.Substring(0, GameEngine.MaxNameLength).TrimEnd();
            }

            return name.Length > 0 ? name : fallback;
        }

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(TMP_Text label, Button kindButton, Button levelButton, TMP_InputField field)
        {
            seatLabel = label;
            kind = kindButton;
            level = levelButton;
            nameField = field;
        }

        private void Refresh()
        {
            if (_texts is null)
            {
                return;
            }

            MenuButtons.Label(kind, _texts.Get(Kind == SeatKind.Human ? TextKeys.LocalHuman : TextKeys.LocalBot));
            MenuButtons.Label(level, _texts.Get(TextKeys.BotLevelName(Level)));

            // A person has no level: the button fades out but keeps its place, so the names stay aligned.
            bool bot = Kind == SeatKind.Bot;
            level.interactable = bot;
            if (level.TryGetComponent(out CanvasGroup group))
            {
                group.alpha = bot ? 1f : 0f;
                group.blocksRaycasts = bot;
            }
            else
            {
                level.gameObject.SetActive(bot);
            }
        }
    }
}
