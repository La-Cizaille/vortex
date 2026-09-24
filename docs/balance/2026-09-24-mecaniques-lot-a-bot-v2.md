# Comparaison de variantes

> Généré par `Vortex.Simulator`. Commande : `compare --players 5 --games 3000 --seed 1 --bot normal --variant docs/balance/variants/posture-defensive.json --variant docs/balance/variants/prime-leader-1.json`.
> Contenu analysé : empreinte `84a87bc60001…` (SHA-256 des quatre fichiers de contenu, variante appliquée). Même commande et même contenu ⇒ mêmes chiffres.

Chaque variante est simulée avec **les mêmes graines** que la référence (bots **normal**). Entre parenthèses : écart avec la référence, ± sa marge d'erreur à 95 %. Un `*` signale un écart au-delà du bruit statistique.

## Variantes

- **Référence** (empreinte `84a87bc60001…`) : Contenu actuel du dépôt.
- **Posture défensive (+2)** (empreinte `6c2b6726b319…`) : Étape 2.3 (ARB-44) : nouvelle action d'équipage, +2 au bouclier effectif jusqu'au prochain tour du joueur (RULES A5.4).
- **Prime sur le leader (+1)** (empreinte `73d9886fa8aa…`) : Étape 2.3 (ARB-44) : +1 à la valeur d'attaque contre le seul joueur qui a le plus de PV (RULES A6).

## 5 joueurs

| Indicateur | Référence | Posture défensive (+2) | Prime sur le leader (+1) |
|---|---|---|---|
| Écart max de position (pts) | 2,6 | 3,5 (+0,9) | 1,7 (-0,8) |
| Victoires en position 1 | 21,5 % | 20,9 % (-0,6 ± 2,1 pts) | 19,7 % (-1,8 ± 2,0 pts) |
| Victoires en position 2 | 22,3 % | 21,4 % (-0,8 ± 2,1 pts) | 21,7 % (-0,5 ± 2,1 pts) |
| Victoires en position 3 | 19,3 % | 19,9 % (+0,6 ± 2,0 pts) | 19,7 % (+0,4 ± 2,0 pts) |
| Victoires en position 4 | 17,4 % | 21,3 % (+3,9 ± 2,0 pts *) | 19,9 % (+2,5 ± 2,0 pts *) |
| Victoires en position 5 | 19,5 % | 16,5 % (-3,0 ± 1,9 pts *) | 18,9 % (-0,6 ± 2,0 pts) |
| Manches (moyenne) | 13,1 | 15,1 (+2,0 ± 0,2 *) | 12,7 (-0,4 ± 0,1 *) |
| Fin des temps atteinte | 20,1 % | 44,4 % (+24,3 ± 2,3 pts *) | 15,4 % (-4,8 ± 1,9 pts *) |
| Victoires par Élection | 7,1 % | 11,4 % (+4,3 ± 1,5 pts *) | 5,5 % (-1,6 ± 1,2 pts *) |
| Égalités | 0,2 % | 0,2 % (-0,0 ± 0,2 pts) | 0,2 % (-0,0 ± 0,2 pts) |
| Première élimination (manche) | 4,7 | 5,4 (+0,8 ± 0,1 *) | 4,7 (+0,1 ± 0,1) |
| Le meneur à mi-partie gagne | 53,1 % | 53,5 % (+0,4 ± 2,6 pts) | 46,9 % (-6,2 ± 2,7 pts *) |
| Attaques sur le meneur | 36,7 % | 36,5 % (-0,2 ± 0,5 pts) | 40,4 % (+3,7 ± 0,4 pts *) |
| Attaques sur le plus faible | 50,1 % | 50,5 % (+0,4 ± 0,5 pts) | 47,8 % (-2,3 ± 0,5 pts *) |
| Attaques sans dégâts | 27,5 % | 25,7 % (-1,8 ± 0,4 pts *) | 26,8 % (-0,7 ± 0,4 pts *) |
| Action d'équipage : Attaque | 74,7 % | 55,5 % (-19,1 ± 0,3 pts *) | 76,1 % (+1,4 ± 0,3 pts *) |
| Action d'équipage : Reparamétrage | 6,5 % | 5,3 % (-1,2 ± 0,2 pts *) | 6,1 % (-0,4 ± 0,2 pts *) |
| Action d'équipage : Sabotage | 3,4 % | 2,4 % (-1,0 ± 0,1 pts *) | 3,3 % (-0,1 ± 0,1 pts) |
| Action d'équipage : Surcharge | 15,4 % | 4,3 % (-11,1 ± 0,2 pts *) | 14,5 % (-0,9 ± 0,3 pts *) |
| Action d'équipage : Posture défensive | 0,0 % | 32,5 % (+32,5 ± 0,2 pts *) | 0,0 % (+0,0 ± 0,0 pts) |
| PV retirés par attaque | 3,2 | 3,3 (+0,1) | 3,3 (+0,1) |
| Attaques par tour | 0,7 | 0,6 (-0,2) | 0,8 (+0,0) |

## Cartes dont l'écart change nettement

Toutes tables confondues. Écart = taux de victoire des preneurs moins la moyenne des cartes. Seules les cartes dont l'écart bouge au-delà du bruit sont listées.
Attention : 108 couples (carte, variante) sont testés au seuil de 95 %, donc environ **5,4** lignes peuvent apparaître par pur hasard. Ne retenir que les changements nets et confirmés par une autre graine.

| Variante | Carte | Écart (référence) | Écart (variante) | Changement |
|---|---|---|---|---|
| Posture défensive (+2) | `A_016` BLITZKRIEG! | -2,2 pts | +1,0 pts | +3,2 ± 2,2 pts |
| Posture défensive (+2) | `A_011` Cruauté | +0,0 pts | -2,9 pts | -3,0 ± 2,0 pts |
| Posture défensive (+2) | `D_026` Le casino gagne toujours | +1,8 pts | -0,7 pts | -2,5 ± 2,1 pts |
| Posture défensive (+2) | `D_025` Chance de cocu | -2,1 pts | -0,0 pts | +2,1 ± 2,1 pts |
| Prime sur le leader (+1) | `A_022` Réseau fongique | +4,4 pts | -1,7 pts | -6,0 ± 3,9 pts |

## Anomalies

Aucune erreur du moteur.
