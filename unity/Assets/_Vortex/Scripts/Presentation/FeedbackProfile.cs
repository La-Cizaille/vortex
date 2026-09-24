using System;
using System.Collections.Generic;
using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Which feedback plays for which kind of game event. Edited in Unity; an event without an entry plays the
    /// fallback, so a new kind of event never breaks the presentation.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Profil", fileName = "FeedbackProfile")]
    public sealed class FeedbackProfile : ScriptableObject
    {
        [SerializeField] private List<Entry> entries = new List<Entry>();
        [SerializeField] private FeedbackAsset? fallback;

        /// <summary>The feedback of an event type, or the fallback (null: nothing to play).</summary>
        public IFeedback? For(GameEventType type)
        {
            foreach (Entry entry in entries)
            {
                if (entry.EventType == type && entry.Feedback != null)
                {
                    return entry.Feedback;
                }
            }

            return fallback;
        }

        /// <summary>
        /// Plays <paramref name="feedback"/> for <paramref name="type"/> unless the profile already has a feedback for
        /// it (editor setup: never replaces the designer's choice). Returns false when nothing was added.
        /// </summary>
        public bool MapIfMissing(GameEventType type, FeedbackAsset feedback)
        {
            if (entries.Exists(e => e.EventType == type && e.Feedback != null))
            {
                return false;
            }

            entries.Add(new Entry { EventType = type, Feedback = feedback });
            return true;
        }

        /// <summary>Replaces the mapping (editor setup and tests).</summary>
        public void Configure(FeedbackAsset? fallbackFeedback, params (GameEventType Type, FeedbackAsset Feedback)[] mapping)
        {
            fallback = fallbackFeedback;
            entries = new List<Entry>();
            foreach ((GameEventType type, FeedbackAsset feedback) in mapping)
            {
                entries.Add(new Entry { EventType = type, Feedback = feedback });
            }
        }

        /// <summary>One line of the profile.</summary>
        [Serializable]
        public sealed class Entry
        {
            /// <summary>Kind of event.</summary>
            public GameEventType EventType;

            /// <summary>What plays for it.</summary>
            public FeedbackAsset? Feedback;
        }
    }
}
