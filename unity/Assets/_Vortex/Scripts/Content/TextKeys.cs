using System.Collections.Generic;
using Vortex.Core.Commands;
using Vortex.Core.Events;
using Vortex.Core.State;

namespace Vortex.Client.Content
{
    /// <summary>
    /// Keys of the interface texts, with their default French text. The defaults only fill a new or incomplete
    /// <see cref="TextTable"/> asset; what the game shows is the asset's text. Texts with placeholders use
    /// <c>{0}</c>, <c>{1}</c>… as documented on each key.
    /// </summary>
    public static class TextKeys
    {
        /// <summary>Attack modifier (card caption, market label).</summary>
        public const string SlotAttack = "card.slot.attack";

        /// <summary>Defense modifier (card caption, market label).</summary>
        public const string SlotDefense = "card.slot.defense";

        /// <summary>Durable card (card caption).</summary>
        public const string UsageDurable = "card.usage.durable";

        /// <summary>Single-use card (card caption).</summary>
        public const string UsageSingleUse = "card.usage.single-use";

        /// <summary>Triggered card (card caption).</summary>
        public const string UsageTriggered = "card.usage.triggered";

        /// <summary>Event card (card caption).</summary>
        public const string KindEvent = "card.kind.event";

        /// <summary>Technology card (card caption).</summary>
        public const string KindTechnology = "card.kind.technology";

        /// <summary>Gallery section: attack modifiers.</summary>
        public const string GalleryAttack = "gallery.attack";

        /// <summary>Gallery section: defense modifiers.</summary>
        public const string GalleryDefense = "gallery.defense";

        /// <summary>Gallery section: events.</summary>
        public const string GalleryEvents = "gallery.events";

        /// <summary>Gallery section: technologies.</summary>
        public const string GalleryTechnologies = "gallery.technologies";

        /// <summary>Round number. {0}: round.</summary>
        public const string BannerRound = "banner.round";

        /// <summary>No event this round.</summary>
        public const string BannerNoEvent = "banner.no-event";

        /// <summary>Rounds before the doom event. {0}: rounds left.</summary>
        public const string BannerDoomIn = "banner.doom-in";

        /// <summary>The doom event has come.</summary>
        public const string BannerDoomPassed = "banner.doom-passed";

        /// <summary>Winner. {0}: player, {1}: how (win condition).</summary>
        public const string BannerWinner = "banner.winner";

        /// <summary>Draw.</summary>
        public const string BannerDraw = "banner.draw";

        /// <summary>Hit points. {0}: HP.</summary>
        public const string SeatHp = "seat.hp";

        /// <summary>Shield. {0}: shield.</summary>
        public const string SeatShield = "seat.shield";

        /// <summary>Name of a seat before the local game menu exists. {0}: seat number (1-based).</summary>
        public const string SeatDefaultName = "seat.default-name";

        /// <summary>Eliminated seat.</summary>
        public const string SeatEliminated = "seat.eliminated";

        /// <summary>Marker of the sole HP leader (leader bounty option).</summary>
        public const string SeatLeader = "seat.leader";

        /// <summary>Cards left in a market's deck. {0}: count.</summary>
        public const string MarketDeck = "market.deck";

        /// <summary>Game log button.</summary>
        public const string LogButton = "log.button";

        /// <summary>A seat that is not a player (a market, nobody).</summary>
        public const string LogNobody = "log.nobody";

        /// <summary>Playback speed button. {0}: speed factor.</summary>
        public const string PlaybackSpeed = "playback.speed";

        /// <summary>Skip the playback.</summary>
        public const string PlaybackSkip = "playback.skip";

        /// <summary>Every key with its default text.</summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> Defaults = BuildDefaults();

        /// <summary>
        /// Game log line of an event type (empty text: not logged). {0}: player, {1}: other player, {2}: amount,
        /// {3}: value, {4}: name of the card, event, technology, status, action or win condition, {5}: dice.
        /// </summary>
        public static string Log(GameEventType type) => "log." + type;

        /// <summary>Name of a status kind (engine id).</summary>
        public static string Status(string kind) => "status." + kind;

        /// <summary>Name of a crew action.</summary>
        public static string Crew(CrewAction action) => "crew." + action;

        /// <summary>Name of a way to win.</summary>
        public static string Win(WinCondition condition) => "win." + condition;

        private static List<KeyValuePair<string, string>> BuildDefaults()
        {
            var texts = new List<KeyValuePair<string, string>>();
            void Add(string key, string text) => texts.Add(new KeyValuePair<string, string>(key, text));

            Add(SlotAttack, "ATK");
            Add(SlotDefense, "DEF");
            Add(UsageDurable, "Durable");
            Add(UsageSingleUse, "Usage unique");
            Add(UsageTriggered, "Déclenchement");
            Add(KindEvent, "Événement");
            Add(KindTechnology, "Technologie");
            Add(GalleryAttack, "Modificateurs d'attaque");
            Add(GalleryDefense, "Modificateurs de défense");
            Add(GalleryEvents, "Événements");
            Add(GalleryTechnologies, "Technologies");

            Add(BannerRound, "Manche {0}");
            Add(BannerNoEvent, "Pas d'événement");
            Add(BannerDoomIn, "Fin des temps dans {0} manches");
            Add(BannerDoomPassed, "Fin des temps passée");
            Add(BannerWinner, "Victoire de {0} : {1}");
            Add(BannerDraw, "Égalité");
            Add(SeatDefaultName, "Joueur {0}");
            Add(SeatHp, "PV {0}");
            Add(SeatShield, "Bouclier {0}");
            Add(SeatEliminated, "Éliminé");
            Add(SeatLeader, "Leader");
            Add(MarketDeck, "Pioche : {0}");
            Add(LogButton, "Journal");
            Add(LogNobody, "—");
            Add(PlaybackSpeed, "Vitesse ×{0}");
            Add(PlaybackSkip, "Passer");

            Add(Log(GameEventType.GameStarted), "Nouvelle partie à {2} joueurs.");
            Add(Log(GameEventType.InitiativeRolled), "{0} lance l'initiative : {3}.");
            Add(Log(GameEventType.InitiativeWon), "{0} gagne l'initiative.");
            Add(Log(GameEventType.RoundStarted), "Manche {3}.");
            Add(Log(GameEventType.EventRevealed), "Événement : {4}.");
            Add(Log(GameEventType.TurnStarted), "Tour de {0}.");
            Add(Log(GameEventType.MarketEnded), string.Empty);
            Add(Log(GameEventType.MarketCardTaken), "{0} prend {4}.");
            Add(Log(GameEventType.MarketRecycled), "Le marché {4} est recyclé.");
            Add(Log(GameEventType.MarketCardRevealed), string.Empty);
            Add(Log(GameEventType.CardEquipped), string.Empty);
            Add(Log(GameEventType.CardDiscarded), "{4} ({0}) est défaussée.");
            Add(Log(GameEventType.CardStolen), "{0} prend {4} à {1}.");
            Add(Log(GameEventType.CardActivated), "{0} utilise {4}.");
            Add(Log(GameEventType.TechnologyActivated), "{0} active la technologie {4}.");
            Add(Log(GameEventType.AttackDeclared), "{0} attaque {1}.");
            Add(Log(GameEventType.AttackRedirected), "L'attaque est déviée vers {0}.");
            Add(Log(GameEventType.DiceRolled), "Dés de {0} : {5} (total gardé {2}).");
            Add(Log(GameEventType.CriticalHit), "Coup critique sur {1}.");
            Add(Log(GameEventType.AttackResolved), "Attaque de {3} contre {1} : {2} dégâts.");
            Add(Log(GameEventType.HpLost), "{0} perd {2} PV.");
            Add(Log(GameEventType.HpGained), "{0} gagne {2} PV.");
            Add(Log(GameEventType.ShieldChanged), "Bouclier de {0} : {3}.");
            Add(Log(GameEventType.ShieldChangeRefused), "Le bouclier de {0} ne peut pas être modifié.");
            Add(Log(GameEventType.OverchargeChanged), "Surcharge de {0} : {3}.");
            Add(Log(GameEventType.TormentPlaced), "Jeton de Tourment posé (carte de {0}).");
            Add(Log(GameEventType.TormentsRemoved), "{2} jetons de Tourment retirés (cartes de {0}).");
            Add(Log(GameEventType.StatusAdded), "Effet sur {0} : {4}.");
            Add(Log(GameEventType.StatusEnded), string.Empty);
            Add(Log(GameEventType.PlayerEliminated), "{0} est éliminé.");
            Add(Log(GameEventType.CrewActionPerformed), string.Empty);
            Add(Log(GameEventType.TurnEnded), string.Empty);
            Add(Log(GameEventType.GameOver), "Fin de partie : {4} ({0}).");
            Add(Log(GameEventType.DieRolled), "{0} lance un dé : {3}.");
            Add(Log(GameEventType.EffectTriggered), string.Empty);
            Add(Log(GameEventType.LeaderBountyApplied), "Prime sur le leader : +{2} contre {1}.");
            Add(Log(GameEventType.EventSetAside), "{0} écarte l'événement {4}.");

            Add(Crew(CrewAction.Attack), "Attaque");
            Add(Crew(CrewAction.RerollShield), "Reparamétrage");
            Add(Crew(CrewAction.Sabotage), "Sabotage");
            Add(Crew(CrewAction.Overcharge), "Surcharge");
            Add(Crew(CrewAction.DefensivePosture), "Posture défensive");

            Add(Win(WinCondition.Domination), "Domination");
            Add(Win(WinCondition.GalacticElection), "Élection galactique");
            Add(Win(WinCondition.Draw), "Égalité");

            // Engine status kinds (RULES B2); an unknown kind shows "#status.Kind" until its text is added.
            Add(Status("ShieldDisabled"), "Bouclier désactivé");
            Add(Status("NextAttackBonus"), "Bonus à la prochaine attaque");
            Add(Status("NextAttackBonusPerNeutral"), "Bonus par carte neutre");
            Add(Status("NextAttackDamageMultiplier"), "Dégâts multipliés");
            Add(Status("NextAttackOvercharged"), "Prochaine attaque surchargée");
            Add(Status("NextAttackAllIn"), "Tapis");
            Add(Status("SwapOnNextAttack"), "Échange à la prochaine attaque");
            Add(Status("KeepOvercharge"), "Garde sa surcharge");
            Add(Status("CannotTargetPlayer"), "Cible interdite");
            Add(Status("RedirectAttacks"), "Dévie les attaques");
            Add(Status("ExtraCrewActions"), "Actions d'équipage en plus");
            Add(Status("OverchargedAttackAdvantage"), "Avantage en surcharge");
            Add(Status("TormentValueBonus"), "Tourment renforcé");
            Add(Status("ForcedCrewAction"), "Action imposée");
            Add(Status("PreRolledDie"), "Dé lancé à l'avance");
            Add(Status("DefensivePosture"), "Posture défensive");
            return texts;
        }
    }
}
