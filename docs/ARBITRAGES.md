# Journal des arbitrages de game design

Ce journal garde la trace **chronologique** de chaque décision de game design : la question posée, la réponse du game designer, sa raison quand elle a été donnée, et l'endroit où la décision est appliquée. Il répond à la question « pourquoi la règle est-elle ainsi ? ».

## Où vit une décision

| Type de décision | Référence à jour | Trace |
|---|---|---|
| Règle générique (déroulé, attaque, Tourment, victoire…) | [`RULES.md`](RULES.md), parties A et B | Ce journal |
| Interprétation d'une carte | Champ `ruling` de la carte dans [`core/Runtime/Data/`](../core/Runtime/Data/) (vue : [`CARDS.md`](CARDS.md)) | Ce journal |
| Valeur réglable (PV, bouclier de départ, options de règles ⚙) | [`config.json`](../core/Runtime/Data/config.json), décrit dans `RULES.md` | Ce journal, et les rapports de [`balance/`](balance/) |
| Décision technique ou structurante | [`adr/`](adr/) | L'ADR lui-même |

**Règles de tenue**
- Une entrée n'est jamais réécrite. Si une décision change, on ajoute une nouvelle entrée qui cite l'ancienne (« remplace ARB-xx »).
- Une question sans réponse va dans les [questions ouvertes de RULES.md](RULES.md#questions-ouvertes-à-trancher-par-le-game-designer), pas ici.
- Le journal peut citer des cartes. Les parties A et B de `RULES.md`, elles, n'en citent jamais (ADR-0007).

## Sommaire
- [2026-09-23 : cadrage du projet](#2026-09-23--cadrage-du-projet) (ARB-01 à ARB-07)
- [2026-09-23 : règles de base](#2026-09-23--règles-de-base) (ARB-10 à ARB-28)
- [2026-09-24 : arbitrages après les briques d'effets (M2)](#2026-09-24--arbitrages-après-les-briques-deffets-m2) (ARB-30 à ARB-36)
- [2026-09-24 : passe d'équilibrage](#2026-09-24--passe-déquilibrage) (ARB-40 à ARB-49)
- [2026-09-24 : décisions après les étapes 2.1 et 2.5](#2026-09-24--décisions-après-les-étapes-21-et-25) (ARB-50 à ARB-53)

---

## 2026-09-23 : cadrage du projet

| # | Question | Décision | Appliqué dans |
|---|---|---|---|
| ARB-01 | Plateformes cibles ? | **Android et Windows**. Ni iOS ni navigateur. | [ADR-0001](adr/0001-moteur-unity.md), README |
| ARB-02 | Architecture du jeu en ligne (phase 2) ? | **Serveur autoritaire** qui exécute le même moteur que le client. | [ADR-0003](adr/0003-reseau-serveur-autoritaire.md) |
| ARB-03 | Premier mode jouable ? | **Hot-seat** local : tous les joueurs sur le même appareil. | Jalon M4 |
| ARB-04 | Dépôt public ou privé ? | **Public** (CodeQL, secret scanning et Dependabot gratuits). | [SECURITY.md](SECURITY.md) |
| ARB-05 | Langues ? | Code et commentaires en **anglais**, documentation et PR en **français**. | [CONTRIBUTING.md](CONTRIBUTING.md) |
| ARB-06 | Où vivent les cartes ? | Dans le **JSON**, source de vérité. Le classeur Excel est retiré. | [ADR-0008](adr/0008-json-source-de-verite.md) |
| ARB-07 | Comment ajouter ou retirer des cartes simplement ? | **Le moteur ne connaît aucune carte** : les cartes agissent seulement par des points d'interception et des actions élémentaires génériques. | [ADR-0007](adr/0007-points-interception-et-briques-effets.md), RULES B |

## 2026-09-23 : règles de base

| # | Question | Décision | Appliqué dans |
|---|---|---|---|
| ARB-10 | PV : 30 (début de partie) ou 20 (lexique) ? | **30 PV** au départ, 30 au maximum. Toutes les valeurs sont indicatives et **doivent rester configurables** pour l'équilibrage. | RULES A3 ⚙ |
| ARB-11 | Attaque surchargée (+1d8) : somme ou meilleur dé ? | **Somme des 2 dés**. Critique si un dé conservé fait 8. | RULES A6 (étapes 4 et 5) |
| ARB-12 | Quand tirer les événements ? | **Un par manche**. Le designer craint qu'il y en ait trop : « un système à challenger ». | RULES A4.1 ⚙ `EventFrequency`, étape 2.4 (ARB-46) |
| ARB-13 | Bonus « +X dégâts » : avant ou après le bouclier ? | **Avant le bouclier** : le bonus agit comme de l'attaque. | RULES A6 (étape 6) |
| ARB-14 | Synergie technologique (bonus passif) ? | **Pas pour l'instant.** Un point d'extension est prévu. | RULES A8 |
| ARB-15 | Fonctionnement de la Fin des temps ? | **Forcée à la manche N**, selon le nombre de joueurs. La carte est hors du paquet. | RULES A3, A4.2 ⚙ `DoomRound` |
| ARB-16 | Moment d'activation d'une carte à usage unique ? | **Après le marché.** | RULES A5.3 |
| ARB-17 | Sens de la colonne « x » et de la note « les usages uniques sont neutres » ? | Les cartes marquées (A_003, A_010, A_015, A_021, A_022) sont **à revoir** mais implémentées telles quelles. La note est obsolète. | Questions ouvertes de RULES, étape 3 |
| ARB-18 | Roulette (D_024) : mes deux voisins ou tout le monde ? | **Rotation générale** des boucliers d'un siège, sens au choix. | `ruling` de D_024 |
| ARB-19 | Générateur auxiliaire (D_011) : quand se déclenche-t-il ? | **Quand le joueur le décide.** | `ruling` de D_011 |
| ARB-20 | Combien d'activations par tour ? | « Tout ce que le joueur possède et peut utiliser, tant qu'il a joué son tour de marché noir. » | RULES A5.3 |
| ARB-21 | Tourment sur un joueur sans modificateur ? | **Rien** : ni jeton, ni perte de PV. | RULES A7 |
| ARB-22 | Avantage et désavantage en même temps ? | **Ils s'annulent.** | RULES B4 |
| ARB-23 | Bouclier de départ ? | **Identique pour tous**, selon le nombre de joueurs. | RULES A3 ⚙ `StartShield` |
| ARB-24 | Un critique inflige-t-il aussi les dégâts normaux ? | **Oui.** | RULES A6 (étape 9) |
| ARB-25 | Synergie et combo ? | **Synergie** : deux cartes actives en même temps (bonus passif). **Combo** : on dépense les cartes pour déclencher un effet unique. | RULES A8 |
| ARB-26 | Paquet vide ? | On **remélange la défausse**, pour tous les paquets. | RULES A4.3 |
| ARB-27 | Nom du projet ? | **Vortex.** | README |
| ARB-28 | Modèle d'effets (RULES partie B) ? | **Validé formellement** par le game designer. | RULES B |

## 2026-09-24 : arbitrages après les briques d'effets (M2)

Ces arbitrages ont été posés en décrivant chaque carte avec les briques d'effets. Ils sont appliqués dans le commit « Apply the designer's rulings of 2026-09-24 ».

| # | Question | Décision | Appliqué dans |
|---|---|---|---|
| ARB-30 | Une carte activée est-elle toujours défaussée ? | **Oui**, si elle est encore dans son emplacement. | RULES A5.3 |
| ARB-31 | Moment exact de l'élimination ? | **Dès que les PV atteignent 0.** Les cartes du joueur sont défaussées et ses effets cessent à cet instant. La victoire se vérifie à la fin de chaque étape de résolution, donc une égalité reste possible. | RULES A9 |
| ARB-32 | Intouchable (D_001) protège-t-il du plafond de Sabotage électoral (D_005) ? | **Non.** Le designer accepte le comportement du modèle générique : un plafond est un calcul de bornes, pas une modification du bouclier. | `ruling` de D_001 et D_005, RULES B2 |
| ARB-33 | Chance de cocu (D_025) s'applique-t-elle aux PV qu'on se retire soi-même ? | **Non**, « ce n'est pas voulu ». Elle ignore les pertes de cause `Self`. | `ruling` de D_025 |
| ARB-34 | Corruption du croupier (A_025) : l'échange peut-il se faire avec la carte de l'attaquant ? | **Non** : ni la cible, ni l'attaquant. | `ruling` de A_025 |
| ARB-35 | Effets « après une attaque » : facultatifs ou obligatoires ? | A_003 et A_014 (« vous pouvez ») sont **facultatifs**. A_018 est **obligatoire**, **même sans dégâts**. | `ruling` de A_003, A_014, A_018 |
| ARB-36 | Mutinerie syndicale (D_006) : quelles actions peut-on imposer ? | Le porteur **peut s'imposer comme cible**. « Aucune action » **n'est pas** un choix possible. Si l'action imposée est impossible le moment venu, l'attaquant ne fait aucune action d'équipage. | `ruling` de D_006 |

## 2026-09-24 : passe d'équilibrage

Le plan complet et l'état d'avancement sont dans [`balance/README.md`](balance/README.md). Principe validé : **d'abord les règles de base, ensuite les cartes**, car la valeur d'une carte dépend des chiffres de base.

| # | Question | Décision | Appliqué dans |
|---|---|---|---|
| ARB-40 | Objectifs chiffrés de l'équilibrage (étape 0) ? | **Validés tels que proposés** : voir le tableau des objectifs. | [balance/README.md](balance/README.md#objectifs-étape-0) |
| ARB-41 | Outillage de mesure (étape 1) ? | **Validé** : variantes et comparaison avec intervalles de confiance, trois niveaux de bot, nouveaux indicateurs, options de règles dans `config.json`. | [ADR-0011](adr/0011-options-de-regles-et-variantes.md) |
| ARB-42 | Avantage du premier joueur (étape 2.1) : quelle piste ? | **Ordre de jeu évolutif** : le tour de table reste horaire, mais le joueur qui commence change à chaque manche. Tester les **deux sens** de rotation. Les autres pistes (pas d'attaque à la première manche, compensation selon la position, marché inversé) restent en réserve. | RULES A4.4 ⚙ `RoundStartRotation` |
| ARB-43 | PV, bouclier de départ et manche de Fin des temps (étape 2.2) ? | **Tests par grille validés.** Le designer juge le réglage « simple à régler plus tard ». | Étape 2.2 |
| ARB-44 | Plus de profondeur au cœur du jeu (étape 2.3) : quelles mécaniques tester ? | **Validées pour simulation** : relancer un dé en dépensant la surcharge ; reparamétrage à 2 dés dont on garde 1 ; coût pour recycler un marché ; **posture défensive** (action d'équipage : +2 bouclier jusqu'au prochain tour) ; **événement annoncé** (le prochain événement est visible une manche à l'avance) ; **prime sur le leader** (+1 dégât contre le joueur qui a le plus de PV ; **prévoir un effet visuel** côté client) ; **pillage** (éliminer un joueur permet de récupérer un de ses modificateurs) ; **fantômes** (un joueur éliminé choisit l'événement de la manche parmi deux) ; **défausse tactique** (défausser un modificateur équipé pour +2 bouclier). | Étape 2.3 |
| ARB-45 | « Pousser sa chance » (ajouter un dé après le jet ; sur un 1, l'attaque échoue) ? | **Refusé** : cela rend la surcharge bien moins intéressante. Contre-proposition du designer : une **surcharge à usage unique** qui donne une chance de **relancer un dé, quel qu'il soit**. Elle rejoint la première mécanique d'ARB-44 et sera testée sous cette forme. | Étape 2.3 |
| ARB-46 | Fréquence des événements (étape 2.4) ? | **Tests validés** : un par manche, un toutes les 2 manches, un toutes les 3, et mesure de l'effet de chaque événement. | RULES A4.1 ⚙ `EventFrequency` |
| ARB-47 | Élection galactique (étape 2.5) ? | Tester **3 technologies au lieu de 4**. Le designer pense que cela équilibre mieux et rend cette victoire plus atteignable. | RULES A9 ⚙ `TechnologiesToWin` |
| ARB-48 | Outil pour la revue des cartes (étape 3) ? | Le designer a besoin d'un **moyen visuel et simple de trier et d'éditer les cartes** pour faire des allers-retours. Proposition : **Vortex Studio**, une application web locale (tableau triable, fiche d'édition validée en direct, bouton « mesurer l'impact »). La décision sera prise au début de l'étape 3. | Étape 3 |
| ARB-49 | Validation humaine (étape 4) ? | Un **prototype jouable dans Unity**, pas une version papier, une fois les étapes précédentes posées proprement. | Jalon M4 |

## 2026-09-24 : décisions après les étapes 2.1 et 2.5

Décisions prises sur le rapport [`balance/2026-09-24-ordre-et-election.md`](balance/2026-09-24-ordre-et-election.md) (32 000 parties) et son analyse dans [`balance/README.md`](balance/README.md).

| # | Question | Décision | Appliqué dans |
|---|---|---|---|
| ARB-50 | Premier joueur de la manche : ordre fixe, rotation horaire ou anti-horaire (suite d'ARB-42) ? | **Rotation horaire.** Les deux sens ramènent l'avantage de position près de la cible (écart maximal de 1,4 à 3,7 pts, contre 9,8 à 21 pts avec l'ordre fixe). Le sens horaire évite le double tour du sens anti-horaire, qui avance la première élimination alors qu'elle est déjà trop précoce. | `config.json` (`roundStartRotation`), RULES A4.4, test `Rule_options_match_the_designer_rulings` |
| ARB-51 | Élection galactique : 3 ou 4 technologies (suite d'ARB-47) ? | **3 technologies.** Les victoires par Élection passent de moins de 0,5 % à 2,4 à 5 %, sans autre effet mesurable. La cible de 5 à 15 % n'est pas encore atteinte : à revoir avec la grille de l'étape 2.2. | `config.json` (`technologiesToWin`), RULES A9, test `Rule_options_match_the_designer_rulings` |
| ARB-52 | Sur quelle taille de table équilibrer ? | **5 joueurs est le mode standard**, et c'est la seule table qu'on équilibre. Raison du game designer : certaines cartes n'ont de sens qu'à 5, en particulier celles qui visent les joueurs adjacents. Par exemple, à 4 joueurs, Accident bactériologique (A_021) touche tous les adversaires si l'on vise le joueur d'en face. 4 joueurs avait d'abord été envisagé, puis écarté pour cette raison. De 2 à 4 joueurs, le jeu reste jouable (moteur et réglages par table conservés), mais sans objectif d'équilibrage. Les tailles de table proposées dans le menu seront décidées au jalon M4. | RULES A2, [balance/README.md](balance/README.md#objectifs-étape-0), simulateur (`--players 5` par défaut) |
| ARB-53 | Objectif de durée pour le mode standard ? | **20 à 25 minutes à 5 joueurs.** Il remplace la cible « 15 à 20 minutes à 4 joueurs » d'ARB-40. La durée réelle d'un tour sera mesurée sur le prototype (étape 4), car plus de choix allongeront les tours. | [balance/README.md](balance/README.md#objectifs-étape-0) |
