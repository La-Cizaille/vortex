# Catalogue des briques d'effets

> **Fichier généré** par `Vortex.ContentTool` depuis le code des briques (`core/Runtime/Effects/Bricks`). Ne pas le modifier à la main.
>
> Une carte, un événement ou une technologie déclare ses effets dans son champ `effects`, par exemple :
> `"effects": [ { "brick": "AttackValueBonus", "amount": 4, "when": "Overcharged" } ]`.
> Les paramètres sont typés et bornés ; un paramètre inconnu ou hors plage est refusé au chargement.
> **Portée** : portée par une carte, une brique agit pour le porteur de la carte ; portée par un événement, pour tous les joueurs.
> **Passive** : agit tant que la carte est équipée ou l'événement actif. **Activation** : s'exécute une fois (carte activée, événement révélé, technologie activée).
> Un besoin qu'aucune brique ne couvre ? On ajoute une brique **générique** dans le code (RULES B1, ADR-0007), jamais une exception propre à une carte.

63 briques.

| Brique | Type | Utilisée par |
|---|---|---|
| [`AttackValueBonus`](#attackvaluebonus) | Passive | `A_005`, `A_017`, `A_019`, `EVT_SURCHARGE_IONIQUE` |
| [`ParityAttackBonus`](#parityattackbonus) | Passive | `A_023` |
| [`IgnoreTargetShield`](#ignoretargetshield) | Passive | `A_016` |
| [`AttackAdvantage`](#attackadvantage) | Passive | `A_026` |
| [`IncomingAttackDisadvantage`](#incomingattackdisadvantage) | Passive | `D_026` |
| [`HealOnHit`](#healonhit) | Passive | `A_011` |
| [`StealShieldBeforeAttack`](#stealshieldbeforeattack) | Passive | `A_001` |
| [`DieBetMultiplier`](#diebetmultiplier) | Passive | `A_024` |
| [`RerollShieldOnHit`](#rerollshieldonhit) | Passive | `A_003` |
| [`DiscardTargetModifierOnHit`](#discardtargetmodifieronhit) | Passive | `A_014` |
| [`TormentTargetOnAttack`](#tormenttargetonattack) | Passive | `A_018` |
| [`TormentMarketOnHit`](#tormentmarketonhit) | Passive | `A_020` |
| [`ScorchedEarthOnKill`](#scorchedearthonkill) | Passive | `A_010` |
| [`DenyShieldChangeByOpponents`](#denyshieldchangebyopponents) | Passive | `D_001` |
| [`CapIncomingAttackLoss`](#capincomingattackloss) | Passive | `D_007`, `D_015` |
| [`PreventNextAttackLoss`](#preventnextattackloss) | Passive | `D_008` |
| [`PreventFatalAttackLoss`](#preventfatalattackloss) | Passive | `D_010` |
| [`BlockAttackerNextTurn`](#blockattackernextturn) | Passive | `D_004` |
| [`CapEnemyShield`](#capenemyshield) | Passive | `D_005` |
| [`DictateAttackerAction`](#dictateattackeraction) | Passive | `D_006` |
| [`HealWhenOthersDamaged`](#healwhenothersdamaged) | Passive | `D_013` |
| [`ReflectAttackLoss`](#reflectattackloss) | Passive | `D_017` |
| [`HealOnOwnTormentLoss`](#healonowntormentloss) | Passive | `D_018` |
| [`TormentAttackerOnLoss`](#tormentattackeronloss) | Passive | `D_022` |
| [`GambleOnHpLoss`](#gambleonhploss) | Passive | `D_025` |
| [`CopyHighestShieldOnTurnStart`](#copyhighestshieldonturnstart) | Passive | `D_019` |
| [`HpBalanceOnTurnStart`](#hpbalanceonturnstart) | Passive | `D_020` |
| [`ShieldChangeOnTurnStart`](#shieldchangeonturnstart) | Passive | `D_027` |
| [`PreRollDie`](#prerolldie) | Passive | `D_023` |
| [`DisableShields`](#disableshields) | Passive | `EVT_TEMPETE_ELECTRO_MAGNETIQUE` |
| [`ExtraMarketPicks`](#extramarketpicks) | Passive | `EVT_NOUVEL_ARRIVAGE` |
| [`DisableOpponentShieldThisTurn`](#disableopponentshieldthisturn) | Activation | `A_002` |
| [`ConvertShieldToNextAttack`](#convertshieldtonextattack) | Activation | `A_004` |
| [`NextAttackDamageMultiplier`](#nextattackdamagemultiplier) | Activation | `A_006` |
| [`NextAttackBonusPerNeutral`](#nextattackbonusperneutral) | Activation | `A_012` |
| [`NextAttackOvercharged`](#nextattackovercharged) | Activation | `A_013` |
| [`AllInAttack`](#allinattack) | Activation | `A_027` |
| [`SwapOnNextAttack`](#swaponnextattack) | Activation | `A_025` |
| [`DiscardOpponentModifiers`](#discardopponentmodifiers) | Activation | `A_007` |
| [`StealOpponentModifiers`](#stealopponentmodifiers) | Activation | `A_009` |
| [`StealOrDestroyOpponentModifier`](#stealordestroyopponentmodifier) | Activation | `A_015` |
| [`RefreshMarketAndPick`](#refreshmarketandpick) | Activation | `A_008`, `D_009` |
| [`RefreshAllMarkets`](#refreshallmarkets) | Activation | `EVT_NOUVEL_ARRIVAGE` |
| [`TormentOpponentAndNeighbours`](#tormentopponentandneighbours) | Activation | `A_021` |
| [`ReactivateTorments`](#reactivatetorments) | Activation | `A_022`, `TECH_GREEN` |
| [`ClearAllTormentsHealPer`](#clearalltormentshealper) | Activation | `D_021` |
| [`TormentAllEquipped`](#tormentallequipped) | Activation | `EVT_NUEE_PARASITAIRE` |
| [`ClearPlayerTorments`](#clearplayertorments) | Activation | `EVT_ESPACE_ASEPTISE` |
| [`TormentValueBonus`](#tormentvaluebonus) | Activation | `TECH_GREEN` |
| [`ConvertHpToShield`](#converthptoshield) | Activation | `D_002` |
| [`SwapTwoShields`](#swaptwoshields) | Activation | `D_003` |
| [`SetOwnShield`](#setownshield) | Activation | `D_011` |
| [`SetAllShields`](#setallshields) | Activation | `EVT_FIN_DES_TEMPS` |
| [`DisableOwnShieldUntilNextTurn`](#disableownshielduntilnextturn) | Activation | `D_016` |
| [`RotateShields`](#rotateshields) | Activation | `D_024` |
| [`Heal`](#heal) | Activation | `D_016` |
| [`HealPerNeutral`](#healperneutral) | Activation | `D_012` |
| [`GainOvercharge`](#gainovercharge) | Activation | `TECH_RED` |
| [`KeepOverchargeUntilNextTurn`](#keepoverchargeuntilnextturn) | Activation | `D_014` |
| [`NextOverchargedAttackAdvantage`](#nextoverchargedattackadvantage) | Activation | `TECH_RED` |
| [`DiscardAllEquippedModifiers`](#discardallequippedmodifiers) | Activation | `EVT_TROU_NOIR`, `EVT_FIN_DES_TEMPS` |
| [`RedirectAttacksUntilNextTurn`](#redirectattacksuntilnextturn) | Activation | `TECH_BLUE` |
| [`ExtraCrewActionsThisTurn`](#extracrewactionsthisturn) | Activation | `TECH_YELLOW` |

### AttackValueBonus

*Passive*. Ajoute `amount` à la valeur d'attaque, avant le bouclier. Porté par une carte : les attaques de son porteur ; porté par un événement : toutes les attaques.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `amount` | entier | -20..20 | **requis** | bonus (négatif possible) |
| `when` | Always / Overcharged / TargetHasTorment |  | `Always` | condition sur l'attaque |

Utilisée par : `A_005`, `A_017`, `A_019`, `EVT_SURCHARGE_IONIQUE`

### ParityAttackBonus

*Passive*. Valeur d'attaque : `even` si la somme des dés conservés est paire, `odd` sinon.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `even` | entier | -20..20 | **requis** | bonus si pair |
| `odd` | entier | -20..20 | **requis** | bonus si impair |

Utilisée par : `A_023`

### IgnoreTargetShield

*Passive*. Le bouclier de la cible compte pour 0 (ignoré, sa valeur ne change pas).

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `when` | Always / Overcharged / TargetHasTorment |  | `Always` | condition sur l'attaque |

Utilisée par : `A_016`

### AttackAdvantage

*Passive*. Avantage sur les attaques couvertes (on lance le lot deux fois, on garde le meilleur).

Utilisée par : `A_026`

### IncomingAttackDisadvantage

*Passive*. Désavantage sur les attaques subies par le porteur.

Utilisée par : `D_026`

### HealOnHit

*Passive*. Si l'attaque fait perdre des PV à la cible, l'attaquant gagne `amount` PV.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `amount` | entier | 1..99 | **requis** | PV gagnés |

Utilisée par : `A_011`

### StealShieldBeforeAttack

*Passive*. Avant le jet, prend jusqu'à `amount` points au bouclier de la cible et les ajoute au sien. Soumis à l'autorisation « modifier un bouclier ».

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `amount` | entier | 1..99 | **requis** | points volés au maximum |

Utilisée par : `A_001`

### DieBetMultiplier

*Passive*. Avant le jet, l'attaquant annonce une face ; si un dé conservé la montre, dégâts × `factor`.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `factor` | entier | 2..10 | **requis** | multiplicateur |

Utilisée par : `A_024`

### RerollShieldOnHit

*Passive*. Si l'attaque fait perdre des PV, l'attaquant peut relancer (1 dé) le bouclier d'un joueur qu'il a le droit de modifier.

Utilisée par : `A_003`

### DiscardTargetModifierOnHit

*Passive*. Si l'attaque fait perdre des PV, l'attaquant peut défausser un modificateur de la cible.

Utilisée par : `A_014`

### TormentTargetOnAttack

*Passive*. Après chaque attaque, l'attaquant pose `count` jetons de Tourment, un par un, sur les modificateurs de la cible (à son choix).

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `count` | entier | 1..10 | **requis** | jetons posés |

Utilisée par : `A_018`

### TormentMarketOnHit

*Passive*. Si l'attaque fait perdre des PV, l'attaquant pose 1 jeton sur `count` cartes différentes des marchés.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `count` | entier | 1..20 | **requis** | cartes visées |

Utilisée par : `A_020`

### ScorchedEarthOnKill

*Passive*. Si une attaque du porteur élimine un vaisseau : tous les autres joueurs perdent leurs modificateurs, puis leur bouclier passe à 0 (soumis à l'autorisation). La carte est ensuite défaussée.

Utilisée par : `A_010`

### DenyShieldChangeByOpponents

*Passive*. Refuse l'autorisation « modifier un bouclier » sur le bouclier du porteur quand la source est un adversaire.

Utilisée par : `D_001`

### CapIncomingAttackLoss

*Passive*. Plafonne à `max` les PV perdus par attaque. Options : seulement à `whenHpAtMost` PV ou moins (0 = toujours), jamais contre une attaque surchargée, défausse de la carte face à une attaque surchargée.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `max` | entier | 0..99 | **requis** | PV perdus au maximum |
| `unlessOvercharged` | booléen |  | `false` | sans effet contre une attaque surchargée |
| `whenHpAtMost` | entier | 0..999 | `0` | seulement si PV ≤ cette valeur (0 = toujours) |
| `discardWhenOvercharged` | booléen |  | `false` | carte défaussée quand une attaque surchargée est subie |

Utilisée par : `D_007`, `D_015`

### PreventNextAttackLoss

*Passive*. La prochaine attaque subie ne fait perdre aucun PV ; la carte est ensuite défaussée.

Utilisée par : `D_008`

### PreventFatalAttackLoss

*Passive*. Une attaque qui éliminerait le porteur ne lui fait perdre aucun PV ; la carte est ensuite défaussée.

Utilisée par : `D_010`

### BlockAttackerNextTurn

*Passive*. Après une attaque subie, cet attaquant ne peut pas attaquer le porteur pendant son prochain tour.

Utilisée par : `D_004`

### CapEnemyShield

*Passive*. À l'équipement, le porteur choisit un ennemi : le bouclier de cet ennemi est borné à (bouclier du porteur − `offset`), minimum 0.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `offset` | entier | 0..8 | **requis** | écart |

Utilisée par : `D_005`

### DictateAttackerAction

*Passive*. Quand une attaque fait perdre des PV au porteur, il impose l'action d'équipage (et les cibles) de l'attaquant pour son prochain tour.

Utilisée par : `D_006`

### HealWhenOthersDamaged

*Passive*. Chaque fois qu'une attaque fait perdre des PV à un autre joueur, le porteur gagne `amount` PV.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `amount` | entier | 1..99 | **requis** | PV gagnés |

Utilisée par : `D_013`

### ReflectAttackLoss

*Passive*. Quand une attaque fait perdre des PV au porteur, l'attaquant en perd autant (cause Reflect, sans réaction).

Utilisée par : `D_017`

### HealOnOwnTormentLoss

*Passive*. Le porteur regagne les PV qu'il perd à cause des jetons de Tourment.

Utilisée par : `D_018`

### TormentAttackerOnLoss

*Passive*. Quand une attaque fait perdre des PV au porteur, il pose `count` jeton(s) sur les modificateurs de l'attaquant.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `count` | entier | 1..10 | **requis** | jetons posés |

Utilisée par : `D_022`

### GambleOnHpLoss

*Passive*. À chaque perte de PV du porteur (hors Tourment et Reflect), 1 dé : pair, aucune perte ; impair, `penalty` PV de plus.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `penalty` | entier | 0..99 | **requis** | PV perdus en plus sur un impair |

Utilisée par : `D_025`

### CopyHighestShieldOnTurnStart

*Passive*. Au début de chaque tour du porteur, son bouclier copie le plus élevé des autres joueurs.

Utilisée par : `D_019`

### HpBalanceOnTurnStart

*Passive*. Au début de chaque tour du porteur : s'il a au moins `threshold` PV, il en perd `amount` (cause Self) ; sinon il en gagne `amount`.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `threshold` | entier | 1..999 | **requis** | seuil de PV |
| `amount` | entier | 1..99 | **requis** | PV perdus ou gagnés |

Utilisée par : `D_020`

### ShieldChangeOnTurnStart

*Passive*. Au début de chaque tour du porteur, son bouclier change de `amount` (borné).

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `amount` | entier | -8..8 | **requis** | variation |

Utilisée par : `D_027`

### PreRollDie

*Passive*. À la fin du marché du porteur, un dé est lancé ; il sert de premier dé à sa prochaine action d'équipage.

Utilisée par : `D_023`

### DisableShields

*Passive*. Les boucliers couverts comptent pour 0 dans les attaques (désactivés). Porté par un événement : tous les boucliers.

Utilisée par : `EVT_TEMPETE_ELECTRO_MAGNETIQUE`

### ExtraMarketPicks

*Passive*. `amount` carte(s) de plus à prendre au marché pour les joueurs couverts.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `amount` | entier | 1..5 | **requis** | cartes en plus |

Utilisée par : `EVT_NOUVEL_ARRIVAGE`

### DisableOpponentShieldThisTurn

*Activation*. Le bouclier d'un adversaire choisi est désactivé jusqu'à la fin du tour du porteur.

Utilisée par : `A_002`

### ConvertShieldToNextAttack

*Activation*. Le porteur retire X points de son bouclier (X choisi) et gagne +X à la valeur de sa prochaine attaque de ce tour.

Utilisée par : `A_004`

### NextAttackDamageMultiplier

*Activation*. Dégâts × `factor` pour la prochaine attaque de ce tour.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `factor` | entier | 2..10 | **requis** | multiplicateur |

Utilisée par : `A_006`

### NextAttackBonusPerNeutral

*Activation*. Prochaine attaque de ce tour : + `amount` par carte neutre visible dans les marchés (compté à l'attaque).

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `amount` | entier | 1..10 | **requis** | bonus par carte |

Utilisée par : `A_012`

### NextAttackOvercharged

*Activation*. La prochaine attaque de ce tour est surchargée sans consommer de jeton.

Utilisée par : `A_013`

### AllInAttack

*Activation*. Défausse les modificateurs du porteur ; sa prochaine attaque de ce tour est surchargée avec `dice` dés dont on garde les `keep` meilleurs.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `dice` | entier | 1..10 | **requis** | dés lancés |
| `keep` | entier | 1..10 | **requis** | dés conservés |

Utilisée par : `A_027`

### SwapOnNextAttack

*Activation*. Après la prochaine attaque de ce tour, le porteur peut échanger un modificateur de la cible avec la carte du même emplacement d'un autre joueur ou du marché correspondant.

Utilisée par : `A_025`

### DiscardOpponentModifiers

*Activation*. Défausse les deux modificateurs d'un adversaire choisi.

Utilisée par : `A_007`

### StealOpponentModifiers

*Activation*. Défausse les modificateurs du porteur, puis prend ceux d'un adversaire choisi (avec leurs jetons).

Utilisée par : `A_009`

### StealOrDestroyOpponentModifier

*Activation*. Choisit un modificateur d'un adversaire, puis le vole (dans l'emplacement correspondant) ou le détruit.

Utilisée par : `A_015`

### RefreshMarketAndPick

*Activation*. Recycle le marché `market`, puis le porteur y prend une carte.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `market` | Attack / Defense |  | **requis** | marché concerné |

Utilisée par : `A_008`, `D_009`

### RefreshAllMarkets

*Activation*. Recycle les deux marchés.

Utilisée par : `EVT_NOUVEL_ARRIVAGE`

### TormentOpponentAndNeighbours

*Activation*. Un adversaire choisi et ses deux voisins (jamais le porteur) reçoivent `count` jeton(s) sur chacun de leurs modificateurs.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `count` | entier | 1..10 | **requis** | jetons par modificateur |

Utilisée par : `A_021`

### ReactivateTorments

*Activation*. Tous les jetons posés sur des modificateurs équipés infligent de nouveau leur perte.

Utilisée par : `A_022`, `TECH_GREEN`

### ClearAllTormentsHealPer

*Activation*. Retire tous les jetons (joueurs et marchés) ; le porteur gagne `amount` PV par jeton retiré.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `amount` | entier | 0..99 | **requis** | PV par jeton |

Utilisée par : `D_021`

### TormentAllEquipped

*Activation*. Chaque joueur reçoit `count` jeton(s) sur chacun de ses modificateurs équipés.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `count` | entier | 1..10 | **requis** | jetons par modificateur |

Utilisée par : `EVT_NUEE_PARASITAIRE`

### ClearPlayerTorments

*Activation*. Retire tous les jetons des modificateurs équipés.

Utilisée par : `EVT_ESPACE_ASEPTISE`

### TormentValueBonus

*Activation*. Chaque jeton de Tourment inflige `amount` de plus, pour le reste de la partie (cumulable).

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `amount` | entier | 1..10 | **requis** | bonus par jeton |

Utilisée par : `TECH_GREEN`

### ConvertHpToShield

*Activation*. Le porteur convertit X PV (X choisi) en X points de bouclier, sans descendre à 0 PV ni dépasser la borne.

Utilisée par : `D_002`

### SwapTwoShields

*Activation*. Échange les boucliers de deux vaisseaux choisis (les deux changements doivent être autorisés).

Utilisée par : `D_003`

### SetOwnShield

*Activation*. Le bouclier du porteur passe à `value` (borné).

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `value` | entier | 0..99 | **requis** | nouvelle valeur |

Utilisée par : `D_011`

### SetAllShields

*Activation*. Le bouclier de chaque joueur passe à `value` (source : le porteur, ou le jeu pour un événement).

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `value` | entier | 0..99 | **requis** | nouvelle valeur |

Utilisée par : `EVT_FIN_DES_TEMPS`

### DisableOwnShieldUntilNextTurn

*Activation*. Le bouclier du porteur est désactivé jusqu'au début de son prochain tour.

Utilisée par : `D_016`

### RotateShields

*Activation*. Tous les boucliers tournent d'un siège dans le sens choisi ; les joueurs dont le bouclier ne peut pas être modifié gardent le leur.

Utilisée par : `D_024`

### Heal

*Activation*. Le porteur gagne `amount` PV.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `amount` | entier | 1..99 | **requis** | PV gagnés |

Utilisée par : `D_016`

### HealPerNeutral

*Activation*. Le porteur gagne `amount` PV par carte neutre visible dans les marchés.

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `amount` | entier | 1..10 | **requis** | PV par carte |

Utilisée par : `D_012`

### GainOvercharge

*Activation*. Le porteur gagne `amount` jeton(s) de surcharge (dans la limite du maximum).

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `amount` | entier | 1..10 | **requis** | jetons |

Utilisée par : `TECH_RED`

### KeepOverchargeUntilNextTurn

*Activation*. Le porteur ne peut pas perdre sa surcharge jusqu'au début de son prochain tour.

Utilisée par : `D_014`

### NextOverchargedAttackAdvantage

*Activation*. La prochaine attaque surchargée du porteur se fait avec avantage (sans limite de temps).

Utilisée par : `TECH_RED`

### DiscardAllEquippedModifiers

*Activation*. Tous les modificateurs équipés de tous les joueurs sont défaussés.

Utilisée par : `EVT_TROU_NOIR`, `EVT_FIN_DES_TEMPS`

### RedirectAttacksUntilNextTurn

*Activation*. Jusqu'au début de son prochain tour, le porteur peut dévier les attaques qui le visent vers un autre joueur (ni l'attaquant, ni lui).

Utilisée par : `TECH_BLUE`

### ExtraCrewActionsThisTurn

*Activation*. `amount` action(s) d'équipage de plus ce tour (toujours différentes).

| Paramètre | Type | Plage | Défaut | Rôle |
|---|---|---|---|---|
| `amount` | entier | 1..3 | **requis** | actions en plus |

Utilisée par : `TECH_YELLOW`
