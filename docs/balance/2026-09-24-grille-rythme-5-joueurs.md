# Grille : Rythme à 5 joueurs

> Généré par `Vortex.Simulator`. Commande : `grid --players 5 --games 1000 --seed 1 --bot normal --grid docs/balance/grids/rythme-5-joueurs.json`.
> Contenu analysé : empreinte `4270b7e75302…` (SHA-256 des quatre fichiers de contenu, variante appliquée). Même commande et même contenu ⇒ mêmes chiffres.

Étape 2.2 (ARB-43) : PV de départ et maximum, bouclier de départ et manche de Fin des temps, sur le mode standard à 5 joueurs (ARB-52). Cibles d'ARB-40 et ARB-53 ; la cible de première élimination, fixée pour 4 joueurs, est transposée à 5 en attendant la confirmation du game designer.

- **27 combinaisons**, 1000 parties chacune à 5 joueurs, bots **normal**. Toutes les combinaisons jouent les mêmes graines.
- **(réf.)** marque la combinaison identique au contenu du dépôt.
- Les valeurs **en gras** sont dans la cible. **Écart** : somme des écarts relatifs aux bornes manquées (0 = toutes les cibles atteintes). Le classement met d'abord le plus de cibles atteintes, puis le plus petit écart.
- Précision : avec 1000 parties, un pourcentage est connu à environ ±3,1 pts. Cette grille sert à **trier** : les meilleures combinaisons se confirment ensuite avec `compare` sur plus de parties.

## Axes et cibles

- **PV** : 25, 30, 35
- **Bouclier de départ** : 4, 5, 6
- **Fin des temps** : 10, 12, 14

| Indicateur | Cible |
|---|---|
| Durée (min) | 20,0 à 25,0 |
| Fin des temps atteinte | au plus 30,0 % |
| Victoires par Élection | 5,0 % à 15,0 % |
| Première élimination (manche) | au moins 6,0 |
| Écart de position (pts) | au plus 3,0 |

## Classement

| Rang | PV | Bouclier de départ | Fin des temps | Cibles atteintes | Écart | Durée (min) | Fin des temps atteinte | Victoires par Élection | Première élimination (manche) | Écart de position (pts) | Manches | PV retirés par attaque |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 25 | 6 | 14 | 4/5 | 0,3 | **20,4** | **27,0 %** | **5,2 %** | 4,1 | **1,3** | 11,9 | 3,1 |
| 2 | 30 | 6 | 14 | 3/5 | 0,8 | **23,3** | 47,2 % | **6,2 %** | 4,8 | **3,0** | 13,3 | 3,3 |
| 3 | 35 | 4 | 14 | 3/5 | 1,0 | **24,4** | 55,4 % | **8,1 %** | 5,0 | **1,3** | 13,9 | 3,5 |
| 4 | 30 | 5 | 12 | 3/5 | 1,5 | **22,3** | 68,6 % | **6,1 %** | 4,7 | **2,4** | 12,8 | 3,3 |
| 5 | 30 | 6 | 12 | 3/5 | 1,6 | **23,0** | 72,4 % | **5,3 %** | 4,8 | **0,9** | 13,1 | 3,3 |
| 6 | 35 | 4 | 12 | 3/5 | 1,8 | **24,2** | 79,3 % | **7,2 %** | 5,0 | **1,4** | 13,9 | 3,6 |
| 7 | 30 | 5 | 10 | 3/5 | 2,2 | **22,2** | 89,0 % | **5,5 %** | 4,7 | **1,1** | 12,8 | 3,4 |
| 8 | 35 | 5 | 10 | 3/5 | 2,2 | **24,9** | 93,9 % | **7,2 %** | 5,3 | **2,4** | 14,2 | 3,5 |
| 9 | 35 | 4 | 10 | 3/5 | 2,3 | **24,1** | 94,1 % | **6,0 %** | 5,0 | **1,6** | 13,9 | 3,7 |
| 10 | 25 | 5 | 14 | 2/5 | 0,5 | 19,6 | **22,8 %** | 4,1 % | 3,9 | **2,3** | 11,6 | 3,1 |
| 11 | 30 | 4 | 14 | 2/5 | 0,6 | **21,8** | 37,9 % | 4,7 % | 4,3 | **2,4** | 12,7 | 3,4 |
| 12 | 30 | 5 | 14 | 2/5 | 0,6 | **22,5** | 41,1 % | **5,7 %** | 4,7 | 3,0 | 12,9 | 3,2 |
| 13 | 25 | 4 | 14 | 2/5 | 0,9 | 18,7 | **19,6 %** | 3,0 % | 3,6 | **1,6** | 11,3 | 3,3 |
| 14 | 35 | 5 | 14 | 2/5 | 1,2 | 25,3 | 61,4 % | **9,0 %** | 5,3 | **1,2** | 14,4 | 3,4 |
| 15 | 25 | 6 | 12 | 2/5 | 1,3 | **20,2** | 54,5 % | 4,2 % | 4,1 | **1,8** | 11,8 | 3,2 |
| 16 | 35 | 6 | 14 | 2/5 | 1,3 | 26,3 | 66,1 % | **9,3 %** | 5,6 | **1,2** | 14,8 | 3,4 |
| 17 | 30 | 4 | 12 | 2/5 | 1,6 | **21,6** | 64,8 % | 4,3 % | 4,3 | **3,0** | 12,6 | 3,5 |
| 18 | 35 | 5 | 12 | 2/5 | 1,9 | 25,2 | 82,9 % | **7,8 %** | 5,3 | **1,3** | 14,3 | 3,4 |
| 19 | 35 | 6 | 12 | 2/5 | 2,0 | 26,0 | 86,5 % | **9,1 %** | 5,6 | **2,5** | 14,6 | 3,4 |
| 20 | 30 | 6 | 10 | 2/5 | 2,3 | **22,8** | 90,6 % | 4,7 % | 4,8 | **2,8** | 13,0 | 3,4 |
| 21 | 25 | 5 | 12 | 1/5 | 1,4 | 19,5 | 51,1 % | 3,5 % | 3,9 | **1,7** | 11,5 | 3,2 |
| 22 | 25 | 4 | 12 | 1/5 | 1,4 | 18,6 | 43,7 % | 2,5 % | 3,6 | **2,4** | 11,1 | 3,3 |
| 23 | 35 | 6 | 10 | 1/5 | 2,4 | 25,8 | 95,5 % | **8,0 %** | 5,6 | 3,5 | 14,5 | 3,5 |
| 24 | 25 | 6 | 10 | 1/5 | 2,5 | 19,9 | 82,4 % | 2,6 % | 4,1 | **1,7** | 11,6 | 3,2 |
| 25 (réf.) | 30 | 4 | 10 | 1/5 | 2,7 | **21,4** | 87,8 % | 3,8 % | 4,3 | 3,9 | 12,5 | 3,5 |
| 26 | 25 | 5 | 10 | 0/5 | 2,5 | 19,4 | 79,7 % | 3,7 % | 3,9 | 3,7 | 11,5 | 3,2 |
| 27 | 25 | 4 | 10 | 0/5 | 2,8 | 18,4 | 75,4 % | 1,7 % | 3,6 | 3,3 | 11,0 | 3,4 |

## Anomalies

Aucune erreur du moteur.
