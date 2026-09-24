# ADR-0016 : Modèles 3D : Blender, export FBX par script, sources versionnées

- **Statut** : accepté, 2026-09-24. Les questions ouvertes de [`ASSETS.md`](../ASSETS.md) §5 (nombre de vaisseaux, style, version de Blender, ressources libres) sont à trancher lors du premier atelier.

## Contexte
Le jeu tourne avec des visuels provisoires générés : cartes à motifs, vaisseaux faits de formes simples. Le game designer veut explorer la modélisation des vaisseaux et des éléments graphiques dans **Blender**. Il ne maîtrise pas encore l'outil et sera accompagné par un assistant IA (Blender MCP).

Contraintes :
- le modèle doit arriver dans Unity avec la bonne échelle, la bonne orientation et des réglages mobiles, sans manipulation fragile ;
- les sources doivent être gardées et versionnées ;
- la doctrine de sécurité s'applique : Blender MCP est un outil de développement qui exécute du code dans Blender (SECURITY.md §0 et §4).

## Options
1. **Exporter à la main** depuis les menus de Blender. C'est simple, mais les réglages d'export (axes, échelle, transformations) sont faciles à oublier, et le résultat n'est pas reproductible.
2. **Importer directement les `.blend` dans Unity.** Unity sait le faire, mais seulement si Blender est installé sur chaque poste qui ouvre le projet, CI comprise. C'est fragile, et ça lie le projet à une version de Blender.
3. **Blender pour les sources, un script d'export versionné vers FBX, des conventions écrites.**

## Décision
L'option 3.

- **Blender** : la version LTS du moment, aujourd'hui 4.5, sauf si le designer préfère la 5.2 (question ouverte). Elle est installée avec `winget install BlenderFoundation.Blender.LTS.4.5`.
- **Sources** dans `art-src/` : un `.blend` par modèle. Les `.blend` sont stockés avec **Git LFS** ; les copies de sauvegarde de Blender sont ignorées. Le quota gratuit de GitHub (1 Gio) suffit largement pour des modèles low-poly de quelques Mo.
- **Export** : `tools/blender/export_unity.py`, lancé sans ouvrir la fenêtre de Blender. Il vérifie les unités, l'échelle appliquée et le budget de triangles, puis exporte en FBX :
  - mètres ;
  - Y vers le haut, −Z vers l'avant ;
  - transformations intégrées au maillage ;
  - modificateurs appliqués ;
  - sans caméra ni lumière ;
  - textures copiées à côté du fichier.

  Le FBX exporté est versionné (LFS), puis importé par Unity avec des réglages mobiles (`ArtImportRules`).
- **Conventions** (échelle, orientation, origine, budgets, noms) : dans [`ASSETS.md`](../ASSETS.md). La scène Galerie et `tools/Capture-Unity.ps1` permettent de vérifier un modèle en contexte.
- **Blender MCP** : outil de développement, déclaré dans `.mcp.json`.
  - Paquet `mcp-for-blender==2.0.4` épinglé, télémétrie coupée (`DISABLE_TELEMETRY`).
  - **Mode sûr** (`BLENDER_MCP_SAFE_MODE=1`) : chaque script est contrôlé avant de s'exécuter. Les accès directs aux fichiers, le lancement de programmes et le réseau sont bloqués ; modéliser, enregistrer, importer et exporter restent possibles.
  - Intégrations de téléchargement (Poly Haven, Sketchfab, Poly Pizza, Hyper3D, Hunyuan3D) **désactivées** dans l'extension.
  - L'extension écoute sur 127.0.0.1:9876 sans authentification : on ne la démarre que pendant un atelier.

## Conséquences
- (+) Un modèle suit toujours le même chemin : on modélise, on exporte par script, on le dépose, et il est vérifié dans la Galerie. Aucun code n'est à toucher pour voir un nouveau vaisseau (`ShipCatalog`).
- (+) Les sources restent dans le dépôt, avec leur historique.
- (−) Le script d'export n'a pas encore tourné sur un vrai modèle : l'orientation est à confirmer au premier export.
- (−) Les fichiers LFS consomment le quota : suivre `git lfs ls-files -s` au fil des ateliers.
- (−) Un outil de plus, qui exécute du code sur le poste. Le mode sûr et l'épinglage réduisent le risque, sans le supprimer.
