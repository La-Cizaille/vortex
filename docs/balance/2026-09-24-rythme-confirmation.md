# Comparaison de variantes

> Généré par `Vortex.Simulator`. Commande : `compare --players 5 --games 3000 --seed 2 --bot normal --variant docs/balance/variants/rythme-30pv-bouclier4-fdt16.json --variant docs/balance/variants/rythme-30pv-bouclier5-fdt16.json --variant docs/balance/variants/rythme-30pv-bouclier6-fdt16.json --variant docs/balance/variants/rythme-30pv-bouclier7-fdt16.json --variant docs/balance/variants/rythme-25pv-bouclier6-fdt16.json`.
> Contenu analysé : empreinte `4270b7e75302…` (SHA-256 des quatre fichiers de contenu, variante appliquée). Même commande et même contenu ⇒ mêmes chiffres.

Chaque variante est simulée avec **les mêmes graines** que la référence (bots **normal**). Entre parenthèses : écart avec la référence, ± sa marge d'erreur à 95 %. Un `*` signale un écart au-delà du bruit statistique.

## Variantes

- **Référence** (empreinte `4270b7e75302…`) : Contenu actuel du dépôt.
- **Rythme : 30 PV, bouclier 4, Fin des temps 16** (empreinte `7f735d794042…`) : Étape 2.2, changement minimal : seule la Fin des temps passe à la manche 16 à 5 joueurs.
- **Rythme : 30 PV, bouclier 5, Fin des temps 16** (empreinte `96c09a7c3df1…`) : Étape 2.2 : Fin des temps à la manche 16 et bouclier de départ 5 à 5 joueurs.
- **Rythme : 30 PV, bouclier 6, Fin des temps 16** (empreinte `a6f70050891e…`) : Étape 2.2 : comme le candidat 1, avec un bouclier de départ de 6 (changement plus modéré).
- **Rythme : 30 PV, bouclier 7, Fin des temps 16** (empreinte `7ea8f44fa260…`) : Étape 2.2, candidat 1 de la grille affinée : PV inchangés, bouclier de départ 7 et Fin des temps à la manche 16 à 5 joueurs.
- **Rythme : 25 PV, bouclier 6, Fin des temps 16** (empreinte `e53c69957075…`) : Étape 2.2 : parties plus courtes, 25 PV de départ et au maximum.

## 5 joueurs

| Indicateur | Référence | Rythme : 30 PV, bouclier 4, Fin des temps 16 | Rythme : 30 PV, bouclier 5, Fin des temps 16 | Rythme : 30 PV, bouclier 6, Fin des temps 16 | Rythme : 30 PV, bouclier 7, Fin des temps 16 | Rythme : 25 PV, bouclier 6, Fin des temps 16 |
|---|---|---|---|---|---|---|
| Écart max de position (pts) | 2,0 | 1,7 (-0,3) | 2,2 (+0,2) | 1,5 (-0,5) | 1,4 (-0,6) | 1,7 (-0,2) |
| Victoires en position 1 | 21,9 % | 21,7 % (-0,3 ± 2,1 pts) | 22,2 % (+0,3 ± 2,1 pts) | 21,5 % (-0,4 ± 2,1 pts) | 20,2 % (-1,7 ± 2,1 pts) | 20,8 % (-1,1 ± 2,1 pts) |
| Victoires en position 2 | 20,4 % | 20,7 % (+0,4 ± 2,0 pts) | 20,1 % (-0,3 ± 2,0 pts) | 20,8 % (+0,5 ± 2,0 pts) | 21,0 % (+0,6 ± 2,1 pts) | 21,4 % (+1,0 ± 2,1 pts) |
| Victoires en position 3 | 20,5 % | 19,3 % (-1,2 ± 2,0 pts) | 19,8 % (-0,8 ± 2,0 pts) | 19,2 % (-1,3 ± 2,0 pts) | 19,8 % (-0,8 ± 2,0 pts) | 18,3 % (-2,3 ± 2,0 pts *) |
| Victoires en position 4 | 18,0 % | 19,8 % (+1,7 ± 2,0 pts) | 19,3 % (+1,2 ± 2,0 pts) | 19,8 % (+1,8 ± 2,0 pts) | 20,5 % (+2,4 ± 2,0 pts *) | 19,9 % (+1,8 ± 2,0 pts) |
| Victoires en position 5 | 19,2 % | 18,5 % (-0,6 ± 2,0 pts) | 18,7 % (-0,5 ± 2,0 pts) | 18,7 % (-0,5 ± 2,0 pts) | 18,6 % (-0,5 ± 2,0 pts) | 19,6 % (+0,5 ± 2,0 pts) |
| Manches (moyenne) | 12,6 | 12,8 (+0,2 ± 0,1 *) | 13,1 (+0,5 ± 0,1 *) | 13,3 (+0,7 ± 0,1 *) | 13,6 (+1,0 ± 0,1 *) | 11,9 (-0,7 ± 0,1 *) |
| Fin des temps atteinte | 88,5 % | 17,0 % (-71,4 ± 1,8 pts *) | 20,3 % (-68,1 ± 1,8 pts *) | 23,3 % (-65,2 ± 1,9 pts *) | 24,9 % (-63,5 ± 1,9 pts *) | 9,0 % (-79,5 ± 1,5 pts *) |
| Victoires par Élection | 4,4 % | 5,4 % (+1,0 ± 1,1 pts) | 6,9 % (+2,5 ± 1,2 pts *) | 6,8 % (+2,5 ± 1,2 pts *) | 7,7 % (+3,3 ± 1,2 pts *) | 5,1 % (+0,7 ± 1,1 pts) |
| Égalités | 0,3 % | 0,2 % (-0,0 ± 0,3 pts) | 0,3 % (+0,0 ± 0,3 pts) | 0,2 % (-0,1 ± 0,2 pts) | 0,3 % (+0,0 ± 0,3 pts) | 0,4 % (+0,1 ± 0,3 pts) |
| Première élimination (manche) | 4,3 | 4,3 (+0,0 ± 0,1) | 4,6 (+0,3 ± 0,1 *) | 4,8 (+0,5 ± 0,1 *) | 4,9 (+0,6 ± 0,1 *) | 4,1 (-0,2 ± 0,1 *) |
| Le meneur à mi-partie gagne | 55,3 % | 56,9 % (+1,6 ± 2,6 pts) | 53,3 % (-1,9 ± 2,6 pts) | 52,4 % (-2,9 ± 2,6 pts *) | 51,7 % (-3,6 ± 2,6 pts *) | 53,1 % (-2,2 ± 2,7 pts) |
| Attaques sur le meneur | 35,8 % | 36,0 % (+0,2 ± 0,5 pts) | 36,8 % (+1,0 ± 0,4 pts *) | 37,8 % (+2,0 ± 0,5 pts *) | 38,2 % (+2,4 ± 0,5 pts *) | 39,9 % (+4,1 ± 0,5 pts *) |
| Attaques sans dégâts | 21,7 % | 23,7 % (+2,0 ± 0,4 pts *) | 27,4 % (+5,7 ± 0,4 pts *) | 29,7 % (+8,0 ± 0,4 pts *) | 29,9 % (+8,2 ± 0,4 pts *) | 30,2 % (+8,5 ± 0,4 pts *) |
| PV retirés par attaque | 3,5 | 3,4 (-0,1) | 3,3 (-0,3) | 3,2 (-0,3) | 3,3 (-0,2) | 3,1 (-0,4) |
| Attaques par tour | 0,8 | 0,8 (-0,0) | 0,7 (-0,0) | 0,7 (-0,1) | 0,7 (-0,1) | 0,7 (-0,1) |

## Cartes dont l'écart change nettement

Toutes tables confondues. Écart = taux de victoire des preneurs moins la moyenne des cartes. Seules les cartes dont l'écart bouge au-delà du bruit sont listées.
Attention : 270 couples (carte, variante) sont testés au seuil de 95 %, donc environ **13,5** lignes peuvent apparaître par pur hasard. Ne retenir que les changements nets et confirmés par une autre graine.

| Variante | Carte | Écart (référence) | Écart (variante) | Changement |
|---|---|---|---|---|
| Rythme : 30 PV, bouclier 4, Fin des temps 16 | `A_018` Appendice laser | +0,7 pts | +2,7 pts | +2,0 ± 2,0 pts |
| Rythme : 30 PV, bouclier 5, Fin des temps 16 | `A_021` Accident bactériologique | +9,2 pts | +3,2 pts | -5,9 ± 3,9 pts |
| Rythme : 30 PV, bouclier 5, Fin des temps 16 | `D_023` Main sûre | -4,6 pts | -1,5 pts | +3,1 ± 2,2 pts |
| Rythme : 30 PV, bouclier 5, Fin des temps 16 | `D_011` Générateur auxiliaire | +5,7 pts | +2,7 pts | -3,0 ± 2,2 pts |
| Rythme : 30 PV, bouclier 5, Fin des temps 16 | `A_018` Appendice laser | +0,7 pts | +3,2 pts | +2,5 ± 2,0 pts |
| Rythme : 30 PV, bouclier 6, Fin des temps 16 | `D_011` Générateur auxiliaire | +5,7 pts | +0,5 pts | -5,3 ± 2,2 pts |
| Rythme : 30 PV, bouclier 6, Fin des temps 16 | `A_018` Appendice laser | +0,7 pts | +4,5 pts | +3,8 ± 2,0 pts |
| Rythme : 30 PV, bouclier 6, Fin des temps 16 | `D_023` Main sûre | -4,6 pts | -1,2 pts | +3,4 ± 2,2 pts |
| Rythme : 30 PV, bouclier 7, Fin des temps 16 | `D_011` Générateur auxiliaire | +5,7 pts | +0,1 pts | -5,7 ± 2,2 pts |
| Rythme : 30 PV, bouclier 7, Fin des temps 16 | `D_014` Nothing else matters | +1,9 pts | +6,2 pts | +4,3 ± 3,6 pts |
| Rythme : 30 PV, bouclier 7, Fin des temps 16 | `A_013` On envoie la sauce | +1,4 pts | +5,6 pts | +4,2 ± 3,9 pts |
| Rythme : 30 PV, bouclier 7, Fin des temps 16 | `D_023` Main sûre | -4,6 pts | -1,3 pts | +3,3 ± 2,2 pts |
| Rythme : 30 PV, bouclier 7, Fin des temps 16 | `A_018` Appendice laser | +0,7 pts | +3,3 pts | +2,6 ± 2,0 pts |
| Rythme : 30 PV, bouclier 7, Fin des temps 16 | `D_004` Favoritisme | -2,0 pts | +0,6 pts | +2,6 ± 2,2 pts |
| Rythme : 30 PV, bouclier 7, Fin des temps 16 | `D_026` Le casino gagne toujours | +1,7 pts | -0,7 pts | -2,4 ± 2,2 pts |
| Rythme : 30 PV, bouclier 7, Fin des temps 16 | `A_020` Spores corrosifs | -4,1 pts | -1,7 pts | +2,4 ± 2,3 pts |
| Rythme : 25 PV, bouclier 6, Fin des temps 16 | `D_011` Générateur auxiliaire | +5,7 pts | +0,2 pts | -5,6 ± 2,3 pts |
| Rythme : 25 PV, bouclier 6, Fin des temps 16 | `A_012` Vindicte populaire | +0,0 pts | +5,4 pts | +5,4 ± 4,1 pts |
| Rythme : 25 PV, bouclier 6, Fin des temps 16 | `A_018` Appendice laser | +0,7 pts | +3,5 pts | +2,8 ± 2,0 pts |
| Rythme : 25 PV, bouclier 6, Fin des temps 16 | `D_023` Main sûre | -4,6 pts | -2,2 pts | +2,4 ± 2,3 pts |

## Anomalies

Aucune erreur du moteur.
