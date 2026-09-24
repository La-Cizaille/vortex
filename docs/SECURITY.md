# Sécurité

> Principe directeur : **le client n'est jamais fiable, le moteur valide tout, rien ne s'exécute implicitement.**
> Ce document est tenu à jour à chaque jalon. Toute nouvelle surface d'attaque (entrée externe, dépendance, service) doit y être ajoutée **avant** d'être fusionnée.

## 0. Doctrine : intransigeance sur le produit, pragmatisme sur l'outillage

Décision du porteur du projet (2026-09-24) :

| Périmètre | Exigence |
|---|---|
| **Produit livré** : builds Android et Windows, serveur et réseau (phase 2), sauvegardes, contenu embarqué, dépendances présentes dans les builds | **Aucun raccourci.** Toutes les mesures de ce document s'appliquent : validation stricte, versions épinglées, surface minimale, pas de télémétrie non voulue. |
| **Outillage de développement** : éditeur et outils Unity (dont Unity MCP), simulateur, outil de contenu, scripts locaux | **Raccourcis acceptés** s'ils accélèrent le travail, à trois conditions : rien de cet outillage n'entre dans un build, aucun secret n'est exposé, et chaque raccourci est documenté. |

Conséquences concrètes :
- un paquet ou un assembly **éditeur uniquement** (par exemple Unity MCP) peut être ajouté pour le confort de développement, à condition de vérifier qu'il n'est pas embarqué dans les builds ;
- toute dépendance **d'exécution** du client, c'est-à-dire présente dans les builds, relève du produit : elle est justifiée, épinglée et inscrite ici ;
- la CI de sécurité (§6) et la revue des PR restent inchangées pour tout le dépôt.

## 1. Actifs à protéger

| Actif | Pourquoi |
|---|---|
| Intégrité des parties | Pas de triche : dés, pioche, actions illégales. |
| Informations cachées | L'ordre des paquets et l'état du RNG permettraient de prédire les tirages. |
| Disponibilité du serveur (phase 2) | Un salon ne doit pas pouvoir bloquer les autres. |
| Postes de développement et secrets | Keystore Android, jetons GitHub, outils IA ayant accès à l'éditeur. |
| Données joueurs (phase 2) | Pseudo et identifiant d'appareil anonyme. Aucune autre donnée n'est collectée. |

## 2. Surfaces d'attaque

| # | Surface | Phase | Entrée non fiable |
|---|---|---|---|
| S1 | `Engine.Submit()` | 1 | Commandes venant de l'UI, et en phase 2 du réseau |
| S2 | Chargement de sauvegardes et de scénarios JSON | 1 | Fichiers sur disque modifiables par l'utilisateur |
| S3 | Fichiers de contenu `core/Runtime/Data/*.json` | 1 | Édités à la main par le designer, chargés par le jeu et les outils |
| S4 | Dépendances (NuGet, packages Unity, GitHub Actions) | 1 | Chaîne d'approvisionnement |
| S5 | Serveurs MCP (Unity, Blender) | 1 | Outils avec exécution de code dans l'éditeur |
| S6 | WebSocket serveur | 2 | Tout le trafic réseau |
| S7 | Builds distribués (APK, exe) | 1–2 | Rétro-ingénierie, modification du client |
| S8 | Variantes et grilles d'équilibrage (`docs/balance/variants/*.json`, `docs/balance/grids/*.json`) et options du simulateur | 1 | Fichiers édités à la main, passés en ligne de commande à un outil de développement |

## 3. Modèle de menaces (STRIDE) et mesures

| Menace | Surface | Mesure | Statut |
|---|---|---|---|
| **Falsification** d'une commande (action illégale, mauvais joueur, mauvaise phase) | S1, S6 | Validation complète dans `Engine` : identité du joueur, phase, légalité, id de décision attendue. Une commande invalide renvoie une erreur typée (`CommandErrorCode`), sans modifier l'état. Tests négatifs systématiques. | **Fait M1** |
| **Falsification** de l'aléatoire | S1, S6 | Le RNG vit uniquement dans le moteur (serveur en phase 2). Le client ne fournit jamais de valeur de dé. | **Fait M1** |
| **Fuite d'information** (ordre des paquets, graine) | S6 | `GameView.Of()` n'expose que la taille des paquets, sans leur contenu (test `The_public_view_never_exposes_hidden_information`). Le client ne travaille que sur la projection dès la phase 1. | **Fait M1** (utilisation côté client : M4) |
| **Exécution de code** par désérialisation | S2, S6 | Newtonsoft avec `TypeNameHandling.None` et `MetadataPropertyHandling.Ignore` (règles d'analyse CA2326–CA2330 en erreur). Membres inconnus et enums numériques rejetés, `MaxDepth` de 16, entrée limitée à 1 Mo. Polymorphisme par discriminateur en liste blanche (M1). | **Fait M0** pour les fichiers de contenu (tests dans `ContentJsonTests`) |
| **Contenu malformé ou piégé** | S3 | Même chargeur durci que le jeu. Validation (ids, énumérations, bornes, unicité entre fichiers), forme canonique vérifiée en CI. **Clés répétées refusées** dans un même objet, à tous les niveaux (`"copies": 1, … "copies": 8`) : Newtonsoft garderait la dernière valeur en silence, et le relecteur lirait une valeur pendant que le jeu en utilise une autre. Les clés sont comparées après décodage des échappements `\uXXXX` et sans tenir compte de la casse, car Newtonsoft associe aussi `"Copies"` au membre `copies`. Tout contenu après la valeur racine est refusé pour la même raison. Ce contrôle est une passe de lecture préalable dans `ContentJson` : la désérialisation reste inchangée, et le surcoût mesuré est d'environ 0,08 ms pour les quatre fichiers. Le message d'erreur donne la clé et les lignes des deux occurrences. Le convertisseur d'effets refuse aussi, de lui-même, un paramètre répété. Les sauvegardes et scénarios (S2) n'ont pas encore de chargeur : il devra passer par le même contrôle. Surface xlsx **supprimée** (ADR-0008) : plus de parseur de classeur, donc plus de risques XXE ni de bombe zip. | **Fait M0** (clés répétées : M3, tests `ContentJsonTests`) |
| **Variante ou grille malformée ou piégée** | S8 | Taille bornée (256 Kio) et profondeur bornée (16). Grille bornée (4 axes, 8 valeurs par axe, 64 combinaisons) et axes indépendants. Noms, descriptions et libellés soumis à la même règle de caractères que le contenu (`GameDataValidator.IsForbiddenTextCharacter`), sur une ligne, sans `|` dans un tableau : un rapport ne peut pas être maquillé. Lecture en arbre JSON brut, sans aucune résolution de type, et clés répétées refusées. Correctif strict : clés, ids et champs inconnus refusés, pas de `null`, ni id ni version de format modifiables. Le contenu obtenu repasse par **le chargeur durci du jeu** (S3), en mémoire uniquement : les fichiers de contenu ne sont jamais réécrits. Options du simulateur validées (bornes, niveaux de bot en liste blanche, au plus 8 variantes). Outil de développement, jamais embarqué dans un build (ADR-0011). | **Fait (équilibrage)** |
| **Logique arbitraire dans les données** | S3 | Les briques d'effets forment un catalogue **fermé** (`BrickCatalog`, enregistrement explicite sans réflexion). Le JSON sélectionne une brique par son nom et la paramètre ; il n'exécute jamais de code ni d'expression. Les paramètres sont typés et bornés, un paramètre inconnu est refusé, et seules les valeurs scalaires (entier, booléen, texte) sont acceptées, via un convertisseur JSON explicite. | **Fait M2** (tests `BrickCatalogTests`) |
| **Texte trompeur** (« Trojan Source », caractères invisibles) | S2, S3, S4 | Le validateur rejette les caractères de contrôle et bidi dans les données. L'importeur retire les caractères invisibles. Un test parcourt tout le dépôt (`SourceHygieneTests`). Les clés JSON répétées, autre forme de texte trompeur, sont traitées dans « Contenu malformé ou piégé ». | **Fait M0** |
| **État incohérent** chargé depuis un fichier | S2 | `GameEngine.ValidateState()` vérifie les invariants (PV, bouclier, cartes conservées, ids et statuts connus, décision en attente cohérente) avant tout chargement. Il tourne aussi après chaque commande dans les tests. | **Fait M1** |
| **Commande malformée** (énumération hors plage, index négatif, décision périmée) | S1, S6 | Validation de chaque champ. Les commandes, événements et décisions sont des types plats, sans polymorphisme. Les noms de joueurs sont contrôlés (longueur, caractères de contrôle). | **Fait M1** |
| **Déni de service** (flood, messages géants, salons zombies) | S6 | Limitation de débit par IP et par session, taille maximale des messages, délais de tour et de décision, expiration des salons, nombre maximal de salons. | Phase 2 |
| **Usurpation** de session ou de salon | S6 | Jetons de session de 128 bits (CSPRNG), stockés hachés, avec expiration. Codes de salon non énumérables et limités en tentatives. WSS/TLS obligatoire et vérification de l'Origin. | Phase 2 |
| **Répudiation** (contestation d'une partie) | S6 | Journal des commandes et graine conservés par partie, donc replay exact possible. Journaux sans données personnelles. | Phase 2 |
| **Élévation** via une dépendance compromise | S4 | Versions épinglées (lockfiles, `packages-lock.json`, actions épinglées par SHA), Dependabot, `dotnet list package --vulnerable` bloquant en CI, CodeQL. | **Fait M0** |
| **Élévation** via les outils IA | S5 | Voir §4. | Prévu M4 (installation de Unity MCP) |
| **Fuite de secrets** | S4 | `.gitignore` sur les keystores et les `.env`, gitleaks en CI, secret scanning et push protection GitHub. Les secrets de CI sont stockés dans les *GitHub Secrets*. | **Fait M0** (push protection : réglage GitHub à activer) |
| **Bot qui triche** (lecture de l'ordre des paquets ou des dés futurs) | S1 | Les bots n'utilisent que l'API publique et **remplacent l'information cachée** par leur propre tirage avant toute simulation (ADR-0010, test `The_heuristic_bot_cannot_see_hidden_information`). Limite documentée : pendant une décision en attente, le bot voit la fin de la commande en cours. | **Fait M3** |
| **Client modifié** | S7 | Accepté en phase 1 (jeu local). En phase 2, le serveur autoritaire rend la modification du client inutile pour tricher, sauf pour des aides visuelles, car toute l'information est publique. | Accepté |

## 4. Outils IA : risque à connaître

Unity MCP et Blender MCP exécutent des actions **avec les droits de l'éditeur**. Blender MCP expose notamment l'exécution de Python arbitraire et des intégrations de téléchargement (Poly Haven, Hyper3D).

Mesures :
- versions épinglées sur un tag, avec relecture du changelog avant chaque mise à jour ;
- intégrations de téléchargement de Blender MCP **désactivées** ;
- serveurs MCP démarrés uniquement quand on en a besoin, et en écoute sur localhost seulement ;
- aucun secret dans les projets ouverts par ces outils.

## 5. Builds

- **IL2CPP**, builds de release sans *Development Build*, sans script debugging ni profiler connecté.
- **Android** : pas de trafic en clair (`usesCleartextTraffic=false`) et aucune permission en phase 1 (INTERNET seulement en phase 2). Le keystore reste hors dépôt.
- **Windows** : build IL2CPP.

## 6. CI de sécurité (GitHub, dépôt public)

- CodeQL (C#) à chaque PR et chaque semaine.
- Secret scanning + push protection (réglages GitHub) et gitleaks en CI.
- Dependabot : alertes et mises à jour pour NuGet et GitHub Actions.
- `dotnet list package --vulnerable --include-transitive` : un résultat fait échouer la CI.
- Permissions minimales du `GITHUB_TOKEN` (`contents: read` par défaut).
- Branche `main` protégée : PR, CI verte et historique linéaire obligatoires.

## 7. Risques résiduels connus

| Risque | Pourquoi il est accepté | Suivi |
|---|---|---|
| gitleaks est vérifié par un fichier de sommes de contrôle publié **dans la même release** que le binaire | Cela protège contre une corruption en transit, pas contre une release compromise. Le secret scanning de GitHub reste en place en parallèle. | Épingler le SHA-256 attendu dans le workflow, ou vérifier la signature cosign. Dependabot ne met pas à jour `GITLEAKS_VERSION` : mise à jour manuelle. |

## 8. Signaler une vulnérabilité

Merci d'utiliser les *GitHub Security Advisories* du dépôt (signalement privé) plutôt qu'une issue publique.
