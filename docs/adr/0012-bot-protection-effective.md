# ADR-0012 : Le bot évalue sa protection effective

- **Statut** : accepté, 2026-09-24. Complète l'ADR-0010.

## Contexte
Le bot heuristique (ADR-0010) simule chaque coup jusqu'à la fin de **son** tour, puis note la position obtenue. Dans cette note, sa protection était la **valeur** de son bouclier.

L'étape 2.3 de l'équilibrage a montré la limite de ce choix. Une protection temporaire, comme la posture défensive (bonus au bouclier effectif jusqu'au prochain tour), ne change pas la valeur du bouclier : le bot lui donnait donc une valeur nulle. Il ne la choisissait que dans 1 % de ses actions, et la mécanique ne pouvait pas être mesurée. Le même angle mort touche tout effet qui agit sur le bouclier **effectif** : un bouclier désactivé par un adversaire, par exemple, ne coûtait rien dans la note du bot.

## Options
1. **Garder la valeur du bouclier.** Aucune protection temporaire n'est visible.
2. **Simuler les tours des adversaires.** C'est plus juste, mais beaucoup plus lent, et le bot devrait deviner leurs choix.
3. **Noter la protection effective calculée par le moteur** : le bouclier effectif du bot (RULES A6, étape 7) face à une attaque simple de chaque adversaire vivant, en moyenne, avec tous les effets actifs.

## Décision
L'option 3.
- Le moteur expose un calcul **en lecture seule**, `GameEngine.AverageEffectiveShield(state, joueur)`. Il travaille sur une copie de l'état et ne résout aucune attaque.
- La note du bot utilise ce calcul à la place de la valeur du bouclier, avec le même poids (1 par point).
- La surcharge de `Evaluate` sans moteur garde l'ancienne note (valeur du bouclier) : les tests qui l'utilisent n'ont pas besoin d'un moteur.

## Conséquences
- (+) Le bot **ne connaît toujours aucune carte** : c'est le moteur qui calcule la protection, avec les mêmes règles qu'une vraie attaque. Toute protection présente ou future est donc prise en compte sans code dédié.
- (+) Les mécaniques et les cartes défensives sont mesurées plus justement.
- (−) Tous les chiffres d'équilibrage bougent un peu : une nouvelle référence est produite (`docs/balance/2026-09-24-reference-v4.md`). Les rapports antérieurs restent valables pour la version du bot qui les a produits.
- (−) Chaque évaluation copie l'état une fois de plus, ce qui la rend un peu plus lente.
- (−) La protection est notée contre une attaque **simple**. Un effet qui ne joue que contre une attaque surchargée n'est pas vu. C'est une approximation assumée.
