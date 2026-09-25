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
- [2026-09-24 : décisions après l'étape 2.2](#2026-09-24--décisions-après-létape-22) (ARB-54 et ARB-55)
- [2026-09-24 : décisions sur le lot A de l'étape 2.3](#2026-09-24--décisions-sur-le-lot-a-de-létape-23) (ARB-56 à ARB-58)

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

## 2026-09-24 : décisions après l'étape 2.2

ARB-54 repose sur les grilles de rythme et la comparaison de confirmation ([`balance/2026-09-24-rythme-confirmation.md`](balance/2026-09-24-rythme-confirmation.md), 18 000 parties avec une autre graine), analysées dans [`balance/README.md`](balance/README.md).

| # | Question | Décision | Appliqué dans |
|---|---|---|---|
| ARB-54 | Rythme du mode standard : PV, bouclier de départ et manche de Fin des temps à 5 joueurs (suite d'ARB-43) ? | **30 PV (inchangés), bouclier de départ 5, Fin des temps à la manche 16**, selon la recommandation du rapport. La Fin des temps n'est plus atteinte que dans environ 20 % des parties (88 % avant) et redevient un filet de sécurité. L'Élection passe à environ 7 % des victoires, dans sa cible. La première élimination recule un peu, pour un coût modéré en attaques sans dégâts (27 % au lieu de 22 %). Les autres tailles de table gardent leurs réglages. | `config.json` (`playerCounts` à 5 joueurs), RULES A3 et A4, test `Rule_options_match_the_designer_rulings` |
| ARB-55 | La première élimination précoce est-elle un problème ? L'objectif « pas avant la manche 6 » (ARB-40) mesure le temps que chaque joueur passe en jeu avant qu'un premier joueur soit éliminé ; ce n'est pas un déblocage de fonctionnalités. | **Oui, c'est un problème.** La cible est gardée au mode standard à 5 joueurs : pas de première élimination avant la manche 6. Aujourd'hui, le premier éliminé sort vers la manche 4,6 et attend environ 14 minutes la fin de la partie. On cherche la solution avec les mécaniques de l'étape 2.3. | [balance/README.md](balance/README.md#objectifs-étape-0) |

## 2026-09-24 : décisions sur le lot A de l'étape 2.3

Décisions prises sur le rapport [`balance/2026-09-24-mecaniques-lot-a.md`](balance/2026-09-24-mecaniques-lot-a.md) et son analyse dans [`balance/README.md`](balance/README.md). Le game designer a aussi validé l'amélioration du bot (décision technique : [ADR-0012](adr/0012-bot-protection-effective.md)).

| # | Question | Décision | Appliqué dans |
|---|---|---|---|
| ARB-56 | Tester des mesures directes contre la première élimination précoce : sursis (pas sous 1 PV pendant les premières manches), dernier carré (+2 au bouclier effectif sous 10 PV) ou malus contre le plus faible ? | **Aucune des trois.** | Rien à appliquer |
| ARB-57 | La prime sur le leader ne retarde pas la première élimination mais crée des retournements : que faire ? | **La garder en réserve.** L'option reste dans le moteur, désactivée, pour une décision ultérieure. | `config.json` (`leaderBounty` à 0), RULES A6 |
| ARB-58 | Faut-il poursuivre l'équilibrage par simulation, ou passer au prototype ? | **Clore l'étape 2 et passer au prototype Unity (M4).** Le game designer estime que les bases sont bonnes et que le réglage fin viendra dans un second temps. Les règles de base actuelles sont figées. La posture défensive, la prime sur le leader et les fantômes restent des options désactivées, activables dans le prototype pour être essayées en partie réelle. Les autres mécaniques de l'étape 2.3 et la fréquence des événements (2.4) attendent les premiers retours. La revue des cartes (étape 3) vient après le prototype, avec Vortex Studio. | [balance/README.md](balance/README.md#plan-et-avancement), README |

## 2026-09-24 : interface du prototype (M4)

Le game designer a décrit l'écran de jeu, puis validé les propositions faites sur les points qu'il n'avait pas couverts. Le détail est dans [`INTERFACE.md`](INTERFACE.md) ; la manière de le construire dans [ADR-0015](adr/0015-scene-de-jeu.md).

| # | Question | Décision | Appliqué dans |
|---|---|---|---|
| ARB-59 | Faut-il décrire l'interface avant de construire la scène de jeu ? | **Oui.** L'interface est décrite avant la scène de jeu (M4.4). Le travail qui n'en dépend pas continue en parallèle, tant qu'aucune décision importante sur les menus n'est en jeu. | [INTERFACE.md](INTERFACE.md), feuille de route du README |
| ARB-60 | Forme générale de l'écran ? | Une **scène 3D à caméra fixe** (vaisseaux, dé, fond, effets) et une **interface 2D par-dessus** (cartes, marché, jauges, actions). Paysage uniquement, référence 1920×1080, cible minimale un téléphone de 6 pouces. Sur écran tactile, l'appui long remplace le survol. Un fond spatial (étoiles qui scintillent, planètes) est prévu, mais pas nécessaire au prototype. | INTERFACE §1 et §2, ADR-0015 |
| ARB-61 | Disposition de l'écran de jeu ? | **Décrite par le designer** : les adversaires face au joueur, avec PV, bouclier et modificateurs agrandis au survol ; son vaisseau devant lui, statique, avec PV, bouclier et trois ronds qui s'allument avec les technologies obtenues ; le marché noir au centre (ATK à gauche, DEF à droite), réductible, avec zoom au survol ; les actions en demi-cercle au-dessus du vaisseau, avec une info-bulle au survol. **Propositions validées** : les adversaires dans l'ordre du tour (celui qui joue après moi à gauche, pour que les voisins soient aux extrémités) ; une épave grisée pour un joueur éliminé ; les modificateurs du joueur de part et d'autre du vaisseau ; un marché qui se réduit tout seul après la phase de marché. | INTERFACE §3.1 à §3.3 |
| ARB-62 | Comment agir ? | **Décrit par le designer** : les actions se font en glisser-déposer vers la cible, avec un aperçu (dés lancés, bonus et malus) ; une carte s'utilise en la glissant au centre ; le combo s'active par un bouton allumé quand il est disponible ; la surcharge a un petit indicateur. **Propositions validées** : un simple toucher pour les actions sans cible ; la fourchette de dégâts dans l'aperçu ; les cibles interdites grisées avec la raison ; l'achat en glissant une carte du marché vers le vaisseau ; les décisions prises directement sur la table quand c'est possible ; un bouton de fin de tour qui change de couleur quand il ne reste rien d'utile à faire. | INTERFACE §3.4 à §3.7 et §6 |
| ARB-63 | Point de vue quand plusieurs joueurs partagent l'appareil ? | Avec un humain et des bots, la vue reste celle de l'humain. Avec plusieurs humains, elle pivote au début du tour de chacun. La décision d'un autre joueur pendant mon tour s'affiche dans une fenêtre, sans pivoter. | INTERFACE §3.6 et §4 |
| ARB-64 | Informations de partie ? | La manche, l'événement en cours et le compte à rebours de la Fin des temps en haut au centre ; un journal repliable ; un bouton pour accélérer ou passer les animations. Un marqueur du leader seulement avec l'option « prime sur le leader ». | INTERFACE §3.1 et §3.8 |
| ARB-65 | Menus hors partie ? | Simples au début, améliorables ensuite : accueil (trouver une partie et social grisés), partie locale, options, pause, écran de fin. La partie locale propose **2 à 5 joueurs, 5 par défaut** (suite d'ARB-52), chaque siège humain ou bot. Les options de règles et la graine sont dans un menu de développement, absent des builds publiés. La sauvegarde d'une partie en cours viendra au M5. | INTERFACE §5 |
| ARB-66 | Langue ? | **Français seulement**, mais tous les textes de l'interface passent par une table de textes, pour pouvoir traduire un jour. | INTERFACE §5, ADR-0015 |
| ARB-67 | Surcharge : comment choisir de dépenser le jeton sur une attaque ou un reparamétrage ? Le moteur le demande à chaque fois ; la question était restée ouverte dans INTERFACE §7. | **Armer le jeton.** On touche le jeton de surcharge pour l'armer : il brille, et l'aperçu montre l'attaque ou le reparamétrage surchargé. La prochaine attaque ou le prochain reparamétrage le dépense. On le touche de nouveau pour le désarmer. | INTERFACE §3.2, §3.4 et §6 |

## 2026-09-24 : premier playtest (mode test, M4.4)

Le game designer a joué ses premières parties contre des bots. Ses remarques sur la lisibilité (cartes, zoom sur le marché, animation des dés) orientent M4.5. Deux décisions portent sur les règles et le contenu.

| # | Question | Décision | Appliqué dans |
|---|---|---|---|
| ARB-68 | La rotation du premier joueur (ARB-50) fait jouer deux fois de suite en duel : la manche suivante commence par le joueur qui vient de finir la précédente. Que faire ? | **La couper dès qu'il ne reste que deux joueurs en vie** : la manche commence alors par le joueur qui n'a pas joué en dernier. Personne ne joue jamais deux fois de suite, en partie à 2 comme en fin de partie à 5 (où le même double tour apparaissait une fois sur deux). Remarque du designer : « ça ne marche pas, il faut désactiver en duel ». | RULES A4.4, `Game.FirstSeatOfRound`, test `With_two_players_left_nobody_plays_twice_in_a_row` |
| ARB-69 | L'événement `EVT_SURCHARGE_IONIQUE` s'appelait « **surcharge** Ionique », avec des marques de gras héritées de l'import Excel. | **« Surcharge ionique »**. | `events.json`, `CARDS.md` |

## 2026-09-25 : idées pour la suite

Le game designer a précisé ses attentes pour les prochaines étapes, après le premier playtest.

| # | Question | Décision | Appliqué dans |
|---|---|---|---|
| ARB-70 | Faut-il limiter le temps de jeu ? | **Oui : un temps de tour limité**, désactivable pour les tests, avec des signaux visuels et sonores. Durée, comportement à l'expiration et délai des décisions restent à trancher. | INTERFACE §3.9 et §7, feuille de route M4.5 |
| ARB-71 | Quelles priorités pour les gestes (M4.5) ? | Les boutons de recyclage au niveau du marché noir ; les actions d'équipage au niveau du vaisseau, avec des pictogrammes reconnaissables ; les informations d'un adversaire au survol de la souris ; le temps de tour limité (ARB-70). Les trois premiers points confirment INTERFACE §3.1, §3.3 et §3.4 : le panneau du mode test les regroupe aujourd'hui en bas à droite, c'est provisoire. | INTERFACE §8, feuille de route du README |
| ARB-72 | Que mettre dans les finitions (M5) ? | L'**audio** et un **fond stellaire animé**, en plus des modèles 3D (ADR-0016). | INTERFACE §8, ASSETS §2 à §4, feuille de route du README |
| ARB-73 | Les cartes doivent-elles rester des éléments d'interface 2D (ADR-0015) ? | **Non : les cartes sont aussi des assets 3D**, comme les vaisseaux (consigne du designer, 2026-09-25). Blender est mis de côté pour l'instant, avec des assets modifiables dans Unity. | ADR-0017, `Card.prefab`, INTERFACE §2 |

## 2026-09-25 : deuxième playtest (prototype complet)

Le game designer a joué le prototype complet (menus, gestes, aperçus). Le jeu est jugé « globalement très fonctionnel » ; ses remarques portent sur la forme.

| # | Question | Décision | Appliqué dans |
|---|---|---|---|
| ARB-74 | Comment disposer les actions d'équipage autour du vaisseau ? | **Un arc de cercle centré sur le vaisseau**, avec les actions d'attaque d'un côté (Attaque, Surcharge) et les actions de bouclier de l'autre (Reparamétrage, Sabotage). Mise en œuvre : attaque à gauche, du côté de la carte ATK, et bouclier à droite, du côté de la carte DEF ; sur chaque côté, l'action qui vise un adversaire est à l'extrémité et celle qui concerne son propre vaisseau vers le haut ; la Posture défensive (option) rejoint le côté bouclier. L'arc reste centré quelles que soient les options. | INTERFACE §3.4, `ActionArc` |
| ARB-75 | Le journal doit-il garder toute la partie ? | **Oui : on peut remonter le journal.** Il garde les 300 dernières lignes, défile à la molette, au doigt ou avec sa barre, et ne suit les nouvelles lignes que si l'on est en bas. | INTERFACE §3.8, `GameLogDisplay` |

## 2026-09-25 : premier atelier Blender

Le game designer a tranché les questions ouvertes d'ASSETS §5 avant le premier atelier Blender, mené en parallèle du prototype.

| # | Question | Décision | Appliqué dans |
|---|---|---|---|
| ARB-76 | Vaisseaux : cinq modèles distincts, ou un modèle recoloré à la couleur de chaque siège ? | **Un modèle recoloré d'abord.** Le jeu peint à la couleur du siège le matériau nommé `Siege` ; les autres matériaux gardent leurs couleurs. Des modèles distincts par siège pourront s'ajouter ensuite, le catalogue des vaisseaux les accepte déjà. | ASSETS §2, `ShipCatalog.PaintSeat` |
| ARB-77 | Style des modèles : low-poly à couleurs unies, ou textures peintes ? | **Low-poly texturé, avec des lumières.** Réponse du designer : « j'aimerais bien de la texture et des lumières, est-ce faisable en low poly ? ». C'est faisable : la forme reste low-poly (budget de triangles inchangé), le détail vient des textures (couleur, relief), et les parties lumineuses sont des matériaux émissifs que l'effet *Bloom* fait rayonner dans Unity. Vérifié sur un modèle d'essai passé par l'export. | ASSETS §2, `ArtImportRules.ConfigureModelTexture` |
| ARB-78 | Version de Blender : 4.5 LTS ou 5.2 ? | **5.2 LTS**, déjà installée : c'est la LTS du moment, comme le demande l'ADR-0016. Le script d'export y a tourné sans changement. | ADR-0016, CONTRIBUTING (Blender) |
| ARB-79 | Autoriser Blender MCP à télécharger des ressources CC0 depuis Poly Haven ? | **Pas pour l'instant.** Aucune ressource externe n'est nécessaire, donc aucune surface d'entrée de plus. Question à rouvrir pour le fond étoilé si besoin. | ASSETS §5, SECURITY §4 (inchangé) |

## 2026-09-25 : fin de l'interface du prototype

Le game designer a tranché les questions ouvertes d'INTERFACE §7 et les derniers points d'interface, sur les recommandations proposées. Les numéros ARB-76 à ARB-79 sont pris par l'atelier Blender.

| # | Question | Décision | Appliqué dans |
|---|---|---|---|
| ARB-80 | Temps de tour limité (ARB-70) : durée, expiration, décisions, réglage par défaut ? | **Durée réglable** dans le menu de partie locale : illimitée, 60, 90 ou 120 secondes. **Pas de limite par défaut en partie locale** ; la limite servira surtout en ligne. **À l'expiration**, le tour s'arrête simplement : fin du marché, puis fin de tour ; aucun coup n'est joué à la place du joueur. Seule une action imposée par une carte est jouée, car le tour ne peut pas finir sans elle. **Une décision demandée pendant le tour d'un autre joueur** a 15 secondes ; ensuite, le choix qu'un bot juge le meilleur pour ce joueur est pris à sa place. Le temps ne court que lorsque le joueur peut agir : ni pendant les animations, ni pendant la pause. | INTERFACE §3.9, `TurnClock`, `TurnTimerDisplay`, `LocalHotSeatSession.Expire` |
| ARB-81 | Marché réduit (INTERFACE §3.3) : où, quand et comment le rouvrir ? | **Une bande de miniatures sous le bandeau de manche**, à 40 % de sa taille. Le marché est **réduit en dehors de ma phase de marché**, y compris pendant les tours des autres, et **s'ouvre tout seul au début de ma phase de marché**. Un bouton « Marché » sous la bande l'ouvre pour le consulter, puis « Réduire le marché » le replie ; ce choix tient jusqu'au prochain changement de phase. Le zoom au survol fonctionne dans les deux états. | INTERFACE §3.3, `MarketDisplay.Follow`, `MarketDisplay.Toggle` |
| ARB-82 | Choix faits directement sur la table : plus aucun bouton pour répondre à une décision ? | **Oui, pour les décisions seulement** ; les autres boutons (Fin de tour, Passer le marché, Recycler, Combo, Pause, Journal, Vitesse) restent. La question s'affiche en bandeau, sans bouton, et ce qui y répond s'allume : les fiches des joueurs, les cartes de la table, les actions d'équipage. **a) Nombres** : une rangée de faces de d8 au centre, seules les valeurs permises actives. **b) Choix facultatifs** : la carte qui propose le choix s'allume dans une autre couleur, et la toucher veut dire « je passe » ; si elle n'est pas sur la table, on touche sa propre fiche (pour une déviation : garder l'attaque) ; à défaut, la carte est montrée au centre. **c) Voler ou détruire** : on glisse la carte vers son vaisseau pour la voler, au centre pour la détruire. Un sens de rotation se choisit en touchant le voisin qui reçoit son bouclier ; un événement fantôme, en touchant une des deux cartes montrées au centre. | INTERFACE §3.6, RULES B6, `DecisionChoices`, `DecisionBoard`, `PlayerControls` |

## 2026-09-25 : suite de l'atelier Blender

| # | Question | Décision | Appliqué dans |
|---|---|---|---|
| ARB-83 | Garder le mode sûr de Blender MCP pendant les ateliers ? | **Non, sur le poste du designer** (« on peut faire sauter la limitation de sécurité sur les scripts »). Le mode sûr refusait des scripts de modélisation inoffensifs : lire un script depuis un fichier, passer une fonction en paramètre. Il fallait renvoyer chaque essai en entier. Le dépôt reste en mode sûr par défaut : `.mcp.json` lit la variable d'environnement `BLENDER_MCP_SAFE_MODE` (`${BLENDER_MCP_SAFE_MODE:-1}`), que chaque poste peut mettre à `0`. C'est un raccourci d'outil de développement (SECURITY §0) : rien n'entre dans un build, et les autres mesures restent (localhost, téléchargements coupés, *Auto Run* désactivé, serveur démarré seulement pendant un atelier). | `.mcp.json`, SECURITY §4, CONTRIBUTING (Blender), ADR-0016 |
| ARB-84 | Premier vaisseau : quelle forme, quelles parties prennent la couleur du siège, quelles lumières, quel nom ? | **Sillage**, d'après une image de référence fournie par le designer (utilisée comme inspiration seulement : ni logo ni texte repris, l'image n'est pas versionnée). **Silhouette fidèle** : toutes les grandes formes de la référence ; le détail fin passe dans les textures. **La coque claire prend la couleur du siège** ; les parties noires, la verrière et les réacteurs gardent leurs couleurs (confirmé sur un essai en rouge). **Seuls les réacteurs brillent.** Forme validée en direct dans Blender. | `tools/blender/build_ship_sillage.py`, `art-src/ships/Ship_Sillage.blend`, `Art/Ships/Ship_Sillage.fbx`, `Theme/ShipCatalog`, ASSETS §2 |
