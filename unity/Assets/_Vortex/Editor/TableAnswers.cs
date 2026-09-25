using System.Linq;
using Vortex.Client.Presentation;

namespace Vortex.Editor
{
    /// <summary>Answers a pending decision the way a person would (ARB-82), for tests and captures that play games.</summary>
    public static class TableAnswers
    {
        /// <summary>
        /// Gives the first answer of the pending decision: on the table (a seat, a card, a die face, a card in the middle,
        /// an action, a card dragged into the middle), or in the window of a decision the table cannot show. False when no
        /// decision is offered.
        /// </summary>
        public static bool Answer(PlayerControls controls)
        {
            if (controls.Decision.gameObject.activeSelf && controls.Decision.Options.Count > 0)
            {
                controls.Decision.Choose(0);
                return true;
            }

            DecisionChoices? choices = controls.Choices;
            if (choices is null)
            {
                return false;
            }

            if (choices.Seats.Count > 0)
            {
                return controls.ChooseSeat(choices.Seats.Keys.First());
            }

            if (choices.Cards.Count > 0)
            {
                return controls.ChooseCard(choices.Cards.Keys.First());
            }

            if (choices.Numbers.Count > 0)
            {
                return controls.ChooseNumber(choices.Numbers[0].Number);
            }

            if (choices.Middle.Count > 0)
            {
                return controls.ChooseMiddle(0);
            }

            if (choices.Actions.Count > 0)
            {
                return controls.UseAction(choices.Actions.Keys.First());
            }

            if (choices.AimedActions.Count > 0)
            {
                var aimed = choices.AimedActions.Keys.First();
                return controls.UseActionOn(aimed.Action, aimed.Seat);
            }

            return choices.DropsInMiddle.Count > 0
                && controls.UseCard(choices.DropsInMiddle.Keys.First(), CardAnchor.ScreenRectOf(controls.ActivationZone).center);
        }
    }
}
