# Comparaison de variantes

> Généré par `Vortex.Simulator`. Commande : `compare --players 5 --games 3000 --seed 1 --bot normal --variant docs/balance/variants/posture-defensive.json --variant docs/balance/variants/prime-leader-1.json --variant docs/balance/variants/prime-leader-2.json --variant docs/balance/variants/fantomes.json --variant docs/balance/variants/posture-prime-fantomes.json`.
> Contenu analysé : empreinte `84a87bc60001…` (SHA-256 des quatre fichiers de contenu, variante appliquée). Même commande et même contenu ⇒ mêmes chiffres.

Chaque variante est simulée avec **les mêmes graines** que la référence (bots **normal**). Entre parenthèses : écart avec la référence, ± sa marge d'erreur à 95 %. Un `*` signale un écart au-delà du bruit statistique.

## Variantes

- **Référence** (empreinte `84a87bc60001…`) : Contenu actuel du dépôt.
- **Posture défensive (+2)** (empreinte `6c2b6726b319…`) : Étape 2.3 (ARB-44) : nouvelle action d'équipage, +2 au bouclier effectif jusqu'au prochain tour du joueur (RULES A5.4).
- **Prime sur le leader (+1)** (empreinte `73d9886fa8aa…`) : Étape 2.3 (ARB-44) : +1 à la valeur d'attaque contre le seul joueur qui a le plus de PV (RULES A6).
- **Prime sur le leader (+2)** (empreinte `86cb6474712e…`) : Étape 2.3 (ARB-44) : comme la prime +1, en plus fort.
- **Fantômes** (empreinte `cd667ae78638…`) : Étape 2.3 (ARB-44) : un joueur éliminé choisit l'événement de la manche entre deux (RULES A4.1).
- **Posture, prime +1 et fantômes** (empreinte `4153724c67fd…`) : Étape 2.3 : les trois mécaniques ensemble.

## 5 joueurs

| Indicateur | Référence | Posture défensive (+2) | Prime sur le leader (+1) | Prime sur le leader (+2) | Fantômes | Posture, prime +1 et fantômes |
|---|---|---|---|---|---|---|
| Écart max de position (pts) | 2,5 | 1,7 (-0,7) | 1,9 (-0,5) | 2,1 (-0,3) | 2,2 (-0,3) | 2,7 (+0,3) |
| Victoires en position 1 | 22,5 % | 21,5 % (-1,0 ± 2,1 pts) | 20,9 % (-1,5 ± 2,1 pts) | 21,7 % (-0,8 ± 2,1 pts) | 22,2 % (-0,3 ± 2,1 pts) | 20,6 % (-1,9 ± 2,1 pts) |
| Victoires en position 2 | 20,7 % | 21,0 % (+0,3 ± 2,1 pts) | 21,9 % (+1,2 ± 2,1 pts) | 21,0 % (+0,3 ± 2,1 pts) | 20,9 % (+0,2 ± 2,1 pts) | 21,0 % (+0,3 ± 2,1 pts) |
| Victoires en position 3 | 20,2 % | 20,3 % (+0,2 ± 2,0 pts) | 20,6 % (+0,5 ± 2,0 pts) | 19,6 % (-0,5 ± 2,0 pts) | 19,3 % (-0,8 ± 2,0 pts) | 21,0 % (+0,8 ± 2,0 pts) |
| Victoires en position 4 | 18,5 % | 18,3 % (-0,2 ± 2,0 pts) | 18,4 % (-0,0 ± 2,0 pts) | 19,8 % (+1,4 ± 2,0 pts) | 18,7 % (+0,3 ± 2,0 pts) | 20,2 % (+1,7 ± 2,0 pts) |
| Victoires en position 5 | 18,3 % | 18,9 % (+0,7 ± 2,0 pts) | 18,1 % (-0,1 ± 2,0 pts) | 17,9 % (-0,4 ± 1,9 pts) | 18,9 % (+0,6 ± 2,0 pts) | 17,3 % (-1,0 ± 1,9 pts) |
| Manches (moyenne) | 13,1 | 13,3 (+0,2 ± 0,1 *) | 12,8 (-0,3 ± 0,1 *) | 12,3 (-0,8 ± 0,1 *) | 13,2 (+0,1 ± 0,2) | 12,9 (-0,2 ± 0,1 *) |
| Fin des temps atteinte | 20,4 % | 22,3 % (+1,9 ± 2,1 pts) | 15,7 % (-4,7 ± 1,9 pts *) | 10,9 % (-9,5 ± 1,8 pts *) | 23,3 % (+2,9 ± 2,1 pts *) | 19,7 % (-0,7 ± 2,0 pts) |
| Victoires par Élection | 6,4 % | 6,0 % (-0,4 ± 1,2 pts) | 5,4 % (-1,1 ± 1,2 pts) | 4,8 % (-1,6 ± 1,2 pts *) | 6,8 % (+0,3 ± 1,3 pts) | 6,6 % (+0,1 ± 1,3 pts) |
| Égalités | 0,1 % | 0,1 % (+0,0 ± 0,2 pts) | 0,2 % (+0,1 ± 0,2 pts) | 0,3 % (+0,2 ± 0,2 pts) | 0,6 % (+0,5 ± 0,3 pts *) | 0,6 % (+0,5 ± 0,3 pts *) |
| Première élimination (manche) | 4,6 | 4,6 (+0,0 ± 0,1) | 4,7 (+0,1 ± 0,1 *) | 4,7 (+0,1 ± 0,1 *) | 4,6 (-0,0 ± 0,1) | 4,7 (+0,0 ± 0,1) |
| Le meneur à mi-partie gagne | 52,5 % | 53,1 % (+0,6 ± 2,6 pts) | 46,1 % (-6,4 ± 2,7 pts *) | 41,0 % (-11,5 ± 2,7 pts *) | 51,7 % (-0,8 ± 2,6 pts) | 46,7 % (-5,8 ± 2,7 pts *) |
| Attaques sur le meneur | 36,8 % | 36,9 % (+0,1 ± 0,4 pts) | 40,4 % (+3,7 ± 0,4 pts *) | 44,3 % (+7,6 ± 0,4 pts *) | 36,8 % (+0,0 ± 0,4 pts) | 40,6 % (+3,8 ± 0,4 pts *) |
| Attaques sur le plus faible | 50,4 % | 50,3 % (-0,1 ± 0,5 pts) | 48,0 % (-2,4 ± 0,5 pts *) | 45,5 % (-4,9 ± 0,5 pts *) | 50,4 % (-0,0 ± 0,5 pts) | 48,1 % (-2,3 ± 0,5 pts *) |
| Attaques sans dégâts | 27,5 % | 27,3 % (-0,2 ± 0,4 pts) | 27,0 % (-0,6 ± 0,4 pts *) | 26,1 % (-1,5 ± 0,4 pts *) | 27,4 % (-0,1 ± 0,4 pts) | 26,8 % (-0,7 ± 0,4 pts *) |
| Action d'équipage : Attaque | 74,6 % | 73,9 % (-0,7 ± 0,3 pts *) | 76,3 % (+1,7 ± 0,3 pts *) | 78,0 % (+3,4 ± 0,3 pts *) | 74,5 % (-0,1 ± 0,3 pts) | 75,5 % (+0,9 ± 0,3 pts *) |
| Action d'équipage : Reparamétrage | 6,5 % | 6,5 % (+0,0 ± 0,2 pts) | 6,1 % (-0,4 ± 0,2 pts *) | 5,7 % (-0,8 ± 0,2 pts *) | 6,7 % (+0,2 ± 0,2 pts *) | 6,0 % (-0,4 ± 0,2 pts *) |
| Action d'équipage : Sabotage | 2,9 % | 2,6 % (-0,3 ± 0,1 pts *) | 2,7 % (-0,2 ± 0,1 pts *) | 2,6 % (-0,3 ± 0,1 pts *) | 2,9 % (-0,0 ± 0,1 pts) | 2,5 % (-0,4 ± 0,1 pts *) |
| Action d'équipage : Surcharge | 16,0 % | 16,0 % (-0,1 ± 0,3 pts) | 14,9 % (-1,1 ± 0,3 pts *) | 13,7 % (-2,3 ± 0,3 pts *) | 15,9 % (-0,1 ± 0,3 pts) | 15,0 % (-1,1 ± 0,3 pts *) |
| Action d'équipage : Posture défensive | 0,0 % | 1,1 % (+1,1 ± 0,1 pts *) | 0,0 % (+0,0 ± 0,0 pts) | 0,0 % (+0,0 ± 0,0 pts) | 0,0 % (+0,0 ± 0,0 pts) | 1,0 % (+1,0 ± 0,1 pts *) |
| PV retirés par attaque | 3,2 | 3,2 (+0,0) | 3,3 (+0,1) | 3,4 (+0,2) | 3,2 (+0,0) | 3,3 (+0,1) |
| Attaques par tour | 0,7 | 0,7 (-0,0) | 0,8 (+0,0) | 0,8 (+0,0) | 0,7 (-0,0) | 0,7 (+0,0) |

## Cartes dont l'écart change nettement

Toutes tables confondues. Écart = taux de victoire des preneurs moins la moyenne des cartes. Seules les cartes dont l'écart bouge au-delà du bruit sont listées.
Attention : 270 couples (carte, variante) sont testés au seuil de 95 %, donc environ **13,5** lignes peuvent apparaître par pur hasard. Ne retenir que les changements nets et confirmés par une autre graine.

| Variante | Carte | Écart (référence) | Écart (variante) | Changement |
|---|---|---|---|---|
| Posture défensive (+2) | `A_006` Grosse Bertha | -0,3 pts | +4,2 pts | +4,5 ± 3,9 pts |
| Posture défensive (+2) | `D_022` Je te touche pas avec un bâton | -2,1 pts | +2,1 pts | +4,3 ± 2,9 pts |
| Posture défensive (+2) | `A_025` Corruption du croupier | +2,3 pts | -1,6 pts | -4,0 ± 3,9 pts |
| Posture défensive (+2) | `A_003` Epuration | -0,2 pts | -3,0 pts | -2,8 ± 2,4 pts |
| Posture défensive (+2) | `D_007` Sous-couche blindée | +0,8 pts | -1,8 pts | -2,6 ± 2,2 pts |
| Prime sur le leader (+1) | `A_022` Réseau fongique | +3,9 pts | -1,1 pts | -5,0 ± 3,9 pts |
| Prime sur le leader (+2) | `D_022` Je te touche pas avec un bâton | -2,1 pts | +1,5 pts | +3,6 ± 2,9 pts |
| Prime sur le leader (+2) | `D_004` Favoritisme | -3,2 pts | -0,8 pts | +2,4 ± 2,2 pts |
| Fantômes | `D_008` T'as pas entendu un truc? | +5,0 pts | +2,5 pts | -2,5 ± 2,2 pts |
| Posture, prime +1 et fantômes | `A_004` La paix a un prix | +2,7 pts | -2,5 pts | -5,2 ± 3,9 pts |
| Posture, prime +1 et fantômes | `D_014` Nothing else matters | +6,8 pts | +2,3 pts | -4,5 ± 3,6 pts |
| Posture, prime +1 et fantômes | `D_022` Je te touche pas avec un bâton | -2,1 pts | +1,5 pts | +3,7 ± 3,0 pts |
| Posture, prime +1 et fantômes | `D_004` Favoritisme | -3,2 pts | -0,8 pts | +2,5 ± 2,2 pts |

## Anomalies

Aucune erreur du moteur.
