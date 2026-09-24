# ADR-0011 : Expérimenter l'équilibrage avec des options de règles et des variantes de contenu

- **Statut** : accepté, 2026-09-24.

## Contexte
La passe d'équilibrage (docs/balance/README.md) doit tester beaucoup de changements, en commençant par les règles de base :
- premier joueur qui tourne à chaque manche ;
- Élection à 3 technologies ;
- grilles de PV, de bouclier de départ et de manche de Fin des temps ;
- nouvelles mécaniques ;
- puis des retouches de cartes.

Chaque changement doit être **comparé** à la référence de façon fiable, puis **décidé** par le game designer. Il faut éviter trois écueils :
- modifier les fichiers de contenu pour chaque essai, avec le risque d'en oublier un ou de committer un essai ;
- multiplier dans le moteur des branches de code propres à une expérience ;
- conclure sur du bruit statistique.

## Options
1. **Éditer le contenu à la main**, lancer deux rapports et les comparer à l'œil. C'est simple, mais fragile et non reproductible, et on ne sait pas si l'écart est réel.
2. **Brancher le moteur par expérience** (drapeaux de compilation ou code dédié). Cela pollue le moteur et contredit l'ADR-0007.
3. **Options de règles génériques dans `config.json`, variantes de contenu en fichiers séparés et commande de comparaison statistique.**

## Décision
L'option 3.

**Options de règles** (`GameConfig`)
- Une règle à l'étude devient une option ⚙ **seulement si elle est générique** : elle ne cite aucune carte (ADR-0007).
- Sa valeur par défaut **reproduit la règle actuelle**. L'option est décrite dans `RULES.md` et testée pour chacune de ses valeurs.
- Premières options : `roundStartRotation` (RULES A4.4) et `technologiesToWin` (RULES A9).
- À la fin de la passe d'équilibrage, chaque option est soit **adoptée** (sa valeur devient la valeur par défaut, avec une entrée dans `docs/ARBITRAGES.md`), soit **retirée** si plus rien ne l'utilise.

**Variantes** (`docs/balance/variants/*.json`)
- Une variante est un **petit correctif JSON** du contenu de référence : `config`, et des cartes, événements ou technologies désignés par leur id.
- Les objets fusionnent récursivement ; les autres valeurs, tableaux compris, sont remplacées.
- Elle est appliquée **en mémoire** : les fichiers de contenu ne sont jamais réécrits.
- Le résultat passe par **le chargeur du jeu** (`GameDataLoader`), avec toutes ses validations : une variante invalide est refusée avec le même message qu'un contenu invalide.
- Sont refusés : une clé inconnue, un id inconnu, un changement d'id, une valeur `null`, un changement de `schemaVersion` ou de `$schema`, un fichier de plus de 256 Kio, une profondeur de plus de 16.
- Les variantes du dépôt sont validées par un test.

**Comparaison** (`Vortex.Simulator compare`)
- La référence et chaque variante jouent **les mêmes parties** : mêmes graines, donc mêmes mélanges et mêmes dés au départ. On compare ainsi des parties jumelles.
- Chaque écart est donné avec sa marge d'erreur à 95 % (1,96 × racine de la somme des variances). Cette marge suppose les deux échantillons indépendants : avec des parties jumelles, elle est **prudente**, car elle surestime le bruit.
- Un `*` signale un écart au-delà de cette marge.
- Chaque rapport porte la commande complète et l'empreinte SHA-256 du contenu simulé, variante appliquée : on peut le reproduire à l'identique.

**Niveaux de bot** (`BotLevel`) : `random`, `naive` (sans fin de tour simulée), `normal` et `strong` (plus d'échantillons). Un déséquilibre retrouvé à plusieurs niveaux est réel. Un déséquilibre qui n'apparaît qu'à un seul niveau est probablement un artefact du bot.

## Conséquences
- (+) Chaque essai est un fichier relu en PR, reproductible et sans risque pour le contenu.
- (+) Le moteur reste indépendant des cartes : une option est une règle générique, testée comme les autres.
- (+) Les décisions reposent sur des écarts mesurés avec leur marge, pas sur une impression.
- (−) Chaque option ajoute une branche au moteur, à tester et à retirer si elle est rejetée.
- (−) **Comparaisons multiples** : un rapport contient des dizaines d'indicateurs. Au seuil de 95 %, environ un sur vingt peut être marqué `*` par hasard. On ne conclut que sur des écarts **attendus**, nets, et confirmés par une seconde graine ou un autre niveau de bot.
- (−) Une variante ne peut pas **retirer** une carte, car le chargeur exige au moins un exemplaire. Pour mesurer un retrait, on modifie le contenu dans une branche dédiée.
