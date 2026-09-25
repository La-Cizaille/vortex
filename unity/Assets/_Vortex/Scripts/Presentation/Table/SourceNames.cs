using System;
using Vortex.Client.Content;
using Vortex.Core.Rules;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The name of an effect's source as the player knows it (a bonus of a preview, what forbids a move): the title of a
    /// card, an event or a technology from the content, the name of a status or of the base rules from the interface
    /// texts. Plain text, shown without rich text.
    /// </summary>
    public static class SourceNames
    {
        /// <summary>The name of a source.</summary>
        public static string Of(TableContext context, SourceKind kind, string? id)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return kind switch
            {
                SourceKind.Rule => context.Texts.Get(TextKeys.PreviewBonusRule),
                SourceKind.Status => context.Texts.Get(TextKeys.Status(id ?? string.Empty)),
                _ => context.Face(id ?? string.Empty).Title,
            };
        }
    }
}
