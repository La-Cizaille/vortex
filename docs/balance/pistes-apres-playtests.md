# Pistes après les playtests (2026-09-26)

> **Statut : pistes de discussion, pas une demande de développement.** Le moteur fonctionne en l'état. Rien de ce document ne doit être codé, ni par la session principale ni par une autre, tant que le game designer ne l'a pas demandé explicitement. Les orientations ci-dessous sont des préférences exprimées pour poursuivre la discussion, pas des décisions : elles ne figurent pas au journal des arbitrages.

Après ses premières parties, le game designer a noté six pistes, trois d'équilibrage et trois de nouvelles fonctionnalités. Nous en avons discuté : pour chacune, une orientation se dégage, qui sert de base à la suite de la discussion.

**Comment le lire.** Chaque piste décrit le problème, l'orientation qui se dégage et des textes possibles. Elle note aussi, **pour mémoire**, ce qu'un développement demanderait : moteur, contenu, mesures, tests, client. Ces notes servent à estimer le coût d'une piste et à nourrir la discussion ; ce n'est pas un cahier des charges.

**Si une piste passe un jour au développement**, à la demande du designer, elle suivra la méthode habituelle (ADR-0011, [plan d'équilibrage](README.md#plan-et-avancement)) :
- une règle à l'étude passe par une option de configuration ou une variante, qui garde par défaut la règle actuelle ;
- le simulateur compare la référence et la variante sur 2 000 parties à 5 joueurs ;
- le designer décide sur rapport.

| § | Piste | Orientation pour la discussion | Ce qu'un développement demanderait | Mesure possible |
|---|---|---|---|---|
| 1 | Régénération parasitaire (D_018) | Soigne avec les pertes de Tourment **des autres** | Une brique avec une portée | Écart de victoire de la carte |
| 2 | Nouvel arrivage | **Marchés à 7 cartes**, une seule prise | Un point d'interception et une brique | Combos pendant la manche de l'événement |
| 3 | Prime sur le leader | Un **événement**, « Avis de recherche » | Une condition d'attaque | Mesures du lot A |
| 4 | Réanimation | Explorer une **carte « Remorquage »** | Une action élémentaire, un événement, une brique | Temps passé hors jeu |
| 5 | Alliances | Explorer une **carte « Pacte de non-agression »** | Une brique, une durée de statut | Surtout en partie réelle |
| 6 | Combo | **Laissé ouvert, rien ne change** | Rien | Rien |

Les noms proposés sont provisoires : les noms et les textes pourront changer avec la direction artistique (ARB-93, ARB-97). Les numéros de carte et d'événement sont attribués au moment de l'ajout ; ceux cités ici sont les prochains libres le 2026-09-26.

---

## 1. Régénération parasitaire (D_018)

**Problème.** « La carte ne fonctionne pas. » Ce n'est pas un bug : le moteur applique l'arbitrage écrit, « +1 PV pour chaque PV que je perds à cause d'un Tourment ». Le porteur perd 1 PV, il en regagne 1 : l'effet net est nul, à l'écran comme dans le journal. La [référence v4](2026-09-24-reference-v4.md) le confirme : la carte ne change rien aux chances de victoire (−0,0 pt). Le texte imprimé, lui, dit « +1 point de vie par dégât infligé par des jetons de tourment » : il décrit une carte parasite.

**Orientation.** Le porteur gagne **+1 PV pour chaque PV que les autres joueurs perdent à cause d'un Tourment**.

**Textes proposés.**
- Texte : « Vous récupérez +1 point de vie par dégât infligé **aux autres joueurs** par des jetons de <TOR> tourment. »
- Arbitrage : « +1 PV pour chaque PV qu'un autre joueur perd à cause d'un Tourment, y compris quand les Tourments sont réactivés. Mes propres pertes de Tourment ne comptent pas. Le soin s'arrête aux PV maximum. »

**Pour mémoire : le moteur.**
- La brique `HealOnTormentLoss(scope)` remplace `HealOnOwnTormentLoss`. Le paramètre `scope` vaut `Self` (le comportement actuel, pour la référence), `Others` ou `All`.
- Elle agit au point d'interception « PV perdus » (`OnHpLost`), quand la perte a pour cause un Tourment, n'est pas nulle, et que la carte a un porteur vivant.
- Soin de 1 PV par PV perdu, borné par `Game.Heal`. La même famille existe déjà pour les dégâts d'attaque (`HealWhenOthersDamaged`), mais elle soigne par perte et non par PV.
- À mettre à jour : `BrickCatalog`, les schémas de contenu, `docs/BRICKS.md` (généré).

**Contenu.** Les effets de D_018 deviennent `[{ "brick": "HealOnTormentLoss", "scope": "Others" }]`.

**Variante** (`variants/d018-parasite.json`, seulement si la piste est développée) :

```json
{
  "name": "D_018 parasite",
  "description": "Piste : +1 PV par PV que les autres joueurs perdent à cause d'un Tourment.",
  "cards": { "D_018": { "effects": [ { "brick": "HealOnTormentLoss", "scope": "Others" } ] } }
}
```

**Mesure et seuil.** On mesure l'écart de victoire de D_018 par rapport à la moyenne (cible : ±5 pts, et plus aucune carte sans effet).
- Si l'écart dépasse +5 pts, on atténue : +1 PV par perte plutôt que par PV perdu.
- Point à surveiller : la réactivation des Tourments (Adeptes de la Corruption, Réseau fongique) peut soigner beaucoup d'un coup.

**Tests attendus.**
- `Others` : un adversaire perd 2 PV par Tourment, le porteur regagne 2 PV ; ses propres pertes ne le soignent pas.
- `Self` garde l'ancien comportement, `All` cumule les deux.
- Pas de soin au-delà des PV maximum, ni pour un porteur éliminé.
- Une réactivation des Tourments soigne pour chaque perte qu'elle cause.

**Client.** Rien à faire : le gain de PV est déjà un événement joué et écrit dans le journal.

**Sous-question.** Soigner par PV perdu (proposé, fidèle au texte) ou par perte ? La mesure tranchera.

---

## 2. Nouvel arrivage

**Problème.** « Trop aléatoire, donne du combo gratuit. » L'événement recycle les deux marchés, puis, pendant la manche, chacun prend 2 cartes dans n'importe quels marchés (`RefreshAllMarkets`, `ExtraMarketPicks 1`). Une ATK et une DEF de la même faction forment une paire, donc un combo, sans rien payer. L'événement sort environ 1,8 fois par partie. Le rapport actuel ne voit pas ce problème : il ne mesure que « le meneur garde la tête » (82,7 %, contre 84 % pour l'événement témoin).

**Orientation.** **Marchés élargis** : pendant la manche, chaque marché montre 7 cartes au lieu de 5, et chacun ne prend **qu'une** carte. Les autres options sont écartées : deuxième prise payante, un seul exemplaire de l'événement.

**Textes proposés.**
- Texte : « Les deux marchés noirs sont entièrement repiochés et proposent **deux modificateurs de plus** pendant ce tour. »
- Arbitrage : « Les deux marchés sont recyclés. Pendant la manche, chaque marché montre 7 cartes au lieu de 5 ; chacun prend une seule carte, comme d'habitude. Après la manche, un marché n'est plus complété tant qu'il a 5 cartes ou plus : les cartes en trop partent au fil des prises. »

**Pour mémoire : le moteur.**
- **Un point d'interception générique**, « taille des marchés » (à ajouter à RULES B2) : le remplissage d'un marché en tient compte. Aujourd'hui, `Game.Actions` remplit jusqu'à `Config.MarketSize`.
- **Une brique globale passive `MarketSizeBonus(amount)`**, de 1 à 5 : +`amount` cartes par marché tant que l'effet est actif.
- **L'ordre des effets compte** : `MarketSizeBonus` passe avant `RefreshAllMarkets`, pour que le recyclage révèle déjà 7 cartes.
- **Le validateur** (`GameStateValidator` : `Visible.Count <= config.MarketSize`) doit accepter la taille du moment et, après la manche, un surplus qui se résorbe.
- L'événement perd `ExtraMarketPicks`, qui reste au catalogue.

**Contenu.** Les effets de `EVT_NOUVEL_ARRIVAGE` deviennent `[{ "brick": "MarketSizeBonus", "amount": 2 }, { "brick": "RefreshAllMarkets" }]`.

**Variante** (`variants/arrivage-elargi.json`, seulement si la piste est développée) :

```json
{
  "name": "Nouvel arrivage élargi",
  "description": "Piste : marchés à 7 cartes pendant la manche, une seule prise.",
  "events": { "EVT_NOUVEL_ARRIVAGE": { "effects": [ { "brick": "MarketSizeBonus", "amount": 2 }, { "brick": "RefreshAllMarkets" } ] } }
}
```

**Mesure et seuil.** Il faut une **nouvelle mesure dans le simulateur** : les combos activés pendant la manche de chaque événement, et le taux de victoire de ceux qui en profitent.
- On adopte si les combos pendant la manche de Nouvel arrivage retombent au niveau de l'événement témoin (Le calme avant la tempête), sans que l'Élection passe sous 5 % des victoires.

**Interactions à tester.** Elles jouent sur le nombre de cartes visibles, jusqu'à 14 au lieu de 10 :
- le bonus par carte neutre visible ;
- les jetons posés sur les cartes des marchés, et leur retrait ;
- le recyclage suivi d'une prise ;
- les décisions « choisir une carte du marché », qui peuvent avoir jusqu'à 7 options ;
- l'échange avec une carte du marché ;
- un recyclage manuel pendant la manche révèle 7 cartes, après la manche 5.

**Pour mémoire : le client.**
- `MarketDisplay` crée déjà ses places selon la règle ; restent à adapter la largeur du panneau et l'échelle des cartes à 7 par marché.
- À vérifier avec 7 cartes : le marché replié (ARB-81) et les décisions prises sur la table (ARB-82).

**Sous-question.** Que deviennent les cartes en trop après la manche ?
- Proposé : elles restent jusqu'à être prises, puis le marché revient de lui-même à 5.
- Autre possibilité : elles sont défaussées dès la fin de la manche.

---

## 3. Avis de recherche (la prime sur le leader devient un événement)

**Contexte.** La prime sur le leader est une option de règle désactivée (ARB-57) : +1 ou +2 à l'attaque contre le seul joueur qui a le plus de PV. Mesurée au [lot A](README.md#étape-23-lot-a--posture-défensive-prime-sur-le-leader-fantômes-2026-09-24), elle crée des retournements (le meneur à mi-partie ne gagne plus que 41 à 46 % des parties, contre 53 %), mais ne retarde pas la première élimination.

**Orientation.** Un **événement** : pendant la manche, +2 contre le seul leader en PV, en **2 exemplaires**. L'option de règle reste en réserve.

**Textes proposés** (nom provisoire, id `EVT_AVIS_DE_RECHERCHE`).
- Texte : « Pendant ce tour, les <ATQ>**attaques** contre le joueur qui a le plus de points de vie infligent +2 points de dégâts. »
- Arbitrage : « +2 à la valeur des attaques contre le seul joueur qui a le plus de PV, pendant la manche. Le meneur est évalué au moment de chaque attaque ; en cas d'égalité en tête, aucun bonus. »

**Pour mémoire : le moteur.**
- **Une nouvelle condition générique d'attaque**, `TargetIsSoleHpLeader`, évaluée par `Game.SoleHpLeader()`. La brique existante `AttackValueBonus` l'utilise, comme Surcharge ionique utilise déjà `AttackValueBonus` (+4) sans condition. Il n'y a pas de nouvelle brique à écrire.
- **Quand ce bonus s'applique**, émettre l'événement existant `LeaderBountyApplied` (attaquant, cible, bonus). L'effet visuel prévu pour la prime (ARB-44) le joue.
- **Vue publique** : un champ qui dit si un bonus contre le leader est actif, et de combien. Le client affiche alors le marqueur du leader sans nommer l'événement. Les scripts du client ne citent jamais un id de contenu ; aujourd'hui le marqueur ne dépend que de l'option de règle.
- **Outil de variante** : il faut accepter une **nouvelle définition** dans une variante. Aujourd'hui, un id inconnu est refusé (voir [Écrire une variante](README.md#écrire-une-variante)). Proposition : une clé `newEvents` (et `newCards`, pour les § 4 et 5), dont chaque entrée est une définition complète, validée par le même chargeur que le jeu.
- **Paquet d'événements** : il passe de 14 à 16 cartes (la Fin des temps reste hors paquet). Chaque autre événement sort un peu moins souvent, environ 1,6 fois par partie au lieu de 1,8.

**Contenu possible** (`events.json`, et la variante `variants/avis-de-recherche.json`, avec `newEvents`) :

```json
{
  "id": "EVT_AVIS_DE_RECHERCHE",
  "name": "Avis de recherche",
  "copies": 2,
  "text": "Pendant ce tour, les <ATQ>**attaques** contre le joueur qui a le plus de points de vie infligent +2 points de dégâts.",
  "ruling": "+2 à la valeur des attaques contre le seul joueur qui a le plus de PV, pendant la manche. Le meneur est évalué au moment de chaque attaque ; en cas d'égalité en tête, aucun bonus.",
  "effects": [ { "brick": "AttackValueBonus", "amount": 2, "when": "TargetIsSoleHpLeader" } ]
}
```

**Mesure et seuil.** On reprend les mesures du lot A : première élimination, attaques sur le plus faible, meneur à mi-partie, Fin des temps atteinte, Élection. S'y ajoute la ligne de l'événement dans la table « le meneur garde la tête ».
- On adopte si les retournements augmentent sans que la partie dépasse 25 minutes.

**Tests attendus.**
- Le bonus ne vaut que contre le seul leader, et seulement pendant la manche.
- En cas d'égalité en tête, aucun bonus.
- `LeaderBountyApplied` est émis quand le bonus s'applique, et le champ de la vue publique reflète l'événement actif.

**Sous-question.** Le nom : « Avis de recherche » est provisoire (direction artistique).

---

## 4. Remorquage (réanimer un joueur), à explorer

**Pourquoi.** La première élimination arrive vers la manche 4,6, et l'éliminé attend alors environ 14 minutes (ARB-55). Le lot A n'y a rien changé, et les protections directes ont été écartées (ARB-56). Une réanimation prend le problème par l'autre bout : l'éliminé peut revenir.

**Orientation.** Explorer par une **carte**. Rien n'est adopté avant la mesure. La seconde chance, la rançon et les fantômes renforcés restent en réserve.

**Carte proposée** (valeurs par défaut à valider ; id `D_028`, DEF, usage unique, **neutre** donc sans combo, 2 exemplaires, ce qui fait passer le paquet DEF de 50 à 52).
- Texte : « Choisissez un joueur éliminé : il revient en jeu avec 8 points de vie, sans modificateur. Pendant la manche suivante, il ne peut pas vous <ATQ>**attaquer**. »
- Arbitrage : « Utilisable seulement s'il y a un joueur éliminé. Le joueur choisi revient avec 8 PV, le bouclier de départ de la table, sans modificateur, sans jeton ni surcharge. Il garde ses technologies obtenues. Il joue à partir de la manche suivante. Jusqu'à la fin de la manche suivante, il ne peut pas attaquer le porteur. »

**Pour mémoire : le moteur.**
- **Une action élémentaire générique** (RULES B3), `Revive(joueur, PV, bouclier)`, et un nouvel événement `PlayerRevived` (le revenant, et celui qui l'a ramené).
  - Le joueur n'est plus éliminé et est ajouté aux joueurs qui ont déjà joué cette manche.
  - Les invariants du validateur restent vrais : un joueur vivant a des PV, un revenant n'a ni carte ni statut.
- **Une brique d'activation**, `ReviveEliminatedPlayer(hp)` :
  - elle n'est utilisable que s'il y a un joueur éliminé ;
  - elle demande au porteur de choisir parmi les éliminés (décision « choisir un joueur », nouvelle question `revive.player`).
- **Protection du sauveteur** : le statut existant « cible interdite » (`CannotTargetPlayer`), avec une durée « jusqu'à la fin de la manche suivante ». Cette durée n'existe pas encore : aujourd'hui, un statut peut durer jusqu'à la fin du tour, jusqu'au début d'un tour, jusqu'à la fin de la manche, ou tout le reste de la partie.
- **Ce qui supposait qu'une élimination est définitive** : 18 fichiers du moteur (vue publique, bots, aperçus, validation…). À revoir aussi :
  - la règle du duel (ARB-68) : avec trois joueurs de nouveau en vie, la rotation reprend ;
  - les fantômes : un revenant ne choisit plus l'événement ;
  - les mesures du simulateur (première élimination).

**Pour mémoire : le client.** L'épave doit redevenir un vaisseau (teinte, inclinaison).
- `TableModel` et `SeatModel` : « éliminé » doit pouvoir repasser à faux.
- Revoir aussi l'aura, l'opacité du panneau et les textes.
- Nouveaux : la ligne du journal (`log.PlayerRevived`) et une animation de retour ([ANIMATIONS.md](../ANIMATIONS.md)).

**Mesures.** Nouvelles mesures : le temps passé hors jeu par joueur éliminé (en manches), les réanimations par partie, la victoire des revenants ; à côté, la durée des parties.
- Seuil : le temps passé hors jeu baisse nettement, sans que les parties dépassent 25 minutes.
- **Limite des bots** : ils ne voient qu'un adversaire de plus et risquent de ne jamais jouer la carte. Le verdict se fera aussi en partie réelle.

**Sous-questions**, avec la réponse proposée :
- PV au retour : 8.
- Bouclier au retour : celui du départ.
- Technologies : conservées.
- Le revenant peut-il ramener plus tard son sauveteur ? Oui : rien ne l'interdit.
- La carte est-elle neutre ou d'une faction ? Neutre.

---

## 5. Pacte de non-agression (alliances), à explorer

**Pourquoi.** Rien dans les règles n'organise une alliance. Quand plusieurs joueurs partagent un appareil, elles existent déjà de fait, à la parole. La direction artistique fait déjà du combo « un pacte avec une faction » : le vocabulaire est prêt.

**Orientation.** Explorer par une **carte**, un pacte court. La victoire partagée et le mode par équipes sont écartés pour l'instant.

**Carte proposée** (id `D_029`, DEF, usage unique, **neutre**, 2 exemplaires ; avec Remorquage, le paquet DEF atteint 54 exemplaires, autant que le paquet ATK).
- Texte : « Proposez un pacte à un adversaire. S'il accepte, vous ne pouvez plus vous <ATQ>**attaquer** ni vous saboter l'un l'autre jusqu'à la fin de votre prochain tour. »
- Arbitrage : « La cible choisit oui ou non. Si elle accepte : jusqu'à la fin du prochain tour du porteur, aucun des deux ne peut attaquer ni saboter l'autre. Si elle refuse : rien, la carte est défaussée. Une attaque déviée reste l'attaque de son auteur : le pacte ne la bloque pas. »

**Pour mémoire : le moteur.**
- **Une brique d'activation**, `MutualNonAggression` :
  - le porteur choisit un adversaire vivant ;
  - celui-ci répond oui ou non (`Game.AskYesNo`, nouvelle question `pact.accept`) ;
  - s'il accepte, deux statuts « cible interdite », un dans chaque sens.
- **Élargir `CannotTargetPlayerStatus`**, qui bloque aujourd'hui l'attaque seulement : une variable dit quelles actions sont interdites (attaque, sabotage). Favoritisme garde l'attaque seule.
- **Une durée « jusqu'à la fin du prochain tour du joueur X »** : aujourd'hui, un statut qui s'arrête en fin de tour s'arrête à la fin du tour en cours.
- **Mutinerie** ne peut pas imposer une cible interdite, puisque la légalité est vérifiée par le moteur.

**Pour mémoire : le client.**
- La réponse oui ou non se prend sur la table (ARB-82), avec les textes `decision.pact.accept` et les options.
- Le pacte se voit : un effet temporaire avec son nom (« Pacte »), plutôt que « Cible interdite ».

**Mesure.** Les bots répondent par leur évaluation habituelle, sans négocier : les chiffres du simulateur diront surtout si la carte est jouée et si elle ne casse rien. **Le verdict se fera en partie réelle.**

**Sous-questions**, avec la réponse proposée :
- Un refus a-t-il un prix ? Non.
- La durée : jusqu'à la fin du prochain tour du porteur.
- Le sabotage est-il inclus ? Oui.
- La carte est-elle neutre ? Oui.

---

## 6. Combo : laissé ouvert

**Problème, selon le designer.** Devoir « utiliser » la paire, c'est-à-dire la défausser pour obtenir la technologie (RULES A8) :
- n'incite pas à changer de cartes ;
- contredit les bonus déjà apportés par les cartes.

L'arbitrage est intéressant, mais le jeu en devient peu fluide et frustrant.

**Orientation.** **Rien ne change pour l'instant.** Trois options ont été proposées, et aucune ne convient au designer ; elles sont notées ici pour ne pas les reproposer telles quelles :
- **pacte automatique** : la technologie est obtenue dès que la paire est formée, sans défausse ni bouton ;
- **activation sans défausse** : on garde le bouton, mais les cartes restent ;
- **synergie continue** : un bonus de faction permanent tant que la paire est équipée.

La question reste ouverte dans [RULES.md](../RULES.md#questions-ouvertes-à-trancher-par-le-game-designer). En attendant, la piste du § 2 retire le principal combo gratuit.

---

## 7. Si une piste passe au développement

Rien n'est à développer tant que le designer ne l'a pas demandé explicitement. Pour estimer le travail, voici ce que les pistes demanderaient, dans un ordre raisonnable.

1. **Les mesures** :
   - la brique de D_018, la condition d'attaque et l'événement, `MarketSizeBonus` et son point d'interception ;
   - les définitions nouvelles dans les variantes, la mesure des combos par événement ;
   - les trois variantes et leur combinaison, puis un rapport de comparaison de 2 000 parties par variante à 5 joueurs.
2. **Les explorations** : Remorquage, puis Pacte. Ce sont des lots plus lourds, surtout côté client pour Remorquage, et à juger aussi en partie réelle.
