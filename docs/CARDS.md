# Catalogue des cartes

> **Fichier généré** par `Vortex.ContentTool` depuis `core/Runtime/Data/*.json`. Ne pas le modifier à la main :
> modifier le JSON puis lancer `dotnet run --project dotnet/Vortex.ContentTool -- docs core/Runtime/Data docs`.
>
> Le **texte** est celui imprimé sur la carte. L'**arbitrage** en donne l'interprétation exacte selon le modèle d'effets ([RULES.md, partie B](RULES.md#partie-b--modèle-deffets)).
> Les **effets (moteur)** sont les briques qui implémentent l'arbitrage ([catalogue des briques](BRICKS.md)).
> Les icônes du jeu apparaissent entre crochets : [ATQ] attaque, [BOU] bouclier, [MOD] modificateur, [TOR] Tourment, [SUR] surcharge, [MKT] marché noir, [DIC] dé.

## Modificateurs d'attaque

27 cartes, 54 exemplaires.

| ID | Nom | Couleur | Usage | Ex. | À revoir |
|---|---|---|---|---|---|
| [`A_001`](#a_001) | Canon à particules | Bleu | Durable | 2 |  |
| [`A_002`](#a_002) | Langue de bois | Bleu | Usage unique | 2 |  |
| [`A_003`](#a_003) | Epuration | Bleu | Durable | 2 | oui |
| [`A_004`](#a_004) | La paix a un prix | Bleu | Usage unique | 2 |  |
| [`A_005`](#a_005) | Rétablir l'ordre | Bleu | Durable | 2 |  |
| [`A_006`](#a_006) | Grosse Bertha | Neutre | Usage unique | 2 |  |
| [`A_007`](#a_007) | Délestage forcé | Neutre | Usage unique | 2 |  |
| [`A_008`](#a_008) | Brocante spatiale | Neutre | Usage unique | 2 |  |
| [`A_009`](#a_009) | Jamais deux sans trois | Neutre | Usage unique | 2 |  |
| [`A_010`](#a_010) | Le grand final | Neutre | Déclenchement | 2 | oui |
| [`A_011`](#a_011) | Cruauté | Neutre | Durable | 2 |  |
| [`A_012`](#a_012) | Vindicte populaire | Neutre | Usage unique | 2 |  |
| [`A_013`](#a_013) | On envoie la sauce | Rouge | Usage unique | 2 |  |
| [`A_014`](#a_014) | Recels en tous genres | Rouge | Durable | 2 |  |
| [`A_015`](#a_015) | Racket | Rouge | Usage unique | 2 | oui |
| [`A_016`](#a_016) | BLITZKRIEG! | Rouge | Durable | 2 |  |
| [`A_017`](#a_017) | Dingo de la surcharge | Rouge | Durable | 2 |  |
| [`A_018`](#a_018) | Appendice laser | Vert | Durable | 2 |  |
| [`A_019`](#a_019) | Agents pathogènes | Vert | Durable | 2 |  |
| [`A_020`](#a_020) | Spores corrosifs | Vert | Durable | 2 |  |
| [`A_021`](#a_021) | Accident bactériologique | Vert | Usage unique | 2 | oui |
| [`A_022`](#a_022) | Réseau fongique | Vert | Usage unique | 2 | oui |
| [`A_023`](#a_023) | Pile ou face | Jaune | Durable | 2 |  |
| [`A_024`](#a_024) | Black Jack | Jaune | Durable | 2 |  |
| [`A_025`](#a_025) | Corruption du croupier | Jaune | Usage unique | 2 |  |
| [`A_026`](#a_026) | Carte sous l'coude | Jaune | Durable | 2 |  |
| [`A_027`](#a_027) | Tapis! | Jaune | Usage unique | 2 |  |

### A_001

**Canon à particules** · `Bleu` · `Durable` · 2 exemplaire(s)

> Vos [ATQ]**attaques** volent 2 points de [BOU]**bouclier** (vous ne pouvez pas dépasser 8 points de bouclier) à votre adversaire avant d'infliger vos points de dégats.

**Arbitrage** : Vol de 2 points avant le jet (étape « avant le jet »). Si la cible a moins de 2 points, on vole ce qu'elle a. Mon bouclier reste borné à 8. Le vol est une modification du bouclier de la cible : il est soumis à l'autorisation « modifier un bouclier ».

**Effets (moteur)** : [`StealShieldBeforeAttack(amount=2)`](BRICKS.md#stealshieldbeforeattack)

### A_002

**Langue de bois** · `Bleu` · `Usage unique` · 2 exemplaire(s)

> Vous désactivez le [BOU]**bouclier** d'un adversaire (au choix) pendant votre tour de jeu.

**Arbitrage** : Cible choisie à l'activation. Son bouclier est désactivé jusqu'à la fin de mon tour.

**Effets (moteur)** : [`DisableOpponentShieldThisTurn`](BRICKS.md#disableopponentshieldthisturn)

### A_003

**Epuration** · `Bleu` · `Durable` · 2 exemplaire(s) · **à revoir**

> Lorsque vos [ATQ]**attaques** infligent des dégâts, vous pouvez changer votre [BOU]**bouclier** ou celui d'un adversaire.

**Arbitrage** : Si mon attaque fait perdre des PV, je peux (facultatif) relancer 1d8 le bouclier d'un joueur de mon choix (« changer » = relancer). Soumis à l'autorisation « modifier un bouclier ».

**Effets (moteur)** : [`RerollShieldOnHit`](BRICKS.md#rerollshieldonhit)

### A_004

**La paix a un prix** · `Bleu` · `Usage unique` · 2 exemplaire(s)

> Vous pouvez convertir des points de [BOU]**bouclier** (maximum 8) en points de dégats pour votre prochaine [ATQ]**attaque**.

**Arbitrage** : Je choisis X ≤ mon bouclier. Mon bouclier baisse de X, et ma prochaine attaque de ce tour gagne +X.

**Effets (moteur)** : [`ConvertShieldToNextAttack`](BRICKS.md#convertshieldtonextattack)

### A_005

**Rétablir l'ordre** · `Bleu` · `Durable` · 2 exemplaire(s)

> Vos [ATQ] **attaques** infligent +4 points de dégats supplémentaires.

**Arbitrage** : +4 à la valeur d'attaque, avant le bouclier.

**Effets (moteur)** : [`AttackValueBonus(amount=4)`](BRICKS.md#attackvaluebonus)

### A_006

**Grosse Bertha** · `Neutre` · `Usage unique` · 2 exemplaire(s)

> Vous pouvez décider d'infliger x2 fois plus dégâts lors de votre prochaine [ATQ]**attaque**.

**Arbitrage** : ×2 sur les dégâts de la prochaine attaque de ce tour.

**Effets (moteur)** : [`NextAttackDamageMultiplier(factor=2)`](BRICKS.md#nextattackdamagemultiplier)

### A_007

**Délestage forcé** · `Neutre` · `Usage unique` · 2 exemplaire(s)

> Vous pouvez défausser les 2 [MOD]**modificateurs** de votre cible.

**Arbitrage** : Défausse les 2 modificateurs de la cible, avec leurs jetons.

**Effets (moteur)** : [`DiscardOpponentModifiers`](BRICKS.md#discardopponentmodifiers)

### A_008

**Brocante spatiale** · `Neutre` · `Usage unique` · 2 exemplaire(s)

> Vous pouvez entièrement re-piocher le [MKT] marché noir de [MOD]**modificateurs** [ATQ] d'**attaque** et choisir 1 nouveau [MOD]**modificateur**, le précédent étant défaussé.

**Arbitrage** : Recycle le marché ATK, puis je prends une carte dans le nouveau marché. Brocante est défaussée.

**Effets (moteur)** : [`RefreshMarketAndPick(market="Attack")`](BRICKS.md#refreshmarketandpick)

### A_009

**Jamais deux sans trois** · `Neutre` · `Usage unique` · 2 exemplaire(s)

> Vous pouvez voler les 2 [MOD]**modificateurs** de votre cible. Les votres sont défaussés.

**Arbitrage** : Je récupère les 2 modificateurs de la cible (avec leurs jetons) dans mes emplacements. Mes cartes sont défaussées.

**Effets (moteur)** : [`StealOpponentModifiers`](BRICKS.md#stealopponentmodifiers)

### A_010

**Le grand final** · `Neutre` · `Déclenchement` · 2 exemplaire(s) · **à revoir**

> Si vos [ATQ]**attaques** détruisent un vaisseau ennemi, tous les joueurs perdent leurs [MOD]**modificateurs** et leurs [BOU]**boucliers** passent à 0 (sauf vous).

**Arbitrage** : Si mon attaque élimine un vaisseau, tous les autres joueurs perdent leurs modificateurs et leur bouclier passe à 0. La carte est ensuite défaussée.

**Effets (moteur)** : [`ScorchedEarthOnKill`](BRICKS.md#scorchedearthonkill)

### A_011

**Cruauté** · `Neutre` · `Durable` · 2 exemplaire(s)

> Si vos [ATQ]**attaques** infligent des dégâts, vous gagnez +4 points de vie.

**Arbitrage** : +4 PV si les dégâts sont > 0.

**Effets (moteur)** : [`HealOnHit(amount=4)`](BRICKS.md#healonhit)

### A_012

**Vindicte populaire** · `Neutre` · `Usage unique` · 2 exemplaire(s)

> Vous pouvez infliger +1 point de dégât pour chaque [MOD]**modificateur** neutre face visible dans les [MKT]marchés noirs lors de votre prochaine [ATQ]**attaque**.

**Arbitrage** : +1 par carte neutre visible dans les deux marchés, compté à l'attaque.

**Effets (moteur)** : [`NextAttackBonusPerNeutral(amount=1)`](BRICKS.md#nextattackbonusperneutral)

### A_013

**On envoie la sauce** · `Rouge` · `Usage unique` · 2 exemplaire(s)

> Vous pouvez surcharger votre prochaine [ATQ]**attaque**.

**Arbitrage** : La prochaine attaque de ce tour est surchargée, sans consommer de jeton de surcharge.

**Effets (moteur)** : [`NextAttackOvercharged`](BRICKS.md#nextattackovercharged)

### A_014

**Recels en tous genres** · `Rouge` · `Durable` · 2 exemplaire(s)

> Vos [ATQ]**attaques** infligeant des dégats vous permettent de défausser 1 [MOD]**modificateur** de votre cible.

**Arbitrage** : Si mon attaque fait perdre des PV, je peux (facultatif) défausser 1 modificateur de la cible, à mon choix.

**Effets (moteur)** : [`DiscardTargetModifierOnHit`](BRICKS.md#discardtargetmodifieronhit)

### A_015

**Racket** · `Rouge` · `Usage unique` · 2 exemplaire(s) · **à revoir**

> Vous pouvez subtiliser ou de détruire 1 [MOD]**modificateur** de votre choix à votre cible.

**Arbitrage** : Je choisis de voler ou de détruire. Une carte volée remplace la mienne dans l'emplacement correspondant.

**Effets (moteur)** : [`StealOrDestroyOpponentModifier`](BRICKS.md#stealordestroyopponentmodifier)

### A_016

**BLITZKRIEG!** · `Rouge` · `Durable` · 2 exemplaire(s)

> Vos [ATQ]**attaques** [SUR]**surchargées** ignorent le [BOU]**bouclier** de votre cible.

**Arbitrage** : Mes attaques surchargées ignorent le bouclier.

**Effets (moteur)** : [`IgnoreTargetShield(when="Overcharged")`](BRICKS.md#ignoretargetshield)

### A_017

**Dingo de la surcharge** · `Rouge` · `Durable` · 2 exemplaire(s)

> Vos [ATQ]**attaques** [SUR]**surchargées** infligent +4 points de dégâts supplémentaires.

**Arbitrage** : +4 sur mes attaques surchargées.

**Effets (moteur)** : [`AttackValueBonus(amount=4, when="Overcharged")`](BRICKS.md#attackvaluebonus)

### A_018

**Appendice laser** · `Vert` · `Durable` · 2 exemplaire(s)

> Vos [ATQ]**attaques** posent 2 jetons de [TOR] tourment sur les [MOD] **modificateurs** de votre cible (au choix).

**Arbitrage** : À chaque attaque, même sans dégâts (obligatoire) : 2 jetons répartis à mon choix sur les modificateurs de la cible.

**Effets (moteur)** : [`TormentTargetOnAttack(count=2)`](BRICKS.md#tormenttargetonattack)

### A_019

**Agents pathogènes** · `Vert` · `Durable` · 2 exemplaire(s)

> Vos [ATQ]**attaques** infligent +4 points de dégats supplémentaires si (au moins) 1 des modificateurs de votre cible est atteint par 1 jeton de[TOR] tourment.

**Arbitrage** : +4 si au moins un modificateur de la cible porte un Tourment.

**Effets (moteur)** : [`AttackValueBonus(amount=4, when="TargetHasTorment")`](BRICKS.md#attackvaluebonus)

### A_020

**Spores corrosifs** · `Vert` · `Durable` · 2 exemplaire(s)

> Si vos [ATQ]**attaques** infligent des dégats, vous posez 5 jetons de [TOR]tourment sur 5 **modificateurs** de votre choix des [MKT] marchés noirs.

**Arbitrage** : Si les dégâts sont > 0, 5 jetons sur 5 cartes différentes des marchés (à mon choix).

**Effets (moteur)** : [`TormentMarketOnHit(count=5)`](BRICKS.md#tormentmarketonhit)

### A_021

**Accident bactériologique** · `Vert` · `Usage unique` · 2 exemplaire(s) · **à revoir**

> Vous pouvez poser 2 jetons de [TOR]tourment sur les 2 **modificateurs** de votre cible et des joueurs adjacents.

**Arbitrage** : 1 jeton par modificateur, pour la cible et ses deux voisins de siège (moi exclu).

**Effets (moteur)** : [`TormentOpponentAndNeighbours(count=1)`](BRICKS.md#tormentopponentandneighbours)

### A_022

**Réseau fongique** · `Vert` · `Usage unique` · 2 exemplaire(s) · **à revoir**

> Vous réactivez les jetons de [TOR]tourment en jeu, ce qui inflige de nouveau les dégats à chaque joueur concerné.

**Arbitrage** : Réactivation des Tourments (RULES A7).

**Effets (moteur)** : [`ReactivateTorments`](BRICKS.md#reactivatetorments)

### A_023

**Pile ou face** · `Jaune` · `Durable` · 2 exemplaire(s)

> Vos prochaines [ATQ]**attaques** infligent +3 points de dégat en cas de tirage au [DIC] dé pair __ou__ infligent -1 points de dégat en cas de tirage au [DIC]dé impair.

**Arbitrage** : Somme des dés conservés paire : +3. Impaire : −1.

**Effets (moteur)** : [`ParityAttackBonus(even=3, odd=-1)`](BRICKS.md#parityattackbonus)

### A_024

**Black Jack** · `Jaune` · `Durable` · 2 exemplaire(s)

> Pariez sur un [DIC]dé. Si vous visez juste, votre [ATQ] **attaque** inflige x2 fois plus de dégats. Sinon, vous [ATQ] **attaquez** normalement.

**Arbitrage** : Annonce avant le jet. ×2 si un dé conservé est égal à la valeur annoncée.

**Effets (moteur)** : [`DieBetMultiplier(factor=2)`](BRICKS.md#diebetmultiplier)

### A_025

**Corruption du croupier** · `Jaune` · `Usage unique` · 2 exemplaire(s)

> Votre prochaine [ATQ] **attaque,** vous permet d'échanger 1 [MOD] modificateur de votre cible avec 1 [MOD] modificateur du [MKT] marché noir correspondant ou d'un autre joueur.

**Arbitrage** : Après ma prochaine attaque de ce tour, même sans dégâts, je peux (facultatif) échanger 1 modificateur de la cible avec la carte du même emplacement d'un autre joueur (ni la cible, ni moi) ou du marché correspondant.

**Effets (moteur)** : [`SwapOnNextAttack`](BRICKS.md#swaponnextattack)

### A_026

**Carte sous l'coude** · `Jaune` · `Durable` · 2 exemplaire(s)

> Vos [ATQ]**attaques** se font avec un avantage.
> (Jetez 2 dés et gardez le meilleur résultat)

**Arbitrage** : Avantage sur mes attaques.

**Effets (moteur)** : [`AttackAdvantage`](BRICKS.md#attackadvantage)

### A_027

**Tapis!** · `Jaune` · `Usage unique` · 2 exemplaire(s)

> Vous pouvez décider de défausser vos 2 [MOD]**modificateurs** afin de réaliser 1 [ATQ]**attaque** [SUR]**surchargée** avec avantage. lors de votre prochaine [ATQ]**attaque,** vous lancez 3 [DIC]dés et conservez les 2 meilleurs.

**Arbitrage** : Je défausse mes 2 modificateurs, puis ma prochaine attaque de ce tour est surchargée avec 3d8, dont on garde les 2 meilleurs. Mon jeton n'est pas consommé.

**Effets (moteur)** : [`AllInAttack(dice=3, keep=2)`](BRICKS.md#allinattack)

## Modificateurs de défense

27 cartes, 50 exemplaires.

| ID | Nom | Couleur | Usage | Ex. | À revoir |
|---|---|---|---|---|---|
| [`D_001`](#d_001) | Intouchable | Bleu | Durable | 2 |  |
| [`D_002`](#d_002) | Dommage collatéral | Bleu | Usage unique | 2 |  |
| [`D_003`](#d_003) | Ni vu ni connu | Bleu | Usage unique | 2 |  |
| [`D_004`](#d_004) | Favoritisme | Bleu | Durable | 2 |  |
| [`D_005`](#d_005) | Sabotage électoral | Bleu | Durable | 2 |  |
| [`D_006`](#d_006) | Mutinerie syndicale | Neutre | Déclenchement | 1 |  |
| [`D_007`](#d_007) | Sous-couche blindée | Neutre | Déclenchement | 2 |  |
| [`D_008`](#d_008) | T'as pas entendu un truc? | Neutre | Déclenchement | 2 |  |
| [`D_009`](#d_009) | Les affaires sont les affaires | Neutre | Usage unique | 2 |  |
| [`D_010`](#d_010) | Niaque | Neutre | Déclenchement | 2 |  |
| [`D_011`](#d_011) | Générateur auxiliaire | Neutre | Déclenchement | 2 |  |
| [`D_012`](#d_012) | Vente de pièces détachées | Neutre | Usage unique | 2 |  |
| [`D_013`](#d_013) | Vautours | Rouge | Durable | 2 |  |
| [`D_014`](#d_014) | Nothing else matters | Rouge | Usage unique | 2 |  |
| [`D_015`](#d_015) | Lève la tête, bombe le torse | Rouge | Durable | 2 |  |
| [`D_016`](#d_016) | Orgueil | Rouge | Usage unique | 2 |  |
| [`D_017`](#d_017) | Loi du Talion | Rouge | Durable | 1 |  |
| [`D_018`](#d_018) | Régénération parasitaire | Vert | Durable | 2 |  |
| [`D_019`](#d_019) | Mimétisme cellulaire | Vert | Durable | 2 |  |
| [`D_020`](#d_020) | Tout est une question d'équilibre | Vert | Durable | 2 |  |
| [`D_021`](#d_021) | Quarantaine obligatoire | Vert | Usage unique | 2 |  |
| [`D_022`](#d_022) | Je te touche pas avec un bâton | Vert | Durable | 1 |  |
| [`D_023`](#d_023) | Main sûre | Jaune | Durable | 2 |  |
| [`D_024`](#d_024) | Roulette | Jaune | Usage unique | 2 |  |
| [`D_025`](#d_025) | Chance de cocu | Jaune | Durable | 2 |  |
| [`D_026`](#d_026) | Le casino gagne toujours | Jaune | Durable | 2 |  |
| [`D_027`](#d_027) | La banque | Jaune | Durable | 1 |  |

### D_001

**Intouchable** · `Bleu` · `Durable` · 2 exemplaire(s)

> Votre [BOU] **bouclier** ne peut pas être modifié par vos adversaires.

**Arbitrage** : Refuse toute modification de la valeur de mon bouclier dont la source est un adversaire (autorisation « modifier un bouclier »). Désactiver ou ignorer un bouclier n'est pas une modification de sa valeur. Les événements n'ont pas de source joueur : ils ne sont pas bloqués.

**Effets (moteur)** : [`DenyShieldChangeByOpponents`](BRICKS.md#denyshieldchangebyopponents)

### D_002

**Dommage collatéral** · `Bleu` · `Usage unique` · 2 exemplaire(s)

> Vous pouvez convertir vos points de vie en points de [BOU] **bouclier,** sans dépasser 8 points de [BOU] **bouclier**.

**Arbitrage** : Je convertis X PV en X points de bouclier. On ne peut pas descendre à 0 PV ni dépasser un bouclier de 8. Source `Self`.

**Effets (moteur)** : [`ConvertHpToShield`](BRICKS.md#converthptoshield)

### D_003

**Ni vu ni connu** · `Bleu` · `Usage unique` · 2 exemplaire(s)

> Vous pouvez échanger la valeur des [BOU] **boucliers** de 2 vaisseaux.

**Arbitrage** : Échange les boucliers de 2 vaisseaux au choix. Chaque changement de valeur est soumis à l'autorisation « modifier un bouclier » ; si l'un des deux est refusé, l'échange n'a pas lieu.

**Effets (moteur)** : [`SwapTwoShields`](BRICKS.md#swaptwoshields)

### D_004

**Favoritisme** · `Bleu` · `Durable` · 2 exemplaire(s)

> Lorsque vous subissez une [ATQ]**attaque,** le même joueur ne peut pas vous [ATQ]**attaquer** de nouveau au tour suivant.

**Arbitrage** : Après une attaque subie, cet attaquant ne peut pas me cibler à son prochain tour.

**Effets (moteur)** : [`BlockAttackerNextTurn`](BRICKS.md#blockattackernextturn)

### D_005

**Sabotage électoral** · `Bleu` · `Durable` · 2 exemplaire(s)

> Le [BOU] **bouclier** d'un vaisseau ennemi de votre choix ne peut pas dépasser la valeur de votre [BOU]  **bouclier** - 2 (minimum 0).

**Arbitrage** : Ennemi choisi à l'équipement. Ajoute une borne maximale au bouclier de cet ennemi : (mon bouclier − 2), minimum 0, recalculée en continu (calcul « bornes du bouclier »).

**Effets (moteur)** : [`CapEnemyShield(offset=2)`](BRICKS.md#capenemyshield)

### D_006

**Mutinerie syndicale** · `Neutre` · `Déclenchement` · 1 exemplaire(s)

> Lorsque vous subissez des dégâts, vous choisissez l'action d'équipage du joueur vous [ATQ]**attaquant** pour son prochain tour.

**Arbitrage** : Si une attaque me fait perdre des PV : je choisis l'action d'équipage de l'attaquant pour son prochain tour, et ses cibles (je peux m'imposer comme cible ; « aucune action » n'est pas un choix possible). Si l'action est impossible à ce moment-là, il ne fait aucune action d'équipage.

**Effets (moteur)** : [`DictateAttackerAction`](BRICKS.md#dictateattackeraction)

### D_007

**Sous-couche blindée** · `Neutre` · `Déclenchement` · 2 exemplaire(s)

> Les [ATQ]**attaques** subies ne peuvent infliger qu'un maximum d'1 point de dégât, sauf s'il s'agit d'une [ATQ]**attaque** [SUR] surchargée. Cette carte est défaussée si vous subissez une **attaque** surchargée.

**Arbitrage** : 1 dégât maximum par attaque, sauf attaque surchargée. Une attaque surchargée défausse la carte.

**Effets (moteur)** : [`CapIncomingAttackLoss(discardWhenOvercharged=true, max=1, unlessOvercharged=true)`](BRICKS.md#capincomingattackloss)

### D_008

**T'as pas entendu un truc?** · `Neutre` · `Déclenchement` · 2 exemplaire(s)

> Vous ne subissez pas de dégâts lors de la prochaine [ATQ]**attaque** subie.

**Arbitrage** : La prochaine attaque subie inflige 0 dégât. La carte est ensuite défaussée.

**Effets (moteur)** : [`PreventNextAttackLoss`](BRICKS.md#preventnextattackloss)

### D_009

**Les affaires sont les affaires** · `Neutre` · `Usage unique` · 2 exemplaire(s)

> Vous pouvez entièrement re-piocher le [MKT] marché noir de [MOD] modificateurs de [BOU] défense et choisir 1 nouveau [MOD] modificateur, le précédent étant défaussé.

**Arbitrage** : Recycle le marché DEF, puis je prends une carte dans le nouveau marché. Cette carte-ci est défaussée.

**Effets (moteur)** : [`RefreshMarketAndPick(market="Defense")`](BRICKS.md#refreshmarketandpick)

### D_010

**Niaque** · `Neutre` · `Déclenchement` · 2 exemplaire(s)

> Vous êtes protégé contre la prochaine [ATQ]**attaque** fatale subie.

**Arbitrage** : Si une attaque devait m'éliminer, elle inflige 0 dégât. La carte est ensuite défaussée.

**Effets (moteur)** : [`PreventFatalAttackLoss`](BRICKS.md#preventfatalattackloss)

### D_011

**Générateur auxiliaire** · `Neutre` · `Déclenchement` · 2 exemplaire(s)

> Votre [BOU] **bouclier** passe à 8 points, puis cette carte est défaussée.

**Arbitrage** : Activation manuelle (RULES A5.3) : mon bouclier passe à 8, puis la carte est défaussée.

**Effets (moteur)** : [`SetOwnShield(value=8)`](BRICKS.md#setownshield)

### D_012

**Vente de pièces détachées** · `Neutre` · `Usage unique` · 2 exemplaire(s)

> Vous pouvez gagner +1 point de vie pour chaque modificateur neutre face visible dans les marchés noirs.

**Arbitrage** : +1 PV par carte neutre visible dans les marchés.

**Effets (moteur)** : [`HealPerNeutral(amount=1)`](BRICKS.md#healperneutral)

### D_013

**Vautours** · `Rouge` · `Durable` · 2 exemplaire(s)

> Dès que des dégats sont infligés par un joueur à un autre, vous gagnez +1 point de vie (hors jetons de [TOR] tourment).

**Arbitrage** : +1 PV pour chaque instance de dégâts d'un joueur à un autre. Pas pour la victime, et pas pour les sources `Torment` ou `Reflect`.

**Effets (moteur)** : [`HealWhenOthersDamaged(amount=1)`](BRICKS.md#healwhenothersdamaged)

### D_014

**Nothing else matters** · `Rouge` · `Usage unique` · 2 exemplaire(s)

> Vous ne pouvez pas perdre votre [SUR]**surcharge** jusqu'à votre prochain tour.

**Arbitrage** : Je ne peux pas perdre ma surcharge jusqu'au début de mon prochain tour.

**Effets (moteur)** : [`KeepOverchargeUntilNextTurn`](BRICKS.md#keepoverchargeuntilnextturn)

### D_015

**Lève la tête, bombe le torse** · `Rouge` · `Durable` · 2 exemplaire(s)

> Si vos points de vie sont en dessous ou égaux à 10, vous ne pouvez pas subir plus d' 1 point de dégat à chaque [ATQ]**attaque,** sauf s'il s'agit d'une [ATQ]**attaque** [SUR] surchargée.

**Arbitrage** : Si PV ≤ 10 : 1 dégât maximum par attaque, sauf attaque surchargée.

**Effets (moteur)** : [`CapIncomingAttackLoss(max=1, unlessOvercharged=true, whenHpAtMost=10)`](BRICKS.md#capincomingattackloss)

### D_016

**Orgueil** · `Rouge` · `Usage unique` · 2 exemplaire(s)

> Vous pouvez récupérer +15 points de vie, mais votre [BOU]**bouclier** est désactivé pendant 1 tour.

**Arbitrage** : +15 PV (plafonnés au maximum). Mon bouclier est désactivé jusqu'au début de mon prochain tour.

**Effets (moteur)** : [`Heal(amount=15)`](BRICKS.md#heal) · [`DisableOwnShieldUntilNextTurn`](BRICKS.md#disableownshielduntilnextturn)

### D_017

**Loi du Talion** · `Rouge` · `Durable` · 1 exemplaire(s)

> Lorsque vous perdez des points de vie, le joueur vous [ATQ]**attaquant** perd un nombre équivalent de points de vie (hors jetons de [TOR]tourment).

**Arbitrage** : Si je perds des PV sur une attaque, l'attaquant perd le même nombre de PV (source `Reflect`).

**Effets (moteur)** : [`ReflectAttackLoss`](BRICKS.md#reflectattackloss)

### D_018

**Régénération parasitaire** · `Vert` · `Durable` · 2 exemplaire(s)

> Vous récupérez +1 point de vie par dégât infligé par des jetons de [TOR] tourment.

**Arbitrage** : +1 PV pour chaque PV que je perds à cause d'un Tourment.

**Effets (moteur)** : [`HealOnOwnTormentLoss`](BRICKS.md#healonowntormentloss)

### D_019

**Mimétisme cellulaire** · `Vert` · `Durable` · 2 exemplaire(s)

> Au début de votre prochain tour et des suivants, vous copiez la valeur du [BOU] **bouclier** le plus élevé en jeu.

**Arbitrage** : À partir de mon prochain tour, à chaque début de tour, mon bouclier copie le plus élevé des autres joueurs.

**Effets (moteur)** : [`CopyHighestShieldOnTurnStart`](BRICKS.md#copyhighestshieldonturnstart)

### D_020

**Tout est une question d'équilibre** · `Vert` · `Durable` · 2 exemplaire(s)

> Si vos points de vie sont supérieurs ou égaux à 10, vous perdez -3 points de vie au début de chacun de vos tours de jeu.
> Si vos points de vie sont inférieurs à 10, vous gagnez +3 points de vie au début de chacun de vos tours de jeu.

**Arbitrage** : En début de tour : −3 PV si PV ≥ 10 (source `Self`), +3 PV si PV < 10.

**Effets (moteur)** : [`HpBalanceOnTurnStart(amount=3, threshold=10)`](BRICKS.md#hpbalanceonturnstart)

### D_021

**Quarantaine obligatoire** · `Vert` · `Usage unique` · 2 exemplaire(s)

> Vous supprimez tous jetons de [TOR] tourment sur le plateau et gagnez +3 points de vie par tourment retiré.

**Arbitrage** : Retire tous les jetons (joueurs et marchés). +3 PV par jeton retiré.

**Effets (moteur)** : [`ClearAllTormentsHealPer(amount=3)`](BRICKS.md#clearalltormentshealper)

### D_022

**Je te touche pas avec un bâton** · `Vert` · `Durable` · 1 exemplaire(s)

> Lorsque vous subissez des dégâts, le joueur vous [ATQ]attaquant reçoit 1 jeton de [TOR]  tourment sur 1 de ses [MOD] modificateurs (selon votre choix).

**Arbitrage** : Si je subis des dégâts d'une attaque, je pose 1 jeton sur un modificateur de l'attaquant (à mon choix).

**Effets (moteur)** : [`TormentAttackerOnLoss(count=1)`](BRICKS.md#tormentattackeronloss)

### D_023

**Main sûre** · `Jaune` · `Durable` · 2 exemplaire(s)

> Vous lancez le [DIC] dé avant de décider quelle sera votre action d'équipage.

**Arbitrage** : Au début de ma phase d'équipage, je lance 1d8. Ce dé sert de premier dé pour l'action choisie (attaque, reparamétrage ou sabotage).

**Effets (moteur)** : [`PreRollDie`](BRICKS.md#prerolldie)

### D_024

**Roulette** · `Jaune` · `Usage unique` · 2 exemplaire(s)

> Vous pouvez échanger les [BOU] **boucliers** entre les joueurs adjacents. Vous décidez du sens.

**Arbitrage** : Tous les boucliers tournent d'un siège, dans le sens que je choisis. Un joueur dont le bouclier ne peut pas être modifié (autorisation refusée) garde le sien et est sauté.

**Effets (moteur)** : [`RotateShields`](BRICKS.md#rotateshields)

### D_025

**Chance de cocu** · `Jaune` · `Durable` · 2 exemplaire(s)

> Lorsque vous perdez des points de vie, lancez un dé (hors jetons de [TOR] tourment).
> Si le résultat est pair, vous ne perdez pas de points de vie,
> Si le résultat est impair, vous subissez +3 points de dégats supplémentaires.

**Arbitrage** : Quand je perds des PV, hors Tourment, Reflect et pertes que je m'inflige (cause Self) : 1d8. Pair : 0 perte. Impair : +3.

**Effets (moteur)** : [`GambleOnHpLoss(penalty=3)`](BRICKS.md#gambleonhploss)

### D_026

**Le casino gagne toujours** · `Jaune` · `Durable` · 2 exemplaire(s)

> Les [ATQ]**attaques** que vous subissez se font avec un désavantage (l'adversaire lance 2 [DIC] dés, et vous subissez le résultat le plus faible).

**Arbitrage** : Désavantage sur les attaques que je subis.

**Effets (moteur)** : [`IncomingAttackDisadvantage`](BRICKS.md#incomingattackdisadvantage)

### D_027

**La banque** · `Jaune` · `Durable` · 1 exemplaire(s)

> Au début de votre prochain tour et des suivants, votre [BOU] **bouclier** augmente de +2 (maximum 8).

**Arbitrage** : À partir de mon prochain tour, à chaque début de tour : bouclier +2 (maximum 8).

**Effets (moteur)** : [`ShieldChangeOnTurnStart(amount=2)`](BRICKS.md#shieldchangeonturnstart)

## Événements

| ID | Nom | Ex. |
|---|---|---|
| `EVT_TEMPETE_ELECTRO_MAGNETIQUE` | tempête électro-magnetique | 2 |
| `EVT_TROU_NOIR` | trou noir | 2 |
| `EVT_NOUVEL_ARRIVAGE` | Nouvel arrivage | 2 |
| `EVT_SURCHARGE_IONIQUE` | **surcharge** Ionique | 2 |
| `EVT_NUEE_PARASITAIRE` | Nuée parasitaire | 2 |
| `EVT_ESPACE_ASEPTISE` | Espace aseptisé | 2 |
| `EVT_LE_CALME_AVANT_LA_TEMPETE` | Le calme avant la tempête | 2 |
| `EVT_FIN_DES_TEMPS` | Fin des temps | 1 |

### EVT_TEMPETE_ELECTRO_MAGNETIQUE — tempête électro-magnetique

2 exemplaire(s)

> Les **boucliers** sont désactivés pendant ce tour de jeu.

**Arbitrage** : Tous les boucliers sont désactivés pendant la manche.

**Effets (moteur)** : [`DisableShields`](BRICKS.md#disableshields)

### EVT_TROU_NOIR — trou noir

2 exemplaire(s)

> Tous les joueurs perdent leurs modificateurs.

**Arbitrage** : Tous les modificateurs équipés sont défaussés. Les marchés ne changent pas.

**Effets (moteur)** : [`DiscardAllEquippedModifiers`](BRICKS.md#discardallequippedmodifiers)

### EVT_NOUVEL_ARRIVAGE — Nouvel arrivage

2 exemplaire(s)

> Les deux marchés noirs sont entièrement repiochés, tous les joueurs peuvent piocher deux modificateurs pendant ce tour.

**Arbitrage** : Les deux marchés sont recyclés. Pendant la manche, chaque joueur peut prendre 2 cartes, quels que soient les marchés.

**Effets (moteur)** : [`RefreshAllMarkets`](BRICKS.md#refreshallmarkets) · [`ExtraMarketPicks(amount=1)`](BRICKS.md#extramarketpicks)

### EVT_SURCHARGE_IONIQUE — **surcharge** Ionique

2 exemplaire(s)

> Toutes les **attaques** infligent +4 points de dégâts pendant ce tour.

**Arbitrage** : +4 à la valeur de toutes les attaques pendant la manche.

**Effets (moteur)** : [`AttackValueBonus(amount=4)`](BRICKS.md#attackvaluebonus)

### EVT_NUEE_PARASITAIRE — Nuée parasitaire

2 exemplaire(s)

> Tous les joueurs posent 2 jetons de tourment sur leurs 2 modificateurs.

**Arbitrage** : 1 jeton sur chaque modificateur équipé de chaque joueur (1 PV perdu par jeton).

**Effets (moteur)** : [`TormentAllEquipped(count=1)`](BRICKS.md#tormentallequipped)

### EVT_ESPACE_ASEPTISE — Espace aseptisé

2 exemplaire(s)

> Tous les joueurs retirent leurs jetons de tourment.

**Arbitrage** : Tous les jetons portés par les joueurs sont retirés.

**Effets (moteur)** : [`ClearPlayerTorments`](BRICKS.md#clearplayertorments)

### EVT_LE_CALME_AVANT_LA_TEMPETE — Le calme avant la tempête

2 exemplaire(s)

> Rien ne se passe.

**Arbitrage** : Aucun effet.

**Effets (moteur)** : aucun

### EVT_FIN_DES_TEMPS — Fin des temps

1 exemplaire(s)

> Tous les modificateurs disparaissent, les joueurs n'ont plus de bouclier.

**Arbitrage** : Hors paquet : se déclenche à la place de l'événement de la manche DoomRound[n] (RULES A4). Tous les modificateurs équipés sont défaussés et tous les boucliers passent à 0. Effet ponctuel : les boucliers peuvent ensuite être reconstruits.

**Effets (moteur)** : [`DiscardAllEquippedModifiers`](BRICKS.md#discardallequippedmodifiers) · [`SetAllShields(value=0)`](BRICKS.md#setallshields)

## Technologies (combos)

### TECH_BLUE — Ordre

`Bleu`

> Vous pouvez dévier les **attaques** subies jusqu'au début de votre prochain tour.

**Arbitrage** : Jusqu'au début de mon prochain tour, quand je suis la cible d'une attaque, je peux la dévier vers un autre joueur vivant, ni l'attaquant ni moi-même (RULES A6). Une attaque déviée ne peut pas être re-déviée.

**Effets (moteur)** : [`RedirectAttacksUntilNextTurn`](BRICKS.md#redirectattacksuntilnextturn)

### TECH_YELLOW — Casino Cosmique

`Jaune`

> Vous pouvez effectuer 2 actions d'équipage (différentes) consécutives.

**Arbitrage** : Ce tour-ci, je fais 2 actions d'équipage différentes.

**Effets (moteur)** : [`ExtraCrewActionsThisTurn(amount=1)`](BRICKS.md#extracrewactionsthisturn)

### TECH_RED — Rebelles

`Rouge`

> Vous **surcharge.** Votre prochaine **attaque** surchargée est réalisée avec avantage (vous tirez 2 fois 2 dés et gardez le meilleur tirage).

**Arbitrage** : Je gagne un jeton de surcharge (dans la limite du maximum). Ma prochaine attaque surchargée se fait avec avantage.

**Effets (moteur)** : [`GainOvercharge(amount=1)`](BRICKS.md#gainovercharge) · [`NextOverchargedAttackAdvantage`](BRICKS.md#nextoverchargedattackadvantage)

### TECH_GREEN — Abomination organique

`Vert`

> Les jetons de tourment infligent +1 point de dégât supplémentaire pour le reste de la partie. Vous réactivez les jetons de [TOR]  tourment en jeu, ce qui inflige de nouveau les dégats à chaque joueur concerné.

**Arbitrage** : Les jetons de Tourment infligent +1 pour le reste de la partie (cumulable, effet global). Puis tous les Tourments en jeu sont réactivés (RULES A7).

**Effets (moteur)** : [`TormentValueBonus(amount=1)`](BRICKS.md#tormentvaluebonus) · [`ReactivateTorments`](BRICKS.md#reactivatetorments)
