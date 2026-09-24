namespace Vortex.Client.Presentation
{
    /// <summary>What the test mode's command panel shows.</summary>
    internal enum PanelState
    {
        /// <summary>Nothing (playback running, or game over).</summary>
        Hidden = 0,

        /// <summary>A message while the bots play.</summary>
        Waiting = 1,

        /// <summary>The moves or answers of the person who must act.</summary>
        Choices = 2,
    }
}
