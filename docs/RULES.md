# Vortex : règles du jeu

> **Statut** : v0.2. **Partie B (modèle d'effets) validée par le game designer le 2026-09-23.** Parties A et C : en attente de validation.
> **Rôle** : c'est la **référence du moteur de règles**. Le code renvoie aux sections d'ici (par ex. `RULES A6`).

Le document a trois parties, qui dépendent uniquement vers le bas :

| Partie | Contenu | Où |
|---|---|---|
| **A. Règles de base** | Déroulé d'une partie. **Aucune carte n'y est citée.** | Ce fichier |
| **B. Modèle d'effets** | Comment *n'importe quelle* carte agit sur la partie : points d'interception, actions élémentaires, empilement, durées. | Ce fichier |
| **C. Cartes** | Texte et arbitrage de chaque carte, exprimés **uniquement** avec la partie B. | [`core/Runtime/Data/*.json`](../core/Runtime/Data/) (source), [`CARDS.md`](CARDS.md) (vue générée) |

Conséquence : ajouter, modifier ou retirer une carte ne touche **jamais** les parties A et B (ADR-0007, ADR-0008).

Les valeurs marquées ⚙ sont **configurables** (`GameConfig`) et seront calibrées par le simulateur (jalon M3).

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

- De 2 à 5 joueurs, un vaisseau par joueur.
- Un vaisseau a : des PV, un bouclier, un **emplacement ATK**, un **emplacement DEF**, au plus 1 ⚙ jeton de surcharge, et les technologies déjà obtenues.
- Un paquet de modificateurs ATK, un paquet de modificateurs DEF et un paquet d'événements. Leur composition est définie dans les données (partie C).
- Un dé à 8 faces.

## A3. Mise en place

1. Chaque vaisseau commence avec **30 PV** ⚙ (maximum 30 ⚙) et un bouclier égal à `StartShield[n]` ⚙, identique pour tous, où n est le nombre de joueurs.
2. On mélange chaque paquet. L'événement « fin des temps » (`DoomEvent` ⚙) est tenu **hors du paquet** (A4).
3. On révèle 5 ⚙ cartes de chaque paquet de modificateurs : ce sont les deux **marchés noirs**.
4. **Initiative** : chaque joueur lance 1d8. Le plus haut commence. En cas d'égalité, seuls les joueurs à égalité relancent.
5. On joue ensuite dans le sens horaire, selon l'ordre des sièges.

## A4. Début de manche

1. Si `EventFrequency` ⚙ le prévoit, on révèle un événement. Il reste **actif** jusqu'au début de la manche suivante.
2. À la manche `DoomRound[n]` ⚙, c'est le `DoomEvent` qui est révélé **à la place**.
3. Un paquet vide se reconstitue en mélangeant sa défausse. Cette règle vaut pour **tous** les paquets.
4. La manche commence par le premier joueur. S'il est éliminé, elle commence au joueur vivant suivant.

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
- Une carte à usage unique est défaussée après son activation.

### A5.4 Action d'équipage
Le joueur fait **une** action, ou passe. Le nombre d'actions est un calcul (B2.1), qui vaut 1 par défaut. Plusieurs actions dans un même tour doivent être **différentes**.

| Action | Déroulé |
|---|---|
| **Attaque** | A6. |
| **Reparamétrage** | Je relance mon bouclier avec le lot de dés « bouclier » : 1d8, ou 2d8 additionnés si je consomme ma surcharge. Le résultat est borné (B2.1, « bornes du bouclier »). Si l'autorisation B2.2 « modifier un bouclier » est refusée, l'action n'a pas d'effet. |
| **Sabotage** | Je relance 1d8 pour le bouclier d'un adversaire. Si l'autorisation B2.2 « modifier un bouclier » est refusée, l'action est **illégale** : on ne peut pas la choisir. La surcharge ne s'applique pas. |
| **Surcharge** | Je gagne un jeton de surcharge, dans la limite du maximum. |

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
| 6 | **Valeur d'attaque** = somme des dés conservés, puis modificateurs. | Calcul « valeur d'attaque ». |
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
- **Domination** : être le dernier joueur vivant.
- **Élection galactique** : avoir obtenu les 4 technologies. La victoire est immédiate.
- **Égalité** : tous les joueurs restants sont éliminés au même moment.

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

---

# Partie C : cartes

La source de vérité est dans [`core/Runtime/Data/`](../core/Runtime/Data/) : `cards.json`, `events.json` et `technologies.json`. Pour chaque carte, ces fichiers donnent le texte imprimé et son **arbitrage**, rédigé uniquement avec la partie B.

La version lisible est [`CARDS.md`](CARDS.md). Elle est générée automatiquement, et la CI vérifie qu'elle est à jour.

---

## Questions ouvertes (à trancher par le game designer)

- Valeurs de `StartShield[n]` et `DoomRound[n]` : elles seront proposées par le simulateur (M3).
- Fréquence des événements : une par manche, jugée potentiellement excessive. À mesurer au M3.
- Cartes marquées « à revoir » (A_003, A_010, A_015, A_021, A_022) : implémentées telles quelles.
- Effets de la synergie technologique : à définir.
