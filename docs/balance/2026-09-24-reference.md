# Rapport d'équilibrage

> Généré par `Vortex.Simulator`. Commande : `dotnet run -c Release --project dotnet/Vortex.Simulator -- --games 1000 --seed 1 --samples 2`.
> Contenu analysé : `cards.json` SHA-256 `15e850e52bda…`. Mêmes paramètres et même contenu ⇒ mêmes chiffres.

## Comment lire ce rapport

- Les parties sont jouées par des **bots**. Le bot *heuristique* essaie chaque coup légal en le simulant (sans voir le futur : il invente les tirages cachés), termine son tour avec une politique simple, puis garde le coup qui donne la meilleure position. Le bot *aléatoire* joue n'importe quel coup légal. Aucun bot ne connaît les cartes : ils les découvrent en les jouant (ADR-0010).
- Les bots ne jouent pas comme des humains. Les tendances et les écarts sont utiles ; les valeurs absolues le sont moins.
- **Écart** d'une carte : taux de victoire des joueurs qui l'ont prise, moins le même taux calculé sur **toutes** les cartes, en points. Comparer à la moyenne des cartes (et non à 1/nombre de joueurs) neutralise le biais de survie : un joueur qui survit longtemps prend plus de cartes et gagne plus souvent. Un `*` signale un écart au-delà du bruit statistique (plus de 2 écarts-types).

## 1. Durée et fin des parties

Bots heuristiques à toutes les places. Durée estimée à 30 s par tour de joueur (hypothèse, à confronter aux parties réelles).

| Joueurs | Parties | Terminées | Égalités | Manches (moy. / méd. / p90) | Tours de joueur | Fin des temps atteinte | Victoires par Élection | Durée estimée |
|---|---|---|---|---|---|---|---|---|
| 2 | 1000 | 100,0 % | 0,0 % | 8,9 / 9 / 13 | 17,1 | 38,0 % | 0,0 % | 8,5 min |
| 3 | 1000 | 100,0 % | 0,3 % | 10,7 / 10 / 14 | 26,3 | 66,7 % | 0,2 % | 13,2 min |
| 4 | 1000 | 100,0 % | 0,2 % | 11,9 / 12 / 16 | 35,1 | 82,1 % | 0,0 % | 17,6 min |
| 5 | 1000 | 100,0 % | 0,4 % | 12,7 / 12 / 17 | 43,1 | 89,8 % | 0,4 % | 21,5 min |

## 2. Avantage de position

Taux de victoire selon l'ordre de jeu (1 = le joueur qui a gagné l'initiative), bots heuristiques. Part équitable = 1/nombre de joueurs.

| Joueurs | Part équitable | Position 1 | Position 2 | Position 3 | Position 4 | Position 5 |
|---|---|---|---|---|---|---|
| 2 | 50,0 % | 70,6 % | 29,4 % | — | — | — |
| 3 | 33,3 % | 46,3 % | 31,7 % | 22,0 % | — | — |
| 4 | 25,0 % | 38,2 % | 24,7 % | 19,5 % | 17,5 % | — |
| 5 | 20,0 % | 29,5 % | 23,5 % | 17,9 % | 16,7 % | 12,4 % |

## 3. Poids des choix (écart de niveau)

Un bot heuristique (place tournante) contre des bots aléatoires. Si le hasard dominait le jeu, il ne gagnerait guère plus que sa part équitable. **Ratio** = victoires ÷ part équitable.

| Joueurs | Parties | Victoires de l'heuristique | Part équitable | Ratio |
|---|---|---|---|---|
| 2 | 1000 | 99,5 % | 50,0 % | 2,0 |
| 3 | 1000 | 94,0 % | 33,3 % | 2,8 |
| 4 | 1000 | 86,0 % | 25,0 % | 3,4 |
| 5 | 1000 | 77,1 % | 20,0 % | 3,9 |

## 4. Combats

| Joueurs | Attaques par tour de joueur | PV retirés par attaque | Attaques sans dégâts |
|---|---|---|---|
| 2 | 0,6 | 3,8 | 21,0 % |
| 3 | 0,7 | 3,6 | 21,0 % |
| 4 | 0,7 | 3,5 | 22,3 % |
| 5 | 0,8 | 3,5 | 21,5 % |

## 5. Cartes

Toutes tailles de table confondues, bots heuristiques. **Prises** = nombre moyen de prises au marché par partie. **Preneurs** = couples (partie, joueur) ayant pris la carte au moins une fois. Trié du plus fort au plus faible écart.

Taux de victoire moyen d'un preneur, toutes cartes confondues : **35,6 %** (référence des écarts).

| Carte | Nom | Prises / partie | Activations / prise | Preneurs | Victoires des preneurs | Écart |
|---|---|---|---|---|---|---|
| `D_016` | Orgueil | 0,3 | 0,8 | 1058 | 47,3 % | +11,7 pts * |
| `D_021` | Quarantaine obligatoire | 0,3 | 0,5 | 1038 | 43,4 % | +7,9 pts * |
| `A_001` | Canon à particules | 1,0 | — | 3574 | 43,1 % | +7,5 pts * |
| `D_012` | Vente de pièces détachées | 0,3 | 0,8 | 1029 | 43,0 % | +7,4 pts * |
| `D_011` | Générateur auxiliaire | 0,8 | 0,7 | 2876 | 41,5 % | +6,0 pts * |
| `A_002` | Langue de bois | 0,2 | 0,5 | 887 | 41,4 % | +5,8 pts * |
| `D_003` | Ni vu ni connu | 0,3 | 0,4 | 1100 | 41,3 % | +5,7 pts * |
| `D_014` | Nothing else matters | 0,3 | 0,3 | 1043 | 41,2 % | +5,7 pts * |
| `D_024` | Roulette | 0,3 | 0,4 | 1020 | 41,1 % | +5,5 pts * |
| `A_021` | Accident bactériologique | 0,2 | 0,7 | 859 | 40,7 % | +5,2 pts * |
| `D_008` | T'as pas entendu un truc? | 0,8 | — | 2934 | 40,4 % | +4,8 pts * |
| `A_027` | Tapis! | 0,2 | 0,6 | 835 | 40,2 % | +4,7 pts * |
| `D_017` | Loi du Talion | 0,4 | — | 1581 | 39,8 % | +4,2 pts * |
| `D_009` | Les affaires sont les affaires | 0,3 | 0,9 | 1024 | 39,2 % | +3,6 pts * |
| `A_012` | Vindicte populaire | 0,2 | 0,6 | 836 | 38,8 % | +3,2 pts |
| `A_005` | Rétablir l'ordre | 1,0 | — | 3873 | 38,5 % | +2,9 pts * |
| `A_004` | La paix a un prix | 0,2 | 0,3 | 857 | 38,2 % | +2,6 pts |
| `A_013` | On envoie la sauce | 0,2 | 0,6 | 868 | 38,0 % | +2,5 pts |
| `A_018` | Appendice laser | 1,0 | — | 3854 | 37,8 % | +2,2 pts * |
| `D_027` | La banque | 0,4 | — | 1561 | 37,3 % | +1,8 pts |
| `A_007` | Délestage forcé | 0,2 | 0,3 | 912 | 37,1 % | +1,5 pts |
| `A_006` | Grosse Bertha | 0,2 | 0,6 | 844 | 36,4 % | +0,8 pts |
| `D_002` | Dommage collatéral | 0,3 | 0,2 | 1064 | 36,3 % | +0,7 pts |
| `A_022` | Réseau fongique | 0,2 | 0,3 | 866 | 36,0 % | +0,5 pts |
| `D_007` | Sous-couche blindée | 0,8 | — | 2920 | 35,9 % | +0,4 pts |
| `D_025` | Chance de cocu | 0,8 | — | 2958 | 35,9 % | +0,3 pts |
| `A_009` | Jamais deux sans trois | 0,2 | 0,6 | 882 | 35,8 % | +0,3 pts |
| `A_015` | Racket | 0,2 | 0,5 | 926 | 35,7 % | +0,2 pts |
| `D_006` | Mutinerie syndicale | 0,4 | — | 1565 | 35,7 % | +0,2 pts |
| `D_022` | Je te touche pas avec un bâton | 0,4 | — | 1602 | 35,6 % | +0,0 pts |
| `A_017` | Dingo de la surcharge | 0,7 | — | 2676 | 35,5 % | -0,1 pts |
| `A_025` | Corruption du croupier | 0,2 | 0,3 | 832 | 35,3 % | -0,2 pts |
| `A_008` | Brocante spatiale | 0,2 | 0,9 | 838 | 34,8 % | -0,7 pts |
| `A_026` | Carte sous l'coude | 0,8 | — | 3095 | 34,6 % | -1,0 pts |
| `D_026` | Le casino gagne toujours | 0,8 | — | 2940 | 34,3 % | -1,2 pts |
| `D_018` | Régénération parasitaire | 0,8 | — | 3086 | 34,3 % | -1,3 pts |
| `D_004` | Favoritisme | 0,8 | — | 2985 | 34,3 % | -1,3 pts |
| `D_005` | Sabotage électoral | 1,0 | — | 3764 | 34,2 % | -1,4 pts |
| `A_011` | Cruauté | 0,9 | — | 3456 | 34,1 % | -1,4 pts |
| `D_019` | Mimétisme cellulaire | 0,8 | — | 2962 | 33,8 % | -1,8 pts * |
| `A_023` | Pile ou face | 0,9 | — | 3293 | 33,6 % | -2,0 pts * |
| `D_023` | Main sûre | 0,8 | — | 2978 | 33,5 % | -2,0 pts * |
| `A_016` | BLITZKRIEG! | 0,7 | — | 2792 | 33,5 % | -2,1 pts * |
| `A_024` | Black Jack | 0,8 | — | 2835 | 33,4 % | -2,2 pts * |
| `A_019` | Agents pathogènes | 0,7 | — | 2821 | 32,9 % | -2,6 pts * |
| `D_013` | Vautours | 0,9 | — | 3471 | 32,7 % | -2,8 pts * |
| `D_001` | Intouchable | 0,8 | — | 2892 | 32,7 % | -2,9 pts * |
| `A_003` | Epuration | 0,7 | — | 2587 | 32,4 % | -3,2 pts * |
| `D_015` | Lève la tête, bombe le torse | 0,8 | — | 2942 | 32,3 % | -3,3 pts * |
| `D_010` | Niaque | 0,8 | — | 3051 | 32,1 % | -3,5 pts * |
| `A_014` | Recels en tous genres | 0,6 | — | 2453 | 32,1 % | -3,5 pts * |
| `A_020` | Spores corrosifs | 0,7 | — | 2566 | 31,5 % | -4,0 pts * |
| `A_010` | Le grand final | 0,6 | — | 2415 | 29,3 % | -6,3 pts * |
| `D_020` | Tout est une question d'équilibre | 0,8 | — | 3032 | 29,0 % | -6,6 pts * |

## 6. Événements

| Événement | Révélations par partie |
|---|---|
| tempête électro-magnetique | 1,5 |
| trou noir | 1,4 |
| Nouvel arrivage | 1,5 |
| **surcharge** Ionique | 1,5 |
| Nuée parasitaire | 1,5 |
| Espace aseptisé | 1,5 |
| Le calme avant la tempête | 1,5 |
| Fin des temps | 0,7 |

## 7. Anomalies

Aucune : toutes les parties se sont terminées sans erreur du moteur.
