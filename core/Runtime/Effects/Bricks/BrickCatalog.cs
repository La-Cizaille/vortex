using System;
using System.Collections.Generic;
using Vortex.Core.Content;

namespace Vortex.Core.Effects.Bricks
{
    /// <summary>One entry of the brick catalog.</summary>
    internal sealed class BrickInfo
    {
        public BrickInfo(string name, BrickKind kind, string description, Func<BrickParams, Effect> factory)
        {
            Name = name;
            Kind = kind;
            Description = description;
            Factory = factory;
            BrickParams describe = BrickParams.Describe();
            factory(describe);
            Parameters = describe.Declared;
        }

        /// <summary>Name used in the content files.</summary>
        public string Name { get; }

        public BrickKind Kind { get; }

        /// <summary>Designer-facing description (French: it is published in docs/BRICKS.md).</summary>
        public string Description { get; }

        /// <summary>Creates the effect from validated parameters.</summary>
        public Func<BrickParams, Effect> Factory { get; }

        /// <summary>Declared parameters, captured by running the factory in describe mode.</summary>
        public IReadOnlyList<BrickParamInfo> Parameters { get; }
    }

    /// <summary>
    /// The closed, whitelisted catalog of effect bricks (ADR-0007). Content files can only select and parameterize
    /// these entries: they never carry logic. Registration is explicit (no reflection, IL2CPP-safe).
    /// Adding a mechanic = adding a generic brick here, with its test; never a card-specific rule.
    /// </summary>
    internal static class BrickCatalog
    {
        private const BrickKind P = BrickKind.Passive;
        private const BrickKind A = BrickKind.Activation;

        /// <summary>Every brick, in documentation order. A new list each call: no shared mutable state.</summary>
        public static IReadOnlyList<BrickInfo> Entries()
        {
            return new List<BrickInfo>
            {
                // Attack (passive)
                new BrickInfo(nameof(AttackValueBonus), P, "Ajoute `amount` à la valeur d'attaque, avant le bouclier. Porté par une carte : les attaques de son porteur ; porté par un événement : toutes les attaques.",
                    p => new AttackValueBonus(p.Int("amount", -20, 20, null, "bonus (négatif possible)"), p.Enum("when", (AttackCondition?)AttackCondition.Always, "condition sur l'attaque"))),
                new BrickInfo(nameof(ParityAttackBonus), P, "Valeur d'attaque : `even` si la somme des dés conservés est paire, `odd` sinon.",
                    p => new ParityAttackBonus(p.Int("even", -20, 20, null, "bonus si pair"), p.Int("odd", -20, 20, null, "bonus si impair"))),
                new BrickInfo(nameof(IgnoreTargetShield), P, "Le bouclier de la cible compte pour 0 (ignoré, sa valeur ne change pas).",
                    p => new IgnoreTargetShield(p.Enum("when", (AttackCondition?)AttackCondition.Always, "condition sur l'attaque"))),
                new BrickInfo(nameof(AttackAdvantage), P, "Avantage sur les attaques couvertes (on lance le lot deux fois, on garde le meilleur).",
                    p => new AttackAdvantage()),
                new BrickInfo(nameof(IncomingAttackDisadvantage), P, "Désavantage sur les attaques subies par le porteur.",
                    p => new IncomingAttackDisadvantage()),
                new BrickInfo(nameof(HealOnHit), P, "Si l'attaque fait perdre des PV à la cible, l'attaquant gagne `amount` PV.",
                    p => new HealOnHit(p.Int("amount", 1, 99, null, "PV gagnés"))),
                new BrickInfo(nameof(StealShieldBeforeAttack), P, "Avant le jet, prend jusqu'à `amount` points au bouclier de la cible et les ajoute au sien. Soumis à l'autorisation « modifier un bouclier ».",
                    p => new StealShieldBeforeAttack(p.Int("amount", 1, 99, null, "points volés au maximum"))),
                new BrickInfo(nameof(DieBetMultiplier), P, "Avant le jet, l'attaquant annonce une face ; si un dé conservé la montre, dégâts × `factor`.",
                    p => new DieBetMultiplier(p.Int("factor", 2, 10, null, "multiplicateur"))),
                new BrickInfo(nameof(RerollShieldOnHit), P, "Si l'attaque fait perdre des PV, l'attaquant peut relancer (1 dé) le bouclier d'un joueur qu'il a le droit de modifier.",
                    p => new RerollShieldOnHit()),
                new BrickInfo(nameof(DiscardTargetModifierOnHit), P, "Si l'attaque fait perdre des PV, l'attaquant peut défausser un modificateur de la cible.",
                    p => new DiscardTargetModifierOnHit()),
                new BrickInfo(nameof(TormentTargetOnAttack), P, "Après chaque attaque, l'attaquant pose `count` jetons de Tourment, un par un, sur les modificateurs de la cible (à son choix).",
                    p => new TormentTargetOnAttack(p.Int("count", 1, 10, null, "jetons posés"))),
                new BrickInfo(nameof(TormentMarketOnHit), P, "Si l'attaque fait perdre des PV, l'attaquant pose 1 jeton sur `count` cartes différentes des marchés.",
                    p => new TormentMarketOnHit(p.Int("count", 1, 20, null, "cartes visées"))),
                new BrickInfo(nameof(ScorchedEarthOnKill), P, "Si une attaque du porteur élimine un vaisseau : tous les autres joueurs perdent leurs modificateurs, puis leur bouclier passe à 0 (soumis à l'autorisation). La carte est ensuite défaussée.",
                    p => new ScorchedEarthOnKill()),

                // Defense (passive)
                new BrickInfo(nameof(DenyShieldChangeByOpponents), P, "Refuse l'autorisation « modifier un bouclier » sur le bouclier du porteur quand la source est un adversaire.",
                    p => new DenyShieldChangeByOpponents()),
                new BrickInfo(nameof(CapIncomingAttackLoss), P, "Plafonne à `max` les PV perdus par attaque. Options : seulement à `whenHpAtMost` PV ou moins (0 = toujours), jamais contre une attaque surchargée, défausse de la carte face à une attaque surchargée.",
                    p => new CapIncomingAttackLoss(
                        p.Int("max", 0, 99, null, "PV perdus au maximum"),
                        p.Bool("unlessOvercharged", false, "sans effet contre une attaque surchargée"),
                        p.Int("whenHpAtMost", 0, 999, 0, "seulement si PV ≤ cette valeur (0 = toujours)"),
                        p.Bool("discardWhenOvercharged", false, "carte défaussée quand une attaque surchargée est subie"))),
                new BrickInfo(nameof(PreventNextAttackLoss), P, "La prochaine attaque subie ne fait perdre aucun PV ; la carte est ensuite défaussée.",
                    p => new PreventNextAttackLoss()),
                new BrickInfo(nameof(PreventFatalAttackLoss), P, "Une attaque qui éliminerait le porteur ne lui fait perdre aucun PV ; la carte est ensuite défaussée.",
                    p => new PreventFatalAttackLoss()),
                new BrickInfo(nameof(BlockAttackerNextTurn), P, "Après une attaque subie, cet attaquant ne peut pas attaquer le porteur pendant son prochain tour.",
                    p => new BlockAttackerNextTurn()),
                new BrickInfo(nameof(CapEnemyShield), P, "À l'équipement, le porteur choisit un ennemi : le bouclier de cet ennemi est borné à (bouclier du porteur − `offset`), minimum 0.",
                    p => new CapEnemyShield(p.Int("offset", 0, 8, null, "écart"))),
                new BrickInfo(nameof(DictateAttackerAction), P, "Quand une attaque fait perdre des PV au porteur, il impose l'action d'équipage (et les cibles) de l'attaquant pour son prochain tour.",
                    p => new DictateAttackerAction()),
                new BrickInfo(nameof(HealWhenOthersDamaged), P, "Chaque fois qu'une attaque fait perdre des PV à un autre joueur, le porteur gagne `amount` PV.",
                    p => new HealWhenOthersDamaged(p.Int("amount", 1, 99, null, "PV gagnés"))),
                new BrickInfo(nameof(ReflectAttackLoss), P, "Quand une attaque fait perdre des PV au porteur, l'attaquant en perd autant (cause Reflect, sans réaction).",
                    p => new ReflectAttackLoss()),
                new BrickInfo(nameof(HealOnOwnTormentLoss), P, "Le porteur regagne les PV qu'il perd à cause des jetons de Tourment.",
                    p => new HealOnOwnTormentLoss()),
                new BrickInfo(nameof(TormentAttackerOnLoss), P, "Quand une attaque fait perdre des PV au porteur, il pose `count` jeton(s) sur les modificateurs de l'attaquant.",
                    p => new TormentAttackerOnLoss(p.Int("count", 1, 10, null, "jetons posés"))),
                new BrickInfo(nameof(GambleOnHpLoss), P, "À chaque perte de PV du porteur (hors Tourment, Reflect et pertes qu'il s'inflige), 1 dé : pair, aucune perte ; impair, `penalty` PV de plus.",
                    p => new GambleOnHpLoss(p.Int("penalty", 0, 99, null, "PV perdus en plus sur un impair"))),
                new BrickInfo(nameof(CopyHighestShieldOnTurnStart), P, "Au début de chaque tour du porteur, son bouclier copie le plus élevé des autres joueurs.",
                    p => new CopyHighestShieldOnTurnStart()),
                new BrickInfo(nameof(HpBalanceOnTurnStart), P, "Au début de chaque tour du porteur : s'il a au moins `threshold` PV, il en perd `amount` (cause Self) ; sinon il en gagne `amount`.",
                    p => new HpBalanceOnTurnStart(p.Int("threshold", 1, 999, null, "seuil de PV"), p.Int("amount", 1, 99, null, "PV perdus ou gagnés"))),
                new BrickInfo(nameof(ShieldChangeOnTurnStart), P, "Au début de chaque tour du porteur, son bouclier change de `amount` (borné).",
                    p => new ShieldChangeOnTurnStart(p.Int("amount", -8, 8, null, "variation"))),
                new BrickInfo(nameof(PreRollDie), P, "À la fin du marché du porteur, un dé est lancé ; il sert de premier dé à sa prochaine action d'équipage.",
                    p => new PreRollDie()),

                // Global (passive, typically events)
                new BrickInfo(nameof(DisableShields), P, "Les boucliers couverts comptent pour 0 dans les attaques (désactivés). Porté par un événement : tous les boucliers.",
                    p => new DisableShields()),
                new BrickInfo(nameof(ExtraMarketPicks), P, "`amount` carte(s) de plus à prendre au marché pour les joueurs couverts.",
                    p => new ExtraMarketPicks(p.Int("amount", 1, 5, null, "cartes en plus"))),

                // Activation: next attack / this turn
                new BrickInfo(nameof(DisableOpponentShieldThisTurn), A, "Le bouclier d'un adversaire choisi est désactivé jusqu'à la fin du tour du porteur.",
                    p => new DisableOpponentShieldThisTurn()),
                new BrickInfo(nameof(ConvertShieldToNextAttack), A, "Le porteur retire X points de son bouclier (X choisi) et gagne +X à la valeur de sa prochaine attaque de ce tour.",
                    p => new ConvertShieldToNextAttack()),
                new BrickInfo(nameof(NextAttackDamageMultiplier), A, "Dégâts × `factor` pour la prochaine attaque de ce tour.",
                    p => new NextAttackDamageMultiplier(p.Int("factor", 2, 10, null, "multiplicateur"))),
                new BrickInfo(nameof(NextAttackBonusPerNeutral), A, "Prochaine attaque de ce tour : + `amount` par carte neutre visible dans les marchés (compté à l'attaque).",
                    p => new NextAttackBonusPerNeutral(p.Int("amount", 1, 10, null, "bonus par carte"))),
                new BrickInfo(nameof(NextAttackOvercharged), A, "La prochaine attaque de ce tour est surchargée sans consommer de jeton.",
                    p => new NextAttackOvercharged()),
                new BrickInfo(nameof(AllInAttack), A, "Défausse les modificateurs du porteur ; sa prochaine attaque de ce tour est surchargée avec `dice` dés dont on garde les `keep` meilleurs.",
                    p => new AllInAttack(p.Int("dice", 1, 10, null, "dés lancés"), p.Int("keep", 1, 10, null, "dés conservés"))),
                new BrickInfo(nameof(SwapOnNextAttack), A, "Après la prochaine attaque de ce tour, le porteur peut échanger un modificateur de la cible avec la carte du même emplacement d'un autre joueur (ni la cible, ni le porteur) ou du marché correspondant.",
                    p => new SwapOnNextAttack()),

                // Activation: modifiers and markets
                new BrickInfo(nameof(DiscardOpponentModifiers), A, "Défausse les deux modificateurs d'un adversaire choisi.",
                    p => new DiscardOpponentModifiers()),
                new BrickInfo(nameof(StealOpponentModifiers), A, "Défausse les modificateurs du porteur, puis prend ceux d'un adversaire choisi (avec leurs jetons).",
                    p => new StealOpponentModifiers()),
                new BrickInfo(nameof(StealOrDestroyOpponentModifier), A, "Choisit un modificateur d'un adversaire, puis le vole (dans l'emplacement correspondant) ou le détruit.",
                    p => new StealOrDestroyOpponentModifier()),
                new BrickInfo(nameof(RefreshMarketAndPick), A, "Recycle le marché `market`, puis le porteur y prend une carte.",
                    p => new RefreshMarketAndPick(p.Enum<CardSlot>("market", null, "marché concerné"))),
                new BrickInfo(nameof(RefreshAllMarkets), A, "Recycle les deux marchés.",
                    p => new RefreshAllMarkets()),

                // Activation: torments
                new BrickInfo(nameof(TormentOpponentAndNeighbours), A, "Un adversaire choisi et ses deux voisins (jamais le porteur) reçoivent `count` jeton(s) sur chacun de leurs modificateurs.",
                    p => new TormentOpponentAndNeighbours(p.Int("count", 1, 10, null, "jetons par modificateur"))),
                new BrickInfo(nameof(ReactivateTorments), A, "Tous les jetons posés sur des modificateurs équipés infligent de nouveau leur perte.",
                    p => new ReactivateTorments()),
                new BrickInfo(nameof(ClearAllTormentsHealPer), A, "Retire tous les jetons (joueurs et marchés) ; le porteur gagne `amount` PV par jeton retiré.",
                    p => new ClearAllTormentsHealPer(p.Int("amount", 0, 99, null, "PV par jeton"))),
                new BrickInfo(nameof(TormentAllEquipped), A, "Chaque joueur reçoit `count` jeton(s) sur chacun de ses modificateurs équipés.",
                    p => new TormentAllEquipped(p.Int("count", 1, 10, null, "jetons par modificateur"))),
                new BrickInfo(nameof(ClearPlayerTorments), A, "Retire tous les jetons des modificateurs équipés.",
                    p => new ClearPlayerTorments()),
                new BrickInfo(nameof(TormentValueBonus), A, "Chaque jeton de Tourment inflige `amount` de plus, pour le reste de la partie (cumulable).",
                    p => new TormentValueBonus(p.Int("amount", 1, 10, null, "bonus par jeton"))),

                // Activation: shields
                new BrickInfo(nameof(ConvertHpToShield), A, "Le porteur convertit X PV (X choisi) en X points de bouclier, sans descendre à 0 PV ni dépasser la borne.",
                    p => new ConvertHpToShield()),
                new BrickInfo(nameof(SwapTwoShields), A, "Échange les boucliers de deux vaisseaux choisis (les deux changements doivent être autorisés).",
                    p => new SwapTwoShields()),
                new BrickInfo(nameof(SetOwnShield), A, "Le bouclier du porteur passe à `value` (borné).",
                    p => new SetOwnShield(p.Int("value", 0, 99, null, "nouvelle valeur"))),
                new BrickInfo(nameof(SetAllShields), A, "Le bouclier de chaque joueur passe à `value` (source : le porteur, ou le jeu pour un événement).",
                    p => new SetAllShields(p.Int("value", 0, 99, null, "nouvelle valeur"))),
                new BrickInfo(nameof(DisableOwnShieldUntilNextTurn), A, "Le bouclier du porteur est désactivé jusqu'au début de son prochain tour.",
                    p => new DisableOwnShieldUntilNextTurn()),
                new BrickInfo(nameof(RotateShields), A, "Tous les boucliers tournent d'un siège dans le sens choisi ; les joueurs dont le bouclier ne peut pas être modifié gardent le leur.",
                    p => new RotateShields()),

                // Activation: health and overcharge
                new BrickInfo(nameof(Heal), A, "Le porteur gagne `amount` PV.",
                    p => new Heal(p.Int("amount", 1, 99, null, "PV gagnés"))),
                new BrickInfo(nameof(HealPerNeutral), A, "Le porteur gagne `amount` PV par carte neutre visible dans les marchés.",
                    p => new HealPerNeutral(p.Int("amount", 1, 10, null, "PV par carte"))),
                new BrickInfo(nameof(GainOvercharge), A, "Le porteur gagne `amount` jeton(s) de surcharge (dans la limite du maximum).",
                    p => new GainOvercharge(p.Int("amount", 1, 10, null, "jetons"))),
                new BrickInfo(nameof(KeepOverchargeUntilNextTurn), A, "Le porteur ne peut pas perdre sa surcharge jusqu'au début de son prochain tour.",
                    p => new KeepOverchargeUntilNextTurn()),
                new BrickInfo(nameof(NextOverchargedAttackAdvantage), A, "La prochaine attaque surchargée du porteur se fait avec avantage (sans limite de temps).",
                    p => new NextOverchargedAttackAdvantage()),

                // Activation: rule changes
                new BrickInfo(nameof(DiscardAllEquippedModifiers), A, "Tous les modificateurs équipés de tous les joueurs sont défaussés.",
                    p => new DiscardAllEquippedModifiers()),
                new BrickInfo(nameof(RedirectAttacksUntilNextTurn), A, "Jusqu'au début de son prochain tour, le porteur peut dévier les attaques qui le visent vers un autre joueur (ni l'attaquant, ni lui).",
                    p => new RedirectAttacksUntilNextTurn()),
                new BrickInfo(nameof(ExtraCrewActionsThisTurn), A, "`amount` action(s) d'équipage de plus ce tour (toujours différentes).",
                    p => new ExtraCrewActionsThisTurn(p.Int("amount", 1, 3, null, "actions en plus"))),
            };
        }

        /// <summary>Compiles a spec into its effect, or explains why it is invalid.</summary>
        public static Effect Compile(IReadOnlyDictionary<string, BrickInfo> catalog, EffectSpec spec)
        {
            if (spec is null || string.IsNullOrEmpty(spec.Brick))
            {
                throw new BrickParamException("effect without a brick name.");
            }

            if (!catalog.TryGetValue(spec.Brick, out BrickInfo? info))
            {
                throw new BrickParamException("unknown brick '" + spec.Brick + "' (see docs/BRICKS.md).");
            }

            BrickParams parameters = BrickParams.For(spec.Parameters);
            Effect effect = info.Factory(parameters);
            parameters.Finish();
            return effect;
        }
    }
}
