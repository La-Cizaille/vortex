# Équilibrage

Ce dossier contient le **plan d'équilibrage**, les **variantes** à tester et les **rapports du simulateur** (`Vortex.Simulator`), avec leur analyse. Un rapport est généré et reproductible : la commande complète et l'empreinte du contenu simulé figurent en tête. Il n'est jamais modifié à la main.

Les décisions du game designer sont consignées dans le [journal des arbitrages](../ARBITRAGES.md) (ARB-40 et suivants). Les choix d'outillage sont justifiés dans l'[ADR-0011](../adr/0011-options-de-regles-et-variantes.md).

## Démarche

On équilibre **d'abord les règles de base, puis les cartes**. La valeur d'une carte dépend des chiffres de base : un « +4 » n'a pas le même poids si une attaque retire 3,5 PV ou 6 PV.

Chaque changement suit la même boucle :

**hypothèse → variante → comparaison simulée → décision du game designer → PR avec le rapport.**

Les bots ne jouent pas comme des humains (ADR-0010). Un écart mesuré est un **signal à confirmer en partie réelle**, pas un verdict.

## Objectifs (étape 0)

Validés par le game designer (ARB-40), puis recentrés sur le **mode standard à 5 joueurs**, seule table équilibrée (ARB-52, ARB-53). De 2 à 4 joueurs, le jeu reste jouable, sans objectif d'équilibrage.

| Critère (5 joueurs) | Cible | Référence initiale | Aujourd'hui ([référence v3](2026-09-24-reference-v3.md)) |
|---|---|---|---|
| Avantage de position | Chaque position à ±3 pts de la part équitable | Premier joueur à +10 pts | **Atteint** : 2,5 pts |
| Durée | 20 à 25 min (ARB-53) | Environ 22 min | **Atteint** : environ 23 min (estimation à 30 s par tour) |
| Rôle de la Fin des temps | Filet de sécurité : atteinte dans moins de 30 % des parties | 90 % | **Atteint** : 20 % |
| Élection galactique | 5 à 15 % des victoires | 0,2 % | **Atteint** : 6,4 % |
| Puissance des cartes | Chaque carte à ±5 pts de la moyenne, aucune carte « morte » | Écarts de −7 à +12 pts | Non atteint : de −5,2 à +9,2 pts ; 9 cartes à +5 pts ou plus, 2 à −5 pts ou moins |
| Part des choix face au hasard | À définir avec les niveaux de bot (étape 1) | Mal mesurée | À définir |
| Première élimination | Pas avant la manche 6 : un joueur ne devrait pas sortir avant d'avoir joué environ 6 tours (cible fixée pour 4 joueurs, **à confirmer** à 5) | Non mesurée | Non atteint : manche 4,6 |

## Plan et avancement

| Étape | Contenu | État |
|---|---|---|
| **0** | Objectifs chiffrés | Validée (ARB-40) |
| **1** | Instruments de mesure : variantes et comparaison, niveaux de bot, nouvelles mesures, options de règles | Faite (ARB-41, ADR-0011) |
| **2.1** | Avantage du premier joueur : premier joueur tournant, sens horaire ou anti-horaire (ARB-42) | **Adoptée** : rotation horaire (ARB-50) |
| **2.2** | Grille PV × bouclier de départ × manche de Fin des temps, à 5 joueurs (ARB-43, ARB-52) | **Adoptée** : 30 PV, bouclier 5, Fin des temps à la manche 16 (ARB-54) |
| **2.3** | Nouvelles mécaniques (ARB-44, ARB-45) : relance d'un dé par la surcharge, reparamétrage à 2 dés dont on garde 1, coût du recyclage, posture défensive, événement annoncé, prime sur le leader, pillage, fantômes, défausse tactique | À faire |
| **2.4** | Fréquence des événements (une par manche, toutes les 2 ou 3 manches) et effet de chaque événement (ARB-46) | À faire |
| **2.5** | Élection galactique à 3 technologies au lieu de 4 (ARB-47) | **Adoptée** : 3 technologies (ARB-51). Fréquence de l'Élection à revoir avec 2.2 |
| **3** | Revue des cartes : tri, ajustement par famille, cartes de hasard, équilibre des couleurs. Outil visuel à construire au début de l'étape (ARB-48) | À faire |
| **4** | Validation humaine sur un prototype Unity (ARB-49) | Après l'étape 3 |

## Outils

### Rapport complet : `run`

```bash
dotnet run -c Release --project dotnet/Vortex.Simulator -- run --games 1000 --seed 1 --out docs/balance/<date>-<sujet>.md
```

Pour chaque taille de table demandée (5 joueurs par défaut), deux scénarios :
- **scénario principal** : tous les bots au niveau `--bot`. Il sert à mesurer la durée, l'avantage de position, les combats, les cartes, les couleurs et les événements ;
- **écart de niveau** : un bot `hero` contre des bots `others` (`--skill hero,others`), pour mesurer le poids des choix.

Avec `--variant <fichier>`, le rapport porte sur la variante au lieu de la référence.

### Comparer des variantes : `compare`

```bash
dotnet run -c Release --project dotnet/Vortex.Simulator -- compare --games 2000 --seed 1 --variant docs/balance/variants/<variante-a>.json --variant docs/balance/variants/<variante-b>.json --out docs/balance/<date>-<sujet>.md
```

La référence (le contenu du dépôt) et chaque variante jouent **les mêmes parties** : mêmes graines, donc mêmes mélanges et mêmes dés au départ. Le rapport donne, pour chaque taille de table, les indicateurs de chaque variante et leur écart avec la référence.

**Lire une comparaison**
- `+4,2 (… ± 2,9 pts *)` : la variante fait 4,2 points de plus que la référence. La marge d'erreur à 95 % est de 2,9 points, et le `*` indique que l'écart la dépasse.
- La marge suppose des échantillons indépendants. Comme les parties sont jumelles, elle est **prudente**.
- Un rapport contient des dizaines d'indicateurs : au seuil de 95 %, environ **un sur vingt** peut être marqué `*` par hasard. On ne conclut que sur des écarts attendus, nets, et confirmés par une autre graine (`--seed 2`) ou un autre niveau de bot.
- Avec 2 000 parties par taille de table, la marge sur un taux de victoire est d'environ ±3 pts. Il faut 4 fois plus de parties pour la diviser par 2.

### Explorer une grille : `grid`

```bash
dotnet run -c Release --project dotnet/Vortex.Simulator -- grid --grid docs/balance/grids/rythme-5-joueurs.json --games 1000 --seed 1 --out docs/balance/<date>-<sujet>.md
```

Une grille croise quelques réglages (au plus 4 axes, 8 valeurs par axe et 64 combinaisons) et simule chaque combinaison sur les mêmes graines, à une seule taille de table (5 joueurs par défaut). Le rapport **classe** les combinaisons :
1. d'abord celles qui atteignent le plus de cibles ;
2. puis celles dont l'**écart** est le plus petit : la somme des écarts relatifs aux bornes manquées. Par exemple, une Fin des temps atteinte dans 60 % des parties pour une cible de 30 % au plus compte pour (60 − 30) / 30 = 1.

Les valeurs dans la cible sont en gras, et la combinaison identique au contenu du dépôt est marquée « (réf.) ». Une grille sert à **trier** : avec 1 000 parties, un pourcentage n'est connu qu'à ±3 pts environ. On confirme ensuite les meilleures combinaisons avec `compare`, sur plus de parties.

Format d'un fichier de grille ([`grids/`](grids/)) : chaque valeur d'un axe est un correctif, écrit comme une variante mais sans `name` ni `description`. Deux axes ne peuvent pas modifier le même réglage : sinon l'un écraserait l'autre en silence.

```json
{
  "name": "Rythme à 5 joueurs",
  "description": "…",
  "axes": [
    { "name": "PV", "values": { "25": { "config": { "startingHp": 25, "maxHp": 25 } }, "30": { "config": { "startingHp": 30, "maxHp": 30 } } } },
    { "name": "Fin des temps", "values": { "10": { "config": { "playerCounts": { "5": { "doomRound": 10 } } } } } }
  ],
  "targets": { "durationMinutes": { "min": 20, "max": 25 }, "doomReached": { "max": 0.3 } }
}
```

Indicateurs disponibles pour les cibles : `durationMinutes` (durée estimée en minutes), `doomReached` (part des parties qui atteignent la Fin des temps, de 0 à 1), `electionShare` (part des victoires par Élection, de 0 à 1), `firstEliminationRound` (manche moyenne de la première élimination), `positionGapPoints` (écart maximal de position, en points).

### Options

| Option | Rôle | Défaut |
|---|---|---|
| `run`, `compare` ou `grid` | Commande (premier argument) | `run` |
| `--players` | Tailles de table simulées, par exemple `2,3,4,5` | `5`, le mode standard (ARB-52) |
| `--games` | Parties par taille de table (et par scénario) | `200` |
| `--seed` | Graine : même commande et même contenu donnent les mêmes chiffres | `1` |
| `--bot` | Niveau des bots du scénario principal : `random`, `naive`, `normal`, `strong` | `normal` |
| `--skill` | `run` seulement : niveaux du scénario « écart de niveau », `hero,others` | `normal,random` |
| `--variant` | Fichier de variante. Au plus 1 avec `run`, de 1 à 8 avec `compare`, aucun avec `grid` | — |
| `--grid` | `grid` seulement : fichier de grille. `grid` n'accepte qu'une taille de table | — |
| `--data` | Dossier du contenu de référence | `core/Runtime/Data` |
| `--out` | Fichier du rapport (sinon, sortie standard) | — |

Niveaux de bot (ADR-0010) :

| Niveau | Jeu |
|---|---|
| `random` | Coups légaux au hasard : aucune compétence |
| `naive` | Essaie chaque coup une fois, sans simuler la fin de son tour |
| `normal` | Essaie chaque coup sur 2 tirages et simule la fin de son tour |
| `strong` | Comme `normal`, sur 6 tirages : plus fort, mais nettement plus lent |

Un déséquilibre qu'on retrouve avec plusieurs niveaux est réel. S'il n'apparaît qu'avec un seul niveau, c'est probablement un artefact du bot.

Codes de sortie : 0 si tout va bien ; 1 si le moteur a rencontré une erreur (le rapport liste alors les graines pour rejouer les parties) ; 2 pour une option ou un contenu invalide.

### Écrire une variante

Une variante est un petit fichier JSON dans [`variants/`](variants/). Il ne contient **que ce qui change** :

```json
{
  "name": "Canon renforcé",
  "description": "A_001 vole 3 points de bouclier au lieu de 2.",
  "cards": { "A_001": { "effects": [ { "brick": "StealShieldBeforeAttack", "amount": 3 } ] } }
}
```

- `name` est obligatoire. `description`, `config` (voir [`variants/rotation-antihoraire.json`](variants/rotation-antihoraire.json)), `cards`, `events` et `technologies` sont facultatifs. Les cartes, événements et technologies sont désignés par leur id.
- Les réglages par taille de table se désignent par le nombre de joueurs : `"config": { "playerCounts": { "5": { "startShield": 5 } } }` ne change que la table à 5 joueurs.
- `name` et `description` suivent la même règle que les textes des cartes : aucun caractère de contrôle ni d'inversion de sens d'écriture, et tout sur une ligne. Un nom affiché dans un tableau ne peut pas contenir `|`.
- Les objets se fusionnent. Toute autre valeur, **tableaux compris**, est remplacée : pour changer une brique, on redonne la liste `effects` complète.
- Sont refusés : une clé ou un id inconnu, un changement d'id, une valeur `null`, une clé répétée, un changement de version de format.
- Le résultat est validé par **le même chargeur que le jeu**. Une variante invalide est refusée avec le message du chargeur.
- Les fichiers de contenu ne sont **jamais** modifiés. Un test vérifie que toutes les variantes du dossier restent valides.

## Évaluer une modification de carte

1. Décrire la modification dans une variante, puis la comparer à la référence avec `compare`, sur au moins 2 000 parties.
2. Si le changement est adopté par le game designer : modifier le JSON de la carte, valider le contenu (`Vortex.ContentTool validate`), consigner l'arbitrage dans le [journal](../ARBITRAGES.md), puis supprimer la variante devenue inutile.
3. Joindre le rapport de comparaison à la PR.

## Constats

### Référence v3 : rythme adopté (2026-09-24)

Rapport : [`2026-09-24-reference-v3.md`](2026-09-24-reference-v3.md). 3 000 parties à 5 joueurs par scénario, bots `normal`, aucune erreur du moteur. Règles : rotation horaire (ARB-50), Élection à 3 technologies (ARB-51), bouclier de départ 5 et Fin des temps à la manche 16 (ARB-54).

1. **Quatre objectifs sur cinq sont atteints** : position (2,5 pts), durée (environ 23 min), Fin des temps (20 %) et Élection (6,4 %).
2. **La première élimination reste précoce** (manche 4,6). Le premier éliminé attend environ 14 minutes. C'est la question ouverte de l'étape 2.3.
3. **L'écart entre les cartes s'est resserré** : de −5,2 à +9,2 pts, contre −7 à +16,7 avant. Orgueil (D_016) descend de +16,7 à +6,0 pts. Restent au-dessus de +5 pts : Quarantaine obligatoire (D_021), Accident bactériologique (A_021), Vente de pièces détachées (D_012), Roulette (D_024), Ni vu ni connu (D_003), Nothing else matters (D_014), Dommage collatéral (D_002), Orgueil (D_016) et, à la limite, T'as pas entendu un truc? (D_008). Les deux cartes des joueurs adjacents (A_021, D_024) sont fortes à 5 joueurs : ce mode leur donne bien leur sens. C'est la matière de la revue des cartes (étape 3).
4. **Les couleurs se sont rapprochées** : de 20,2 % (jaune) à 23,5 % (bleu) de victoires pour les joueurs à dominante, pour une part équitable de 20 %.
5. **Événements** : Tempête électro-magnétique et Surcharge ionique rebattent toujours le plus les cartes (le meneur garde la tête dans 71 à 74 % des cas, contre 85 % pour l'événement témoin). Trou noir semble toujours protéger le meneur (89 %). À examiner à l'étape 2.4.

### Étape 2.2 : rythme à 5 joueurs (2026-09-24)

Trois rapports, tous sans erreur du moteur :
1. [`2026-09-24-grille-rythme-5-joueurs.md`](2026-09-24-grille-rythme-5-joueurs.md) : PV (25, 30, 35) × bouclier de départ (4, 5, 6) × Fin des temps (manche 10, 12, 14), soit 27 combinaisons de 1 000 parties ;
2. [`2026-09-24-grille-rythme-5-joueurs-affine.md`](2026-09-24-grille-rythme-5-joueurs-affine.md) : les meilleures valeurs étaient au bord de la grille (bouclier 6, manche 14), donc second passage avec PV (25, 30) × bouclier (6, 7) × Fin des temps (14, 16, 18) ;
3. [`2026-09-24-rythme-confirmation.md`](2026-09-24-rythme-confirmation.md) : les cinq meilleurs candidats, 3 000 parties chacun, avec **une autre graine** (2).

**Confirmation à 5 joueurs** (durée estimée à 30 s par tour, d'après les grilles) :

| Candidat | Fin des temps atteinte | Élection | Première élimination | Attaques sans dégâts | Durée |
|---|---|---|---|---|---|
| Aujourd'hui : 30 PV, bouclier 4, manche 10 | 88,5 % | 4,4 % | manche 4,3 | 21,7 % | environ 21 min |
| 30 PV, bouclier 4, manche 16 | **17,0 %** | **5,4 %** (à la limite) | 4,3 | 23,7 % | environ 22 min |
| **30 PV, bouclier 5, manche 16** | **20,3 %** | **6,9 %** | 4,6 | 27,4 % | environ 22 min |
| 30 PV, bouclier 6, manche 16 | **23,3 %** | **6,8 %** | 4,8 | 29,7 % | environ 23 min |
| 30 PV, bouclier 7, manche 16 | **24,9 %** | **7,7 %** | 4,9 | 29,9 % | environ 23,5 min |
| 25 PV, bouclier 6, manche 16 | **9,0 %** | **5,1 %** (à la limite) | 4,1 | 30,2 % | environ 20,5 min |

1. **La Fin des temps à la manche 16 règle le problème principal.** Elle n'est plus atteinte que dans 9 à 25 % des parties, au lieu de 88 %. Les parties se terminent par élimination vers la manche 13, et leur durée bouge à peine : la Fin des temps redevient un **filet de sécurité**, son rôle voulu. Au-delà de la manche 16, rien ne change, sauf la part de parties qui l'atteignent.
2. **L'Élection entre dans sa cible** (5 à 15 %), grâce aux parties un peu plus longues. Elle est au plus haut avec un bouclier de départ élevé.
3. **Un bouclier de départ plus haut retarde un peu la première élimination** (+0,3 à +0,6 manche), mais il a un coût : davantage d'attaques sans dégâts, jusqu'à près d'une sur trois avec un bouclier de 6 ou 7. C'est un risque de frustration. Effet secondaire utile : Générateur auxiliaire (D_011, bouclier à 8), trop fort aujourd'hui, revient près de la moyenne.
4. **Aucun réglage de rythme ne place la première élimination à la manche 6.** Le maximum mesuré est la manche 5,6, avec 35 PV et un bouclier de 6, et les parties dépassent alors 25 minutes. À 5 joueurs, 4 adversaires peuvent viser le plus faible dès le début. Ce point relève soit des mécaniques de l'étape 2.3 (posture défensive, prime sur le leader, fantômes), soit d'une cible adaptée au mode à 5 joueurs.

**Recommandation : 30 PV, bouclier de départ 5, Fin des temps à la manche 16** (à 5 joueurs). C'est le meilleur compromis :
- toutes les cibles de rythme sont atteintes avec de la marge ;
- la première élimination recule un peu ;
- le coût en attaques sans dégâts reste modéré (27 % au lieu de 22 %).

Si l'on veut toucher le moins de choses possible, **30 PV, bouclier 4, manche 16** ne change que la Fin des temps, mais l'Élection est alors à la limite de sa cible. **Décision : la recommandation est adoptée (ARB-54).**

### Référence v2 : règles adoptées (2026-09-24)

Rapport : [`2026-09-24-reference-v2.md`](2026-09-24-reference-v2.md). 2 000 parties par taille de table et par scénario, bots `normal`, aucune erreur du moteur. Règles : rotation horaire du premier joueur (ARB-50) et Élection à 3 technologies (ARB-51). Lecture sur le mode standard à 5 joueurs (ARB-52).

1. **Position et durée sont dans les cibles** à 5 joueurs : écart maximal de 2,9 pts, environ 21 minutes par partie.
2. **La Fin des temps décide encore de la plupart des parties** (89 %), alors que la **première élimination arrive trop tôt** (manche 4,3) : le premier éliminé attend alors environ 15 minutes. Ces deux cibles tirent en sens opposés : il faut un début de partie plus sûr et une fin plus rapide. C'est l'objet de la grille de l'étape 2.2 (PV, bouclier de départ et manche de Fin des temps). La mécanique « fantômes » (étape 2.3), qui garde les joueurs éliminés dans la partie, prend de l'importance à 5.
3. **L'Élection recule un peu avec la rotation** : 3,7 % à 5 joueurs, contre 5,0 % avec 3 technologies et l'ordre fixe. Les parties sont un peu plus courtes, donc on réunit moins de combos. Elle reste un objectif de l'étape 2.2.
4. **Cartes** (toutes tables confondues ; les prochains rapports, à 5 joueurs par défaut, isoleront le mode standard) : une quinzaine de cartes sont à +5 pts ou plus, et deux sous −5 pts. Les cartes à **usage unique** occupent presque tout le haut du classement. Deux explications possibles, à départager à l'étape 3 : elles sont réellement trop fortes, ou les bots les prennent surtout quand ils sont déjà en position de force (biais de sélection). Orgueil (D_016) reste en tête (+16,7 pts). Tout est une question d'équilibre (D_020, −7,0 pts) et Le grand final (A_010, −5,6 pts) restent en bas.
5. **Couleurs** : les joueurs à dominante **bleue** gagnent 33,4 % de leurs parties, contre 27,5 % pour le jaune. Un point à examiner à l'étape 3.
6. **Événements** : Tempête électro-magnétique et Surcharge ionique rebattent le plus les cartes (le meneur garde la tête dans 72 % des cas, contre 83 % pour l'événement témoin sans effet). Trou noir semble plutôt protéger le meneur (87 %). À approfondir à l'étape 2.4.

### Étapes 2.1 et 2.5 : ordre de jeu et Élection (2026-09-24)

Rapport : [`2026-09-24-ordre-et-election.md`](2026-09-24-ordre-et-election.md). 2 000 parties par taille de table et par variante (32 000 parties au total), bots `normal`, aucune erreur du moteur.

**Premier joueur tournant (2.1, ARB-42).** Écart maximal d'une position à la part équitable, en points :

| Joueurs | Cible | Référence | Rotation horaire | Rotation anti-horaire |
|---|---|---|---|---|
| 2 | ±5 | 21,0 | **1,4** | **1,4** |
| 3 | ±3 | 14,9 | 3,5 | 3,7 |
| 4 | ±3 | 11,4 | **1,7** | **1,4** |
| 5 | ±3 | 9,8 | **2,8** | **2,3** |

1. **La rotation supprime presque tout l'avantage du premier joueur**, dans les deux sens. La cible est atteinte à 2, 4 et 5 joueurs, et presque atteinte à 3 joueurs (3,5 pts pour ±3).
2. **En duel, les deux sens donnent exactement le même ordre** (A B, B A, A B…) : les chiffres sont identiques, ce qui confirme la cohérence de l'outil.
3. **Les deux sens se valent sur l'équité.** Ils diffèrent sur le rythme :
   - en **horaire**, le joueur qui vient d'ouvrir la manche joue en dernier à la suivante : il attend deux fois plus longtemps que les autres ;
   - en **anti-horaire**, le dernier joueur d'une manche ouvre la suivante : il joue **deux tours d'affilée**. Les attaques deviennent plus efficaces (à 3 joueurs : 18,5 % d'attaques sans dégâts au lieu de 21,1 %, 4,2 PV retirés par attaque au lieu de 3,7), et la première élimination arrive un peu plus tôt (−0,2 manche).
4. **Effets secondaires faibles** : à 2 et 3 joueurs, les parties sont un peu plus courtes (−0,4 à −0,5 manche) et la Fin des temps est un peu moins souvent atteinte (−3 à −6 pts). Rien de mesurable à 4 et 5 joueurs.
5. **Cartes** : la rotation change réellement la valeur de certaines cartes. On compte 32 mouvements au-delà du bruit pour les deux sens, là où le hasard seul en produirait environ 5. Chaque ligne prise isolément reste toutefois incertaine. Le signal le plus solide est Orgueil (D_016), déjà la carte la plus forte : elle passe de +11,7 à +16,8 pts, avec le même mouvement dans les deux sens. L'Élection à 3 technologies ne déplace aucune carte.

**Recommandation** : adopter la rotation **horaire**. Elle atteint les mêmes cibles, et elle ne crée pas de double tour, qui avance la première élimination alors que celle-ci est déjà trop précoce (voir plus bas). Le sens anti-horaire reste un bon choix si l'on veut des parties plus nerveuses. **Décision : rotation horaire adoptée (ARB-50).**

**Élection à 3 technologies (2.5, ARB-47).** Part des victoires par Élection galactique :

| Joueurs | Référence (4 technologies) | 3 technologies | Cible |
|---|---|---|---|
| 2 | 0,0 % | 2,4 % | 5 à 15 % |
| 3 | 0,2 % | 2,6 % | 5 à 15 % |
| 4 | 0,1 % | 3,6 % | 5 à 15 % |
| 5 | 0,2 % | 5,0 % | 5 à 15 % |

1. **Trois technologies rendent l'Élection possible, mais pas encore assez fréquente** : la cible n'est atteinte qu'à 5 joueurs.
2. **Aucun effet secondaire mesurable** : durée, avantage de position et combats sont inchangés.
3. **Limite des bots** : ils valorisent chaque technologie, mais ils ne planifient pas une collection. Un joueur humain qui vise l'Élection l'obtiendra plus souvent. La mesure est donc un **minimum**.

**Recommandation** : adopter 3 technologies, qui va dans le bon sens sans rien dégrader, puis revoir l'Élection avec la grille de l'étape 2.2 : des parties plus longues laissent plus de temps pour réunir des combos. **Décision : 3 technologies adoptées (ARB-51).**

**Nouvelles mesures de l'étape 1, sur la référence**

| Joueurs | Première élimination (manche) | Le meneur à mi-partie gagne | Attaques sur le meneur |
|---|---|---|---|
| 2 | 8,9 | 69 % | — |
| 3 | 6,1 | 65 % | 52 % |
| 4 | **5,0** | 60 % | 41 % |
| 5 | 4,3 | 56 % | 36 % |

- **La première élimination est trop précoce** : manche 5 à 4 joueurs, pour une cible « pas avant la manche 6 ». La rotation ne la corrige pas. Le levier naturel est la grille PV et bouclier de départ de l'étape 2.2.
- **Les retournements existent** : le meneur à mi-partie gagne 56 à 69 % du temps, d'autant moins que la table est grande.
- **Les bots visent le meneur un peu plus que le hasard** (41 % à 4 joueurs, pour 33 % avec un choix au hasard), en partie à cause des égalités de PV, qui comptent comme « meneur ». La « prime sur le leader » (étape 2.3) mesurera l'effet d'une vraie incitation.

### Référence du 2026-09-24

Rapport : [`2026-09-24-reference.md`](2026-09-24-reference.md), 8 000 parties, aucune erreur du moteur. Il a été produit avant l'étape 1, avec les bots `normal` (anciennement `--samples 2`).

1. **Fort avantage au premier joueur.** Le joueur qui gagne l'initiative remporte 71 % des duels (au lieu de 50 %), 46 % des parties à 3 (au lieu de 33 %) et 38 % à 4 (au lieu de 25 %). Le taux de victoire décroît avec la position dans le tour. C'est le déséquilibre le plus net, et il est structurel.
2. **La Fin des temps rythme les parties.** Elle est atteinte dans 38 % des duels, 67 % des parties à 3, 82 % à 4 et 90 % à 5. Les parties durent en moyenne 9 à 13 manches, soit environ 9 à 22 minutes à 30 s par tour de joueur (hypothèse). Le réglage de `DoomRound[n]` sera le levier principal de la durée.
3. **L'Élection galactique n'arrive presque jamais** (0 à 0,4 % des victoires). Réunir 4 paires de couleurs en sacrifiant ses cartes semble irréaliste dans la durée d'une partie.
4. **Les choix comptent face au hasard pur.** Un bot `normal` bat des bots aléatoires dans 77 à 99 % des parties. C'est rassurant, mais la référence aléatoire est faible (ADR-0010).
5. **Combats.** Environ 3,5 PV retirés par attaque, et une attaque sur cinq ne fait aucun dégât.
6. **Cartes à surveiller** (écart par rapport à la moyenne des cartes) :
   - **fortes** : Orgueil (D_016, +12 pts), Quarantaine obligatoire (D_021), Canon à particules (A_001), Vente de pièces détachées (D_012), Générateur auxiliaire (D_011) ;
   - **faibles** : Tout est une question d'équilibre (D_020, −7 pts), Le grand final (A_010, −6 pts), Spores corrosifs (A_020), Recels en tous genres (A_014), Niaque (D_010).

   Les soins massifs et le bouclier ressortent en tête : dans un jeu où l'on retire peu de PV par attaque, récupérer des PV ou monter son bouclier est très rentable. Ces classements dépendent en partie du style des bots ; ils seront confirmés lors de la revue des cartes.
