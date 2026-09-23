# ADR-0007 : Moteur indépendant des cartes : points d'interception et briques d'effets

- **Statut** : accepté, 2026-09-23. Complète [ADR-0006](0006-une-classe-par-carte.md).

## Contexte
Les cartes vont évoluer : ajouts, retraits, changements d'effet. Le game designer veut pouvoir le faire **simplement**, et que les superpositions d'effets restent **propres et prévisibles**, y compris entre cartes que personne n'avait prévu de combiner.

La première version des règles mélangeait règles de base et exceptions de cartes, par exemple « le sabotage est impossible contre *Intouchable* ». Chaque nouvelle carte risquait donc de modifier les règles de base.

## Décision
1. **Le moteur ne connaît aucune carte.** Les règles de base (RULES.md partie A) n'exposent que des **points d'interception** typés (RULES.md B2) :
   - des *calculs*, où un effet modifie une valeur ;
   - des *autorisations*, où un effet peut refuser ;
   - des *réactions*, où un effet agit après un fait.
2. **Les effets agissent uniquement par des actions élémentaires** (RULES.md B3). Chaque action applique elle-même les autorisations et calculs qui la concernent. Par exemple, toute modification de bouclier demande l'autorisation « modifier un bouclier ».
3. **Empilement, durées, choix et ordre de résolution sont génériques** (RULES.md B4 à B7). Deux effets sur un même calcul se combinent toujours selon l'ordre remplacer → ajouter → multiplier → borner → annuler.
4. **Deux façons de donner un comportement à une carte** (à partir du jalon M2) :
   - **Briques d'effets paramétrées**, déclarées dans le JSON de la carte, pour les cas courants : bonus conditionnel, avantage, plafond, prévention, soin, refus… Par exemple `{"brick": "AttackValueBonus", "amount": 4, "when": "TargetHasTorment"}`. Aucun code n'est nécessaire.
   - **Classe dédiée** (ADR-0006) pour les effets exotiques, comme un pari sur le dé ou le choix de l'action d'un autre joueur. Elle utilise les **mêmes** points d'interception et actions.

   Le catalogue de briques est une **liste blanche** fermée, versionnée dans le code. Le JSON ne peut que sélectionner et paramétrer une brique existante : il n'exécute jamais de logique arbitraire (voir SECURITY.md).

## Garde-fous automatiques
- **Aucun id de carte dans le moteur** : un test (`SourceHygieneTests.Engine_code_never_references_a_specific_card`) échoue si un id de carte apparaît dans le code de `core/Runtime` hors du dossier `Cards/`.
- **Aucun arbitrage ne cite une autre carte** : `GameDataContentTests.Rulings_never_reference_another_card`.
- **Les tests du moteur utilisent des cartes factices**, construites dans les tests, et jamais le contenu réel. Modifier les cartes ne peut donc pas casser ces tests. Seul `GameDataContentTests` dépend du contenu réel, pour vérifier les quantités attendues.
- **Cohérence données ↔ comportements** (M2) : chaque carte du JSON a un comportement (briques ou classe), et chaque classe correspond à une carte existante. Les classes orphelines sont signalées.

## Options écartées
- **Exceptions de cartes dans les règles de base** : chaque carte ajoutée modifierait le moteur, et les interactions deviendraient imprévisibles.
- **Langage de description complet (DSL)** : il deviendrait aussi complexe que du code, en moins lisible et moins testable (voir ADR-0006). Les briques couvrent les cas courants, sans ambition d'exhaustivité.

## Conséquences
- (+) Ajouter une carte simple = ajouter une entrée JSON. Ajouter une carte exotique = une entrée JSON + une petite classe. Dans les deux cas, le moteur reste inchangé.
- (+) Retirer une carte = supprimer son entrée. Les tests signalent les restes.
- (+) Toute combinaison de cartes a un résultat défini par B4 à B7.
- (−) Le jeu de points d'interception doit être conçu avec soin. Si une carte future a besoin d'un point qui n'existe pas, on l'ajoute **au moteur de façon générique** (nouveau point dans RULES.md B2 et son test), jamais comme une exception propre à cette carte.
