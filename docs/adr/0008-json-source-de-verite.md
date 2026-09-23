# ADR-0008 : Le JSON est la source de vérité du contenu (Excel retiré)

- **Statut** : accepté, 2026-09-23.

## Contexte
Au départ, les cartes étaient définies dans `design/Vortex.xlsx`, converti en JSON par un importeur. Ce choix posait plusieurs problèmes :
- **Un fichier binaire** : il fallait Git LFS, et une PR n'affichait aucun diff lisible.
- **Une structure limitée** : un tableur représente mal des effets structurés (briques paramétrées, ADR-0007).
- **Une surface d'attaque** : il fallait un parseur xlsx, donc des protections contre XXE, les bombes zip et la traversée de chemin.
- **Deux sources à synchroniser** : le classeur et le JSON généré, avec en plus les arbitrages qui vivaient dans RULES.md.

## Options
1. Garder Excel et l'importeur.
2. **Markdown** comme source : très lisible, mais non typé, et le parser de manière fiable est fragile.
3. **YAML** : agréable à écrire, mais il a des pièges de typage implicite, demande une dépendance supplémentaire dans Unity et expose des risques d'expansion d'alias.
4. **JSON** comme source : typé, diffable, validable, lu directement par le moteur.

## Décision
Option 4 :
- **Source** : `core/Runtime/Data/cards.json`, `events.json` et `technologies.json`. Chaque entrée porte le **texte imprimé** et l'**arbitrage** de la carte, rédigé avec le modèle d'effets (RULES.md partie B). Plus tard (M2), elle portera aussi ses briques d'effets.
- **Aide à l'édition** : des JSON Schemas (`core/Runtime/Data/schema/`) référencés par `$schema` donnent l'autocomplétion et la validation en direct dans VS Code. **Le validateur C# fait foi** ; un test vérifie que les énumérations des deux restent alignées.
- **Vue lisible** : `docs/CARDS.md` est **généré** par `Vortex.ContentTool`.
- **Forme canonique** : `Vortex.ContentTool format` réécrit les fichiers avec un ordre des champs et une indentation stables, pour des diffs propres.
- **CI** : validation, forme canonique et fraîcheur de `CARDS.md` sont vérifiées à chaque PR.
- Le classeur et l'importeur sont **retirés** du dépôt. Ils restent accessibles dans l'historique Git (commit `37eeb49`).

## Conséquences
- (+) Une seule source par carte : texte, arbitrage et, plus tard, effets.
- (+) Les PR montrent exactement ce qui change dans une carte.
- (+) Moins de surface d'attaque : plus de parseur xlsx, plus de Git LFS pour le contenu.
- (−) Éditer du JSON est moins confortable qu'un tableur pour comparer des valeurs d'équilibrage. On compense par l'autocomplétion du schéma, `CARDS.md`, et plus tard les rapports du simulateur (M3). Un éditeur de cartes dédié pourra venir si le besoin se confirme.
