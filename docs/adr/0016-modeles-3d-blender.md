# ADR-0016 : Modèles 3D : Blender, export FBX par script, sources versionnées

- **Statut** : accepté, 2026-09-24. Les questions ouvertes de [`ASSETS.md`](../ASSETS.md) §5 (nombre de vaisseaux, style, version de Blender, ressources libres) ont été tranchées au premier atelier, le 2026-09-25 (ARB-76 à ARB-79). Complété le même jour (voir « Complément »).

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

- **Blender** : la version LTS du moment. En 2026-09, c'est la **5.2 LTS** (ARB-78), installée avec `winget install BlenderFoundation.Blender`.
- **Sources** dans `art-src/` : un `.blend` par modèle. Les `.blend` sont stockés avec **Git LFS** ; les copies de sauvegarde de Blender sont ignorées. Le quota gratuit de GitHub (1 Gio) suffit largement pour des modèles low-poly de quelques Mo.
- **Export** : `tools/blender/export_unity.py`, lancé sans ouvrir la fenêtre de Blender. Il vérifie les unités, l'échelle appliquée et le budget de triangles, puis exporte en FBX :
  - mètres ;
  - Y vers le haut, −Z vers l'avant ;
  - transformations intégrées au maillage ;
  - modificateurs appliqués ;
  - sans caméra ni lumière ;
  - textures copiées dans le dossier `<Modèle>.fbm/`, à côté du fichier.

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
- (+) Le premier export réel (2026-09-25) a confirmé l'orientation : −Y de Blender devient +Z dans Unity, sans rotation ni changement d'échelle sur l'objet importé. Un test Unity vérifie désormais les cotes, les axes et le budget du modèle de carte.
- (−) Les fichiers LFS consomment le quota : suivre `git lfs ls-files -s` au fil des ateliers.
- (−) Un outil de plus, qui exécute du code sur le poste. Le mode sûr et l'épinglage réduisent le risque, sans le supprimer.

## Complément (2026-09-25, premier atelier)
- **Modèles paramétriques construits par script.** Un modèle simple et entièrement décrit par ses cotes (le corps de carte, plus tard le dé) est construit par un script versionné, par exemple `tools/blender/build_card.py`, qui écrit le `.blend`. Changer une cote revient à changer une constante et à relancer le script, et le changement se relit dans le diff du script (le `.blend` est binaire). Le `.blend` reste la source exportée, et peut toujours être retravaillé à la main : dans ce cas, on ne relance plus le script. Les vaisseaux, qui demandent des choix de design, se modélisent dans Blender avec le designer (Blender MCP).
- **Commandes sans fenêtre** : toujours avec `--disable-autoexec`, pour qu'un `.blend` ne lance jamais ses scripts embarqués, quelles que soient les préférences du poste ; la construction d'un modèle ajoute `--factory-startup`, pour ne dépendre ni des préférences ni des extensions installées.
- **Mode sûr réglable par poste** (ARB-83) : `.mcp.json` lit `BLENDER_MCP_SAFE_MODE` dans l'environnement, avec le mode sûr par défaut. Le designer l'a levé sur son poste : le contrôle refusait des scripts de modélisation inoffensifs. Risque et mesures qui restent : SECURITY.md §4.
- **Travail en parallèle** : les modèles n'ont aucun lien avec le code du jeu (catalogues, visuels provisoires). L'atelier Blender avance sur sa propre branche, pendant que le prototype continue sur les autres. Une seule instance de Blender peut être pilotée par Blender MCP à la fois ; les scripts sans fenêtre, eux, peuvent tourner à côté.
