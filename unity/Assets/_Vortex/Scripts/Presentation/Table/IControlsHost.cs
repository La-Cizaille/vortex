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

        /// <summary>Highlights the seats a dragged action may target (null: back to normal).</summary>
        void ShowTargets(IReadOnlyCollection<int>? seats);

        /// <summary>What a command would likely do, computed by the engine (ADR-0018), or null.</summary>
        CommandPreview? Preview(int seat, Command command);
    }
}
