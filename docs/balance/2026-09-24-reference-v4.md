# Rapport d'équilibrage

> Généré par `Vortex.Simulator`. Commande : `run --players 5 --games 3000 --seed 1 --bot normal --skill normal,random`.
> Contenu analysé : empreinte `84a87bc60001…` (SHA-256 des quatre fichiers de contenu, variante appliquée). Même commande et même contenu ⇒ mêmes chiffres.

## Comment lire ce rapport

- Les parties sont jouées par des **bots** qui ne connaissent aucune carte : ils essaient chaque coup en le simulant, sans voir le futur (ADR-0010). Niveau des bots du scénario principal : **normal**.
- Les bots ne jouent pas comme des humains : les tendances et les écarts sont utiles, les valeurs absolues le sont moins.
- **Écart** d'une carte : taux de victoire des joueurs qui l'ont prise, moins le même taux sur toutes les cartes (ce qui neutralise le biais de survie). Un `*` signale un écart au-delà du bruit (plus de 2 écarts-types).

## 1. Durée et fin des parties

Durée estimée à 30 s par tour de joueur (hypothèse, à confronter aux parties réelles).

| Joueurs | Parties | Terminées | Égalités | Manches (moy. / méd. / p90) | Tours de joueur | Fin des temps atteinte | Victoires par Élection | Durée estimée |
|---|---|---|---|---|---|---|---|---|
| 5 | 3000 | 100,0 % | 0,2 % | 13,1 / 13 / 17 | 45,5 | 20,1 % | 7,1 % | 22,7 min |

## 2. Avantage de position

Taux de victoire selon la position dans le premier tour de table (1 = gagnant de l'initiative). Part équitable = 1/nombre de joueurs. **Écart max** = plus grand écart d'une position à la part équitable.

| Joueurs | Part équitable | Position 1 | Position 2 | Position 3 | Position 4 | Position 5 | Écart max |
|---|---|---|---|---|---|---|---|
| 5 | 20,0 % | 21,5 % | 22,3 % | 19,3 % | 17,4 % | 19,5 % | 2,6 pts |

## 3. Poids des choix (écart de niveau)

Un bot **normal** (place tournante) contre des bots **random**. **Ratio** = victoires ÷ part équitable : proche de 1, le niveau ne compte guère.

| Joueurs | Parties | Victoires du bot normal | Part équitable | Ratio |
|---|---|---|---|---|
| 5 | 3000 | 77,3 % | 20,0 % | 3,9 |

## 4. Combats, éliminations et retournements

- **Sur le meneur / le plus faible** : parmi les attaques faites avec au moins deux adversaires en vie, part visant l'adversaire qui avait le plus / le moins de PV.
- **Le meneur à mi-partie gagne** : le joueur qui avait seul le plus de PV à la manche du milieu remporte la partie. Élevé = peu de retournements.

| Joueurs | Attaques par tour | PV retirés par attaque | Attaques sans dégâts | Sur le meneur | Sur le plus faible | Première élimination (manche) | Le meneur à mi-partie gagne |
|---|---|---|---|---|---|---|---|
| 5 | 0,7 | 3,2 | 27,5 % | 36,7 % | 50,1 % | 4,7 | 53,1 % |

**Actions d'équipage choisies** (part de toutes les actions d'équipage) :

| Joueurs | Attaque | Reparamétrage | Sabotage | Surcharge | Posture défensive |
|---|---|---|---|---|---|
| 5 | 74,7 % | 6,5 % | 3,4 % | 15,4 % | 0,0 % |

## 5. Cartes

Toutes tailles de table confondues. **Prises** = prises au marché par partie. **Preneurs** = couples (partie, joueur) ayant pris la carte au moins une fois. Taux de victoire moyen d'un preneur, toutes cartes confondues : **28,0 %**.

| Carte | Nom | Prises / partie | Activations / prise | Preneurs | Victoires des preneurs | Écart |
|---|---|---|---|---|---|---|
| `D_016` | Orgueil | 0,4 | 0,6 | 1140 | 40,1 % | +12,1 pts * |
| `A_021` | Accident bactériologique | 0,4 | 0,8 | 1060 | 36,9 % | +8,9 pts * |
| `D_012` | Vente de pièces détachées | 0,4 | 0,8 | 1155 | 36,4 % | +8,4 pts * |
| `D_021` | Quarantaine obligatoire | 0,4 | 0,5 | 1166 | 36,1 % | +8,1 pts * |
| `D_024` | Roulette | 0,4 | 0,4 | 1181 | 36,0 % | +8,0 pts * |
| `D_002` | Dommage collatéral | 0,4 | 0,2 | 1232 | 35,9 % | +7,9 pts * |
| `D_003` | Ni vu ni connu | 0,4 | 0,3 | 1208 | 33,1 % | +5,1 pts * |
| `D_014` | Nothing else matters | 0,4 | 0,3 | 1248 | 33,1 % | +5,1 pts * |
| `A_001` | Canon à particules | 1,4 | — | 3799 | 32,9 % | +4,9 pts * |
| `A_027` | Tapis! | 0,3 | 0,6 | 1020 | 32,5 % | +4,6 pts * |
| `A_013` | On envoie la sauce | 0,4 | 0,6 | 1049 | 32,4 % | +4,4 pts * |
| `A_022` | Réseau fongique | 0,3 | 0,3 | 995 | 32,4 % | +4,4 pts * |
| `A_008` | Brocante spatiale | 0,4 | 0,9 | 1060 | 32,4 % | +4,4 pts * |
| `A_002` | Langue de bois | 0,4 | 0,4 | 1041 | 32,3 % | +4,3 pts * |
| `D_008` | T'as pas entendu un truc? | 1,2 | — | 3260 | 31,8 % | +3,8 pts * |
| `A_006` | Grosse Bertha | 0,3 | 0,5 | 1008 | 31,7 % | +3,8 pts * |
| `D_009` | Les affaires sont les affaires | 0,4 | 0,8 | 1200 | 31,6 % | +3,6 pts * |
| `A_018` | Appendice laser | 1,5 | — | 4058 | 31,2 % | +3,2 pts * |
| `D_017` | Loi du Talion | 0,6 | — | 1714 | 31,0 % | +3,0 pts * |
| `A_009` | Jamais deux sans trois | 0,3 | 0,7 | 1019 | 30,8 % | +2,8 pts * |
| `A_015` | Racket | 0,3 | 0,5 | 1006 | 30,7 % | +2,7 pts |
| `A_025` | Corruption du croupier | 0,4 | 0,3 | 1078 | 30,3 % | +2,3 pts |
| `D_026` | Le casino gagne toujours | 1,2 | — | 3329 | 29,8 % | +1,8 pts * |
| `A_012` | Vindicte populaire | 0,3 | 0,6 | 1012 | 29,7 % | +1,7 pts |
| `D_011` | Générateur auxiliaire | 1,2 | 0,6 | 3231 | 29,2 % | +1,2 pts |
| `A_007` | Délestage forcé | 0,3 | 0,3 | 998 | 29,2 % | +1,2 pts |
| `D_027` | La banque | 0,6 | — | 1771 | 28,9 % | +0,9 pts |
| `D_007` | Sous-couche blindée | 1,2 | — | 3233 | 28,1 % | +0,2 pts |
| `A_011` | Cruauté | 1,3 | — | 3631 | 28,0 % | +0,0 pts |
| `D_022` | Je te touche pas avec un bâton | 0,6 | — | 1769 | 28,0 % | -0,0 pts |
| `D_018` | Régénération parasitaire | 1,2 | — | 3414 | 28,0 % | -0,0 pts |
| `D_019` | Mimétisme cellulaire | 1,2 | — | 3340 | 27,8 % | -0,2 pts |
| `A_004` | La paix a un prix | 0,4 | 0,3 | 1049 | 27,7 % | -0,3 pts |
| `A_005` | Rétablir l'ordre | 1,5 | — | 4053 | 27,5 % | -0,5 pts |
| `A_017` | Dingo de la surcharge | 1,1 | — | 3091 | 26,9 % | -1,1 pts |
| `D_004` | Favoritisme | 1,2 | — | 3234 | 26,5 % | -1,5 pts |
| `D_023` | Main sûre | 1,2 | — | 3331 | 26,2 % | -1,8 pts * |
| `D_005` | Sabotage électoral | 1,4 | — | 3837 | 26,1 % | -1,9 pts * |
| `D_025` | Chance de cocu | 1,2 | — | 3265 | 25,8 % | -2,1 pts * |
| `A_016` | BLITZKRIEG! | 1,1 | — | 3142 | 25,8 % | -2,2 pts * |
| `D_013` | Vautours | 1,4 | — | 3792 | 25,8 % | -2,2 pts * |
| `A_024` | Black Jack | 1,1 | — | 3060 | 25,7 % | -2,3 pts * |
| `A_026` | Carte sous l'coude | 1,2 | — | 3321 | 25,5 % | -2,5 pts * |
| `A_020` | Spores corrosifs | 1,0 | — | 2807 | 25,5 % | -2,5 pts * |
| `A_003` | Epuration | 1,0 | — | 2825 | 25,4 % | -2,6 pts * |
| `D_001` | Intouchable | 1,2 | — | 3341 | 25,3 % | -2,7 pts * |
| `D_015` | Lève la tête, bombe le torse | 1,2 | — | 3325 | 25,2 % | -2,8 pts * |
| `A_019` | Agents pathogènes | 1,1 | — | 3145 | 25,0 % | -3,0 pts * |
| `A_014` | Recels en tous genres | 1,0 | — | 2850 | 24,7 % | -3,3 pts * |
| `D_010` | Niaque | 1,2 | — | 3321 | 24,2 % | -3,8 pts * |
| `D_006` | Mutinerie syndicale | 0,6 | — | 1777 | 24,1 % | -3,9 pts * |
| `A_023` | Pile ou face | 1,2 | — | 3523 | 23,7 % | -4,3 pts * |
| `A_010` | Le grand final | 0,9 | — | 2698 | 23,6 % | -4,3 pts * |
| `D_020` | Tout est une question d'équilibre | 1,2 | — | 3397 | 22,8 % | -5,2 pts * |

## 6. Couleurs et technologies

**Couleur dominante** d'un joueur : la couleur non neutre qu'il a le plus prise au marché (joueurs à égalité exclus).

| Couleur | Joueurs à dominante | Taux de victoire | Combos activés par partie |
|---|---|---|---|
| Bleu | 2755 | 24,1 % | 0,9 |
| Rouge | 1936 | 19,9 % | 0,6 |
| Vert | 2699 | 22,5 % | 0,9 |
| Jaune | 2571 | 19,4 % | 0,8 |

## 7. Événements

**Le meneur garde la tête** : le joueur qui avait seul le plus de PV avant l'événement l'a encore à la fin de la manche. Plus c'est bas, plus l'événement rebat les cartes. « Le calme avant la tempête » (sans effet) sert de témoin.

| Événement | Révélations par partie | Le meneur garde la tête |
|---|---|---|
| tempête électro-magnetique | 1,9 | 71,3 % |
| trou noir | 1,8 | 87,9 % |
| Nouvel arrivage | 1,8 | 82,7 % |
| **surcharge** Ionique | 1,9 | 75,4 % |
| Nuée parasitaire | 1,9 | 76,8 % |
| Espace aseptisé | 1,8 | 85,4 % |
| Le calme avant la tempête | 1,8 | 84,0 % |
| Fin des temps | 0,2 | 80,2 % |

## 8. Anomalies

Aucune : toutes les parties se sont terminées sans erreur du moteur.
