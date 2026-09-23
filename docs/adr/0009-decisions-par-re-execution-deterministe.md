# ADR-0009 : Décisions par re-exécution déterministe

- **Statut** : accepté, 2026-09-23. **Remplace [ADR-0005](0005-file-de-resolution-et-decisions.md).**

## Contexte
L'ADR-0005 prévoyait une file de résolution explicite : chaque étape de la résolution serait un objet de données capable de se suspendre sur une décision puis de reprendre. Au moment d'implémenter le moteur (M1), le coût de cette approche est apparu clairement :
- chaque étape du pipeline d'attaque (RULES A6), chaque réaction et chaque effet de carte devraient être découpés en objets sérialisables ;
- le code deviendrait une machine à états difficile à lire, alors que les règles se lisent naturellement comme une procédure.

Les exigences, elles, restent les mêmes :
- un état sérialisable à tout instant, y compris pendant une décision ;
- une décision adressée à un joueur précis, avec un identifiant ;
- aucune triche possible ;
- un fonctionnement identique en local et en ligne.

## Décision
La résolution est écrite comme une **procédure C# ordinaire**, qui demande ses choix à `Game.Ask(...)`. Quand un choix manque :
1. la résolution en cours est **abandonnée** ;
2. le moteur renvoie l'**état d'avant la commande**, auquel il ajoute un `PendingState` : la commande, les réponses déjà données et la décision attendue ;
3. à chaque réponse, le moteur **ré-exécute la commande depuis cet état**, avec toutes les réponses connues.

Le hasard vient uniquement du générateur contenu dans l'état (ADR-0004). La ré-exécution tire donc les mêmes dés et atteint le même point de décision. La réponse enregistrée y est consommée, et la résolution continue.

Les faits (`GameEvent`) produits avant la décision sont livrés tout de suite, pour qu'on voie par exemple le dé avant de choisir. À la reprise, ce préfixe n'est pas renvoyé : le moteur retient combien de faits ont déjà été livrés.

## Conséquences
- (+) Le code des règles se lit comme les règles, sans découpage artificiel.
- (+) L'état en attente de décision est un simple objet de données. Il se sauvegarde, se transmet et se reprend, comme le montre le test `A_suspended_game_survives_serialization`.
- (+) Pour valider une réponse, il suffit de vérifier l'identifiant, le joueur et l'option. Une réponse invalide ne modifie jamais l'état.
- (−) Une commande avec *n* décisions est exécutée *n + 1* fois. Le coût est négligeable, car une résolution prend quelques microsecondes. La garde `maxDecisionsPerCommand` borne ce nombre.
- (!) **Invariant critique** : la résolution doit être une fonction pure de l'état, de la commande et des réponses. Pas d'horloge, pas de `System.Random`, pas d'état statique mutable. Si un rejeu diverge, le moteur lève une `EngineException` au lieu de continuer silencieusement. Les tests `Replaying_the_same_commands_gives_the_same_game` et les parties aléatoires vérifient cet invariant en continu.
- La source de dés injectable (`Game.DiceOverride`) est réservée aux tests à dés imposés. Elle n'est jamais utilisée par l'API publique.
