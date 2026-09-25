# ADR-0019 : Menus dans une scène à part, partie transmise par un objet de lancement

- **Statut** : accepté, 2026-09-25.

## Contexte
M4.6 ajoute les menus décrits dans [`INTERFACE.md`](../INTERFACE.md) §5 (ARB-65) :
- l'accueil ;
- la partie locale : 2 à 5 joueurs, 5 par défaut ; pour chaque siège, humain ou bot (et le niveau du bot), et un nom ;
- le menu de développement : options de règles et graine, absent des builds publiés ;
- les options : vitesse des animations ; plein écran et résolution sous Windows ;
- la pause et l'écran de fin de partie.

Il ajoute aussi le point de vue en hot-seat (§4) : avec plusieurs humains, la vue pivote au début du tour de chacun.

Il faut décider :
- où vivent les menus ;
- comment la partie choisie arrive à la table ;
- comment le menu de développement disparaît d'un build publié ;
- où sont gardées les options.

## Options
**Emplacement des menus**
1. **Dans la scène de jeu**, par-dessus la table. Pas de changement de scène, mais la scène de jeu grossit, et la table vide sert de fond d'accueil.
2. **Une scène `Menu` à part**, ouverte en premier. C'est la séparation habituelle dans Unity : chaque scène garde un rôle simple.

**Passage de la partie choisie d'une scène à l'autre**
- a. **Un champ statique.** C'est simple, mais c'est un état global modifiable, que le projet évite, et il survit d'une partie à l'autre dans l'éditeur.
- b. **Un ScriptableObject modifié pendant le jeu.** Dans l'éditeur, les modifications restent dans l'asset après le mode Play.
- c. **Un objet de lancement** gardé d'une scène à l'autre (`DontDestroyOnLoad`), que la scène de jeu retrouve au démarrage.

## Décision
- **Option 2 et option c** : une scène `Menu`, première du build, et un objet `MatchLauncher`.
  - Le menu remplit un `MatchSetup` : les sièges, et en développement la graine et les options de règles (`RuleOptions`).
  - `MatchLauncher.Launch` le garde, puis ouvre la scène `Game`.
  - Au démarrage, `GameDirector` joue la partie du lanceur. S'il n'en trouve pas (scène ouverte directement dans l'éditeur), il joue la partie de test réglée dans l'inspecteur, comme avant.
  - « Quitter la partie » et « Menu » suppriment le lanceur, puis rouvrent le menu.
- **Liste des scènes du build** : `BuildSceneList` la tient à jour, avec `Menu`, puis `Game`, chacune sous son identifiant courant.
- **Menu de développement** :
  - il n'existe que si `Debug.isDebugBuild` est vrai, c'est-à-dire dans l'éditeur et dans les builds de développement ;
  - dans un build publié, le menu de partie locale le détruit, masque son bouton et ne lit jamais ses valeurs ;
  - la directive `DEVELOPMENT_BUILD` n'est plus utilisable : Unity 6.6 la refuse comme obsolète et recommande ce test à l'exécution ;
  - les options de règles passent par `GameConfig.WithRuleOptions` (ADR-0011), et le moteur les valide à sa création.
- **Options de l'appareil** (`UserOptions`) :
  - elles sont gardées avec `PlayerPrefs` ;
  - chaque valeur relue est vérifiée : une vitesse hors de la liste proposée, ou une résolution hors bornes, revient à la valeur par défaut ;
  - le plein écran et la résolution ne s'appliquent que dans le lecteur Windows.
- **Pause** : le jeu attend. Aucun événement ne se joue et aucun bot ne joue. Un voile couvre la table sous le menu et ses options.
- **Fin de partie** : une fenêtre reprend le résultat du bandeau, avec « Rejouer » (mêmes sièges, nouvelle graine sauf graine fixée en développement) et « Menu ».
- **Point de vue** : avec au moins deux humains, la vue pivote vers l'humain dont le tour commence, avec le bandeau « Tour de X ». Une décision demandée à un autre humain ne la fait pas pivoter, et le tour d'un bot non plus.

## Conséquences
- (+) Aucun état global : la partie choisie voyage dans un objet visible dans la hiérarchie, qui disparaît avec la partie.
- (+) La scène de jeu reste jouable seule dans l'éditeur, ce qui est utile pour les tests et les captures.
- (+) Un build publié n'expose ni options de règles ni graine.
- (−) Deux scènes à régénérer quand leur disposition change. Elles appartiennent au designer une fois créées : une modification faite à la main est perdue si on les régénère.
- (−) Le menu de développement est exclu à l'exécution, pas à la compilation : son code est présent dans un build publié, mais inerte et détruit au démarrage du menu.
