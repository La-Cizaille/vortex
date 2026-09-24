# Rapport d'équilibrage

> Généré par `Vortex.Simulator`. Commande : `run --players 2,3,4,5 --games 2000 --seed 1 --bot normal --skill normal,random`.
> Contenu analysé : empreinte `4270b7e75302…` (SHA-256 des quatre fichiers de contenu, variante appliquée). Même commande et même contenu ⇒ mêmes chiffres.

## Comment lire ce rapport

- Les parties sont jouées par des **bots** qui ne connaissent aucune carte : ils essaient chaque coup en le simulant, sans voir le futur (ADR-0010). Niveau des bots du scénario principal : **normal**.
- Les bots ne jouent pas comme des humains : les tendances et les écarts sont utiles, les valeurs absolues le sont moins.
- **Écart** d'une carte : taux de victoire des joueurs qui l'ont prise, moins le même taux sur toutes les cartes (ce qui neutralise le biais de survie). Un `*` signale un écart au-delà du bruit (plus de 2 écarts-types).

## 1. Durée et fin des parties

Durée estimée à 30 s par tour de joueur (hypothèse, à confronter aux parties réelles).

| Joueurs | Parties | Terminées | Égalités | Manches (moy. / méd. / p90) | Tours de joueur | Fin des temps atteinte | Victoires par Élection | Durée estimée |
|---|---|---|---|---|---|---|---|---|
| 2 | 2000 | 100,0 % | 0,2 % | 8,4 / 8 / 11 | 16,1 | 33,0 % | 0,8 % | 8,0 min |
| 3 | 2000 | 100,0 % | 0,0 % | 10,2 / 10 / 13 | 25,1 | 61,0 % | 1,9 % | 12,6 min |
| 4 | 2000 | 100,0 % | 0,3 % | 11,7 / 11 / 15 | 34,4 | 79,7 % | 2,4 % | 17,2 min |
| 5 | 2000 | 100,0 % | 0,3 % | 12,5 / 12 / 16 | 42,9 | 88,8 % | 3,7 % | 21,4 min |

## 2. Avantage de position

Taux de victoire selon la position dans le premier tour de table (1 = gagnant de l'initiative). Part équitable = 1/nombre de joueurs. **Écart max** = plus grand écart d'une position à la part équitable.

| Joueurs | Part équitable | Position 1 | Position 2 | Position 3 | Position 4 | Position 5 | Écart max |
|---|---|---|---|---|---|---|---|
| 2 | 50,0 % | 48,5 % | 51,5 % | — | — | — | 1,5 pts |
| 3 | 33,3 % | 36,9 % | 33,0 % | 30,2 % | — | — | 3,5 pts |
| 4 | 25,0 % | 26,8 % | 23,8 % | 24,5 % | 24,9 % | — | 1,8 pts |
| 5 | 20,0 % | 22,3 % | 20,6 % | 19,7 % | 20,4 % | 17,1 % | 2,9 pts |

## 3. Poids des choix (écart de niveau)

Un bot **normal** (place tournante) contre des bots **random**. **Ratio** = victoires ÷ part équitable : proche de 1, le niveau ne compte guère.

| Joueurs | Parties | Victoires du bot normal | Part équitable | Ratio |
|---|---|---|---|---|
| 2 | 2000 | 99,4 % | 50,0 % | 2,0 |
| 3 | 2000 | 95,3 % | 33,3 % | 2,9 |
| 4 | 2000 | 87,2 % | 25,0 % | 3,5 |
| 5 | 2000 | 76,3 % | 20,0 % | 3,8 |

## 4. Combats, éliminations et retournements

- **Sur le meneur / le plus faible** : parmi les attaques faites avec au moins deux adversaires en vie, part visant l'adversaire qui avait le plus / le moins de PV.
- **Le meneur à mi-partie gagne** : le joueur qui avait seul le plus de PV à la manche du milieu remporte la partie. Élevé = peu de retournements.

| Joueurs | Attaques par tour | PV retirés par attaque | Attaques sans dégâts | Sur le meneur | Sur le plus faible | Première élimination (manche) | Le meneur à mi-partie gagne |
|---|---|---|---|---|---|---|---|
| 2 | 0,6 | 4,8 | 16,7 % | — | — | 8,5 | 68,0 % |
| 3 | 0,7 | 3,8 | 20,7 % | 52,0 % | 68,3 % | 6,1 | 65,9 % |
| 4 | 0,7 | 3,7 | 21,2 % | 41,4 % | 57,4 % | 4,9 | 59,1 % |
| 5 | 0,8 | 3,6 | 21,6 % | 36,0 % | 50,0 % | 4,3 | 54,8 % |

## 5. Cartes

Toutes tailles de table confondues. **Prises** = prises au marché par partie. **Preneurs** = couples (partie, joueur) ayant pris la carte au moins une fois. Taux de victoire moyen d'un preneur, toutes cartes confondues : **35,4 %**.

| Carte | Nom | Prises / partie | Activations / prise | Preneurs | Victoires des preneurs | Écart |
|---|---|---|---|---|---|---|
| `D_016` | Orgueil | 0,3 | 0,8 | 2141 | 52,1 % | +16,7 pts * |
| `D_024` | Roulette | 0,3 | 0,4 | 2222 | 45,6 % | +10,2 pts * |
| `D_012` | Vente de pièces détachées | 0,3 | 0,8 | 2183 | 45,1 % | +9,6 pts * |
| `A_021` | Accident bactériologique | 0,2 | 0,7 | 1817 | 44,9 % | +9,4 pts * |
| `D_003` | Ni vu ni connu | 0,3 | 0,4 | 2228 | 43,8 % | +8,4 pts * |
| `D_021` | Quarantaine obligatoire | 0,3 | 0,5 | 2255 | 43,8 % | +8,3 pts * |
| `D_009` | Les affaires sont les affaires | 0,3 | 0,8 | 2228 | 42,9 % | +7,4 pts * |
| `A_002` | Langue de bois | 0,2 | 0,5 | 1844 | 42,5 % | +7,0 pts * |
| `A_027` | Tapis! | 0,2 | 0,6 | 1774 | 42,1 % | +6,6 pts * |
| `D_014` | Nothing else matters | 0,3 | 0,4 | 2159 | 42,0 % | +6,5 pts * |
| `A_006` | Grosse Bertha | 0,2 | 0,6 | 1747 | 41,7 % | +6,3 pts * |
| `A_013` | On envoie la sauce | 0,2 | 0,6 | 1750 | 41,5 % | +6,1 pts * |
| `A_022` | Réseau fongique | 0,2 | 0,4 | 1747 | 40,6 % | +5,2 pts * |
| `A_008` | Brocante spatiale | 0,2 | 0,8 | 1879 | 40,4 % | +5,0 pts * |
| `A_015` | Racket | 0,2 | 0,5 | 1805 | 40,4 % | +5,0 pts * |
| `D_017` | Loi du Talion | 0,4 | — | 2975 | 40,4 % | +4,9 pts * |
| `A_001` | Canon à particules | 0,9 | — | 7079 | 40,0 % | +4,6 pts * |
| `D_002` | Dommage collatéral | 0,3 | 0,2 | 2230 | 39,7 % | +4,3 pts * |
| `D_008` | T'as pas entendu un truc? | 0,8 | — | 5740 | 39,6 % | +4,2 pts * |
| `D_011` | Générateur auxiliaire | 0,8 | 0,7 | 5613 | 39,4 % | +4,0 pts * |
| `A_007` | Délestage forcé | 0,2 | 0,3 | 1805 | 39,4 % | +4,0 pts * |
| `A_004` | La paix a un prix | 0,2 | 0,3 | 1810 | 39,2 % | +3,7 pts * |
| `A_018` | Appendice laser | 1,0 | — | 7397 | 39,0 % | +3,6 pts * |
| `A_012` | Vindicte populaire | 0,2 | 0,6 | 1744 | 38,5 % | +3,0 pts * |
| `A_009` | Jamais deux sans trois | 0,2 | 0,6 | 1806 | 38,4 % | +3,0 pts * |
| `A_025` | Corruption du croupier | 0,2 | 0,4 | 1762 | 37,0 % | +1,6 pts |
| `A_005` | Rétablir l'ordre | 1,0 | — | 7564 | 36,3 % | +0,9 pts |
| `D_027` | La banque | 0,4 | — | 3094 | 35,9 % | +0,5 pts |
| `D_007` | Sous-couche blindée | 0,7 | — | 5638 | 34,9 % | -0,5 pts |
| `D_026` | Le casino gagne toujours | 0,7 | — | 5582 | 34,8 % | -0,6 pts |
| `D_005` | Sabotage électoral | 1,0 | — | 7306 | 34,6 % | -0,9 pts |
| `D_022` | Je te touche pas avec un bâton | 0,4 | — | 3054 | 34,4 % | -1,0 pts |
| `D_019` | Mimétisme cellulaire | 0,8 | — | 5833 | 34,1 % | -1,3 pts * |
| `A_017` | Dingo de la surcharge | 0,7 | — | 5423 | 34,1 % | -1,3 pts * |
| `D_018` | Régénération parasitaire | 0,8 | — | 5932 | 33,9 % | -1,6 pts * |
| `A_016` | BLITZKRIEG! | 0,7 | — | 5473 | 33,8 % | -1,6 pts * |
| `A_024` | Black Jack | 0,7 | — | 5489 | 33,5 % | -2,0 pts * |
| `D_023` | Main sûre | 0,7 | — | 5635 | 33,2 % | -2,2 pts * |
| `D_025` | Chance de cocu | 0,8 | — | 5664 | 33,0 % | -2,4 pts * |
| `A_003` | Epuration | 0,7 | — | 4939 | 32,9 % | -2,5 pts * |
| `A_011` | Cruauté | 0,9 | — | 6647 | 32,7 % | -2,7 pts * |
| `D_001` | Intouchable | 0,7 | — | 5655 | 32,6 % | -2,9 pts * |
| `D_004` | Favoritisme | 0,8 | — | 5688 | 32,4 % | -3,1 pts * |
| `D_010` | Niaque | 0,8 | — | 5737 | 32,1 % | -3,4 pts * |
| `A_019` | Agents pathogènes | 0,7 | — | 5398 | 32,0 % | -3,4 pts * |
| `A_026` | Carte sous l'coude | 0,8 | — | 6100 | 31,9 % | -3,5 pts * |
| `D_015` | Lève la tête, bombe le torse | 0,8 | — | 5688 | 31,8 % | -3,7 pts * |
| `D_013` | Vautours | 0,9 | — | 6761 | 31,6 % | -3,8 pts * |
| `A_020` | Spores corrosifs | 0,6 | — | 4808 | 31,4 % | -4,0 pts * |
| `D_006` | Mutinerie syndicale | 0,4 | — | 2965 | 31,4 % | -4,1 pts * |
| `A_014` | Recels en tous genres | 0,6 | — | 4730 | 31,0 % | -4,5 pts * |
| `A_023` | Pile ou face | 0,8 | — | 6262 | 30,6 % | -4,9 pts * |
| `A_010` | Le grand final | 0,6 | — | 4560 | 29,9 % | -5,6 pts * |
| `D_020` | Tout est une question d'équilibre | 0,8 | — | 5814 | 28,4 % | -7,0 pts * |

## 6. Couleurs et technologies

**Couleur dominante** d'un joueur : la couleur non neutre qu'il a le plus prise au marché (joueurs à égalité exclus).

| Couleur | Joueurs à dominante | Taux de victoire | Combos activés par partie |
|---|---|---|---|
| Bleu | 5409 | 33,4 % | 0,6 |
| Rouge | 3568 | 29,8 % | 0,4 |
| Vert | 4840 | 28,5 % | 0,5 |
| Jaune | 4692 | 27,5 % | 0,5 |

## 7. Événements

**Le meneur garde la tête** : le joueur qui avait seul le plus de PV avant l'événement l'a encore à la fin de la manche. Plus c'est bas, plus l'événement rebat les cartes. « Le calme avant la tempête » (sans effet) sert de témoin.

| Événement | Révélations par partie | Le meneur garde la tête |
|---|---|---|
| tempête électro-magnetique | 1,5 | 72,0 % |
| trou noir | 1,4 | 86,9 % |
| Nouvel arrivage | 1,4 | 81,0 % |
| **surcharge** Ionique | 1,5 | 72,7 % |
| Nuée parasitaire | 1,5 | 78,6 % |
| Espace aseptisé | 1,4 | 83,6 % |
| Le calme avant la tempête | 1,4 | 82,9 % |
| Fin des temps | 0,7 | 80,8 % |

## 8. Anomalies

Aucune : toutes les parties se sont terminées sans erreur du moteur.
