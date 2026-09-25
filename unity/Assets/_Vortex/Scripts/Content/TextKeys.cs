using System.Collections.Generic;
using Vortex.Core.Bots;
using Vortex.Core.Commands;
using Vortex.Core.Events;
using Vortex.Core.Rules;
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

        /// <summary>Command panel title on a human's turn (test mode). {0}: player.</summary>
        public const string PanelYourTurn = "panel.your-turn";

        /// <summary>Command panel title while waiting for the bots.</summary>
        public const string PanelWaiting = "panel.waiting";

        /// <summary>Take a market card. {0}: card, {1}: market.</summary>
        public const string CommandPick = "command.pick";

        /// <summary>Recycle a market. {0}: market.</summary>
        public const string CommandRecycle = "command.recycle";

        /// <summary>Leave the market phase.</summary>
        public const string CommandEndMarket = "command.end-market";

        /// <summary>Use a card. {0}: card.</summary>
        public const string CommandActivate = "command.activate";

        /// <summary>Activate the combo.</summary>
        public const string CommandTechnology = "command.technology";

        /// <summary>Attack. {0}: target.</summary>
        public const string CommandAttack = "command.attack";

        /// <summary>Attack spending the overcharge token. {0}: target.</summary>
        public const string CommandAttackOvercharged = "command.attack-overcharged";

        /// <summary>Reroll one's shield.</summary>
        public const string CommandReroll = "command.reroll";

        /// <summary>Reroll one's shield spending the overcharge token.</summary>
        public const string CommandRerollOvercharged = "command.reroll-overcharged";

        /// <summary>Sabotage. {0}: target.</summary>
        public const string CommandSabotage = "command.sabotage";

        /// <summary>Take an overcharge token.</summary>
        public const string CommandOvercharge = "command.overcharge";

        /// <summary>Defensive posture (rule option).</summary>
        public const string CommandPosture = "command.posture";

        /// <summary>End the turn.</summary>
        public const string CommandEndTurn = "command.end-turn";

        /// <summary>Where a card offered in a decision lies, when it is in a market.</summary>
        public const string OptionMarket = "option.market";

        /// <summary>End turn button.</summary>
        public const string ButtonEndTurn = "button.end-turn";

        /// <summary>Button leaving the market phase.</summary>
        public const string ButtonEndMarket = "button.end-market";

        /// <summary>Button opening the folded market (ARB-81).</summary>
        public const string ButtonMarketOpen = "button.market-open";

        /// <summary>Button folding the open market (ARB-81).</summary>
        public const string ButtonMarketFold = "button.market-fold";

        /// <summary>Button recycling a market.</summary>
        public const string ButtonRecycle = "button.recycle";

        /// <summary>Combo button.</summary>
        public const string ButtonCombo = "button.combo";

        /// <summary>Help of the overcharge token of the player.</summary>
        public const string HelpOvercharge = "help.overcharge";

        /// <summary>Help of the combo button.</summary>
        public const string HelpCombo = "help.combo";

        /// <summary>Marker of an eliminated player who chooses the events (ghosts option).</summary>
        public const string SeatGhost = "seat.ghost";

        /// <summary>Help of the combo button when a combo is ready. {0}: technology, {1}: its effect.</summary>
        public const string HelpComboReady = "help.combo-ready";

        /// <summary>Preview, dice of an attack. {0}: dice per throw, {1}: faces.</summary>
        public const string PreviewDice = "preview.dice";

        /// <summary>Preview, dice of an attack keeping the best ones. {0}: dice per throw, {1}: faces, {2}: dice kept.</summary>
        public const string PreviewDiceKept = "preview.dice-kept";

        /// <summary>Preview, added to the dice when thrown with advantage.</summary>
        public const string PreviewAdvantage = "preview.advantage";

        /// <summary>Preview, added to the dice when thrown with disadvantage.</summary>
        public const string PreviewDisadvantage = "preview.disadvantage";

        /// <summary>Preview, effective shield of the target. {0}: value or range.</summary>
        public const string PreviewShield = "preview.shield";

        /// <summary>Preview, bonuses and maluses added to the dice. {0}: the list of <see cref="PreviewBonusItem"/>.</summary>
        public const string PreviewBonuses = "preview.bonuses";

        /// <summary>Preview, one bonus. {0}: where it comes from, {1}: signed amount or range.</summary>
        public const string PreviewBonusItem = "preview.bonus-item";

        /// <summary>Preview, name of a bonus given by the base rules (a rule option).</summary>
        public const string PreviewBonusRule = "preview.bonus-rule";

        /// <summary>Preview, damage. {0}: range, {1}: average.</summary>
        public const string PreviewDamage = "preview.damage";

        /// <summary>Preview, damage when it is certain. {0}: damage.</summary>
        public const string PreviewDamageExact = "preview.damage-exact";

        /// <summary>Preview, chances. {0}: hit, {1}: critical.</summary>
        public const string PreviewChances = "preview.chances";

        /// <summary>Preview, chances when the target may be destroyed. {0}: hit, {1}: critical, {2}: destroyed.</summary>
        public const string PreviewChancesDestroyed = "preview.chances-destroyed";

        /// <summary>Preview, the target may redirect the attack.</summary>
        public const string PreviewRedirect = "preview.redirect";

        /// <summary>Preview, HP the acting player may lose. {0}: most HP lost.</summary>
        public const string PreviewSelfLoss = "preview.self-loss";

        /// <summary>Preview, shield of a sabotaged target. {0}: now, {1}: average after, {2}: range after.</summary>
        public const string PreviewSabotage = "preview.sabotage";

        /// <summary>Preview, a range of values. {0}: lowest, {1}: highest.</summary>
        public const string PreviewRange = "preview.range";

        /// <summary>Preview, a chance. {0}: percentage.</summary>
        public const string PreviewPercent = "preview.percent";

        /// <summary>Preview, decimal separator of averages.</summary>
        public const string PreviewDecimal = "preview.decimal";

        /// <summary>Title of the home menu.</summary>
        public const string MenuTitle = "menu.title";

        /// <summary>Home menu: local game.</summary>
        public const string MenuLocalGame = "menu.local-game";

        /// <summary>Home menu: online game (not yet available).</summary>
        public const string MenuFindGame = "menu.find-game";

        /// <summary>Home menu: options.</summary>
        public const string MenuOptions = "menu.options";

        /// <summary>Home menu: social (not yet available).</summary>
        public const string MenuSocial = "menu.social";

        /// <summary>Home menu: quit the game (Windows).</summary>
        public const string MenuQuit = "menu.quit";

        /// <summary>Back to the previous menu.</summary>
        public const string MenuBack = "menu.back";

        /// <summary>Title of the local game menu.</summary>
        public const string LocalTitle = "local.title";

        /// <summary>Number of players. {0}: count.</summary>
        public const string LocalPlayers = "local.players";

        /// <summary>Starts the local game.</summary>
        public const string LocalLaunch = "local.launch";

        /// <summary>Opens the development menu.</summary>
        public const string LocalDevelopment = "local.development";

        /// <summary>A seat of the local game. {0}: seat number.</summary>
        public const string LocalSeat = "local.seat";

        /// <summary>A seat played by a person.</summary>
        public const string LocalHuman = "local.human";

        /// <summary>A seat played by a bot.</summary>
        public const string LocalBot = "local.bot";

        /// <summary>Hint of the name field.</summary>
        public const string LocalName = "local.name";

        /// <summary>Title of the development menu.</summary>
        public const string DevTitle = "dev.title";

        /// <summary>Defensive posture option. {0}: bonus or off.</summary>
        public const string DevPosture = "dev.posture";

        /// <summary>Bounty on the leader option. {0}: bonus or off.</summary>
        public const string DevBounty = "dev.bounty";

        /// <summary>Ghosts choose the event option. {0}: yes or no.</summary>
        public const string DevGhosts = "dev.ghosts";

        /// <summary>Hint of the seed field.</summary>
        public const string DevSeed = "dev.seed";

        /// <summary>A rule option switched off.</summary>
        public const string DevOff = "dev.off";

        /// <summary>A rule option bonus. {0}: bonus.</summary>
        public const string DevBonus = "dev.bonus";

        /// <summary>Yes.</summary>
        public const string Yes = "common.yes";

        /// <summary>No.</summary>
        public const string No = "common.no";

        /// <summary>Title of the options.</summary>
        public const string OptionsTitle = "options.title";

        /// <summary>Default animation speed. {0}: speed.</summary>
        public const string OptionsSpeed = "options.speed";

        /// <summary>Full screen (Windows). {0}: yes or no.</summary>
        public const string OptionsFullScreen = "options.full-screen";

        /// <summary>Screen resolution (Windows). {0}: width, {1}: height.</summary>
        public const string OptionsResolution = "options.resolution";

        /// <summary>Button opening the pause menu.</summary>
        public const string PauseButton = "pause.button";

        /// <summary>Title of the pause menu.</summary>
        public const string PauseTitle = "pause.title";

        /// <summary>Pause menu: resume.</summary>
        public const string PauseResume = "pause.resume";

        /// <summary>Pause menu: restart the game.</summary>
        public const string PauseRestart = "pause.restart";

        /// <summary>Pause menu: options.</summary>
        public const string PauseOptions = "pause.options";

        /// <summary>Pause menu: back to the home menu.</summary>
        public const string PauseQuit = "pause.quit";

        /// <summary>End of game: play again with the same seats.</summary>
        public const string GameOverReplay = "game-over.replay";

        /// <summary>End of game: back to the home menu.</summary>
        public const string GameOverMenu = "game-over.menu";

        /// <summary>Banner when the view turns to a person. {0}: name.</summary>
        public const string TurnOf = "turn.of";

        /// <summary>Local game menu: time of a turn. {0}: the time chosen.</summary>
        public const string LocalTurnTime = "local.turn-time";

        /// <summary>Local game menu: turns are not timed.</summary>
        public const string LocalTurnTimeOff = "local.turn-time-off";

        /// <summary>Local game menu: a turn time. {0}: seconds.</summary>
        public const string LocalTurnTimeSeconds = "local.turn-time-seconds";

        /// <summary>A refusal with what forbids it. {0}: the reason, {1}: the card or status.</summary>
        public const string RefusalSource = "refusal.with-source";

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

        /// <summary>Short name of a crew action, shown while its pictogram is missing.</summary>
        public static string ActionShort(CrewAction action) => "action.short." + action;

        /// <summary>Help of a crew action, shown when its button is hovered.</summary>
        public static string ActionHelp(CrewAction action) => "action.help." + action;

        /// <summary>Why the engine refuses a move, by its refusal code.</summary>
        public static string Refusal(CommandErrorCode code) => "refusal." + code;

        /// <summary>Name of a bot level in the local game menu.</summary>
        public static string BotLevelName(BotLevel level) => "bot.level." + level;

        /// <summary>Question of a decision, by its engine prompt key (e.g. <c>critical.discard</c>).</summary>
        public static string Decision(string prompt) => "decision." + prompt;

        /// <summary>
        /// Optional gesture hint after the question of a decision answered on the table (ARB-82), by its engine prompt key;
        /// only questions whose gesture is not obvious have one.
        /// </summary>
        public static string DecisionHint(string prompt) => "decision.hint." + prompt;

        /// <summary>How to decline a decision: touch the card that asks ({0}: its name).</summary>
        public const string DecisionPassCard = "decision.pass.card";

        /// <summary>How to decline a decision: touch one's own panel.</summary>
        public const string DecisionPassSeat = "decision.pass.seat";

        /// <summary>How to decline a decision: touch the card shown in the middle in grey.</summary>
        public const string DecisionPassMiddle = "decision.pass.middle";

        /// <summary>Word for a decision answer, by its engine key (e.g. <c>yes</c>, <c>clockwise</c>).</summary>
        public static string Option(string key) => "option." + key;

        private const string NoBreakSpace = "\u00A0";

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
            Add(PanelYourTurn, "{0}, à vous de jouer");
            Add(PanelWaiting, "Les bots jouent…");
            Add(CommandPick, "Prendre {0} ({1})");
            Add(CommandRecycle, "Recycler le marché {0}");
            Add(CommandEndMarket, "Passer le marché");
            Add(CommandActivate, "Utiliser {0}");
            Add(CommandTechnology, "Activer le combo");
            Add(CommandAttack, "Attaquer {0}");
            Add(CommandAttackOvercharged, "Attaquer {0} en surcharge");
            Add(CommandReroll, "Reparamétrer son bouclier");
            Add(CommandRerollOvercharged, "Reparamétrer en surcharge");
            Add(CommandSabotage, "Saboter le bouclier de {0}");
            Add(CommandOvercharge, "Prendre un jeton de surcharge");
            Add(CommandPosture, "Posture défensive");
            Add(CommandEndTurn, "Fin de tour");
            Add(OptionMarket, "marché");
            Add(ButtonEndTurn, "Fin de tour");
            Add(ButtonEndMarket, "Passer le marché");
            Add(ButtonMarketOpen, "Marché");
            Add(ButtonMarketFold, "Réduire le marché");
            Add(ButtonRecycle, "Recycler");
            Add(ButtonCombo, "Combo");
            Add(HelpOvercharge, "Surcharge : touchez le jeton pour l'armer. La prochaine attaque ou le prochain reparamétrage la dépense (un dé de plus). Touchez de nouveau pour la désarmer.");
            Add(HelpCombo, "Combo : vos deux modificateurs sont de la même technologie. Ils sont défaussés et la technologie est obtenue.");
            Add(SeatGhost, "Fantôme : choisit l'événement");
            Add(HelpComboReady, "Combo : vos deux modificateurs sont défaussés et vous obtenez {0}. {1}");
            Add(PreviewDice, "Dés : {0}d{1}");
            Add(PreviewDiceKept, "Dés : {0}d{1}, les {2} meilleurs gardés");
            Add(PreviewAdvantage, ", avec avantage");
            Add(PreviewDisadvantage, ", avec désavantage");
            Add(PreviewShield, "Bouclier de la cible : {0}");
            Add(PreviewBonuses, "Bonus : {0}");
            Add(PreviewBonusItem, "{1} {0}");
            Add(PreviewBonusRule, "règle de la partie");
            Add(PreviewDamage, "Dégâts : {0}, {1} en moyenne");
            Add(PreviewDamageExact, "Dégâts : {0}");
            Add(PreviewChances, "Touche : {0} · critique : {1}");
            Add(PreviewChancesDestroyed, "Touche : {0} · critique : {1} · détruit : {2}");
            Add(PreviewRedirect, "La cible peut dévier l'attaque.");
            Add(PreviewSelfLoss, "Vous pouvez perdre jusqu'à {0} PV.");
            Add(PreviewSabotage, "Bouclier de la cible : {0} maintenant, {1} en moyenne après ({2})");
            Add(PreviewRange, "{0} à {1}");
            Add(PreviewPercent, "{0}" + NoBreakSpace + "%");
            Add(PreviewDecimal, ",");

            Add(MenuTitle, "VORTEX");
            Add(MenuLocalGame, "Partie locale");
            Add(MenuFindGame, "Trouver une partie (bientôt)");
            Add(MenuOptions, "Options");
            Add(MenuSocial, "Social (bientôt)");
            Add(MenuQuit, "Quitter");
            Add(MenuBack, "Retour");
            Add(LocalTitle, "Partie locale");
            Add(LocalPlayers, "Joueurs : {0}");
            Add(LocalLaunch, "Lancer la partie");
            Add(LocalDevelopment, "Développement");
            Add(LocalSeat, "Siège {0}");
            Add(LocalHuman, "Humain");
            Add(LocalBot, "Bot");
            Add(LocalName, "Nom");
            Add(DevTitle, "Développement (absent des builds publiés)");
            Add(DevPosture, "Posture défensive : {0}");
            Add(DevBounty, "Prime sur le leader : {0}");
            Add(DevGhosts, "Les éliminés choisissent l'événement : {0}");
            Add(DevSeed, "Graine (vide : au hasard)");
            Add(DevOff, "non");
            Add(DevBonus, "+{0}");
            Add(Yes, "oui");
            Add(No, "non");
            Add(OptionsTitle, "Options");
            Add(OptionsSpeed, "Vitesse des animations : ×{0}");
            Add(OptionsFullScreen, "Plein écran : {0}");
            Add(OptionsResolution, "Résolution : {0} × {1}");
            Add(PauseButton, "Pause");
            Add(PauseTitle, "Pause");
            Add(PauseResume, "Reprendre");
            Add(PauseRestart, "Recommencer");
            Add(PauseOptions, "Options");
            Add(PauseQuit, "Quitter la partie");
            Add(GameOverReplay, "Rejouer");
            Add(GameOverMenu, "Menu");
            Add(TurnOf, "Tour de {0}");
            Add(RefusalSource, "{0} Cause : {1}.");
            Add(LocalTurnTime, "Temps de tour : {0}");
            Add(LocalTurnTimeOff, "illimité");
            Add(LocalTurnTimeSeconds, "{0} s");
            Add(Refusal(CommandErrorCode.GameOver), "La partie est terminée.");
            Add(Refusal(CommandErrorCode.DecisionPending), "Une décision est en attente.");
            Add(Refusal(CommandErrorCode.NoDecisionPending), "Aucune décision n'est en attente.");
            Add(Refusal(CommandErrorCode.NotYourDecision), "Cette décision revient à un autre joueur.");
            Add(Refusal(CommandErrorCode.WrongDecision), "Cette décision n'est plus valable.");
            Add(Refusal(CommandErrorCode.InvalidOption), "Ce choix n'est pas proposé.");
            Add(Refusal(CommandErrorCode.NotYourTurn), "Ce n'est pas votre tour.");
            Add(Refusal(CommandErrorCode.WrongPhase), "Pas à ce moment du tour.");
            Add(Refusal(CommandErrorCode.InvalidMarketCard), "Cette carte n'est plus au marché.");
            Add(Refusal(CommandErrorCode.CannotRecycleAfterPick), "Pas de recyclage après avoir pris une carte.");
            Add(Refusal(CommandErrorCode.NoPicksLeft), "Vous avez déjà pris votre carte ce tour.");
            Add(Refusal(CommandErrorCode.InvalidCard), "Cette carte n'est pas à vous.");
            Add(Refusal(CommandErrorCode.CardNotActivatable), "Cette carte ne peut pas être utilisée maintenant.");
            Add(Refusal(CommandErrorCode.NoTechnologyCombo), "Vos deux modificateurs ne forment pas de combo.");
            Add(Refusal(CommandErrorCode.NoCrewActionLeft), "Plus d'action d'équipage ce tour.");
            Add(Refusal(CommandErrorCode.CrewActionAlreadyUsed), "Action déjà faite ce tour.");
            Add(Refusal(CommandErrorCode.ForcedActionRequired), "Une action vous est imposée ce tour.");
            Add(Refusal(CommandErrorCode.InvalidTarget), "Cible impossible.");
            Add(Refusal(CommandErrorCode.TargetNotAllowed), "Cible protégée contre vous.");
            Add(Refusal(CommandErrorCode.NoOvercharge), "Pas de jeton de surcharge.");
            Add(Refusal(CommandErrorCode.OverchargeFull), "Jeton de surcharge déjà au maximum.");
            Add(Refusal(CommandErrorCode.ShieldChangeNotAllowed), "Son bouclier ne peut pas être modifié.");
            Add(Refusal(CommandErrorCode.MalformedCommand), "Coup impossible.");
            Add(Refusal(CommandErrorCode.ActionNotAvailable), "Action désactivée par les règles de la partie.");
            Add(BotLevelName(BotLevel.Random), "Aléatoire");
            Add(BotLevelName(BotLevel.Naive), "Naïf");
            Add(BotLevelName(BotLevel.Normal), "Normal");
            Add(BotLevelName(BotLevel.Strong), "Fort");

            Add(ActionShort(CrewAction.Attack), "ATQ");
            Add(ActionShort(CrewAction.Sabotage), "SAB");
            Add(ActionShort(CrewAction.RerollShield), "REP");
            Add(ActionShort(CrewAction.Overcharge), "SUR");
            Add(ActionShort(CrewAction.DefensivePosture), "POS");
            Add(ActionHelp(CrewAction.Attack), "Attaque : glissez vers un adversaire. Dés lancés, plus les bonus, moins son bouclier : ce sont ses dégâts. Surcharge armée : un dé de plus.");
            Add(ActionHelp(CrewAction.Sabotage), "Sabotage : glissez vers un adversaire pour relancer son bouclier (1d8).");
            Add(ActionHelp(CrewAction.RerollShield), "Reparamétrage : touchez pour relancer votre bouclier (1d8 ; avec la surcharge armée, 2d8 plafonnés à 8).");
            Add(ActionHelp(CrewAction.Overcharge), "Surcharge : touchez pour gagner un jeton de surcharge (un au plus).");
            Add(ActionHelp(CrewAction.DefensivePosture), "Posture défensive : touchez pour renforcer votre bouclier jusqu'à votre prochain tour.");

            // Engine decision prompts (RULES B6); an unknown prompt shows "#decision.key" until its text is added.
            Add(Decision("bet.face"), "Annoncez une valeur de dé");
            Add(Decision("cap.enemy"), "Choisissez l'ennemi visé");
            Add(Decision("convert.hp"), "Combien de PV convertir ?");
            Add(Decision("convert.shield"), "Combien de points de bouclier convertir ?");
            Add(Decision("critical.discard"), "Coup critique : quel modificateur perdez-vous ?");
            Add(Decision("dictate.action"), "Imposez une action d'équipage");
            Add(Decision("disable.shield"), "Quel bouclier désactiver ?");
            Add(Decision("discard.opponent"), "Quel adversaire défausse ?");
            Add(Decision("discard.targetModifier"), "Quel modificateur de la cible défausser ?");
            Add(Decision("ghost.event"), "Choisissez l'événement de la manche");
            Add(Decision("market.pick"), "Choisissez une carte du marché");
            Add(Decision("redirect.attack"), "Vers qui dévier l'attaque ?");
            Add(Decision("reroll.shield"), "Quel bouclier relancer ?");
            Add(Decision("rotate.direction"), "Dans quel sens tourner les boucliers ?");
            Add(Decision("steal.card"), "Quelle carte prendre ?");
            Add(Decision("steal.opponent"), "À quel adversaire ?");
            Add(Decision("steal.or.destroy"), "Voler ou détruire ?");
            Add(Decision("swap.partner"), "Avec qui échanger ?");
            Add(Decision("swap.shield.first"), "Premier bouclier à échanger");
            Add(Decision("swap.shield.second"), "Second bouclier à échanger");
            Add(Decision("swap.targetCard"), "Avec quelle carte échanger ?");
            Add(Decision("torment.market"), "Sur quelle carte du marché poser le Tourment ?");
            Add(Decision("torment.place"), "Sur quel modificateur poser le Tourment ?");
            Add(Decision("torment.target"), "Quel joueur reçoit le Tourment ?");
            Add(DecisionHint("rotate.direction"), "Touchez le voisin qui reçoit votre bouclier.");
            Add(DecisionHint("steal.or.destroy"), "Glissez la carte vers votre vaisseau pour la voler, ou au centre de la table pour la détruire.");
            Add(DecisionPassCard, "Touchez {0} en gris pour passer.");
            Add(DecisionPassSeat, "Touchez votre fiche pour passer.");
            Add(DecisionPassMiddle, "Touchez la carte grise au centre pour passer.");

            Add(Option("yes"), "Oui");
            Add(Option("no"), "Non");
            Add(Option("none"), "Aucun");
            Add(Option("clockwise"), "Sens horaire");
            Add(Option("counterclockwise"), "Sens anti-horaire");
            Add(Option("destroy"), "Détruire");
            Add(Option("steal"), "Voler");
            Add(Option("first"), "Le premier");
            Add(Option("second"), "Le second");
            Add(Option("attack"), "Attaque");
            Add(Option("reroll"), "Reparamétrage");
            Add(Option("sabotage"), "Sabotage");
            Add(Option("overcharge"), "Surcharge");
            Add(Option("posture"), "Posture défensive");

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
