using System;
using System.Globalization;
using Vortex.Client.Content;
using Vortex.Core.Rules;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Words for why the engine refuses a move (INTERFACE.md 1: what is greyed out says why): a text per refusal code,
    /// with the name of the card or status that forbids it when an effect does. The reason itself always comes from the
    /// engine (<see cref="GameEngine.Explain"/>).
    /// </summary>
    public sealed class RefusalText
    {
        private readonly TableContext _context;

        /// <summary>Creates the words of a game.</summary>
        public RefusalText(TableContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

        /// <summary>The reason of a refusal, or null when the move is allowed.</summary>
        public string? Describe(CommandError? error)
        {
            if (error is null)
            {
                return null;
            }

            TextTable texts = _context.Texts;
            string reason = texts.Get(TextKeys.Refusal(error.Code));
            return error.SourceKind is SourceKind kind
                ? string.Format(CultureInfo.InvariantCulture, texts.Get(TextKeys.RefusalSource), reason, SourceNames.Of(_context, kind, error.SourceId))
                : reason;
        }
    }
}
