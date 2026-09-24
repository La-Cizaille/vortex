# ADR-0010 : Bots génériques par simulation (détermination et fin de tour simulée)

- **Statut** : accepté, 2026-09-24.

## Contexte
Le simulateur d'équilibrage (M3) a besoin d'adversaires qui jouent des milliers de parties. Ces bots serviront aussi plus tard d'IA en jeu local, ou de remplaçants pour un joueur inactif en ligne.

Contraintes :
- Les cartes évoluent. Un bot qui « connaît » les cartes devrait être réécrit à chaque changement, et la revue des cartes serait biaisée par ce que le bot a été programmé à croire.
- Un bot ne doit **pas tricher**. Il ne doit voir ni les dés futurs ni l'ordre des paquets, qui sont pourtant dans l'état qu'il reçoit.
- Il doit être assez rapide pour jouer des milliers de parties en quelques minutes.

## Options
1. **Bot à règles écrites à la main** (« prendre les cartes +4 », etc.) : rapide, mais lié aux cartes et biaisé.
2. **Recherche arborescente complète** (MCTS) : fort, mais lent et complexe pour un jeu à 5 joueurs avec du hasard.
3. **Bot glouton par simulation**. Pour chaque coup légal, il :
   - remplace l'information cachée par son propre tirage (« détermination ») ;
   - joue le coup avec le vrai moteur ;
   - termine son tour avec une politique simple (finir le marché, attaquer le plus faible) ;
   - évalue la position obtenue.

## Décision
L'option 3, `HeuristicBot`, plus un `RandomBot` qui sert de référence « sans compétence ».

- **Aucune connaissance des cartes** : le bot découvre l'effet d'une carte en la simulant. Il n'utilise que des métadonnées publiques, comme l'usage d'une carte (durable ou à usage unique).
- **Détermination** : avant de simuler, il remplace le générateur aléatoire par un tirage à lui. Les paquets sont remis dans un ordre canonique puis mélangés avec ce même tirage. Sa supposition ne dépend donc que de ce qui est visible (test `The_heuristic_bot_cannot_see_hidden_information`).
- **Évaluation** : une somme pondérée et réglable (`HeuristicWeights`) de ses PV, de son bouclier, de ses modificateurs, de sa surcharge et de ses technologies, moins les PV des adversaires, plus un bonus par adversaire éliminé.

## Limites connues
- **Décision en attente** : pour reprendre une commande suspendue, le moteur doit la rejouer avec **le même** générateur (ADR-0009). Pendant une décision, le bot voit donc comment se termine **la commande en cours**, mais rien au-delà. Le nouveau tirage de l'information cachée intervient dès qu'elle est terminée. Ce cas a été découvert parce que le moteur a refusé le rejeu divergent, exactement comme prévu.
- **Le bot n'est pas un humain.** Il ne voit qu'un tour à l'avance et ne comprend ni les alliances implicites ni la mémoire des parties. Les résultats du simulateur indiquent des **tendances** à confirmer en partie réelle ; ce ne sont pas des vérités absolues.
- **Le bot aléatoire est une référence faible.** Pour mesurer la part du hasard plus finement, on comparera plus tard deux niveaux de bot heuristique (plus ou moins d'échantillons).

## Conséquences
- (+) Ajouter ou modifier une carte ne demande aucun changement de bot : la revue des cartes reste objective.
- (+) Les bots sont déterministes (graine), donc chaque rapport est reproductible.
- (+) Les mêmes bots pourront jouer dans Unity ou sur le serveur, puisqu'ils n'utilisent que l'API publique du moteur.
- (−) Coût : chaque décision simule chaque coup légal. Environ 8 000 parties prennent à peu près 1 min 40 sur une machine de bureau (en parallèle).
