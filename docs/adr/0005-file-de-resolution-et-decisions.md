# ADR-0005 : File de résolution explicite et décisions interrompantes

- **Statut** : accepté, 2026-09-23

## Contexte
De nombreux effets demandent un choix **en cours de résolution**, parfois à un joueur autre que le joueur actif :
- le modificateur détruit sur un critique ;
- la déviation (Ordre) ;
- Mutinerie ;
- Je te touche pas avec un bâton ;
- la répartition des jetons de Tourment…

L'état doit rester sauvegardable et transmissible à tout moment : sauvegarde, reconnexion réseau.

## Options
1. Coroutines ou `async/await` qui attendent la réponse : simples à écrire, mais leur état ne se sérialise pas.
2. **File de résolution explicite** : une liste d'étapes (objets de données) stockée dans `GameState`. Une étape peut se suspendre en émettant un `DecisionRequest`, et le moteur reprend à la réception d'`AnswerDecision`.

## Décision
Option 2.

## Conséquences
- (+) L'état est sérialisable à tout instant. Cela permet la sauvegarde en pleine attaque et la reconnexion en ligne.
- (+) Chaque décision a un id unique et un destinataire. Le moteur rejette les réponses du mauvais joueur ou portant un id périmé.
- (+) En ligne, un délai avec choix par défaut (ou le bot) remplace un joueur inactif.
- (−) Le code est plus verbeux que des coroutines. On compense par des types d'étapes réutilisables et des tests.
