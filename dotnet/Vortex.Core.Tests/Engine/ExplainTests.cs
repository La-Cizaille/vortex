using NUnit.Framework;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Dice;
using Vortex.Core.Rules;
using Vortex.Core.State;
using Vortex.Core.Tests.Support;

namespace Vortex.Core.Tests.Engine
{
    /// <summary>Why a move is refused, shown next to what the interface greys out (INTERFACE.md 1).</summary>
    [TestFixture]
    public class ExplainTests
    {
        [Test]
        public void A_legal_move_needs_no_explanation_and_an_illegal_one_gives_the_submit_error()
        {
            var s = Scenario.Start(2);
            int me = s.Current;
            Assert.That(s.Engine.Explain(s.State, me, Command.EndMarket()), Is.Null);

            CommandError? early = s.Engine.Explain(s.State, me, Command.Attack(1 - me));
            Assert.That(early!.Code, Is.EqualTo(CommandErrorCode.WrongPhase));
            Assert.That(early.Code, Is.EqualTo(s.Engine.Submit(s.State, me, Command.Attack(1 - me)).Error!.Code), "The same reason as a submit.");
            Assert.That(s.Engine.Explain(s.State, 1 - me, Command.EndMarket())!.Code, Is.EqualTo(CommandErrorCode.NotYourTurn));
            Assert.That(early.SourceKind, Is.Null, "No effect is involved.");
        }

        [Test]
        public void A_protected_target_names_what_protects_it()
        {
            var catalog = new TestCatalog().Card("D_901", new TestUntargetable());
            var s = Scenario.Start(2, catalog);
            s.ToMain();
            int me = s.Current;
            int foe = 1 - me;
            s.Equip(foe, "D_901");

            CommandError? refused = s.Engine.Explain(s.State, me, Command.Attack(foe));
            Assert.That(refused!.Code, Is.EqualTo(CommandErrorCode.TargetNotAllowed));
            Assert.That((refused.SourceKind, refused.SourceId), Is.EqualTo(((SourceKind?)SourceKind.Card, "D_901")));
        }

        [Test]
        public void A_protected_shield_names_what_protects_it()
        {
            var catalog = new TestCatalog().Card("D_901", new TestDenyShieldChange());
            var s = Scenario.Start(2, catalog);
            s.ToMain();
            int me = s.Current;
            s.Equip(1 - me, "D_901");

            CommandError? refused = s.Engine.Explain(s.State, me, Command.Sabotage(1 - me));
            Assert.That(refused!.Code, Is.EqualTo(CommandErrorCode.ShieldChangeNotAllowed));
            Assert.That(refused.SourceId, Is.EqualTo("D_901"));
        }

        [Test]
        public void Explaining_changes_nothing_and_reads_no_hidden_information()
        {
            var s = Scenario.Start(2);
            s.ToMain();
            int me = s.Current;
            string before = Scenario.Json(s.State);
            GameState other = s.State.Clone();
            other.Rng = Pcg32.Seeded(77, 7);
            other.AttackMarket.Deck.Reverse();

            foreach (Command command in new[] { Command.Attack(me), Command.Attack(1 - me), Command.RecycleMarket(CardSlot.Attack), Command.EndTurn() })
            {
                Assert.That(s.Engine.Explain(other, me, command)?.Code, Is.EqualTo(s.Engine.Explain(s.State, me, command)?.Code), command.Type.ToString());
            }

            Assert.That(Scenario.Json(s.State), Is.EqualTo(before));
        }
    }
}
