# Grille : Rythme à 5 joueurs, affiné

> Généré par `Vortex.Simulator`. Commande : `grid --players 5 --games 1000 --seed 1 --bot normal --grid docs/balance/grids/rythme-5-joueurs-affine.json`.
> Contenu analysé : empreinte `4270b7e75302…` (SHA-256 des quatre fichiers de contenu, variante appliquée). Même commande et même contenu ⇒ mêmes chiffres.

Étape 2.2, second passage : la première grille place ses meilleures combinaisons au bord (bouclier de départ 6, Fin des temps à la manche 14). Celle-ci prolonge ces deux axes et garde les deux meilleures valeurs de PV. Mêmes cibles.

- **12 combinaisons**, 1000 parties chacune à 5 joueurs, bots **normal**. Toutes les combinaisons jouent les mêmes graines.
- **(réf.)** marque la combinaison identique au contenu du dépôt.
- Les valeurs **en gras** sont dans la cible. **Écart** : somme des écarts relatifs aux bornes manquées (0 = toutes les cibles atteintes). Le classement met d'abord le plus de cibles atteintes, puis le plus petit écart.
- Précision : avec 1000 parties, un pourcentage est connu à environ ±3,1 pts. Cette grille sert à **trier** : les meilleures combinaisons se confirment ensuite avec `compare` sur plus de parties.

## Axes et cibles

- **PV** : 25, 30
- **Bouclier de départ** : 6, 7
- **Fin des temps** : 14, 16, 18

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
| 1 | 30 | 7 | 16 | 4/5 | 0,2 | **23,5** | **23,3 %** | **8,6 %** | 4,8 | **1,7** | 13,4 | 3,3 |
| 2 | 30 | 7 | 18 | 4/5 | 0,2 | **23,5** | **10,3 %** | **9,1 %** | 4,8 | **1,5** | 13,5 | 3,3 |
| 3 | 30 | 6 | 18 | 4/5 | 0,2 | **23,4** | **10,2 %** | **7,3 %** | 4,8 | **2,6** | 13,5 | 3,2 |
| 4 | 25 | 6 | 14 | 4/5 | 0,3 | **20,4** | **27,0 %** | **5,2 %** | 4,1 | **1,3** | 11,9 | 3,1 |
| 5 | 25 | 6 | 16 | 4/5 | 0,3 | **20,4** | **9,1 %** | **5,5 %** | 4,1 | **1,3** | 11,9 | 3,1 |
| 6 | 25 | 6 | 18 | 4/5 | 0,3 | **20,4** | **3,3 %** | **5,6 %** | 4,1 | **1,0** | 11,9 | 3,1 |
| 7 | 25 | 7 | 16 | 4/5 | 0,3 | **20,6** | **12,2 %** | **7,0 %** | 4,0 | **1,2** | 12,1 | 3,2 |
| 8 | 25 | 7 | 18 | 4/5 | 0,3 | **20,6** | **3,9 %** | **7,2 %** | 4,0 | **0,7** | 12,1 | 3,2 |
| 9 | 30 | 6 | 16 | 3/5 | 0,3 | **23,4** | **24,2 %** | **7,1 %** | 4,8 | 3,4 | 13,4 | 3,2 |
| 10 | 25 | 7 | 14 | 3/5 | 0,3 | **20,5** | 30,2 % | **6,6 %** | 4,0 | **1,4** | 12,0 | 3,3 |
| 11 | 30 | 6 | 14 | 3/5 | 0,8 | **23,3** | 47,2 % | **6,2 %** | 4,8 | **3,0** | 13,3 | 3,3 |
| 12 | 30 | 7 | 14 | 3/5 | 0,8 | **23,4** | 48,5 % | **7,3 %** | 4,8 | **1,9** | 13,4 | 3,4 |

## Anomalies

Aucune erreur du moteur.
