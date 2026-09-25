using System;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The time a person has to play (INTERFACE.md 3.9, ARB-80): a count for each turn, and a shorter one for a decision
    /// asked during another seat's turn. It only runs while the person can act (their controls offered, the game not
    /// paused, no event playing): animations and bots never eat into it. A turn keeps its remaining time while another
    /// person decides during it. Disabled when the turn time is 0.
    /// </summary>
    public sealed class TurnClock
    {
        private (int Round, int Seat) _turn = (-1, -1);
        private string? _decision;
        private float _turnLeft;
        private float _decisionLeft;

        /// <summary>Creates a clock; <paramref name="turnSeconds"/> 0 disables it.</summary>
        public TurnClock(float turnSeconds, float decisionSeconds)
        {
            TurnSeconds = Math.Max(0f, turnSeconds);
            DecisionSeconds = Math.Max(1f, decisionSeconds);
        }

        /// <summary>Time of a turn, in seconds (0: no limit).</summary>
        public float TurnSeconds { get; }

        /// <summary>Time of a decision asked during another seat's turn, in seconds.</summary>
        public float DecisionSeconds { get; }

        /// <summary>True when turns are timed.</summary>
        public bool Enabled => TurnSeconds > 0f;

        /// <summary>True while a count is followed (a person has to act).</summary>
        public bool Counting { get; private set; }

        /// <summary>True when the count followed is a decision's.</summary>
        public bool ForDecision => _decision != null;

        /// <summary>Seconds left in the count followed.</summary>
        public float Remaining => ForDecision ? _decisionLeft : _turnLeft;

        /// <summary>Length of the count followed, in seconds.</summary>
        public float Limit => ForDecision ? DecisionSeconds : TurnSeconds;

        /// <summary>True when the count followed has run out.</summary>
        public bool Expired => Enabled && Counting && Remaining <= 0f;

        /// <summary>
        /// Follows who has to act now: the turn of <paramref name="currentPlayer"/> in <paramref name="round"/>, or, when
        /// <paramref name="decisionId"/> is set and asked of another seat than the current player, that decision. A new
        /// turn or a new decision starts a full count.
        /// </summary>
        public void Follow(int round, int currentPlayer, int actor, string? decisionId)
        {
            Counting = Enabled;
            if ((round, currentPlayer) != _turn)
            {
                _turn = (round, currentPlayer);
                _turnLeft = TurnSeconds;
            }

            string? decision = decisionId != null && actor != currentPlayer ? decisionId : null;
            if (decision != _decision)
            {
                _decision = decision;
                _decisionLeft = DecisionSeconds;
            }
        }

        /// <summary>Nobody (or a bot) has to act: the count stops where it is.</summary>
        public void Stop() => Counting = false;

        /// <summary>Advances the count followed by <paramref name="deltaTime"/> seconds.</summary>
        public void Tick(float deltaTime)
        {
            if (!Counting || !Enabled || deltaTime <= 0f)
            {
                return;
            }

            if (ForDecision)
            {
                _decisionLeft = Math.Max(0f, _decisionLeft - deltaTime);
            }
            else
            {
                _turnLeft = Math.Max(0f, _turnLeft - deltaTime);
            }
        }
    }
}
