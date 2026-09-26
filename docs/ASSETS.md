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
- **Orientation** : le dessus vers +Z, l'avant (le nez d'un vaisseau) vers −Y, c'est-à-dire face à la vue de face de Blender (pavé numérique 1). Après l'export, l'avant pointe vers +Z dans Unity, l'axe par lequel le jeu tourne chaque vaisseau vers le centre de la table. **Vérifié au premier export** (2026-09-25) : −Y de Blender devient +Z dans Unity, +Z devient +Y, +X devient −X, sans rotation ni changement d'échelle sur l'objet importé.
- **Origine** : au centre du vaisseau. Le jeu pose l'origine sur le plan de la table.
- **Style** : low-poly texturé (ARB-77). La forme reste simple (budget de triangles ci-dessous) ; le détail vient des textures, et les parties lumineuses brillent dans le jeu.
- **Matériaux** : trois au plus par modèle, rendus en URP Lit.
  - `Siege` : la peinture que le jeu teinte à la couleur du siège (ARB-76). Sa texture doit rester **claire, en niveaux de gris** : la couleur du siège la multiplie. Les copies faites par Blender (`Siege.001`) comptent aussi.
  - Les autres matériaux (par exemple `Coque`, `Feux`) gardent leurs couleurs.
  - Plusieurs matériaux peuvent partager **une même texture** (un atlas) : chacun n'en lit que les zones de ses faces. C'est ainsi qu'une verrière colorée tient dans l'atlas de `Coque`, sans quatrième matériau. La couleur de base d'un matériau texturé reste **blanche** dans Blender : l'export la transmet à Unity, qui la multiplie par la texture.
  - **Lumières** : une partie lumineuse (réacteurs, hublots, feux) est un matériau **émissif**. Dans Blender, *Emission Color* avec une *Emission Strength* supérieure à 1 ; Unity le reçoit en émission HDR, que l'effet *Bloom* fait rayonner. Le Bloom est **allumé** depuis le premier vaisseau (profil de volume par défaut : seuil 1,5, intensité 2 ; post-traitement activé sur les caméras des scènes Jeu et Galerie) : seules les parties émissives dépassent le seuil, pas les blancs de l'interface ni des cartes. Le jeu n'applique pas de *tone mapping* : une couleur dont les trois composantes dépassent 1 s'affiche blanche. Pour garder la teinte, gardez une composante sous 1 (réacteurs de Sillage : couleur (1 ; 0,35 ; 0,06), force 2,5). Une partie lumineuse doit aussi **se voir de la caméra de la table**, qui regarde les vaisseaux de haut (environ 60° pour celui du joueur) : un fond lumineux trop enfoncé disparaît. Les lampes de Blender ne sont pas exportées : l'éclairage de la scène se règle dans Unity.
- **Textures** : PNG, 1024×1024 au plus pour un vaisseau. Elles se nomment d'après le modèle :
  - `<Modèle>_BaseColor.png` : couleur de base ;
  - `<Modèle>_Normal.png` : relief (carte de normales). Le suffixe `_Normal` est **obligatoire** : c'est lui qui la fait importer comme carte de normales ;
  - `<Modèle>_Emission.png` : facultative, pour dessiner les lumières ;
  - `<Modèle>_MetallicSmoothness.png` : facultative, pour un métal dont la brillance varie : métal dans le rouge, brillance dans l'alpha, comme URP Lit la lit. Le suffixe la fait importer comme une donnée, sans conversion sRGB.

  L'export les copie dans le dossier `<Modèle>.fbm/`, à côté du FBX, y compris celles qui passent par une teinte ou un calcul dans Blender. Unity relie seul la couleur de base et le relief aux matériaux (vérifié le 2026-09-25 ; Sillage n'utilise pas de texture d'émission, dont le branchement reste à vérifier), puis leur applique des réglages mobiles (mipmaps, ASTC 6×6 sur Android).
- **Pas de caméra ni de lumière** dans le fichier exporté, **pas d'animation** pour l'instant : le vaisseau est statique (INTERFACE §3.2). Unity l'importe sans composant d'animation.
- **Export** : `tools/blender/export_unity.py`, qui vérifie l'échelle et le budget de triangles avant d'écrire le FBX.

| Visuel | Nombre | Rôle dans le jeu | Taille, budget | Fichiers | Priorité |
|---|---|---|---|---|---|
| **Vaisseau** | 1, recoloré par le jeu à la couleur de chaque siège (ARB-76). Des modèles distincts par siège pourront s'ajouter ensuite. **Fait : Sillage** (ARB-84), 1 328 triangles, vaisseau par défaut de `Theme/ShipCatalog` | Un par joueur autour de la table ; celui du joueur est au premier plan | Environ 2 m de long, 2,4 m d'envergure au plus (comme le vaisseau provisoire) ; 5 000 triangles au plus | `art-src/ships/Ship_<Nom>.blend` exporté en `Art/Ships/Ship_<Nom>.fbx`, puis associé à un siège dans `Theme/ShipCatalog` | 1 |
| **Carte : un module** | 1 modèle, **fait** (ARB-96, ARB-97) : 896 triangles, le module de la planche v3 | Le corps de toutes les cartes (ADR-0017) : un **module de vaisseau branchable** (bible §6.5 bis). Boîtier d'acier noirci vissé aux quatre coins, poignée en haut, connecteur à broches en bas ; plaque sombre du nom avec l'étiquette de l'emplacement (cadre ambre) ; écran serti de l'illustration ; terminal sombre de la règle ; rangée d'état : voyant et diodes de faction, usage près du fusible, identifiant ; grille d'aération. Les liserés (tour du boîtier, écran) et les diodes prennent la couleur de la faction, ou celle qui marque une carte ; le voyant s'allume pour une carte de faction | 1 × 1,4 m hors tout (boîtier de 1 × 1,29, poignée et connecteur compris), **0,04 m d'épaisseur** ; debout (largeur selon X, hauteur selon Z) ; face avant vers **+Y**, à l'inverse d'un vaisseau, car la face d'une carte regarde la caméra (−Z dans Unity). Matériaux `Cadre` (acier), `Panneau` (faces qui portent du texte), `Lisere`, `Gemme`, que le jeu remplace par les siens (`Theme/Materials/CardFrame`, `CardPanel`, `CardTrim`, `CardGem`). Une seule texture peinte à la disposition de la face (1024 × 1434) porte les couleurs : les matériaux restent blancs. Repères vides `Zone_Illustration`, `Zone_Nom`, `Zone_Emplacement`, `Zone_Texte`, `Zone_Usage`, `Zone_Identifiant`, `Zone_Tourments`, et `Zone_Dos` pour la taille du dos (leur échelle X et Z donne la taille de la zone). 1 000 triangles au plus | `art-src/cards/Card.blend` et ses textures `Card_BaseColor`, `Card_Normal`, `Card_MetallicSmoothness`, construits par `tools/blender/build_card.py` et exportés en `Art/Cards3D/Card.fbx`. Le jeu en fait tout seul le corps de `Prefabs/Card.prefab` et y place l'illustration et les textes (`CardPrefabSync`) ; sans lui, le corps redevient une boîte | 2 |
| **Cockpit du joueur** | 1 modèle, **fait** (ARB-90, ARB-92) : 1 960 triangles, métal brossé (`Cockpit_Dark_BaseColor`, `Cockpit_Light_BaseColor`, `Cockpit_Normal`, 1024 px) | Sous le vaisseau du joueur, à la place de son panneau de chiffres : jauge de PV, manomètre de bouclier, deux sockets pour ses cartes, interrupteur de surcharge, trois diodes de technologies, plaque du nom | 3,84 × 1 m, 0,1 m de profondeur ; debout comme une carte, face avant vers +Y. Pièces nommées que le jeu anime : `Remplissage_PV` (le liquide, origine au bout gauche, échelle X = PV), `Aiguille_Bouclier` (origine au centre du cadran, ±120° autour de la profondeur), `Levier_Surcharge` (origine à la charnière, bascule autour de X), `Diode_Surcharge`, `Diode_1` à `Diode_3`, `Zone_Rouge` ; pièces de verre `Verre_Cadran` et `Tube_PV` ; repères `Zone_Nom`, `Zone_PV`, `Chiffre_0` à `Chiffre_8`, `Socket_ATK`, `Socket_DEF` (le plan où reposent les cartes : le jeu place le cockpit pour que ce plan tombe à la profondeur des cartes). Matériau `Siege` peint à la couleur du siège. 3 000 triangles au plus | `art-src/cockpit/Cockpit.blend`, construit par `tools/blender/build_cockpit.py` et exporté en `Art/Cockpit/Cockpit.fbx` ; le jeu le prend dans *Cockpit Model* de `Theme/ThemeSettings` | fait |
| Épave | 0 à 1 | Vaisseau d'un joueur éliminé | Aujourd'hui, le jeu grise et incline le vaisseau lui-même : un modèle dédié est facultatif | `Ship_<Nom>_Epave` | 3 |
| Dé à 8 faces (d8) | 1 | Un dé en 3D pourra remplacer les dés 2D actuels | Octaèdre d'environ 0,5 m, faces numérotées de 1 à 8 ; 500 triangles au plus | `art-src/dice/D8.blend` exporté en `Art/Dice/D8.fbx` | 3 |
| Planètes, décor | 2 à 3 | Arrière-plan spatial (INTERFACE §2, pas nécessaire au prototype) | Sphères de 2 000 triangles au plus, texture équirectangulaire de 2048×1024 | `art-src/props/Planet_<Nom>.blend` exporté en `Art/Props/` | 3 |

## 3. Images

| Visuel | Nombre | Rôle dans le jeu | Format | Fichiers | Priorité |
|---|---|---|---|---|---|
| **Illustrations** | 66 : 27 ATK, 27 DEF, 8 événements, 4 technologies | La fenêtre sertie de chaque carte, entre le nom et le texte (ARB-85) ; le nom et le texte sont ajoutés par le jeu | PNG en 16:9, 1024×576, **sans texte**, visible en entier. Les liserés et la gemme de la carte prennent la couleur de la technologie | `Art/Cards/<ID>.png` : identifiants dans [`CARDS.md`](CARDS.md). Le menu *Vortex → Habillage → Vérifier les illustrations* liste ceux qui manquent | 2 |
| **Icônes des actions d'équipage** | 5 : Attaque, Sabotage, Reparamétrage, Surcharge, Posture défensive | L'arc d'actions au-dessus du vaisseau (INTERFACE §3.4, ARB-74) | PNG transparent 256×256, silhouette lisible à 48 px | `Art/Icons/Action_Attaque.png`, `Action_Sabotage`, `Action_Reparametrage`, `Action_Surcharge`, `Action_Posture` | 2 |
| **Icônes d'emplacement** | 4, **faites** (ARB-86) : pointe de flèche (ATK), bouclier (DEF), éclat (événement), hexagone à noyau (technologie) | Le disque en haut à gauche de chaque carte, à la place du texte court | PNG transparent 256×256, un seul trait blanc, épuré ; le jeu la teinte de la couleur du texte. Dessinées par `tools/blender/build_icons.py` ; une image du même nom les remplace | `Art/Icons/Slot_ATK.png`, `Slot_DEF`, `Slot_EVT`, `Slot_TECH` | fait |
| **Icônes du texte des cartes** | 7 : ATQ, BOU, DIC, MKT, MOD, SUR, TOR | Les petites icônes dans le texte des cartes (aujourd'hui omises) | PNG transparent 128×128, une seule couleur claire, lisible à 16 px | `Art/Icons/Text_ATQ.png`… ; assemblées ensuite en *sprite asset* TextMeshPro | 2 |
| Icônes des technologies | 4 | Les ronds des technologies obtenues | PNG transparent 128×128 | `Art/Icons/Tech_BLUE.png`, `Tech_RED`, `Tech_GREEN`, `Tech_YELLOW` | 3 |
| Jetons | 2 : Tourment, surcharge | Sur les cartes et près du vaisseau | PNG transparent 128×128 | `Art/Icons/Token_Tourment.png`, `Token_Surcharge.png` | 3 |
| Icônes des effets temporaires | 16, facultatives | À côté du texte des effets | PNG transparent 128×128 | `Art/Icons/Status_<Type>.png` (types : clés `status.*` de la table des textes) | 4 |
| Dos de carte | 1 | Le dos des cartes (retournement, pioche) | PNG 1024×1434 (proportions de la carte) | `Art/Cards3D/CardBack.png`, à mettre dans le matériau `Theme/Materials/CardBack` | 3 |
| Cadres de carte | Plus nécessaires : le modèle de carte a son cadre de métal et ses liserés (ARB-85) | Remplaçaient le cadre de couleur unie de la boîte | PNG 512×716 découpable en 9 zones (bords fixes) | `Art/Frames/Frame_Neutral.png`, `Frame_Blue`… | 4 |
| Fond spatial | 1 | Ciel étoilé derrière la table ; le scintillement se fait dans Unity | Panorama équirectangulaire 4096×2048, PNG ou EXR | `Art/Backgrounds/Space.png` | 3 |
| Logo | 1 | Écran d'accueil (M4.6) | PNG transparent, 2048 px de large | `Art/Brand/Logo.png` | 3 |
| Icône de l'application | 1 | Android et Windows (M5) | PNG 1024×1024 ; pour Android, deux calques 432×432 (motif et fond) | `Art/Brand/AppIcon.png`, `AppIcon_Foreground.png`, `AppIcon_Background.png` | 4 |

Les icônes des actions d'équipage sont branchées : une image déposée remplace aussitôt le nom court de l'action. Les autres icônes, les cadres, le fond et le logo le seront en M5 ; les déposer dans ces dossiers n'a donc pas encore d'effet visible, mais ils sont prêts.

## 4. Plus tard

- **Sons** (M5, ARB-72) : lancer de dés, impact, bouclier, élimination, achat au marché, fin de tour, signaux du temps de tour (dernières secondes, temps écoulé, ARB-70 ; le tic des dix dernières secondes est aujourd'hui un bip généré, remplacé par le son *Timer Tick* de `Theme/ThemeSettings`), musique d'ambiance. Format : WAV 48 kHz pour les effets, OGG pour la musique.
- **Fond stellaire animé** (M5, ARB-72) : le panorama du §3 sert de base ; le scintillement des étoiles et le mouvement lent des planètes se font dans Unity (particules, shaders), sans autre visuel à fournir.

## 5. Questions ouvertes pour l'atelier Blender

Les quatre premières questions ont été tranchées le 2026-09-25 : un vaisseau recoloré par siège (ARB-76), un style low-poly texturé avec des parties lumineuses (ARB-77), Blender 5.2 LTS (ARB-78), aucun téléchargement de ressources pour l'instant (ARB-79).

1. **Illustrations** : qui les réalise (toi, un illustrateur, un outil de génération) ? La réponse fixe les droits d'utilisation à noter dans `art-src/LICENCES.md`.
