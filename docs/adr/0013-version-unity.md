# ADR-0013 : Version de Unity : versions Update en développement, LTS pour publier

- **Statut** : accepté, 2026-09-24. Remplace le choix de version de l'ADR-0001 (Unity 6.3 LTS). Le choix du moteur Unity, lui, reste celui de l'ADR-0001.

## Contexte
Unity 6 publie deux sortes de versions :
- **LTS** : une par an, maintenue deux ans. Unity la recommande pour figer une production, par exemple un jeu sur le point de sortir ou déjà en exploitation. Aujourd'hui, c'est la 6.3 LTS, maintenue jusqu'en décembre 2027.
- **Update** : plusieurs par an (6.4, 6.5, 6.6…). Chacune est maintenue jusqu'à la suivante, avec le même niveau de tests. Unity la recommande **pour les nouvelles productions**. Une version Update ne devient jamais LTS.

Vortex en est au prototype (jalon M4), et sa publication est lointaine. L'ADR-0001 avait retenu la 6.3 LTS pour sa durée de support, mais la réinstallation de l'éditeur a posé la question : le game designer a installé la 6.6 (6000.6.3f1).

## Options
1. **Rester sur la LTS en cours** (6.3) jusqu'à la publication, puis passer à la LTS suivante. On a la stabilité maximale, mais pas les nouveautés, et le saut sera plus grand plus tard.
2. **Suivre les versions Update pendant le développement**, puis se fixer sur la LTS du moment avant toute publication.

## Décision
L'option 2.
- On développe sur la **dernière version Update** : aujourd'hui 6000.6.3f1.
- On passe à la version suivante quand elle sort. Chaque montée de version fait l'objet d'une PR : import, tests automatisés, notes de version relues.
- **Avant toute publication**, qu'il s'agisse d'une bêta ouverte ou d'une mise en boutique, on se fixe sur la **LTS du moment**. Ça suit la doctrine de sécurité : aucun raccourci sur le produit livré (SECURITY.md §0).
- La version est épinglée dans `unity/ProjectSettings/ProjectVersion.txt`, et les paquets dans `unity/Packages/manifest.json` et `packages-lock.json`. Le script `tools/Test-Unity.ps1` choisit l'éditeur d'après cette version.

## Conséquences
- (+) On dispose des dernières fonctions et corrections, comme Unity le recommande pour une nouvelle production.
- (+) Le passage à la LTS de publication sera un petit saut : on sera déjà sur une version proche.
- (−) Une montée de version environ tous les trois mois. Le projet étant jeune, chacune reste courte ; les tests automatisés (EditMode, puis PlayMode) détectent les régressions.
- (−) Une version Update peut contenir des régressions plus fraîches qu'une LTS : c'est accepté en développement, pas pour un build publié.
