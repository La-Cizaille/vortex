# ADR-0020 : Une direction artistique et un univers communs, décrits dans une bible

- **Statut** : accepté, 2026-09-26.

## Contexte
Le jeu est jouable, animé, et ses premiers modèles existent (cartes, vaisseau, dé, cockpit). Chacun a été fait pour sa fonction, sans direction d'ensemble. Le designer veut un jeu fun, à l'humour noir, sarcastique et adulte, dans une ambiance de pirates de l'espace (ARB-93), puis une revue de tous les assets à cette lumière.

Deux sessions de travail produisent des assets en parallèle : le jeu (interface, textes, animations) et l'atelier Blender (modèles, textures). Sans référence écrite commune, leurs choix divergent.

## Options
1. **Décider au fil de l'eau**, asset par asset, dans les PR. Pas de document à tenir, mais aucune cohérence garantie, et les mêmes questions reviennent.
2. **Une bible de direction artistique et d'univers**, validée par le designer, à laquelle chaque asset se réfère.
3. **Une bible, plus une refonte technique** (nouveaux shaders, rendu stylisé, cel shading). Plus marqué, mais cela reprend tous les modèles et le rendu, pour un gain incertain sur mobile.

## Décision
**Option 2** : `docs/DIRECTION_ARTISTIQUE.md` est la référence de l'apparence et du ton. Elle comprend :
- le pitch, l'univers et les factions ;
- la charte d'écriture : le ton, les limites, les endroits où va l'humour, la voix du narrateur ;
- la palette, les typographies, les matériaux, les formes et l'iconographie ;
- les principes d'animation et de son ;
- les consignes pour les illustrations de cartes.

- **Registres retenus** (ARB-93) : futur usé, rouille et néon pour la 3D ; pulp rétro de propagande pour la 2D.
- **Rien ne change dans le moteur ni dans les règles.** La direction passe par les points d'extension existants : le thème et ses catalogues, la table des textes, les profils d'animation, les modèles à pièces nommées (ADR-0007, ADR-0014, ADR-0015, ADR-0016).
- **Les mots des règles restent les mêmes** (PV, bouclier, surcharge…). L'humour ne remplace jamais une information de jeu.
- Les **noms** du contenu peuvent changer après validation du designer, dans le cadre de l'équilibrage. Les **identifiants**, jamais.
- Chaque choix validé est noté dans `ARBITRAGES.md` ; la bible est mise à jour en même temps.

## Conséquences
- Un asset nouveau ou repris cite la section de la bible qu'il applique ; une revue de tous les assets suit la bible.
- Les polices, sons et images de sources externes arrivent avec leur licence dans `art-src/LICENCES.md`. Les polices livrées dans le jeu sont des dépendances de l'application : leur version est tracée (SECURITY.md).
- Une option pour couper les commentaires du narrateur pourra s'ajouter aux options (texte seulement, pas de règle).
- Un champ facultatif de texte d'ambiance dans les fichiers de contenu est envisagé. Il demanderait sa propre décision, car il change le format du contenu (ADR-0008).
