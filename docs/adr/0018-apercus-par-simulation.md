# ADR-0018 : Les aperçus sont calculés en jouant le coup sur des parties supposées

- **Statut** : accepté, 2026-09-25. Précise l'ADR-0015, qui demande des aperçus calculés par le moteur.

## Contexte
Pendant le glisser d'une attaque, l'interface doit montrer ce que le coup va probablement faire ([`INTERFACE.md`](../INTERFACE.md) §3.4) :
- les dés lancés ;
- le détail des bonus et malus ;
- le bouclier effectif de la cible ;
- la fourchette de dégâts.

L'ADR-0015 exige que ce calcul vienne du moteur, avec le même code que l'action réelle. Sans cela, l'aperçu mentirait dès qu'une carte modifie une attaque (avantage, bonus, bouclier désactivé, dégâts plafonnés, déviation…).

Deux contraintes :
- **ne rien révéler** : un aperçu calculé avec le vrai générateur de la partie donnerait le prochain dé, et avec le vrai ordre des paquets la prochaine carte ;
- **rester indépendant des cartes** (ADR-0007) : le moteur ne doit pas connaître « Pile ou face » ou « Tapis! » pour les décrire.

## Options
1. **Un calcul d'aperçu à part**, étape par étape (dés, bonus, bouclier), en demandant aux effets de décrire leur contribution. Il faudrait un second chemin pour chaque point d'interception, et donc le maintenir en double. Les effets qui dépendent du jet (parité, dé lancé à l'avance, critique) seraient difficiles à décrire sans les jouer.
2. **Jouer la vraie commande avec le vrai état**, sans garder le résultat. C'est exact, mais cela révèle le prochain dé : c'est exclu.
3. **Jouer la vraie commande sur des copies « supposées » de la partie.** L'information cachée (ordre des paquets, générateur) est remplacée par un tirage indépendant, comme le font déjà les bots (ADR-0010). Le coup est joué un grand nombre de fois, et l'aperçu résume les résultats.

## Décision
L'option 3.

- **`HiddenInformation.Guess(state, guesses)`** remplace l'information cachée par une supposition tirée d'un autre générateur :
  - un nouveau générateur ;
  - des paquets remis dans un ordre canonique, puis mélangés.

  Les bots l'utilisent aussi : le code qui cache l'information est unique. La suite des tirages est identique à l'ancienne méthode privée des bots, donc les parties simulées ne changent pas.
- **`GameEngine.Preview(state, player, command, guesses, samples)`** :
  - vérifie que la commande est légale ; sinon, ou pendant une décision en attente, il renvoie `null` ;
  - joue la commande sur `samples` parties supposées (400 par défaut) ;
  - répond au hasard aux décisions rencontrées ;
  - ne modifie jamais l'état reçu.
- **Résultat** (`CommandPreview`), résumé par un minimum, un maximum et une moyenne (`Estimate`) :
  - pour chaque siège : PV, bouclier, chance d'être éliminé ;
  - pour l'attaque lancée, le cas échéant (`AttackPreview`) :
    - les dés (nombre par lancer, nombre gardé, avantage ou désavantage) ;
    - le bouclier effectif ;
    - les dégâts ;
    - les chances de toucher, de critique et de déviation ;
    - **le détail des bonus par source**. Le moteur note, pendant le calcul de la valeur d'attaque, ce que chaque effet ajoute (carte, statut, événement, technologie, ou règle de la partie). Une source qui dépend du jet apparaît comme une fourchette (par exemple « −1 à +3 »).
- **Côté client** :
  - `IGameSession.Preview` : la session locale garde, pour les aperçus, un générateur à elle, distinct de celui de la partie ;
  - `PreviewText` met les chiffres en mots, sans rien recalculer ; les noms viennent du contenu (titre de la carte, de l'événement, de la technologie) ou des textes de l'interface (statuts) ;
  - `PlayerControls` affiche l'aperçu dans la bulle d'aide, à côté de la cible survolée pendant le glisser. Il le calcule une fois par cible et par offre de coups.

## Conséquences
- (+) L'aperçu est exact au sens où il joue les vraies règles : toute carte, présente ou future, y est prise en compte sans code spécifique.
- (+) Il ne révèle rien de caché : il ne dépend que de l'information publique et du générateur des suppositions (tests `The_preview_depends_only_on_public_information` et `A_guess_keeps_what_is_visible_and_hides_what_is_not`).
- (+) Il est générique : il sert aussi au sabotage (bouclier de la cible après la relance), et il servira aux cartes à utiliser.
- (−) Ce sont des **estimations** : avec 400 parties, une chance est connue à environ 2,5 points près. Les pourcentages sont donc arrondis à 5 % et jamais présentés comme certains (« < 5 % », « > 95 % »). Le maximum observé peut manquer une issue très rare.
- (−) Les choix des autres joueurs (déviation, carte détruite au critique) sont joués au hasard. La chance de déviation n'est donc pas une prédiction du comportement de la cible : l'interface dit seulement que la cible **peut** dévier l'attaque.
- (−) Coût : environ 400 exécutions de la commande par aperçu, soit quelques millisecondes sur PC. Le client ne calcule un aperçu qu'au survol d'une cible et le garde ensuite. En phase 2, c'est le serveur qui le calcule : il faudra en limiter le débit (docs/SECURITY.md).
- (−) Le texte affiché diffère légèrement d'INTERFACE §3.4 : un bonus qui dépend du jet est affiché comme une fourchette, et non par sa condition (« +3 si la somme est paire »). La condition exacte reste lisible sur la carte, que le joueur peut agrandir.
