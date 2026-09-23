# ADR-0004 : Générateur aléatoire déterministe maison (PCG32)

- **Statut** : accepté, 2026-09-23

## Contexte
Plusieurs besoins demandent un aléatoire maîtrisé :
- des replays exacts, pour le débogage et le signalement de bugs ;
- des simulations reproductibles ;
- des tests fiables ;
- plus tard, la réponse aux contestations en ligne.

Or `System.Random` n'a pas d'implémentation garantie identique entre Mono (Unity) et .NET, et son état n'est pas sérialisable.

## Décision
Un PCG32 (O'Neill, 2014) implémenté dans `core/Runtime/Dice`. Son état (2 × `ulong`) est stocké dans `GameState`, et tous les tirages du moteur passent par lui.

## Conséquences
- (+) Même graine et mêmes commandes donnent exactement la même partie, sur toutes les plateformes.
- (+) L'état du RNG est sauvegardé avec la partie.
- (!) PCG32 **n'est pas cryptographique**. Ce n'est pas un problème, parce que son état ne quitte jamais le moteur autoritaire : `StateProjection` le masque (voir `SECURITY.md`). Les jetons de session (phase 2) utilisent un CSPRNG (`RandomNumberGenerator`).
- La règle d'analyse CA5394 (« aléatoire non sécurisé ») est désactivée pour cette raison documentée.
