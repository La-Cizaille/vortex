# Comparaison de variantes

> Généré par `Vortex.Simulator`. Commande : `compare --players 2,3,4,5 --games 2000 --seed 1 --bot normal --variant docs/balance/variants/rotation-horaire.json --variant docs/balance/variants/rotation-antihoraire.json --variant docs/balance/variants/election-3-technologies.json`.
> Contenu analysé : empreinte `c6b1bf14dbe1…` (SHA-256 des quatre fichiers de contenu, variante appliquée). Même commande et même contenu ⇒ mêmes chiffres.

Chaque variante est simulée avec **les mêmes graines** que la référence (bots **normal**). Entre parenthèses : écart avec la référence, ± sa marge d'erreur à 95 %. Un `*` signale un écart au-delà du bruit statistique.

## Variantes

- **Référence** (empreinte `c6b1bf14dbe1…`) : Contenu actuel du dépôt.
- **Rotation horaire** (empreinte `c013eba7aacd…`) : Étape 2.1 : le premier joueur de chaque manche tourne dans le sens horaire (RULES A4.4). Le tour de jeu reste horaire.
- **Rotation anti-horaire** (empreinte `3d8f84e9b266…`) : Étape 2.1 : le premier joueur de chaque manche tourne dans le sens anti-horaire, donc le dernier joueur d'une manche ouvre la suivante (RULES A4.4). Le tour de jeu reste horaire.
- **Élection à 3 technologies** (empreinte `16d8204283c5…`) : Étape 2.5 : l'Élection galactique demande 3 technologies différentes au lieu de 4 (RULES A9).

## 2 joueurs

| Indicateur | Référence | Rotation horaire | Rotation anti-horaire | Élection à 3 technologies |
|---|---|---|---|---|
| Écart max de position (pts) | 21,0 | 1,4 (-19,5) | 1,4 (-19,5) | 21,2 (+0,2) |
| Victoires en position 1 | 71,0 % | 48,6 % (-22,4 ± 3,0 pts *) | 48,6 % (-22,4 ± 3,0 pts *) | 71,2 % (+0,2 ± 2,8 pts) |
| Victoires en position 2 | 29,1 % | 51,4 % (+22,4 ± 3,0 pts *) | 51,4 % (+22,4 ± 3,0 pts *) | 28,9 % (-0,2 ± 2,8 pts) |
| Manches (moyenne) | 8,9 | 8,5 (-0,5 ± 0,2 *) | 8,5 (-0,5 ± 0,2 *) | 8,9 (-0,1 ± 0,2) |
| Fin des temps atteinte | 39,0 % | 33,4 % (-5,6 ± 3,0 pts *) | 33,4 % (-5,6 ± 3,0 pts *) | 38,0 % (-1,0 ± 3,0 pts) |
| Victoires par Élection | 0,0 % | 0,0 % (+0,0 ± 0,0 pts) | 0,0 % (+0,0 ± 0,0 pts) | 2,4 % (+2,4 ± 0,7 pts *) |
| Égalités | 0,0 % | 0,2 % (+0,2 ± 0,2 pts) | 0,2 % (+0,2 ± 0,2 pts) | 0,0 % (+0,0 ± 0,0 pts) |
| Première élimination (manche) | 8,9 | 8,5 (-0,5 ± 0,2 *) | 8,5 (-0,5 ± 0,2 *) | 8,9 (-0,1 ± 0,2) |
| Le meneur à mi-partie gagne | 69,2 % | 68,2 % (-1,0 ± 2,9 pts) | 68,2 % (-1,0 ± 2,9 pts) | 69,0 % (-0,2 ± 2,9 pts) |
| Attaques sur le meneur | — | — | — | — |
| Attaques sans dégâts | 21,1 % | 16,7 % (-4,4 ± 0,8 pts *) | 16,7 % (-4,4 ± 0,8 pts *) | 21,1 % (+0,0 ± 0,8 pts) |
| PV retirés par attaque | 3,8 | 4,8 (+1,0) | 4,8 (+1,0) | 3,8 (+0,0) |
| Attaques par tour | 0,6 | 0,6 (-0,1) | 0,6 (-0,1) | 0,6 (-0,0) |

## 3 joueurs

| Indicateur | Référence | Rotation horaire | Rotation anti-horaire | Élection à 3 technologies |
|---|---|---|---|---|
| Écart max de position (pts) | 14,9 | 3,5 (-11,4) | 3,7 (-11,2) | 14,9 (+0,0) |
| Victoires en position 1 | 48,3 % | 36,9 % (-11,4 ± 3,0 pts *) | 37,0 % (-11,2 ± 3,0 pts *) | 48,3 % (+0,0 ± 3,1 pts) |
| Victoires en position 2 | 29,7 % | 33,1 % (+3,4 ± 2,9 pts *) | 32,4 % (+2,7 ± 2,9 pts) | 29,6 % (-0,1 ± 2,8 pts) |
| Victoires en position 3 | 22,1 % | 30,1 % (+8,0 ± 2,7 pts *) | 30,6 % (+8,5 ± 2,7 pts *) | 22,2 % (+0,1 ± 2,6 pts) |
| Manches (moyenne) | 10,7 | 10,3 (-0,4 ± 0,2 *) | 10,3 (-0,4 ± 0,2 *) | 10,6 (-0,1 ± 0,2) |
| Fin des temps atteinte | 65,1 % | 62,1 % (-3,0 ± 3,0 pts *) | 61,7 % (-3,5 ± 3,0 pts *) | 64,2 % (-0,9 ± 3,0 pts) |
| Victoires par Élection | 0,2 % | 0,1 % (-0,1 ± 0,2 pts) | 0,1 % (-0,2 ± 0,2 pts) | 2,6 % (+2,4 ± 0,7 pts *) |
| Égalités | 0,3 % | 0,0 % (-0,3 ± 0,2 pts *) | 0,1 % (-0,2 ± 0,3 pts) | 0,3 % (+0,0 ± 0,3 pts) |
| Première élimination (manche) | 6,1 | 6,1 (-0,1 ± 0,1) | 5,9 (-0,2 ± 0,1 *) | 6,1 (-0,0 ± 0,1) |
| Le meneur à mi-partie gagne | 65,2 % | 66,3 % (+1,1 ± 3,0 pts) | 68,5 % (+3,2 ± 3,0 pts *) | 65,1 % (-0,2 ± 3,0 pts) |
| Attaques sur le meneur | 52,1 % | 52,0 % (-0,1 ± 0,9 pts) | 52,3 % (+0,2 ± 0,9 pts) | 52,1 % (+0,0 ± 0,9 pts) |
| Attaques sans dégâts | 21,1 % | 20,7 % (-0,4 ± 0,6 pts) | 18,5 % (-2,6 ± 0,6 pts *) | 21,1 % (+0,0 ± 0,6 pts) |
| PV retirés par attaque | 3,7 | 3,8 (+0,2) | 4,2 (+0,6) | 3,7 (-0,0) |
| Attaques par tour | 0,7 | 0,7 (-0,0) | 0,6 (-0,1) | 0,7 (-0,0) |

## 4 joueurs

| Indicateur | Référence | Rotation horaire | Rotation anti-horaire | Élection à 3 technologies |
|---|---|---|---|---|
| Écart max de position (pts) | 11,4 | 1,7 (-9,6) | 1,4 (-10,0) | 11,4 (+0,0) |
| Victoires en position 1 | 36,4 % | 26,7 % (-9,6 ± 2,9 pts *) | 26,4 % (-10,0 ± 2,9 pts *) | 36,4 % (+0,0 ± 3,0 pts) |
| Victoires en position 2 | 24,9 % | 23,7 % (-1,3 ± 2,7 pts) | 24,3 % (-0,7 ± 2,7 pts) | 24,8 % (-0,2 ± 2,7 pts) |
| Victoires en position 3 | 20,2 % | 24,8 % (+4,6 ± 2,6 pts *) | 24,5 % (+4,3 ± 2,6 pts *) | 20,2 % (+0,1 ± 2,5 pts) |
| Victoires en position 4 | 18,5 % | 24,8 % (+6,3 ± 2,5 pts *) | 24,8 % (+6,4 ± 2,5 pts *) | 18,6 % (+0,1 ± 2,4 pts) |
| Manches (moyenne) | 11,9 | 11,8 (-0,1 ± 0,2) | 11,6 (-0,3 ± 0,2 *) | 11,8 (-0,1 ± 0,2) |
| Fin des temps atteinte | 81,9 % | 80,8 % (-1,1 ± 2,4 pts) | 79,2 % (-2,8 ± 2,5 pts *) | 80,6 % (-1,3 ± 2,4 pts) |
| Victoires par Élection | 0,1 % | 0,0 % (-0,1 ± 0,1 pts) | 0,1 % (-0,0 ± 0,2 pts) | 3,6 % (+3,5 ± 0,8 pts *) |
| Égalités | 0,2 % | 0,3 % (+0,1 ± 0,3 pts) | 0,2 % (-0,1 ± 0,3 pts) | 0,2 % (+0,0 ± 0,3 pts) |
| Première élimination (manche) | 5,0 | 4,9 (-0,1 ± 0,1) | 4,8 (-0,2 ± 0,1 *) | 5,0 (-0,0 ± 0,1) |
| Le meneur à mi-partie gagne | 59,5 % | 59,5 % (-0,0 ± 3,2 pts) | 59,8 % (+0,3 ± 3,2 pts) | 58,9 % (-0,6 ± 3,2 pts) |
| Attaques sur le meneur | 41,1 % | 41,4 % (+0,2 ± 0,7 pts) | 41,7 % (+0,6 ± 0,7 pts) | 41,2 % (+0,0 ± 0,7 pts) |
| Attaques sans dégâts | 21,9 % | 21,2 % (-0,7 ± 0,5 pts *) | 19,8 % (-2,1 ± 0,5 pts *) | 21,9 % (+0,0 ± 0,5 pts) |
| PV retirés par attaque | 3,6 | 3,7 (+0,1) | 4,0 (+0,4) | 3,6 (-0,0) |
| Attaques par tour | 0,7 | 0,7 (-0,0) | 0,7 (-0,0) | 0,7 (-0,0) |

## 5 joueurs

| Indicateur | Référence | Rotation horaire | Rotation anti-horaire | Élection à 3 technologies |
|---|---|---|---|---|
| Écart max de position (pts) | 9,8 | 2,8 (-7,0) | 2,3 (-7,5) | 9,7 (-0,1) |
| Victoires en position 1 | 29,8 % | 22,4 % (-7,4 ± 2,7 pts *) | 20,6 % (-9,3 ± 2,7 pts *) | 29,7 % (-0,1 ± 2,8 pts) |
| Victoires en position 2 | 22,9 % | 20,2 % (-2,7 ± 2,6 pts *) | 22,3 % (-0,6 ± 2,6 pts) | 22,9 % (-0,0 ± 2,6 pts) |
| Victoires en position 3 | 17,7 % | 19,9 % (+2,2 ± 2,4 pts) | 19,4 % (+1,6 ± 2,4 pts) | 17,5 % (-0,2 ± 2,4 pts) |
| Victoires en position 4 | 16,5 % | 20,2 % (+3,6 ± 2,4 pts *) | 19,0 % (+2,5 ± 2,4 pts *) | 16,8 % (+0,2 ± 2,3 pts) |
| Victoires en position 5 | 13,0 % | 17,2 % (+4,2 ± 2,2 pts *) | 18,7 % (+5,7 ± 2,3 pts *) | 13,1 % (+0,1 ± 2,1 pts) |
| Manches (moyenne) | 12,7 | 12,7 (-0,0 ± 0,2) | 12,8 (+0,1 ± 0,2) | 12,6 (-0,2 ± 0,2) |
| Fin des temps atteinte | 89,8 % | 90,3 % (+0,4 ± 1,9 pts) | 91,4 % (+1,6 ± 1,8 pts) | 88,3 % (-1,5 ± 1,9 pts) |
| Victoires par Élection | 0,2 % | 0,2 % (-0,0 ± 0,3 pts) | 0,2 % (+0,0 ± 0,3 pts) | 5,0 % (+4,8 ± 1,0 pts *) |
| Égalités | 0,3 % | 0,4 % (+0,2 ± 0,4 pts) | 0,4 % (+0,1 ± 0,3 pts) | 0,2 % (-0,1 ± 0,3 pts) |
| Première élimination (manche) | 4,3 | 4,3 (+0,0 ± 0,1) | 4,2 (-0,1 ± 0,1 *) | 4,3 (-0,0 ± 0,1) |
| Le meneur à mi-partie gagne | 55,6 % | 55,5 % (-0,0 ± 3,2 pts) | 57,9 % (+2,3 ± 3,2 pts) | 55,0 % (-0,6 ± 3,2 pts) |
| Attaques sur le meneur | 35,8 % | 36,0 % (+0,3 ± 0,6 pts) | 36,3 % (+0,6 ± 0,6 pts *) | 35,8 % (+0,0 ± 0,6 pts) |
| Attaques sans dégâts | 21,6 % | 21,6 % (+0,0 ± 0,4 pts) | 20,8 % (-0,8 ± 0,4 pts *) | 21,7 % (+0,0 ± 0,4 pts) |
| PV retirés par attaque | 3,5 | 3,6 (+0,0) | 3,8 (+0,3) | 3,5 (-0,0) |
| Attaques par tour | 0,8 | 0,8 (-0,0) | 0,7 (-0,0) | 0,8 (+0,0) |

## Cartes dont l'écart change nettement

Toutes tables confondues. Écart = taux de victoire des preneurs moins la moyenne des cartes. Seules les cartes dont l'écart bouge au-delà du bruit sont listées.
Attention : 162 couples (carte, variante) sont testés au seuil de 95 %, donc environ **8,1** lignes peuvent apparaître par pur hasard. Ne retenir que les changements nets et confirmés par une autre graine.

| Variante | Carte | Écart (référence) | Écart (variante) | Changement |
|---|---|---|---|---|
| Rotation horaire | `A_009` Jamais deux sans trois | -1,7 pts | +3,5 pts | +5,1 ± 3,1 pts |
| Rotation horaire | `D_016` Orgueil | +11,7 pts | +16,8 pts | +5,1 ± 2,9 pts |
| Rotation horaire | `A_006` Grosse Bertha | +1,9 pts | +6,6 pts | +4,7 ± 3,2 pts |
| Rotation horaire | `A_013` On envoie la sauce | +2,0 pts | +6,5 pts | +4,5 ± 3,2 pts |
| Rotation horaire | `D_024` Roulette | +5,6 pts | +10,1 pts | +4,5 ± 2,9 pts |
| Rotation horaire | `A_008` Brocante spatiale | +1,2 pts | +5,2 pts | +4,0 ± 3,1 pts |
| Rotation horaire | `A_015` Racket | +0,6 pts | +4,6 pts | +4,0 ± 3,1 pts |
| Rotation horaire | `A_022` Réseau fongique | +1,5 pts | +5,2 pts | +3,7 ± 3,2 pts |
| Rotation horaire | `A_021` Accident bactériologique | +5,9 pts | +9,4 pts | +3,5 ± 3,2 pts |
| Rotation horaire | `D_006` Mutinerie syndicale | -0,8 pts | -4,0 pts | -3,2 ± 2,4 pts |
| Rotation horaire | `D_012` Vente de pièces détachées | +6,9 pts | +9,9 pts | +3,0 ± 2,9 pts |
| Rotation horaire | `D_003` Ni vu ni connu | +5,5 pts | +8,5 pts | +3,0 ± 2,8 pts |
| Rotation horaire | `D_014` Nothing else matters | +3,6 pts | +6,5 pts | +2,9 ± 2,9 pts |
| Rotation horaire | `A_001` Canon à particules | +6,9 pts | +4,5 pts | -2,4 ± 1,6 pts |
| Rotation horaire | `A_023` Pile ou face | -2,7 pts | -4,9 pts | -2,3 ± 1,7 pts |
| Rotation horaire | `A_005` Rétablir l'ordre | +2,9 pts | +0,8 pts | -2,1 ± 1,5 pts |
| Rotation horaire | `D_025` Chance de cocu | -0,4 pts | -2,4 pts | -2,0 ± 1,7 pts |
| Rotation horaire | `A_026` Carte sous l'coude | -1,7 pts | -3,7 pts | -1,9 ± 1,7 pts |
| Rotation horaire | `D_011` Générateur auxiliaire | +6,0 pts | +4,1 pts | -1,9 ± 1,8 pts |
| Rotation horaire | `D_020` Tout est une question d'équilibre | -5,6 pts | -7,3 pts | -1,7 ± 1,7 pts |
| Rotation anti-horaire | `A_009` Jamais deux sans trois | -1,7 pts | +4,3 pts | +6,0 ± 3,1 pts |
| Rotation anti-horaire | `D_016` Orgueil | +11,7 pts | +16,7 pts | +5,0 ± 2,8 pts |
| Rotation anti-horaire | `A_004` La paix a un prix | +0,3 pts | +4,4 pts | +4,0 ± 3,1 pts |
| Rotation anti-horaire | `A_013` On envoie la sauce | +2,0 pts | +5,9 pts | +3,9 ± 3,1 pts |
| Rotation anti-horaire | `A_006` Grosse Bertha | +1,9 pts | +5,1 pts | +3,1 ± 3,1 pts |
| Rotation anti-horaire | `D_027` La banque | +1,9 pts | -1,2 pts | -3,1 ± 2,4 pts |
| Rotation anti-horaire | `D_019` Mimétisme cellulaire | -1,6 pts | -4,7 pts | -3,1 ± 1,7 pts |
| Rotation anti-horaire | `D_002` Dommage collatéral | +1,9 pts | +4,9 pts | +3,0 ± 2,8 pts |
| Rotation anti-horaire | `D_014` Nothing else matters | +3,6 pts | +6,4 pts | +2,8 ± 2,8 pts |
| Rotation anti-horaire | `A_001` Canon à particules | +6,9 pts | +4,2 pts | -2,6 ± 1,6 pts |
| Rotation anti-horaire | `A_005` Rétablir l'ordre | +2,9 pts | +0,3 pts | -2,6 ± 1,5 pts |
| Rotation anti-horaire | `D_006` Mutinerie syndicale | -0,8 pts | -3,4 pts | -2,6 ± 2,4 pts |

## Anomalies

Aucune erreur du moteur.
