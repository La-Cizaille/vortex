# Vortex : règles du jeu

> **Statut** : v0.5. **Partie B (modèle d'effets) validée par le game designer le 2026-09-23.** Parties A et C : en attente de validation. Tous les arbitrages jusqu'au 2026-09-24 sont intégrés ; leur historique et leurs raisons sont dans le [journal des arbitrages](ARBITRAGES.md).
> **Rôle** : c'est la **référence du moteur de règles**. Le code renvoie aux sections d'ici (par ex. `RULES A6`).

Le document a trois parties, qui dépendent uniquement vers le bas :

| Partie | Contenu | Où |
|---|---|---|
| **A. Règles de base** | Déroulé d'une partie. **Aucune carte n'y est citée.** | Ce fichier |
| **B. Modèle d'effets** | Comment *n'importe quelle* carte agit sur la partie : points d'interception, actions élémentaires, empilement, durées. | Ce fichier |
| **C. Cartes** | Texte et arbitrage de chaque carte, exprimés **uniquement** avec la partie B. | [`core/Runtime/Data/*.json`](../core/Runtime/Data/) (source), [`CARDS.md`](CARDS.md) (vue générée) |

Conséquence : ajouter, modifier ou retirer une carte ne touche **jamais** les parties A et B (ADR-0007, ADR-0008).

Les valeurs marquées ⚙ sont **configurables** (`config.json`) et sont calibrées pendant la [passe d'équilibrage](balance/README.md). Une **option de règle** ⚙ (par exemple `RoundStartRotation`) garde par défaut la règle actuelle tant que le game designer n'a pas tranché (ADR-0011).

---

# Partie A : règles de base

## A1. Lexique

| Terme | Définition |
|---|---|
| **Tour** | Le tour d'**un** joueur. |
| **Manche** | Un tour de table complet. Un effet qui dure « ce tour » dans un **événement** dure la manche. |
| **Prochain tour** | Pour un effet de carte : le prochain tour **du propriétaire de l'effet**. |
| **Propriétaire** | Le joueur qui a la carte équipée ou qui a activé l'effet. Un événement n'a pas de propriétaire. |
| **Source** | Qui cause une action : un joueur (avec l'effet ou l'action d'équipage en cause) ou le jeu (événement, règle de base). |
| **Adversaire** | Tout autre joueur vivant. |
| **Dégâts** | Montant calculé par une attaque, avant d'être appliqué comme perte de PV. |
| **Perte de PV** | Toute diminution de PV. Elle porte une **cause** : `Attack`, `Reflect` (renvoi d'une perte), `Torment`, `Event`, `Self` (coût payé ou effet sur soi). |
| **Bouclier** | Valeur de 0 à 8 ⚙. |
| **Désactiver ou ignorer un bouclier** | Le bouclier compte pour 0 dans un calcul d'attaque, **sans que sa valeur change**. Ce n'est pas une *modification* du bouclier. |
| **Modifier un bouclier** | Changer sa valeur : la fixer, l'augmenter, la diminuer, la relancer ou l'échanger. Toute modification passe par l'autorisation B2.2 « modifier un bouclier ». |
| **Couleur** | Neutre, Bleu, Rouge, Vert ou Jaune. Neutre n'est pas une technologie et ne forme jamais de combo. |

## A2. Matériel

- De 2 à 5 joueurs, un vaisseau par joueur. **5 joueurs est le mode standard** : c'est la table sur laquelle le jeu est équilibré (ARB-52). De 2 à 4 joueurs, le jeu reste jouable, sans garantie d'équilibre.
- Un vaisseau a : des PV, un bouclier, un **emplacement ATK**, un **emplacement DEF**, au plus 1 ⚙ jeton de surcharge, et les technologies déjà obtenues.
- Un paquet de modificateurs ATK, un paquet de modificateurs DEF et un paquet d'événements. Leur composition est définie dans les données (partie C).
- Un dé à 8 faces.

## A3. Mise en place

1. Chaque vaisseau commence avec **30 PV** ⚙ (maximum 30 ⚙) et un bouclier égal à `StartShield[n]` ⚙, identique pour tous, où n est le nombre de joueurs. Au mode standard à 5 joueurs, ce bouclier vaut **5** (ARB-54).
2. On mélange chaque paquet. L'événement « fin des temps » (`DoomEvent` ⚙) est tenu **hors du paquet** (A4).
3. On révèle 5 ⚙ cartes de chaque paquet de modificateurs : ce sont les deux **marchés noirs**.
4. **Initiative** : chaque joueur lance 1d8. Le plus haut commence. En cas d'égalité, seuls les joueurs à égalité relancent.
5. On joue ensuite dans le sens horaire, selon l'ordre des sièges.

## A4. Début de manche

1. Si `EventFrequency` ⚙ le prévoit (manches 1, 1+N, 1+2N…), on révèle un événement, dès la première manche. Il reste **actif** jusqu'au début de la manche suivante.
   - *Option à l'étude* **Fantômes** (`GhostsChooseEvent` ⚙, désactivée) : si au moins un joueur est éliminé, on pioche **deux** événements. Un joueur éliminé choisit celui qui s'applique, et l'autre va à la défausse. Les éliminés choisissent chacun leur tour, dans l'ordre des sièges. Ce choix n'a pas lieu à la manche de la Fin des temps.
2. À la manche `DoomRound[n]` ⚙, c'est le `DoomEvent` qui est révélé **à la place**. Au mode standard à 5 joueurs, c'est la **manche 16** (ARB-54).
3. Un paquet vide se reconstitue en mélangeant sa défausse. Cette règle vaut pour **tous** les paquets.
4. **Premier joueur de la manche** : à la première manche, le gagnant de l'initiative. Ensuite, le premier joueur **avance d'un siège dans le sens horaire** à chaque manche. S'il est éliminé, la manche commence au joueur vivant suivant dans le sens horaire. Le tour de table reste toujours horaire.
   - Réglage `RoundStartRotation` ⚙ : sens horaire (ARB-50). Pour l'équilibrage, on peut aussi choisir « aucun déplacement » ou « sens anti-horaire ». En sens anti-horaire, le dernier joueur d'une manche est aussi le premier de la suivante.
   - **Duel** (ARB-68) : quand il ne reste que **deux joueurs en vie**, la rotation ne s'applique plus. La manche commence par le joueur qui n'a **pas joué en dernier**, pour que personne ne joue deux fois de suite. Cela vaut pour une partie à 2 joueurs comme pour la fin d'une partie plus grande.

## A5. Tour d'un joueur

### A5.1 Début du tour
1. Les jetons de Tourment infligent leur perte (A7).
2. Réaction « début de tour » (B2.3).
3. Les effets qui duraient « jusqu'au prochain tour » de ce joueur expirent.

### A5.2 Marché noir : une option parmi trois
- **Prendre** une carte d'un marché. Le nombre de cartes prenables est un calcul (B2.1), qui vaut 1 par défaut.
  - La carte va dans l'emplacement de son type.
  - La carte qui occupait cet emplacement est défaussée avec ses jetons, sans que son effet soit déclenché.
  - Le marché est complété immédiatement.
  - Les jetons posés sur la carte prise la suivent.
- **Recycler** un marché : ses cartes sont défaussées et on en révèle autant de nouvelles.
- **Passer**.

### A5.3 Fenêtre d'activation
- Elle s'ouvre après le marché et reste ouverte jusqu'à la fin du tour, avant comme après l'action d'équipage.
- Le joueur peut y activer, **sans limite de nombre**, tout effet activable qu'il possède : cartes à usage unique, cartes à déclenchement manuel et combos (A8).
- Toute carte activée (à usage unique ou à déclenchement manuel) est **défaussée après son activation**, si elle est encore dans son emplacement.

### A5.4 Action d'équipage
Le joueur fait **une** action, ou passe. Le nombre d'actions est un calcul (B2.1), qui vaut 1 par défaut. Plusieurs actions dans un même tour doivent être **différentes**.

| Action | Déroulé |
|---|---|
| **Attaque** | A6. |
| **Reparamétrage** | Je relance mon bouclier avec le lot de dés « bouclier » : 1d8, ou 2d8 additionnés si je consomme ma surcharge. Le résultat est borné (B2.1, « bornes du bouclier »). Si l'autorisation B2.2 « modifier un bouclier » est refusée, l'action n'a pas d'effet. |
| **Sabotage** | Je relance 1d8 pour le bouclier d'un adversaire. Si l'autorisation B2.2 « modifier un bouclier » est refusée, l'action est **illégale** : on ne peut pas la choisir. La surcharge ne s'applique pas. |
| **Surcharge** | Je gagne un jeton de surcharge, dans la limite du maximum. |
| **Posture défensive** (*option à l'étude*, `DefensivePostureBonus` ⚙, désactivée) | Jusqu'au début de mon prochain tour, mon bouclier effectif (A6, étape 7) gagne le bonus. La valeur du bouclier ne change pas : ce n'est pas une modification, et le bonus compte même si le bouclier est désactivé ou ignoré. Une nouvelle posture remplace la précédente. |

**Surcharge** : un jeton se consomme **volontairement** lors d'une attaque ou d'un reparamétrage. Il est perdu si le joueur subit une perte de PV de cause `Attack`, `Reflect` ou `Event`, sauf si l'autorisation B2.2 « perdre la surcharge » est refusée.

### A5.5 Fin du tour
- Les effets qui duraient « ce tour » expirent.
- Un effet « prochaine attaque » non utilisé expire.
- On vérifie la victoire (A9).

## A6. Attaque

Chaque étape nomme le point d'interception (B2) où les effets peuvent agir.

| # | Étape | Point d'interception |
|---|---|---|
| 1 | **Déclaration** : l'attaquant choisit un adversaire. | Autorisation « cibler » ; une action imposée peut fixer la cible. |
| 2 | **Redirection** : chaque effet de la cible qui le permet peut rediriger l'attaque vers un autre joueur vivant (ni l'attaquant, ni la cible). Une attaque ne peut être redirigée **qu'une fois**. | Réaction « attaque déclarée ». |
| 3 | **Avant le jet** : effets préparatoires, comme un pari sur le dé ou une modification de bouclier. | Réaction « avant le jet ». |
| 4 | **Jet** : on lance le lot de dés d'attaque. Par défaut 1d8, plus 1d8 si l'attaque est surchargée. | Calcul « lot de dés d'attaque ». |
| 5 | **Critique** : au moins un dé **conservé** affiche 8. | — |
| 6 | **Valeur d'attaque** = somme des dés conservés, puis modificateurs. *Option à l'étude* **prime sur le leader** (`LeaderBounty` ⚙, désactivée) : si la cible est **seule** à avoir le plus de PV parmi les joueurs vivants, la valeur gagne la prime. Comme les autres bonus, elle s'applique avant le bouclier (ARB-13). | Calcul « valeur d'attaque ». |
| 7 | **Bouclier effectif** de la cible. | Calcul « bouclier effectif ». |
| 8 | **Dégâts** = max(0, valeur d'attaque − bouclier effectif), puis modificateurs. | Calcul « dégâts ». |
| 9 | **Effet du critique** : si la cible a au moins un modificateur, **elle choisit** celui qui est défaussé (avec ses jetons). Sinon, dégâts +1 ⚙. | — |
| 10 | **Application** : la cible subit une perte de PV de cause `Attack`. | Calcul « perte de PV », puis réaction « perte de PV subie ». |
| 11 | **Après l'attaque** : effets de l'attaquant et de la cible. | Réaction « attaque résolue » (avec le montant). |
| 12 | **Éliminations et victoire** (A9). | Réaction « joueur éliminé ». |

**Exemple sans aucune carte** : dé 5, bouclier de la cible 3. Dégâts : max(0, 5 − 3) = **2**.

## A7. Tourment

- **Poser un jeton** : l'action élémentaire « poser un Tourment » le place sur un modificateur **équipé**, et son propriétaire perd immédiatement la valeur d'un Tourment (cause `Torment`).
- **Pas de modificateur, pas de jeton** : un joueur sans modificateur ne peut pas recevoir de jeton, et il ne perd rien.
- Les jetons se cumulent. Ils restent sur **la carte** : ils disparaissent avec elle (défausse, remplacement) et la suivent (vol, échange). Une carte au marché peut porter des jetons.
- **Au début de son tour**, chaque joueur perd la valeur d'un Tourment pour chaque jeton posé sur ses modificateurs.
- **Réactiver** : on applique de nouveau, immédiatement, la perte de tous les jetons posés sur des modificateurs équipés.
- **Valeur d'un Tourment** : 1, modifiable par un calcul (B2.1).

## A8. Technologies (combos)

- **Condition** : les modificateurs ATK et DEF équipés ont la **même couleur non neutre**.
- **Activation** (fenêtre A5.3) :
  1. les deux modificateurs sont défaussés, sans que leurs effets d'usage unique soient déclenchés ;
  2. l'effet de la technologie de cette couleur s'applique ;
  3. la technologie est marquée comme **obtenue** par le joueur.
- **Synergie** (bonus passif quand deux cartes de même couleur sont équipées) : **non activée** dans cette version. Elle se branchera comme un effet de durée « tant que la condition est vraie » (B5).

## A9. Victoire et élimination

- **Élimination** : un joueur à 0 PV est éliminé. Ses cartes sont défaussées avec leurs jetons.
- **Élimination immédiate** : un joueur est éliminé **dès que ses PV atteignent 0**. Ses cartes sont défaussées à cet instant et ses effets cessent, y compris ses réactions au coup qui l'élimine.
- **Victoire** : elle est vérifiée à la fin de chaque étape de résolution (fin d'une attaque, d'une activation, d'un début de tour, d'un début de manche).
- **Élimination pendant son propre tour** (par exemple par une perte renvoyée) : le tour s'arrête immédiatement et le joueur suivant joue.
- **Domination** : être le dernier joueur vivant.
- **Élection galactique** : avoir obtenu **3** technologies différentes (`TechnologiesToWin` ⚙, ARB-51). La victoire est immédiate.
- **Égalité** : tous les joueurs restants sont éliminés au cours de la même étape de résolution.

---

# Partie B : modèle d'effets

## B1. Principe

- Une carte, un événement ou une technologie n'est **qu'un ensemble d'effets**.
- Un effet intervient **uniquement** à travers les points d'interception définis ci-dessous, et agit **uniquement** au moyen des actions élémentaires (B3).
- Une règle de base ne cite jamais une carte, et un arbitrage de carte ne cite jamais une autre carte.
- Deux cartes interagissent donc toujours par l'intermédiaire d'un point d'interception, jamais directement. C'est ce qui rend leurs combinaisons prévisibles, même quand personne ne les avait anticipées.

## B2. Points d'interception

### B2.1 Calculs : un effet modifie une valeur
| Calcul | Valeur de base |
|---|---|
| **Lot de dés d'attaque** : dés lancés, dés conservés, avantage, désavantage, dés imposés | 1d8 (+1d8 si surchargée), tous conservés |
| **Valeur d'attaque** | somme des dés conservés |
| **Bouclier effectif** (pour une attaque) | bouclier de la cible |
| **Dégâts** | max(0, valeur d'attaque − bouclier effectif) |
| **Perte de PV** : toute perte, avec sa cause | montant reçu |
| **Gain de PV** | montant reçu, borné au maximum de PV |
| **Bornes du bouclier** (par joueur) | min 0 ⚙, max 8 ⚙ |
| **Valeur d'un Tourment** | 1 |
| **Cartes prenables au marché** | 1 |
| **Actions d'équipage** | 1 |

### B2.2 Autorisations : un effet peut refuser
| Autorisation | Question posée |
|---|---|
| **Cibler** | Ce joueur peut-il cibler celui-ci avec cette action ? |
| **Modifier un bouclier** | Cette source peut-elle changer la valeur du bouclier de ce joueur ? |
| **Perdre la surcharge** | Ce joueur peut-il perdre son jeton de surcharge maintenant ? |
| **Activer** | Cet effet activable peut-il être activé maintenant ? |

### B2.3 Réactions : un effet agit après un fait
Les réactions sont : début de manche ; début de tour ; fin du marché ; carte équipée ; carte quittant un emplacement (défausse, destruction, vol) ; attaque déclarée ; avant le jet ; attaque résolue (avec le montant) ; perte de PV subie (avec la cause) ; bouclier modifié ; Tourment posé ; joueur éliminé ; fin de tour.

## B3. Actions élémentaires

Un effet agit **uniquement** au moyen des actions suivantes. Chacune passe **automatiquement** par les calculs et les autorisations qui la concernent : c'est l'action elle-même qui les applique, pas la carte qui l'utilise.

| Action | Passe par |
|---|---|
| Infliger une perte de PV (avec cause), soigner | Perte de PV ou gain de PV |
| Fixer, augmenter, diminuer, relancer ou échanger un bouclier | Autorisation « modifier un bouclier » (pour **chaque** bouclier concerné), puis bornes du bouclier |
| Désactiver ou ignorer un bouclier (avec durée) | — (ce n'est pas une modification) |
| Poser, retirer ou réactiver des Tourments | Valeur d'un Tourment ; A7 |
| Défausser, détruire, voler ou échanger un modificateur | Réaction « carte quittant un emplacement » |
| Recycler un marché, prendre au marché | — |
| Gagner ou consommer la surcharge | Autorisation « perdre la surcharge » pour la perte |
| Accorder avantage ou désavantage, ajouter ou retirer des dés | Lot de dés d'attaque |
| Imposer l'action d'équipage d'un joueur | Autorisation « cibler » |
| Rediriger une attaque | A6, étape 2 |

**Exemple** : un sabotage (A5.4), un vol de bouclier et un échange de boucliers utilisent tous une action de modification de bouclier. Une carte qui refuse l'autorisation « modifier un bouclier » les bloque donc **tous**, sans qu'aucune de ces règles ne la connaisse.

## B4. Empilement

Quand plusieurs effets modifient **le même calcul**, on les applique toujours dans cet ordre :

| Ordre | Catégorie | Exemples |
|---|---|---|
| 1 | **Remplacer** la valeur | « le bouclier passe à 8 », « compte pour 0 » |
| 2 | **Ajouter ou retrancher** | +4, −1 |
| 3 | **Multiplier** | ×2. Les multiplicateurs se multiplient entre eux : deux ×2 donnent ×4. |
| 4 | **Borner** (plafond, plancher) | « 1 au maximum ». Le plafond le plus bas l'emporte, le plancher le plus haut aussi. |
| 5 | **Annuler** | « aucun dégât ». L'annulation l'emporte sur tout. |

Règles complémentaires :
- **Plusieurs remplacements** : le dernier appliqué dans l'ordre de résolution (B7) l'emporte.
- **Avantage et désavantage** : ils se compensent un pour un ; seul le solde s'applique. Avec avantage, on lance le lot deux fois et on garde le meilleur total ; avec désavantage, le pire.
- **Autorisations** : un seul refus suffit, et il l'emporte sur toute autorisation.
- **Résultat final** : une valeur n'est jamais négative, sauf mention contraire, et reste dans les bornes de la règle concernée.

## B5. Durées

Tout effet non instantané déclare une durée parmi :
- **permanente** ;
- **tant que la carte est équipée** ;
- **ce tour** (du propriétaire) ;
- **jusqu'au prochain tour** (du propriétaire) ;
- **cette manche** ;
- **prochaine attaque de ce tour** ;
- **une fois** : la carte est défaussée après son premier déclenchement.

Quand la carte qui porte un effet quitte son emplacement, tous ses effets « tant que la carte est équipée » cessent immédiatement.

## B6. Choix

- Un choix revient au **propriétaire de l'effet**, sauf si le texte désigne un autre joueur (par exemple « la cible choisit »).
- Un choix se fait parmi les options **légales** seulement. S'il n'y en a aucune, l'effet ne fait rien.
- En ligne (phase 2), tout choix a un délai et une option par défaut.
- Chaque option d'un choix nomme ce qu'elle désigne : un joueur, une carte, un nombre, un contenu (un événement). Une option qui mène quelque part le nomme aussi : « voler » nomme la carte et le joueur qui la reçoit, « détruire » la carte seule ; un sens de rotation nomme le voisin qui reçoit le bouclier du porteur. Le client peut ainsi faire répondre sur la table sans connaître aucune carte (ARB-82).

## B7. Ordre de résolution

Quand plusieurs effets interviennent au même point d'interception :

1. d'abord les effets **globaux** : événement actif, puis effets permanents de la partie ;
2. puis les joueurs, en partant du **joueur actif** et dans le sens horaire ;
3. pour chaque joueur : l'emplacement ATK, puis l'emplacement DEF, puis ses effets temporaires dans leur ordre de création.

**Réactions en chaîne** :
- Une réaction peut en provoquer d'autres. Elles se résolvent immédiatement, en profondeur.
- Un effet ne réagit jamais à un fait qu'il a lui-même provoqué.
- Une perte de PV de cause `Reflect` ne déclenche **aucune** réaction.
- Par sécurité, une chaîne ne peut pas dépasser 16 niveaux. Au-delà, le moteur signale une erreur de conception de carte, que les tests doivent détecter.

## B8. Portée des effets

- Un effet porté par une **carte** agit pour le **porteur** de la carte : ses attaques, ses pertes de PV, son bouclier…
- Un effet porté par un **événement** (effet global) agit pour **tous les joueurs**.
- Un effet porté par une **technologie** agit pour le joueur qui l'active.

Une même brique sert donc à plusieurs contenus : « +4 à la valeur d'attaque » est un bonus personnel sur une carte, et un bonus pour tout le monde sur un événement.

## B9. Briques d'effets

Chaque carte, événement ou technologie déclare ses effets comme une liste de **briques** paramétrées, dans son champ `effects` (ADR-0007). Les briques s'appuient uniquement sur les points B2 à B7.

Le catalogue des briques, avec leurs paramètres et les cartes qui les utilisent, est généré dans [`BRICKS.md`](BRICKS.md).

---

# Partie C : cartes

La source de vérité est dans [`core/Runtime/Data/`](../core/Runtime/Data/) : `cards.json`, `events.json` et `technologies.json`. Pour chaque carte, ces fichiers donnent le texte imprimé et son **arbitrage**, rédigé uniquement avec la partie B.

Chaque entrée porte aussi ses **briques d'effets** (B9), qui implémentent l'arbitrage.

La version lisible est [`CARDS.md`](CARDS.md). Elle est générée automatiquement, et la CI vérifie qu'elle est à jour.

---

## Questions ouvertes (à trancher par le game designer)

Une question tranchée quitte cette liste et entre dans le [journal des arbitrages](ARBITRAGES.md).

**Règles**
- **Coup fatal** : la victime est éliminée immédiatement (ARB-31), donc ses réactions (D_017, D_022, D_006) ne s'appliquent pas au coup qui l'élimine. À confirmer.
- **Cartes marquées « à revoir »** (A_003, A_010, A_015, A_021, A_022) : implémentées telles quelles (ARB-17), à traiter lors de la revue des cartes (étape 3 de l'équilibrage).
- **Synergie technologique** : effets à définir (ARB-14).

**Équilibrage** (plan et mesures : [`balance/README.md`](balance/README.md))
- **Première élimination trop précoce** (ARB-55) : en simulation, seule la posture défensive la recule (de la manche 4,7 à 5,4), au prix de parties plus lentes. La prime sur le leader est en réserve (ARB-57), et les mesures directes ont été écartées (ARB-56). À évaluer en partie réelle sur le prototype (ARB-58) : les joueurs s'acharnent-ils autant que les bots ?
- **Fréquence des événements** : une par manche, jugée potentiellement excessive (ARB-12, ARB-46).
- **Nouvelles mécaniques** validées pour simulation (ARB-44, ARB-45) : aucune n'est une règle tant qu'elle n'a pas été mesurée puis adoptée. Les trois premières (posture défensive, prime sur le leader, fantômes) sont codées comme options ⚙ désactivées. Leur interprétation (A4.1, A5.4, A6) est **provisoire** : elle sera confirmée ou corrigée au moment de l'adoption.
