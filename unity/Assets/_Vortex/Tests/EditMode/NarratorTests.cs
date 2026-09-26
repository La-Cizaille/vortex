using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using Vortex.Client.Content;
using Vortex.Client.Presentation;
using Vortex.Core.Events;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>SINISTRA, the narrator (docs/DIRECTION_ARTISTIQUE.md 5.4, ARB-95): remarks from the texts, never from code.</summary>
    public class NarratorTests
    {
        private static TextTable Texts => AssetDatabase.LoadAssetAtPath<TextTable>(ThemeAssets.TextsPath);

        [Test]
        public void She_always_comments_an_elimination_with_one_of_her_lines()
        {
            var narrator = new Narrator(Texts, speaks: true, () => 0.99f);
            string? remark = narrator.RemarkOn(new GameEvent { Type = GameEventType.PlayerEliminated });
            Assert.That(remark, Is.Not.Null.And.Not.Empty);
            Assert.That(Texts.Get(TextKeys.Quip(GameEventType.PlayerEliminated)).Split(Narrator.Separator), Has.Member(remark));
        }

        [Test]
        public void She_keeps_quiet_when_turned_off_and_on_events_without_lines()
        {
            var silent = new Narrator(Texts, speaks: false, () => 0f);
            Assert.That(silent.RemarkOn(new GameEvent { Type = GameEventType.PlayerEliminated }), Is.Null, "Turned off.");
            Assert.That(silent.RemarkOnTurn(), Is.Null);

            var narrator = new Narrator(Texts, speaks: true, () => 0f);
            Assert.That(narrator.RemarkOn(new GameEvent { Type = GameEventType.TurnEnded }), Is.Null, "No lines for this event.");
        }

        [Test]
        public void An_attack_without_damage_has_its_own_lines_and_frequent_events_are_commented_about_half_the_time()
        {
            var narrator = new Narrator(Texts, speaks: true, () => 0f);
            string? missed = narrator.RemarkOn(new GameEvent { Type = GameEventType.AttackResolved, Amount = 0 });
            Assert.That(Texts.Get(TextKeys.QuipMissed).Split(Narrator.Separator), Has.Member(missed));

            var unlucky = new Narrator(Texts, speaks: true, () => 0.9f);
            Assert.That(unlucky.RemarkOn(new GameEvent { Type = GameEventType.AttackResolved, Amount = 5 }), Is.Null, "A frequent event: not every time.");
        }

        [Test]
        public void She_never_says_the_same_line_twice_in_a_row()
        {
            var narrator = new Narrator(Texts, speaks: true, () => 0f);
            var said = new List<string?>();
            for (int i = 0; i < 6; i++)
            {
                said.Add(narrator.RemarkOn(new GameEvent { Type = GameEventType.PlayerEliminated }));
            }

            for (int i = 1; i < said.Count; i++)
            {
                Assert.That(said[i], Is.Not.EqualTo(said[i - 1]));
            }
        }
    }
}
