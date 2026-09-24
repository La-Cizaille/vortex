using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Vortex.Core.Commands;
using Vortex.Core.Config;
using Vortex.Core.Content;
using Vortex.Core.Decisions;
using Vortex.Core.Effects;
using Vortex.Core.Effects.Statuses;
using Vortex.Core.Events;
using Vortex.Core.Rules;
using Vortex.Core.State;
using Vortex.Core.Tests.Support;

namespace Vortex.Core.Tests.Engine
{
    /// <summary>Configurable base-rule options used by the balance work (RULES A4.1, A4.4, A5.4, A6, A9).</summary>
    [TestFixture]
    public class RuleOptionTests
    {
        [TestCase(RoundStartRotation.None, 0, 0, 0)]
        [TestCase(RoundStartRotation.Clockwise, 0, 1, 2)]
        [TestCase(RoundStartRotation.CounterClockwise, 0, 2, 1)]
        public void The_first_player_of_each_round_follows_the_rotation(RoundStartRotation rotation, int round1, int round2, int round3)
        {
            var s = Scenario.Start(3, config: TestContent.Config(rotation: rotation));
            int initiative = s.State.InitiativeSeat;
            var starts = new List<int> { s.Current };
            while (starts.Count < 3)
            {
                int round = s.State.Round;
                s.ToMain();
                s.MustAccept(s.Current, Command.EndTurn());
                if (s.State.Round != round)
                {
                    starts.Add(s.Current);
                }
            }

            Assert.That(starts, Is.EqualTo(new[] { round1, round2, round3 }.Select(o => (initiative + o) % 3)));
        }

        [Test]
        public void Counter_clockwise_rotation_gives_the_last_player_two_turns_in_a_row()
        {
            var s = Scenario.Start(3, config: TestContent.Config(rotation: RoundStartRotation.CounterClockwise));
            int last = (s.State.InitiativeSeat + 2) % 3;
            s.TurnOf(last);
            s.MustAccept(last, Command.EndTurn());
            Assert.That(s.State.Round, Is.EqualTo(2));
            Assert.That(s.Current, Is.EqualTo(last));
        }

        [Test]
        public void Rotation_skips_an_eliminated_starting_seat()
        {
            // Four seats, so that three players remain: with two, the duel rule decides who starts (ARB-68).
            var s = Scenario.Start(4, config: TestContent.Config(rotation: RoundStartRotation.Clockwise));
            int next = (s.State.InitiativeSeat + 1) % 4;
            s.P(next).Hp = 0;
            s.P(next).Eliminated = true;
            while (s.State.Round == 1)
            {
                int seat = s.Current;
                s.ToMain();
                s.MustAccept(seat, Command.EndTurn());
            }

            Assert.That(s.Current, Is.EqualTo((next + 1) % 4), "The round would start with the eliminated seat: the next alive one starts.");
        }

        [TestCase(2, RoundStartRotation.None)]
        [TestCase(2, RoundStartRotation.Clockwise)]
        [TestCase(2, RoundStartRotation.CounterClockwise)]
        [TestCase(5, RoundStartRotation.None)]
        [TestCase(5, RoundStartRotation.Clockwise)]
        [TestCase(5, RoundStartRotation.CounterClockwise)]
        public void With_two_players_left_nobody_plays_twice_in_a_row(int players, RoundStartRotation rotation)
        {
            // ARB-68: a duel, whether the game started with two players or three of five are out. The survivors of
            // the five-seat game are not neighbours, so the rotation has eliminated seats to skip.
            var s = Scenario.Start(players, config: TestContent.Config(rotation: rotation));
            var alive = new HashSet<int> { s.Current, (s.Current + (players == 2 ? 1 : 2)) % players };
            for (int seat = 0; seat < players; seat++)
            {
                if (!alive.Contains(seat))
                {
                    s.P(seat).Hp = 0;
                    s.P(seat).Eliminated = true;
                }
            }

            var turns = new List<int>();
            while (s.State.Round <= 8 && s.State.Outcome == null)
            {
                int seat = s.Current;
                turns.Add(seat);
                s.ToMain();
                s.MustAccept(seat, Command.EndTurn());
            }

            Assert.That(turns.Count, Is.GreaterThanOrEqualTo(16));
            for (int i = 1; i < turns.Count; i++)
            {
                Assert.That(turns[i], Is.Not.EqualTo(turns[i - 1]), "Turn " + i + " of " + string.Join(",", turns));
            }
        }

        [Test]
        public void The_galactic_election_needs_the_configured_number_of_technologies()
        {
            var s = Scenario.Start(2, config: TestContent.Config(technologiesToWin: 3));
            s.ToMain();
            int me = s.Current;
            s.P(me).Technologies.AddRange(new[] { TechColor.Red, TechColor.Green });
            s.Equip(me, "A_907");
            s.Equip(me, "D_907");
            s.MustAccept(me, Command.ActivateTechnology());
            Assert.That(s.State.Outcome!.Condition, Is.EqualTo(WinCondition.GalacticElection));
        }

        [Test]
        public void Configuration_rejects_impossible_technology_counts()
        {
            GameConfig config = TestContent.Config(technologiesToWin: 5);
            Assert.That(config.Validate(), Has.Some.Contains("technologiesToWin"));
        }

        // ---------------------------------------------------------------- Defensive posture (RULES A5.4)

        [Test]
        public void The_defensive_posture_is_refused_when_the_option_is_off()
        {
            var s = Scenario.Start(2);
            s.ToMain();
            string before = Scenario.Json(s.State);
            EngineResult r = s.Submit(s.Current, Command.DefensivePosture());
            Assert.That(r.Error!.Code, Is.EqualTo(CommandErrorCode.ActionNotAvailable));
            Assert.That(Scenario.Json(s.State), Is.EqualTo(before));
            Assert.That(s.Engine.LegalCommands(s.State, s.Current).Select(c => c.Type), Has.None.EqualTo(CommandType.DefensivePosture));
        }

        [Test]
        public void The_defensive_posture_raises_the_effective_shield_until_the_next_turn()
        {
            var s = Scenario.Start(2, config: TestContent.Config(defensivePostureBonus: 2));
            s.ToMain();
            int me = s.Current;
            int foe = 1 - me;
            s.MustAccept(me, Command.DefensivePosture());
            Assert.That(s.P(me).Shield, Is.EqualTo(4), "The shield value does not change.");
            Assert.That(s.Submit(me, Command.Attack(foe)).Error!.Code, Is.EqualTo(CommandErrorCode.NoCrewActionLeft), "It is the turn's crew action.");

            s.MustAccept(me, Command.EndTurn());
            s.ToMain();
            AttackInfo a = s.Game(6).ResolveAttack(foe, me, false);
            Assert.That(a.EffectiveShield, Is.EqualTo(6));
            Assert.That(a.Damage, Is.Zero);

            s.MustAccept(foe, Command.EndTurn());
            Assert.That(s.Current, Is.EqualTo(me));
            Assert.That(s.P(me).Statuses, Has.None.Matches<StatusState>(st => st.Kind == StatusKinds.DefensivePosture), "Expires at the start of the next turn.");
        }

        [Test]
        public void The_defensive_posture_counts_even_when_the_shield_is_disabled()
        {
            var s = Scenario.Start(2, config: TestContent.Config(defensivePostureBonus: 2));
            s.ToMain();
            int me = s.Current;
            int foe = 1 - me;
            s.MustAccept(me, Command.DefensivePosture());
            Game g = s.Game(5);
            g.AddStatus(me, BrickStatuses.ShieldDisabled, foe, StatusExpiry.EndOfTurn, foe);
            AttackInfo a = g.ResolveAttack(foe, me, false);
            Assert.That(a.EffectiveShield, Is.EqualTo(2), "Disabled shield (0) + posture (2), RULES B4: replace then add.");
            Assert.That(a.Damage, Is.EqualTo(3));
        }

        [Test]
        public void An_imposed_crew_action_can_be_the_defensive_posture_only_when_enabled()
        {
            foreach (int bonus in new[] { 0, 2 })
            {
                var catalog = new TestCatalog().Card("D_901", Scenario.Brick("DictateAttackerAction"));
                var s = Scenario.Start(2, catalog, TestContent.Config(defensivePostureBonus: bonus));
                s.ToMain();
                int me = s.Current;
                int foe = 1 - me;
                s.Equip(foe, "D_901");
                DecisionNeededException? asked = null;
                try
                {
                    s.Game(7).ResolveAttack(me, foe, false);
                }
                catch (DecisionNeededException e)
                {
                    asked = e;
                }

                Assert.That(asked, Is.Not.Null);
                Assert.That(asked!.Request.Options.Any(o => o.Key == "posture"), Is.EqualTo(bonus > 0));
            }
        }

        // ---------------------------------------------------------------- Bounty on the leader (RULES A6 step 6)

        [TestCase(0, 29, 2)]
        [TestCase(1, 29, 3)]
        [TestCase(1, 30, 2)]
        public void The_bounty_applies_only_against_the_sole_hp_leader(int bounty, int thirdHp, int expectedDamage)
        {
            var s = Scenario.Start(3, config: TestContent.Config(leaderBounty: bounty));
            s.ToMain();
            int me = s.Current;
            int foe = (me + 1) % 3;
            int third = (me + 2) % 3;
            s.P(me).Hp = 20;
            s.P(third).Hp = thirdHp;
            Game g = s.Game(6);
            AttackInfo a = g.ResolveAttack(me, foe, false);
            Assert.That(a.Damage, Is.EqualTo(expectedDamage), "Die 6 (+bounty) against shield 4.");
            Assert.That(g.Events.Any(e => e.Type == GameEventType.LeaderBountyApplied && e.Other == foe), Is.EqualTo(expectedDamage == 3));
        }

        [Test]
        public void Configuration_rejects_out_of_range_options()
        {
            Assert.That(TestContent.Config(defensivePostureBonus: 9).Validate(), Has.Some.Contains("defensivePostureBonus"));
            Assert.That(TestContent.Config(leaderBounty: -1).Validate(), Has.Some.Contains("leaderBounty"));
        }

        // ---------------------------------------------------------------- Ghosts choose the event (RULES A4.1)

        [Test]
        public void An_eliminated_player_chooses_the_event_among_two()
        {
            var s = Scenario.Start(3, config: TestContent.Config(ghostsChooseEvent: true));
            int ghost = (s.State.InitiativeSeat + 2) % 3;
            s.P(ghost).Hp = 0;
            s.P(ghost).Eliminated = true;
            int discarded = s.State.EventDiscard.Count;
            s.ToMain();
            s.MustAccept(s.Current, Command.EndTurn());
            s.ToMain();
            EngineResult r = s.MustAccept(s.Current, Command.EndTurn());

            DecisionRequest d = r.Decision!;
            Assert.That(d.Player, Is.EqualTo(ghost));
            Assert.That(d.Options.Select(o => o.ContentId), Is.EqualTo(new[] { TestContent.CalmEvent, TestContent.CalmEvent }));
            EngineResult done = s.MustAccept(ghost, Command.Answer(d.Id, "second"));
            Assert.That(s.State.Round, Is.EqualTo(2));
            Assert.That(s.State.ActiveEventId, Is.EqualTo(TestContent.CalmEvent));
            Assert.That(s.State.EventDiscard, Has.Count.EqualTo(discarded + 2), "Round 1's event and the one set aside.");
            Assert.That(done.Events.Any(e => e.Type == GameEventType.EventSetAside && e.Player == ghost), Is.True);
        }

        [Test]
        public void Without_ghosts_or_without_the_option_the_event_is_drawn_as_usual()
        {
            foreach ((bool option, bool eliminate) in new[] { (true, false), (false, true) })
            {
                var s = Scenario.Start(3, config: TestContent.Config(ghostsChooseEvent: option));
                if (eliminate)
                {
                    int seat = (s.State.InitiativeSeat + 2) % 3;
                    s.P(seat).Hp = 0;
                    s.P(seat).Eliminated = true;
                }

                for (int i = 0; s.State.Round == 1 && i < 3; i++)
                {
                    s.ToMain();
                    Assert.That(s.MustAccept(s.Current, Command.EndTurn()).Decision, Is.Null);
                }

                Assert.That(s.State.Round, Is.EqualTo(2));
            }
        }

        [Test]
        public void Ghosts_do_not_choose_at_the_doom_round()
        {
            var s = Scenario.Start(3, config: TestContent.Config(ghostsChooseEvent: true, doomRound: 2));
            int ghost = (s.State.InitiativeSeat + 2) % 3;
            s.P(ghost).Hp = 0;
            s.P(ghost).Eliminated = true;
            for (int i = 0; s.State.Round == 1 && i < 3; i++)
            {
                s.ToMain();
                Assert.That(s.MustAccept(s.Current, Command.EndTurn()).Decision, Is.Null);
            }

            Assert.That(s.State.ActiveEventId, Is.EqualTo(TestContent.DoomEvent));
        }
    }
}
