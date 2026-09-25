using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Vortex.Client.Content;
using Vortex.Client.Presentation;
using Vortex.Core.Commands;
using Vortex.Core.Rules;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>What the engine refuses says why (INTERFACE.md 1, 3.4 and 3.5): the reason always comes from the engine.</summary>
    public class ReasonsTests
    {
        private GameDirector _director = null!;
        private TextTable _texts = null!;

        private PlayerControls Controls => _director.Controls;

        [SetUp]
        public void OpenGame()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            _director = scene.GetRootGameObjects().Select(o => o.GetComponent<GameDirector>()).Single(d => d != null);
            _director.HumanFirstSeat = true;
            _director.ShowCommandPanel = false;
            _director.Begin();
            _texts = AssetDatabase.LoadAssetAtPath<TextTable>(ThemeAssets.TextsPath);
        }

        [TearDown]
        public void CloseGame() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void During_the_market_the_actions_say_why_they_wait()
        {
            PlayUntilOffered();
            Assume.That(Controls.CanEndMarket, Is.True, "The person starts at the market.");
            string wrongPhase = _texts.Get(TextKeys.Refusal(CommandErrorCode.WrongPhase));
            ActionButton attack = Controls.Actions.Single(a => a.Action == CrewAction.Attack);

            Controls.ShowActionHelp(CrewAction.Attack, (RectTransform)attack.transform);
            Assert.That(Controls.Help.Text, Does.StartWith(_texts.Get(TextKeys.ActionHelp(CrewAction.Attack))).And.EndWith(wrongPhase));
            int opponent = _director.Seats.Keys.First(s => s != _director.Viewer);
            Assert.That(Controls.ReasonOn(CrewAction.Attack, opponent), Is.EqualTo(wrongPhase));
        }

        [Test]
        public void A_market_card_that_cannot_be_taken_says_why_until_released()
        {
            PlayUntilOffered();
            if (Controls.CanEndMarket)
            {
                Controls.EndMarket();
                PlayUntilOffered();
            }

            Rect market = CardAnchor.ScreenRectOf((RectTransform)Object.FindAnyObjectByType<MarketDisplay>().transform);
            CardDrag card = Object.FindObjectsByType<CardDrag>().First(d => market.Contains(d.GetComponent<CardAnchor>().ScreenRect.center));

            var pointer = new PointerEventData(null);
            card.OnBeginDrag(pointer);
            Assert.That(pointer.pointerDrag, Is.Null, "The card stays in the market.");
            Assert.That(Controls.Help.Text, Is.EqualTo(_texts.Get(TextKeys.Refusal(CommandErrorCode.WrongPhase))));
            card.OnPointerUp(pointer);
            Assert.That(Controls.Help.Text, Is.Empty);
        }

        [Test]
        public void A_refusal_names_the_card_that_forbids_the_move()
        {
            var refusals = new RefusalText(new TableContext(
                AssetDatabase.LoadAssetAtPath<Vortex.Client.Theme.ThemeSettings>(ThemeAssets.ThemePath),
                AssetDatabase.LoadAssetAtPath<Vortex.Client.Theme.CardArtCatalog>(ThemeAssets.CardArtPath),
                _texts,
                AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath).LoadData(),
                AssetDatabase.LoadAssetAtPath<GameObject>(ThemeAssets.CardPrefabPath).GetComponent<CardDisplay>(),
                new GameObject("Cartes").transform,
                Object.FindAnyObjectByType<Camera>(),
                6f));
            var error = new CommandError(CommandErrorCode.TargetNotAllowed, "test", SourceKind.Status, "CannotTargetPlayer");

            Assert.That(refusals.Describe(null), Is.Null);
            Assert.That(refusals.Describe(error), Is.EqualTo(_texts.Get(TextKeys.Refusal(CommandErrorCode.TargetNotAllowed)) + " Cause : " + _texts.Get(TextKeys.Status("CannotTargetPlayer")) + "."));
        }

        private void PlayUntilOffered()
        {
            for (int frame = 0; frame < 20000 && !Controls.Offered && !_director.Session!.IsOver; frame++)
            {
                _director.Advance(2f);
            }

            Assume.That(Controls.Offered, Is.True, "The person's turn came.");
        }
    }
}
