using System.Collections.Generic;
using UnityEngine;
using Vortex.Core.Commands;
using Vortex.Core.Rules;

namespace Vortex.Client.Presentation
{
    /// <summary>What the player controls need from the table around them (implemented by <see cref="GameDirector"/>).</summary>
    public interface IControlsHost
    {
        /// <summary>The opponent seat at a screen position, or -1.</summary>
        int SeatAt(Vector2 screen);

        /// <summary>The panel of a seat, or null.</summary>
        RectTransform? SeatPanel(int seat);

        /// <summary>Sends a command of a seat to the session.</summary>
        void Submit(int seat, Command command);

        /// <summary>Highlights the seats a dragged action may target, or a decision offers (null: back to normal).</summary>
        void ShowTargets(IReadOnlyCollection<int>? seats);

        /// <summary>The person dropped this card where it goes: its trip is not animated (playtest 4).</summary>
        void PlacedByHand(int uid);

        /// <summary>Whether a card with this uid is on the table now (a decision among cards not shown opens its window).</summary>
        bool ShowsCard(int uid);

        /// <summary>Marks cards of the table (seats and markets) by uid with a colour (null: every card back to normal).</summary>
        void MarkCards(IReadOnlyDictionary<int, Color>? marks);

        /// <summary>What a command would likely do, computed by the engine (ADR-0018), or null.</summary>
        CommandPreview? Preview(int seat, Command command);

        /// <summary>Why a command is refused now, or null (ADR-0015: the reason comes from the engine).</summary>
        CommandError? Explain(int seat, Command command);
    }
}
