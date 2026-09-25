# Visuels à créer

Ce document liste les visuels du jeu et le format attendu pour chacun. Tant qu'un visuel manque, le jeu affiche un **visuel provisoire généré** : motif teinté pour une carte, vaisseau fait de formes simples. Chaque visuel peut donc arriver seul, dans n'importe quel ordre, sans code.

La chaîne de production (Blender, export, versions) est décrite dans [ADR-0016](adr/0016-modeles-3d-blender.md). La marche à suivre pas à pas est dans [`CONTRIBUTING.md`](CONTRIBUTING.md#blender-modèles-3d).

## 1. Conventions communes

- **Sources** : les fichiers de travail vont dans `art-src/` (scènes Blender, images en calques). Ce qui entre dans le jeu est **exporté** dans `unity/Assets/_Vortex/Art/`.
- **Noms** : en ASCII, sans espace ni accent, sous la forme `Categorie_Nom` (`Ship_Faucon`, `Action_Attaque`). Pour les illustrations, le nom est l'identifiant du contenu (`A_005`, `EVT_TROU_NOIR`, `TECH_BLUE`).
- **Réglages d'import** : Unity les applique tout seul à la première importation. Ils peuvent être ajustés ensuite (`ArtImportRules`).
- **Licences** : seulement nos propres créations, ou des ressources libres dont la source et la licence sont notées dans `art-src/LICENCES.md`. Rien de téléchargé sans cette trace.

## 2. Modèles 3D (Blender, export FBX)

**Conventions de modélisation**
- **Échelle** : 1 unité Blender = 1 mètre = 1 unité Unity. Unités métriques, échelle 1 (réglage par défaut de Blender). Échelle des objets appliquée (*Object → Apply → Scale*).
- **Orientation** : le dessus vers +Z, l'avant (le nez d'un vaisseau) vers −Y, c'est-à-dire face à la vue de face de Blender (pavé numérique 1). Après l'export, l'avant doit pointer vers +Z dans Unity, car le jeu tourne chaque vaisseau vers le centre de la table par cet axe. **À confirmer au premier export**, en regardant le vaisseau dans la scène Galerie.
- **Origine** : au centre du vaisseau. Le jeu pose l'origine sur le plan de la table.
- **Matériaux** : un ou deux par modèle. Deux styles possibles (question ouverte 2) :
  - couleurs unies (palette) ;
  - textures peintes : PNG, 1024×1024 au plus, couleur de base plus éventuellement une carte de normales.

  Le rendu est URP Lit.
- **Pas de caméra ni de lumière** dans le fichier exporté, **pas d'animation** pour l'instant : le vaisseau est statique (INTERFACE §3.2).
- **Export** : `tools/blender/export_unity.py`, qui vérifie l'échelle et le budget de triangles avant d'écrire le FBX.

| Visuel | Nombre | Rôle dans le jeu | Taille, budget | Fichiers | Priorité |
|---|---|---|---|---|---|
| **Vaisseau** | 5, un par siège, ou 1 modèle recoloré par siège (question ouverte 1) | Un par joueur autour de la table ; celui du joueur est au premier plan | Environ 2 m de long, 2,4 m d'envergure au plus (comme le vaisseau provisoire) ; 5 000 triangles au plus | `art-src/ships/Ship_<Nom>.blend` exporté en `Art/Ships/Ship_<Nom>.fbx`, puis associé à un siège dans `Theme/ShipCatalog` | 1 |
| **Carte** | 1 modèle | Le corps de toutes les cartes (ADR-0017) ; la face (illustration, textes) est posée dessus par le jeu | 1 × 1,4 m, 0,02 m d'épaisseur ; coins arrondis et biseau bienvenus ; face avant vers −Y, comme un vaisseau ; 500 triangles au plus | `art-src/cards/Card.blend` exporté en `Art/Cards3D/Card.fbx`, puis placé comme corps dans `Prefabs/Card.prefab` | 2 |
| Épave | 0 à 1 | Vaisseau d'un joueur éliminé | Aujourd'hui, le jeu grise et incline le vaisseau lui-même : un modèle dédié est facultatif | `Ship_<Nom>_Epave` | 3 |
| Dé à 8 faces (d8) | 1 | Un dé en 3D pourra remplacer les dés 2D actuels | Octaèdre d'environ 0,5 m, faces numérotées de 1 à 8 ; 500 triangles au plus | `art-src/dice/D8.blend` exporté en `Art/Dice/D8.fbx` | 3 |
| Planètes, décor | 2 à 3 | Arrière-plan spatial (INTERFACE §2, pas nécessaire au prototype) | Sphères de 2 000 triangles au plus, texture équirectangulaire de 2048×1024 | `art-src/props/Planet_<Nom>.blend` exporté en `Art/Props/` | 3 |

## 3. Images

| Visuel | Nombre | Rôle dans le jeu | Format | Fichiers | Priorité |
|---|---|---|---|---|---|
| **Illustrations** | 66 : 27 ATK, 27 DEF, 8 événements, 4 technologies | Le haut de chaque carte ; le nom et le texte sont ajoutés par le jeu | PNG en 16:9, 1024×576, **sans texte**. Le cadre prend la couleur de la technologie | `Art/Cards/<ID>.png` : identifiants dans [`CARDS.md`](CARDS.md). Le menu *Vortex → Habillage → Vérifier les illustrations* liste ceux qui manquent | 2 |
| **Icônes des actions d'équipage** | 5 : Attaque, Sabotage, Reparamétrage, Surcharge, Posture défensive | L'arc d'actions au-dessus du vaisseau (INTERFACE §3.4, ARB-74) | PNG transparent 256×256, silhouette lisible à 48 px | `Art/Icons/Action_Attaque.png`, `Action_Sabotage`, `Action_Reparametrage`, `Action_Surcharge`, `Action_Posture` | 2 |
| **Icônes du texte des cartes** | 7 : ATQ, BOU, DIC, MKT, MOD, SUR, TOR | Les petites icônes dans le texte des cartes (aujourd'hui omises) | PNG transparent 128×128, une seule couleur claire, lisible à 16 px | `Art/Icons/Text_ATQ.png`… ; assemblées ensuite en *sprite asset* TextMeshPro | 2 |
| Icônes des technologies | 4 | Les ronds des technologies obtenues | PNG transparent 128×128 | `Art/Icons/Tech_BLUE.png`, `Tech_RED`, `Tech_GREEN`, `Tech_YELLOW` | 3 |
| Jetons | 2 : Tourment, surcharge | Sur les cartes et près du vaisseau | PNG transparent 128×128 | `Art/Icons/Token_Tourment.png`, `Token_Surcharge.png` | 3 |
| Icônes des effets temporaires | 16, facultatives | À côté du texte des effets | PNG transparent 128×128 | `Art/Icons/Status_<Type>.png` (types : clés `status.*` de la table des textes) | 4 |
| Dos de carte | 1 | Le dos des cartes (retournement, pioche) | PNG 1024×1434 (proportions de la carte) | `Art/Cards3D/CardBack.png`, à mettre dans le matériau `Theme/Materials/CardBack` | 3 |
| Cadres de carte | 5, facultatifs : neutre et 4 technologies | Remplacent le cadre de couleur unie | PNG 512×716 découpable en 9 zones (bords fixes) | `Art/Frames/Frame_Neutral.png`, `Frame_Blue`… | 4 |
| Fond spatial | 1 | Ciel étoilé derrière la table ; le scintillement se fait dans Unity | Panorama équirectangulaire 4096×2048, PNG ou EXR | `Art/Backgrounds/Space.png` | 3 |
| Logo | 1 | Écran d'accueil (M4.6) | PNG transparent, 2048 px de large | `Art/Brand/Logo.png` | 3 |
| Icône de l'application | 1 | Android et Windows (M5) | PNG 1024×1024 ; pour Android, deux calques 432×432 (motif et fond) | `Art/Brand/AppIcon.png`, `AppIcon_Foreground.png`, `AppIcon_Background.png` | 4 |

Les icônes, les cadres, le fond et le logo ne sont pas encore branchés dans le jeu. Ils le seront au fil des étapes : les actions en M4.5, le reste en M5. Les déposer dans ces dossiers n'a donc aucun effet visible pour l'instant, mais ils sont prêts.

## 4. Plus tard

- **Sons** (M5, ARB-72) : lancer de dés, impact, bouclier, élimination, achat au marché, fin de tour, signaux du temps de tour (dernières secondes, temps écoulé, ARB-70), musique d'ambiance. Format : WAV 48 kHz pour les effets, OGG pour la musique.
- **Fond stellaire animé** (M5, ARB-72) : le panorama du §3 sert de base ; le scintillement des étoiles et le mouvement lent des planètes se font dans Unity (particules, shaders), sans autre visuel à fournir.

## 5. Questions ouvertes pour l'atelier Blender

1. **Vaisseaux** : cinq modèles distincts (un par siège), ou un modèle que le jeu recolore à la couleur du siège ? Le second demande une petite convention (un matériau nommé `Siege`, teinté par le jeu), à coder lors de l'atelier.
2. **Style** : low-poly à couleurs unies, rapide à produire et cohérent sur mobile, ou textures peintes ?
3. **Version de Blender** : la LTS 4.5, stable et pleinement compatible avec Blender MCP (recommandée), ou la 5.2, plus récente ?
4. **Ressources libres** : autoriser Blender MCP à télécharger des ressources CC0 depuis Poly Haven (ciels, textures) ? Les autres sources restent désactivées.
5. **Illustrations** : qui les réalise (toi, un illustrateur, un outil de génération) ? La réponse fixe les droits d'utilisation à noter dans `art-src/LICENCES.md`.
