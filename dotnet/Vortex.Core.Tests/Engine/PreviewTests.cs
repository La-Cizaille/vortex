using System.Linq;
using NUnit.Framework;
using Vortex.Core.Commands;
using Vortex.Core.Dice;
using Vortex.Core.Projection;
using Vortex.Core.Rules;
using Vortex.Core.State;
using Vortex.Core.Tests.Support;

namespace Vortex.Core.Tests.Engine
{
    /// <summary>Previews computed by the engine on guessed games (ADR-0018).</summary>
    [TestFixture]
    public class PreviewTests
    {
        private const int Samples = 2000;
        private const double Tolerance = 0.05;

        private static (Scenario S, int Me, int Foe) Duel(TestCatalog? catalog = null)
        {
            var s = Scenario.Start(2, catalog);
            s.ToMain();
            return (s, s.Current, 1 - s.Current);
        }

        private static CommandPreview Preview(Scenario s, int seat, Command command, ulong guesses = 7)
        {
            CommandPreview? preview = s.Engine.Preview(s.State, seat, command, Pcg32.Seeded(guesses, 3), Samples);
            Assert.That(preview, Is.Not.Null);
            return preview!;
        }

        [Test]
        public void A_plain_attack_shows_its_die_the_shield_and_the_damage_it_can_do()
        {
            // 1d8 against shield 4: 0 damage on 1-4, then 1, 2, 3, and 4 + 1 on a critical 8 without modifier (RULES A6).
            (Scenario s, int me, int foe) = Duel();
            AttackPreview attack = Preview(s, me, Command.Attack(foe)).Attack!;

            Assert.That((attack.DicePerThrow, attack.DiceKept, attack.NetAdvantage), Is.EqualTo((1, 1, 0)));
            Assert.That(attack.Bonus, Is.EqualTo(new Estimate(0, 0, 0)));
            Assert.That(attack.Bonuses, Is.Empty);
            Assert.That(attack.EffectiveShield.IsExact && attack.EffectiveShield.Min == 4, Is.True);
            Assert.That((attack.Damage.Min, attack.Damage.Max), Is.EqualTo((0, 5)));
            Assert.That(attack.Damage.Average, Is.EqualTo(11.0 / 8).Within(0.15));
            Assert.That(attack.Hit, Is.EqualTo(0.5).Within(Tolerance));
            Assert.That(attack.Critical, Is.EqualTo(0.125).Within(Tolerance));
            Assert.That(attack.Redirected, Is.Zero);
        }

        [Test]
        public void Every_active_effect_counts_since_the_real_rules_run()
        {
            var catalog = new TestCatalog().Card("A_901", new TestAttackBonus(4)).Card("A_902", new TestAdvantage(true));
            (Scenario s, int me, int foe) = Duel(catalog);
            s.Equip(me, "A_901");
            AttackPreview bonus = Preview(s, me, Command.Attack(foe)).Attack!;
            Assert.That(bonus.Bonus.IsExact && bonus.Bonus.Min == 4, Is.True);
            BonusPreview source = bonus.Bonuses.Single();
            Assert.That((source.Origin, source.Id, source.Amount.Min, source.Amount.Max), Is.EqualTo((BonusOrigin.Card, "A_901", 4, 4)), "Each bonus says where it comes from.");
            Assert.That(bonus.Hit, Is.EqualTo(1.0), "1d8 + 4 against 4 always hits.");

            s.Equip(me, "A_902");
            s.P(me).Statuses.Clear();
            s.P(me).Overcharge = 1;
            AttackPreview overcharged = Preview(s, me, Command.Attack(foe, useOvercharge: true)).Attack!;
            Assert.That((overcharged.DicePerThrow, overcharged.DiceKept, overcharged.NetAdvantage), Is.EqualTo((2, 2, 1)));
            Assert.That(overcharged.Damage.Average, Is.GreaterThan(bonus.Damage.Average - 4), "2d8 with advantage beats 1d8 + 4 on average.");
        }

        [Test]
        public void A_bonus_of_the_base_rules_is_shown_as_such()
        {
            var s = Scenario.Start(3, config: TestContent.Config(leaderBounty: 2));
            s.ToMain();
            int me = s.Current;
            int leader = (me + 1) % 3;
            s.P(me).Hp = 20;
            s.P((me + 2) % 3).Hp = 20;
            BonusPreview bounty = Preview(s, me, Command.Attack(leader)).Attack!.Bonuses.Single();
            Assert.That((bounty.Origin, bounty.Id, bounty.Amount.Min), Is.EqualTo((BonusOrigin.Rule, (string?)null, 2)));
        }

        [Test]
        public void The_preview_depends_only_on_public_information()
        {
            (Scenario s, int me, int foe) = Duel();
            GameState other = s.State.Clone();
            other.Rng = Pcg32.Seeded(424242, 9);
            other.AttackMarket.Deck.Reverse();
            other.EventDeck.Reverse();
            string before = Scenario.Json(s.State);

            CommandPreview real = Preview(s, me, Command.Attack(foe));
            CommandPreview? guessed = s.Engine.Preview(other, me, Command.Attack(foe), Pcg32.Seeded(7, 3), Samples);

            Assert.That(guessed!.Attack!.Damage, Is.EqualTo(real.Attack!.Damage), "Neither the generator nor the deck order is read.");
            Assert.That(guessed.Attack.Hit, Is.EqualTo(real.Attack.Hit));
            Assert.That(guessed.Seats.Select(p => p.Hp), Is.EqualTo(real.Seats.Select(p => p.Hp)));
            Assert.That(Scenario.Json(s.State), Is.EqualTo(before), "The state is not changed.");
        }

        [Test]
        public void A_guess_keeps_what_is_visible_and_hides_what_is_not()
        {
            (Scenario s, _, _) = Duel();
            GameState shuffled = s.State.Clone();
            shuffled.AttackMarket.Deck.Reverse();
            shuffled.Rng = Pcg32.Seeded(5, 5);

            GameState a = HiddenInformation.Guess(s.State, Pcg32.Seeded(1, 1));
            GameState b = HiddenInformation.Guess(shuffled, Pcg32.Seeded(1, 1));

            Assert.That(Scenario.Json(a), Is.EqualTo(Scenario.Json(b)), "Same public information, same guess.");
            Assert.That(Scenario.Json(GameView.Of(a)), Is.EqualTo(Scenario.Json(GameView.Of(s.State))), "Nothing visible changes.");
            Assert.That(a.Rng.State, Is.Not.EqualTo(s.State.Rng.State));
        }

        [Test]
        public void A_sabotage_shows_the_shield_it_can_leave_and_starts_no_attack()
        {
            (Scenario s, int me, int foe) = Duel();
            CommandPreview preview = Preview(s, me, Command.Sabotage(foe));
            Assert.That(preview.Attack, Is.Null);
            Assert.That((preview.Seats[foe].Shield.Min, preview.Seats[foe].Shield.Max), Is.EqualTo((1, 8)));
            Assert.That(preview.Seats[foe].Shield.Average, Is.EqualTo(4.5).Within(0.2));
            Assert.That(preview.Seats[foe].Hp.IsExact, Is.True);
        }

        [Test]
        public void An_attack_that_can_destroy_the_target_says_how_likely()
        {
            (Scenario s, int me, int foe) = Duel();
            s.P(foe).Hp = 2;
            CommandPreview preview = Preview(s, me, Command.Attack(foe));
            Assert.That(preview.Seats[foe].Eliminated, Is.EqualTo(3.0 / 8).Within(Tolerance), "6, 7 or 8 against shield 4.");
            Assert.That(preview.Attack!.Damage.Max, Is.EqualTo(2), "No more than the hit points left.");
        }

        [Test]
        public void No_preview_for_an_illegal_command()
        {
            var s = Scenario.Start(2);
            int me = s.Current;
            Assert.That(s.Engine.Preview(s.State, me, Command.Attack(1 - me), Pcg32.Seeded(1, 1)), Is.Null, "Market phase.");
            s.ToMain();
            Assert.That(s.Engine.Preview(s.State, me, Command.Attack(me), Pcg32.Seeded(1, 1)), Is.Null, "Not oneself.");
            Assert.That(s.Engine.Preview(s.State, 1 - me, Command.Attack(me), Pcg32.Seeded(1, 1)), Is.Null, "Not one's turn.");
        }
    }
}
