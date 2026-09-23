using System;
using Vortex.Core.Effects;
using Vortex.Core.Tests.Support;

namespace Vortex.Core.Tests.Bricks
{
    /// <summary>
    /// Brick tests run on synthetic content (cards A_9xx / D_9xx) with bricks compiled from the production catalog,
    /// so they test the brick's generic behaviour, not a particular card (ADR-0007).
    /// </summary>
    public abstract class BrickTestBase
    {
        /// <summary>Starts a game with <paramref name="cardEffects"/> assigned to test cards, in the current player's main phase.</summary>
        internal static (Scenario S, int Me, int Foe) Setup(int players, params (string CardId, Effect Effect)[] cardEffects)
        {
            var catalog = new TestCatalog();
            foreach ((string id, Effect effect) in cardEffects)
            {
                catalog.Card(id, effect);
            }

            var s = Scenario.Start(players, catalog);
            s.ToMain();
            return (s, s.Current, (s.Current + 1) % players);
        }

        internal static Effect B(string name, params (string Key, object Value)[] parameters) => Scenario.Brick(name, parameters);

        internal static string P(int seat) => "p" + seat;

        internal static string C(int uid) => "c" + uid;

        internal static string N(int n) => "n" + n.ToString(System.Globalization.CultureInfo.InvariantCulture);

        internal static string[] Answers(params string[] keys) => keys;

        internal static int Next(int seat, int players) => (seat + 1) % players;

        internal static Exception? Catch(Action action)
        {
            try
            {
                action();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
